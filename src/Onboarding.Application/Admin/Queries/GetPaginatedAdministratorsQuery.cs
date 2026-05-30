using Onboarding.Application.Common;

namespace Onboarding.Application.Admin.Queries;

/// <summary>
/// Query: paginated list of administrators with optional name/email/status filters.
/// MGMT-01: 20 per page.
/// MGMT-02: filter by name (contains, case-insensitive), email (contains), and status (active/inactive/all).
/// Filtering is done in-memory over the full Keycloak role-members list (PERF-04 accepted technical debt):
/// Keycloak Admin REST API does not support server-side filtering by role+name/email in a single call;
/// pagination at DB level is not applicable here. Admin sets are small (typically &lt; 100), so O(N)
/// in-memory pagination is acceptable. Revisit if admin count exceeds ~500.
/// </summary>
public sealed record GetPaginatedAdministratorsQuery(
    int Page,
    int PageSize,
    string? Name,
    string? Email,
    string? Status);   // "active" | "inactive" | null = all

public sealed class GetPaginatedAdministratorsQueryHandler
    : IQueryHandler<GetPaginatedAdministratorsQuery, PaginatedResult<AdminUserDto>>
{
    private readonly IKeycloakUserService _keycloakUserService;

    public GetPaginatedAdministratorsQueryHandler(IKeycloakUserService keycloakUserService)
        => _keycloakUserService = keycloakUserService;

    public async Task<PaginatedResult<AdminUserDto>> HandleAsync(
        GetPaginatedAdministratorsQuery query, CancellationToken ct = default)
    {
        var all = await _keycloakUserService.GetUsersByRoleAsync("backoffice", "admin", ct);

        // Apply filters (MGMT-02)
        var filtered = all.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(query.Name))
            filtered = filtered.Where(a =>
                a.FullName.Contains(query.Name, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(query.Email))
            filtered = filtered.Where(a =>
                (a.Email ?? string.Empty).Contains(query.Email, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var active = query.Status.Equals("active", StringComparison.OrdinalIgnoreCase);
            filtered = filtered.Where(a => a.IsEnabled == active);
        }

        var list = filtered.ToList();
        var total = list.Count;
        var pageSize = query.PageSize > 0 ? Math.Min(query.PageSize, 100) : 20;
        var page = query.Page > 0 ? query.Page : 1;

        var items = list
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PaginatedResult<AdminUserDto>(items, total, page, pageSize);
    }
}
