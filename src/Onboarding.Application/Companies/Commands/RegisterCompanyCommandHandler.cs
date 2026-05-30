using Microsoft.Extensions.Logging;
using Onboarding.Application.Common;
using Onboarding.Application.Companies.DTOs;
using Onboarding.Domain.Aggregates.Audit;
using Onboarding.Domain.Aggregates.CompanyAggregate;
using Onboarding.Domain.Aggregates.EmployeeAggregate;
using Onboarding.Domain.Exceptions;
using Onboarding.Domain.Repositories;

namespace Onboarding.Application.Companies.Commands;

/// <summary>
/// Handler: register a new PJ company with duplicate check, Keycloak user creation,
/// default AccessGroup seeding, and compensation on failure (REG-01, REG-02, T-38-01).
/// </summary>
public sealed class RegisterCompanyCommandHandler
    : ICommandHandler<RegisterCompanyCommand, RegisterCompanyResult>
{
    private readonly ICompanyRepository _companyRepository;
    private readonly IAccessGroupRepository _accessGroupRepository;
    private readonly IKeycloakUserService _keycloakUserService;
    private readonly IAuditService _auditService;
    private readonly ILogger<RegisterCompanyCommandHandler> _logger;

    public RegisterCompanyCommandHandler(
        ICompanyRepository companyRepository,
        IAccessGroupRepository accessGroupRepository,
        IKeycloakUserService keycloakUserService,
        IAuditService auditService,
        ILogger<RegisterCompanyCommandHandler> logger)
    {
        _companyRepository = companyRepository;
        _accessGroupRepository = accessGroupRepository;
        _keycloakUserService = keycloakUserService;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<RegisterCompanyResult> HandleAsync(
        RegisterCompanyCommand command, CancellationToken ct = default)
    {
        // 1. Check duplicate CNPJ (REG-02, T-38-01)
        if (await _companyRepository.ExistsByCnpjAsync(command.Cnpj, ct))
            throw new DuplicateCompanyException("A company with this CNPJ already exists.");

        // 2. Check duplicate email (SEC-08: generic message)
        if (await _companyRepository.ExistsByEmailAsync(command.Email, ct))
            throw new DuplicateCompanyException("A company with this email already exists.");

        // 3. Create TermsAcceptance value object (D-12)
        var termsAcceptance = TermsAcceptance.Create(
            command.TermsVersion,
            command.IpAddress ?? "unknown");

        // 4. Create Company aggregate via factory method (D-03)
        var company = Company.Register(
            command.RazaoSocial,
            command.Cnpj,
            command.Email,
            command.Phone,
            termsAcceptance);

        // 5. Persist company to DB
        await _companyRepository.AddAsync(company, ct);
        _logger.LogInformation("Company {CompanyId} created with CNPJ {Cnpj}", company.Id, command.Cnpj);

        // 6. Create Keycloak user — compensation if this fails (T-38-05)
        string keycloakUserId;
        try
        {
            keycloakUserId = await _keycloakUserService.CreateUserAsync(
                "client", command.Email, command.Email, command.Password, command.RazaoSocial, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Keycloak user creation failed. Removing company {CompanyId} from DB.",
                company.Id);
            await _companyRepository.DeleteAsync(company.Id, ct);
            throw;
        }

        // 7. Set KeycloakUserId on company and persist (D-01)
        company.SetKeycloakUserId(keycloakUserId);
        await _companyRepository.SaveAsync(company, ct);

        // 8. Seed default AccessGroups (D-08)
        var defaultGroups = AccessGroup.CreateDefaultGroups(company.Id);
        await _accessGroupRepository.AddRangeAsync(defaultGroups, ct);

        // 8b. Provision Keycloak groups for each default AccessGroup (D-13)
        // Best-effort: DB is source of truth; Keycloak sync failure is logged but not rethrown
        var targetRealm = "client";
        foreach (var group in defaultGroups)
        {
            try
            {
                await _keycloakUserService.CreateGroupAsync(targetRealm, group.Name, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create Keycloak group {GroupName} for company {CompanyId}. DB group exists, Keycloak group may need manual creation.", group.Name, company.Id);
            }
        }

        // 8c. Assign the company owner to the admin-empresa group
        try
        {
            var adminEmpresaGroupId = await _keycloakUserService.GetGroupByNameAsync(targetRealm, "admin-empresa", ct);
            if (adminEmpresaGroupId is not null)
            {
                await _keycloakUserService.AddUserToGroupAsync(targetRealm, keycloakUserId, adminEmpresaGroupId, ct);
            }
            else
            {
                _logger.LogWarning("Keycloak group 'admin-empresa' not found. Could not assign company owner {UserId} to admin-empresa group.", keycloakUserId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to assign company owner {UserId} to 'admin-empresa' group.", keycloakUserId);
        }

        // 9. Audit (REG-03)
        await _auditService.RecordAsync(
            actorSub: "",
            actorEmail: command.Email,
            action: ActionType.CompanyRegistered,
            targetUserId: company.Id,
            targetUserName: command.RazaoSocial,
            details: $"Company registered with CNPJ {command.Cnpj}",
            ipAddress: command.IpAddress,
            ct: ct);

        return new RegisterCompanyResult(company.Id, keycloakUserId);
    }
}