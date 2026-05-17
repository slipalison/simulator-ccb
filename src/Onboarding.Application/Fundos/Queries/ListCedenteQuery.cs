using Onboarding.Application.Common;
using Onboarding.Application.Fundos.DTOs;

namespace Onboarding.Application.Fundos.Queries;

/// <summary>
/// Paginated listing query for Cedente — company-scoped per D-01 (CAD-16).
/// </summary>
public sealed record ListCedenteQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null) : IQuery<PaginatedResult<CedenteDto>>;