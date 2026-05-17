using Onboarding.Application.Common;

namespace Onboarding.Application.Fundos.Queries.Admin;

/// <summary>
/// Cross-company admin query for paginated Fundo listing (D-8).
/// IgnoreQueryFilters applied — no tenant isolation. Admin use only.
/// Optional CompanyId filter for narrowing to a specific tenant (D-12 pattern).
/// </summary>
public sealed record ListAdminFundoQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    Guid? CompanyId = null) : IQuery<PaginatedResult<AdminFundoDto>>;
