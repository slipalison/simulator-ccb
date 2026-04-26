using Onboarding.Application.Admin.DTOs;
using Onboarding.Application.Common;

namespace Onboarding.Application.Admin.Queries;

/// <summary>
/// Query: paginated list of employees with optional search, status and company filters (ADMIN-01).
/// Full implementation deferred to Phase 38/41.
/// </summary>
public sealed record GetPaginatedEmployeesQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    string? Status = null,
    Guid? CompanyId = null)
    : IQuery<PaginatedResult<EmployeeSummaryDto>>;

public sealed class GetPaginatedEmployeesHandler
    : IQueryHandler<GetPaginatedEmployeesQuery, PaginatedResult<EmployeeSummaryDto>>
{
    public Task<PaginatedResult<EmployeeSummaryDto>> HandleAsync(
        GetPaginatedEmployeesQuery query, CancellationToken ct = default)
        => throw new NotImplementedException("Full implementation in Phase 38/41");
}