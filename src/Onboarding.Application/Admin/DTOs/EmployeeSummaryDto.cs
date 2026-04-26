namespace Onboarding.Application.Admin.DTOs;

/// <summary>
/// Summary item for paginated employee listing (ADMIN-01).
/// </summary>
public sealed record EmployeeSummaryDto(
    Guid Id,
    string Nome,
    string Cpf,
    string Email,
    string Phone,
    Guid CompanyId,
    string? CompanyRazaoSocial,
    Guid AccessGroupId,
    string? AccessGroupName,
    bool IsDeleted,
    string? KeycloakUserId);