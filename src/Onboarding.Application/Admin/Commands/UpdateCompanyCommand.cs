using Microsoft.Extensions.Logging;
using Onboarding.Application.Common;
using Onboarding.Domain.Aggregates.Audit;
using Onboarding.Domain.Repositories;

namespace Onboarding.Application.Admin.Commands;

/// <summary>
/// Command: update company data (ADMIN-03).
/// </summary>
public sealed record UpdateCompanyCommand(
    Guid CompanyId,
    string RazaoSocial,
    string Email,
    string Phone);

/// <summary>
/// Handler: update company data in DB + sync email to Keycloak + audit (ADMIN-03).
/// Throws KeyNotFoundException if company not found.
/// </summary>
public sealed class UpdateCompanyCommandHandler : ICommandHandler<UpdateCompanyCommand, Unit>
{
    private readonly ICompanyRepository _companyRepository;
    private readonly IKeycloakUserService _keycloakUserService;
    private readonly IAuditService _auditService;
    private readonly ILogger<UpdateCompanyCommandHandler> _logger;

    public UpdateCompanyCommandHandler(
        ICompanyRepository companyRepository,
        IKeycloakUserService keycloakUserService,
        IAuditService auditService,
        ILogger<UpdateCompanyCommandHandler> logger)
    {
        _companyRepository = companyRepository;
        _keycloakUserService = keycloakUserService;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<Unit> HandleAsync(UpdateCompanyCommand command, CancellationToken ct = default)
    {
        var company = await _companyRepository.GetByIdAsync(command.CompanyId, ct);
        if (company is null)
            throw new KeyNotFoundException($"Company with ID '{command.CompanyId}' not found.");

        var previousEmail = company.Email.Value;

        // Update domain aggregate (validates inputs)
        company.Update(command.RazaoSocial, command.Email, command.Phone);
        await _companyRepository.SaveAsync(company, ct);

        // Sync email to Keycloak if changed
        if (!string.Equals(previousEmail, command.Email, StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrEmpty(company.KeycloakUserId))
        {
            try
            {
                await _keycloakUserService.UpdateAdminUserAsync(
                    "client", company.KeycloakUserId, company.RazaoSocial, company.Email.Value, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sync email change to Keycloak for company {CompanyId}", company.Id);
                // Don't rethrow — DB update succeeded, Keycloak sync is best-effort
            }
        }

        // Audit
        await _auditService.RecordAsync(
            actorSub: "",
            actorEmail: "",
            action: ActionType.CompanyUpdated,
            targetUserId: company.Id,
            targetUserName: company.RazaoSocial,
            details: $"Company updated: {command.RazaoSocial}",
            ipAddress: null,
            ct: ct);

        return Unit.Value;
    }
}