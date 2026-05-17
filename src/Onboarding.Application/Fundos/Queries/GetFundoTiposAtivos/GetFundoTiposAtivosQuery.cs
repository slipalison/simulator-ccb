using Onboarding.Application.Common;
using Onboarding.Application.Fundos.DTOs;

namespace Onboarding.Application.Fundos.Queries.GetFundoTiposAtivos;

public sealed record GetFundoTiposAtivosQuery(
    Guid FundoId,
    int Page = 1,
    int PageSize = 20) : IQuery<PaginatedResult<RelFundoTipoAtivoDto>>;
