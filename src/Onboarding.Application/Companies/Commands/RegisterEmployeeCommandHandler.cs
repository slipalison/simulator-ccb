using Microsoft.Extensions.Logging;
using Onboarding.Application.Common;
using Onboarding.Application.Companies.DTOs;
using Onboarding.Domain.Aggregates.Audit;
using Onboarding.Domain.Aggregates.CompanyAggregate;
using Onboarding.Domain.Aggregates.EmployeeAggregate;
using Onboarding.Domain.Exceptions;
using Onboarding.Domain.Repositories;
using System.Security.Cryptography;

namespace Onboarding.Application.Companies.Commands;

/// <summary>
/// Handler: register a new employee (PF) linked to a company (PJ).
/// Verifies company exists, checks for duplicate CPF/email, resolves AccessGroup,
/// generates cryptographic temp password, creates Employee + Keycloak user,
/// with compensation on Keycloak failure (REG-03, MGMT-01, T-38-04, T-38-05, T-38-07).
/// </summary>
public sealed class RegisterEmployeeCommandHandler
    : ICommandHandler<RegisterEmployeeCommand, RegisterEmployeeResult>
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly IAccessGroupRepository _accessGroupRepository;
    private readonly IKeycloakUserService _keycloakUserService;
    private readonly IAuditService _auditService;
    private readonly ILogger<RegisterEmployeeCommandHandler> _logger;

    public RegisterEmployeeCommandHandler(
        IEmployeeRepository employeeRepository,
        ICompanyRepository companyRepository,
        IAccessGroupRepository accessGroupRepository,
        IKeycloakUserService keycloakUserService,
        IAuditService auditService,
        ILogger<RegisterEmployeeCommandHandler> logger)
    {
        _employeeRepository = employeeRepository;
        _companyRepository = companyRepository;
        _accessGroupRepository = accessGroupRepository;
        _keycloakUserService = keycloakUserService;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<RegisterEmployeeResult> HandleAsync(
        RegisterEmployeeCommand command, CancellationToken ct = default)
    {
        // 1. Verify company exists (T-38-07: company isolation)
        var company = await _companyRepository.GetByIdAsync(command.CompanyId, ct);
        if (company is null)
            throw new KeyNotFoundException($"Company with ID {command.CompanyId} not found.");

        // 2. Check duplicate CPF within company (MGMT-01)
        if (await _employeeRepository.ExistsByCpfAsync(command.Cpf, ct))
            throw new BadRequestException("An employee with this CPF already exists.");

        // 3. Check duplicate email within company (SEC-08, T-38-04)
        if (await _employeeRepository.ExistsByEmailAsync(command.Email, ct))
            throw new BadRequestException("An employee with this email already exists.");

        // 4. Resolve AccessGroupId — if null, default to "viewer" group (D-08)
        Guid accessGroupId;
        if (command.AccessGroupId.HasValue)
        {
            var group = await _accessGroupRepository.GetByIdAsync(command.AccessGroupId.Value, ct);
            if (group is null)
                throw new BadRequestException($"Access group with ID {command.AccessGroupId.Value} not found.");
            accessGroupId = group.Id;
        }
        else
        {
            var viewerGroup = await _accessGroupRepository.GetByCompanyAndNameAsync(command.CompanyId, "viewer", ct)
                ?? throw new BadRequestException("Default 'viewer' access group not found for this company.");
            accessGroupId = viewerGroup.Id;
        }

        // 5. Generate cryptographic temp password (T-38-12: not predictable)
        var tempPassword = GenerateTempPassword();

        // 6. Create Employee via factory method (D-04)
        var employee = Employee.Register(
            command.Nome, command.Cpf, command.Email, command.Phone,
            command.CompanyId, accessGroupId);

        // 7. Persist employee to DB
        await _employeeRepository.AddAsync(employee, ct);
        _logger.LogInformation("Employee {EmployeeId} created for company {CompanyId}",
            employee.Id, command.CompanyId);

        // 8. Create Keycloak user in realm "client" — compensation on failure (T-38-05)
        string keycloakUserId;
        try
        {
            keycloakUserId = await _keycloakUserService.CreateUserAsync(
                "client", command.Email, command.Email, tempPassword, command.Nome, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Keycloak user creation failed. Removing employee {EmployeeId} from DB.",
                employee.Id);
            await _employeeRepository.DeleteAsync(employee.Id, ct);
            throw;
        }

        // 9. Set KeycloakUserId and persist (D-01)
        employee.SetKeycloakUserId(keycloakUserId);
        await _employeeRepository.SaveAsync(employee, ct);

        // 9b. Add employee to Keycloak group (D-16)
        // Best-effort: resolved group name → Keycloak group ID → AddUserToGroupAsync
        try
        {
            var accessGroup = await _accessGroupRepository.GetByIdAsync(employee.AccessGroupId, ct);
            if (accessGroup is not null)
            {
                var keycloakGroupId = await _keycloakUserService.GetGroupByNameAsync("client", accessGroup.Name, ct);
                if (keycloakGroupId is not null)
                {
                    await _keycloakUserService.AddUserToGroupAsync("client", keycloakUserId, keycloakGroupId, ct);
                }
                else
                {
                    _logger.LogWarning("Keycloak group '{GroupName}' not found for employee {EmployeeId}. Employee created but not added to Keycloak group.", accessGroup.Name, employee.Id);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to add employee {EmployeeId} to Keycloak group. Employee created in DB, group membership may need manual sync.", employee.Id);
        }

        // 10. Audit (MGMT-04, T-38-10)
        await _auditService.RecordAsync(
            actorSub: command.ActorSub,
            actorEmail: command.ActorEmail,
            action: ActionType.EmployeeCreated,
            targetUserId: employee.Id,
            targetUserName: employee.Nome,
            details: $"CompanyId={command.CompanyId}",
            ipAddress: command.IpAddress,
            ct: ct);

        return new RegisterEmployeeResult(employee.Id, tempPassword);
    }

    /// <summary>
    /// Generates a cryptographically secure temporary password (16+ chars, T-38-12).
    /// Uses RandomNumberGenerator for unpredictability. Appends "!Aa1" to satisfy Keycloak password policy.
    /// </summary>
    private static string GenerateTempPassword()
    {
        var bytes = new byte[16];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "x").Replace("/", "y").Replace("=", "") + "!Aa1";
    }
}