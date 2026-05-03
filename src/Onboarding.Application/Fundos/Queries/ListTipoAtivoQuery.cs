using Onboarding.Application.Common;
using Onboarding.Application.Fundos.DTOs;

namespace Onboarding.Application.Fundos.Queries;

/// <summary>
/// Paginated listing query for TipoAtivo — global scope, no company filter per D-03/TEN-03 (CAD-20).
/// </summary>
public sealed record ListTipoAtivoQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null) : IQuery<PaginatedResult<TipoAtivoDto>>;