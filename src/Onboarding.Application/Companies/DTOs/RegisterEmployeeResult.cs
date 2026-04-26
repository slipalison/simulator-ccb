namespace Onboarding.Application.Companies.DTOs;

/// <summary>
/// Result of employee registration — temp password shown only once (MGMT-01, T-38-12).
/// </summary>
public sealed record RegisterEmployeeResult(Guid EmployeeId, string TemporaryPassword);