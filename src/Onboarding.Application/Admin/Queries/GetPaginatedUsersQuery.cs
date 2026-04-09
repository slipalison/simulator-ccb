using Onboarding.Application.Admin.DTOs;
using Onboarding.Application.Common;

namespace Onboarding.Application.Admin.Queries;

/// <summary>
/// Query: paginated list of users with optional search and status filters (ADMIN-01).
/// </summary>
public sealed record GetPaginatedUsersQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    string? Status = null)
    : IQuery<PaginatedResult<UserSummaryDto>>;

public sealed class GetPaginatedUsersHandler
    : IQueryHandler<GetPaginatedUsersQuery, PaginatedResult<UserSummaryDto>>
{
    public Task<PaginatedResult<UserSummaryDto>> HandleAsync(
        GetPaginatedUsersQuery query, CancellationToken ct = default)
    {
        throw new NotImplementedException("Handler implementation will be added in Plan 02.");
    }
}
