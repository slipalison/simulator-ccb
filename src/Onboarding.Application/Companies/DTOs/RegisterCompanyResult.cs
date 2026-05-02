namespace Onboarding.Application.Companies.DTOs;

/// <summary>
/// Result of a successful company registration (REG-01).
/// </summary>
public sealed record RegisterCompanyResult(
    Guid CompanyId,
    string KeycloakUserId);