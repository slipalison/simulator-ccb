using Onboarding.Application.Common;
using Onboarding.Application.Fundos.DTOs;

namespace Onboarding.Application.Fundos.Queries;

/// <summary>
/// Paginated listing query for Custodiante — company-scoped per D-01 (CAD-06).
/// </summary>
public sealed record ListCustodianteQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null) : IQuery<PaginatedResult<CustodianteDto>>;