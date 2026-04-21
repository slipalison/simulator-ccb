using System.Text.Json;
using Microsoft.Extensions.Logging;
using Onboarding.Application.Common;
using Onboarding.Domain.Aggregates.Audit;
using Onboarding.Domain.Common;
using Onboarding.Domain.Repositories;

namespace Onboarding.Application.Admin.Commands;

/// <summary>
/// Command: update user data (ADMIN-03).
/// </summary>
public sealed record UpdateUserCommand(
    Guid UserId,
    string Name,
    string? RazaoSocial,
    string Email,
    string Phone,
    string AdminSub,
    string AdminEmail);

public sealed class UpdateUserCommandHandler : ICommandHandler<UpdateUserCommand, Unit>
{
    private readonly IAdminRepository _adminRepository;
    private readonly IAuditService _auditService;
    private readonly IClientRepository _clientRepository;
    private readonly ILogger<UpdateUserCommandHandler> _logger;

    public UpdateUserCommandHandler(
        IAdminRepository adminRepository,
        IAuditService auditService,
        IClientRepository clientRepository,
        ILogger<UpdateUserCommandHandler> logger)
    {
        _adminRepository = adminRepository;
        _auditService = auditService;
        _clientRepository = clientRepository;
        _logger = logger;
    }

    public async Task<Unit> HandleAsync(UpdateUserCommand command, CancellationToken ct = default)
    {
        var client = await _adminRepository.GetByIdAsync(command.UserId, ct)
            ?? throw new KeyNotFoundException("User not found.");

        // Check email uniqueness if email changed
        if (!client.Email.Value.Equals(command.Email, StringComparison.OrdinalIgnoreCase))
        {
            if (await _clientRepository.ExistsByEmailAsync(command.Email, ct))
                throw new ArgumentException("Email already in use.");
        }

        // Capture snapshot before update
        var before = JsonSerializer.Serialize(new
        {
            client.Name,
            Email = client.Email.Value,
            Phone = client.Phone.Value,
            client.RazaoSocial
        });

        // Apply domain update
        client.Update(command.Name, command.RazaoSocial, command.Email, command.Phone);

        // Capture snapshot after update
        var after = JsonSerializer.Serialize(new
        {
            client.Name,
            Email = client.Email.Value,
            Phone = client.Phone.Value,
            client.RazaoSocial
        });

        // Persist
        await _adminRepository.UpdateAsync(client, ct);
        await _adminRepository.SaveChangesAsync(ct);

        // Audit log
        await _auditService.RecordAsync(
            actorSub: command.AdminSub,
            actorEmail: command.AdminEmail,
            action: ActionType.UserUpdated,
            targetUserId: command.UserId,
            targetUserName: client.Email.Value,
            details: JsonSerializer.Serialize(new { Before = before, After = after }),
            ct: ct);

        return Unit.Value;
    }
}
