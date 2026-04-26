using System.Text.Json;
using Microsoft.Extensions.Logging;
using Onboarding.Application.Common;
using Onboarding.Domain.Aggregates.Audit;
using Onboarding.Domain.Common;
using Onboarding.Domain.Repositories;

namespace Onboarding.Application.Admin.Commands;

/// <summary>
/// Command: update company data (ADMIN-03).
/// </summary>
public sealed record UpdateUserCommand(
    Guid UserId,
    string Name,       // Mapped to RazaoSocial
    string? RazaoSocial, // Ignored — kept for backward compat during transition
    string Email,
    string Phone,
    string AdminSub,
    string AdminEmail);

public sealed class UpdateUserCommandHandler : ICommandHandler<UpdateUserCommand, Unit>
{
    private readonly IAdminRepository _adminRepository;
    private readonly IAuditService _auditService;
    private readonly ICompanyRepository _companyRepository;
    private readonly ILogger<UpdateUserCommandHandler> _logger;

    public UpdateUserCommandHandler(
        IAdminRepository adminRepository,
        IAuditService auditService,
        ICompanyRepository companyRepository,
        ILogger<UpdateUserCommandHandler> logger)
    {
        _adminRepository = adminRepository;
        _auditService = auditService;
        _companyRepository = companyRepository;
        _logger = logger;
    }

    public async Task<Unit> HandleAsync(UpdateUserCommand command, CancellationToken ct = default)
    {
        var company = await _adminRepository.GetByIdAsync(command.UserId, ct)
            ?? throw new KeyNotFoundException("User not found.");

        // Check email uniqueness if email changed
        if (!company.Email.Value.Equals(command.Email, StringComparison.OrdinalIgnoreCase))
        {
            if (await _companyRepository.ExistsByEmailAsync(command.Email, ct))
                throw new ArgumentException("Email already in use.");
        }

        // Capture snapshot before update
        var before = JsonSerializer.Serialize(new
        {
            company.RazaoSocial,
            Email = company.Email.Value,
            Phone = company.Phone.Value
        });

        // Apply domain update — Name maps to RazaoSocial
        company.Update(command.Name, command.Email, command.Phone);

        // Capture snapshot after update
        var after = JsonSerializer.Serialize(new
        {
            company.RazaoSocial,
            Email = company.Email.Value,
            Phone = company.Phone.Value
        });

        // Persist
        await _adminRepository.UpdateAsync(company, ct);
        await _adminRepository.SaveChangesAsync(ct);

        // Audit log
        await _auditService.RecordAsync(
            actorSub: command.AdminSub,
            actorEmail: command.AdminEmail,
            action: ActionType.UserUpdated,
            targetUserId: command.UserId,
            targetUserName: company.Email.Value,
            details: JsonSerializer.Serialize(new { Before = before, After = after }),
            ct: ct);

        return Unit.Value;
    }
}