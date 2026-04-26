using Onboarding.Application.Admin.DTOs;
using Onboarding.Application.Common;

namespace Onboarding.Application.Admin.Queries;

/// <summary>
/// Query: paginated list of companies with optional search and status filters (ADMIN-01).
/// Full implementation deferred to Phase 38/41.
/// </summary>
public sealed record GetPaginatedCompaniesQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    string? Status = null)
    : IQuery<PaginatedResult<CompanySummaryDto>>;

public sealed class GetPaginatedCompaniesHandler
    : IQueryHandler<GetPaginatedCompaniesQuery, PaginatedResult<CompanySummaryDto>>
{
    public Task<PaginatedResult<CompanySummaryDto>> HandleAsync(
        GetPaginatedCompaniesQuery query, CancellationToken ct = default)
        => throw new NotImplementedException("Full implementation in Phase 38/41");
}