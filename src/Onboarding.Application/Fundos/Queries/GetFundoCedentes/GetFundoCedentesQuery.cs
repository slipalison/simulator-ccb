using Onboarding.Application.Common;
using Onboarding.Application.Fundos.DTOs;

namespace Onboarding.Application.Fundos.Queries.GetFundoCedentes;

/// <summary>
/// Paginated query for FundoCedente associations by FundoId (Phase 50, D-21).
/// </summary>
public sealed record GetFundoCedentesQuery(
    Guid FundoId,
    int Page = 1,
    int PageSize = 20) : IQuery<PaginatedResult<RelFundoCedenteDto>>;
