namespace Onboarding.Application.Companies.DTOs;

/// <summary>
/// Employee list item returned by paginated query — scoped to company (MGMT-02).
/// </summary>
public sealed record EmployeeListItemDto(
    Guid Id,
    string Nome,
    string? Cpf,
    string Email,
    string Phone,
    Guid AccessGroupId,
    string AccessGroupName,
    bool IsDeleted,
    bool KeycloakEnabled);