using Onboarding.Application.Common;
using Onboarding.Application.Companies.DTOs;

namespace Onboarding.Application.Companies.Queries;

/// <summary>
/// Paginated employee listing query — scoped to company (MGMT-02).
/// </summary>
public sealed record GetCompanyEmployeesQuery(
    Guid CompanyId,
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    string? Status = null) : IQuery<PaginatedResult<EmployeeListItemDto>>;