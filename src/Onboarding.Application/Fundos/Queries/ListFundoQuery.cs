using Onboarding.Application.Common;
using Onboarding.Application.Fundos.DTOs;

namespace Onboarding.Application.Fundos.Queries;

/// <summary>
/// Paginated listing query for Fundo — company-scoped per D-01 (CAD-10).
/// </summary>
public sealed record ListFundoQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null) : IQuery<PaginatedResult<FundoDto>>;