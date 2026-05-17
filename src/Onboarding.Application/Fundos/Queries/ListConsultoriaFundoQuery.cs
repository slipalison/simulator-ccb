using Onboarding.Application.Common;
using Onboarding.Application.Fundos.DTOs;

namespace Onboarding.Application.Fundos.Queries;

/// <summary>
/// Paginated listing query for ConsultoriaFundo — company-scoped per D-01 (CAD-02).
/// </summary>
public sealed record ListConsultoriaFundoQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null) : IQuery<PaginatedResult<ConsultoriaFundoDto>>;