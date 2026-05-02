using System.Security.Cryptography;
using Onboarding.Application.Common;
using Onboarding.Domain.Aggregates.Audit;
using Onboarding.Domain.Aggregates.EmployeeAggregate;
using Onboarding.Domain.Repositories;

namespace Onboarding.Application.Companies.Commands;

/// <summary>
/// Command result for password reset — temp password shown only once (MGMT-04, T-38-12).
/// </summary>
public sealed record ResetEmployeePasswordResult(string TemporaryPassword);

/// <summary>
/// Command to reset an employee's password — generates crypto-random temp password,
/// sets it as temporary in Keycloak (forces UPDATE_PASSWORD on next login).
/// Company isolation enforced (T-38-08).
/// </summary>
public sealed record ResetEmployeePasswordCommand(
    Guid EmployeeId,
    Guid CompanyId,
    string ActorSub,
    string ActorEmail,
    string? IpAddress);