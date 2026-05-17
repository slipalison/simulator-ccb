using Onboarding.Application.Common;
using Onboarding.Application.Fundos.DTOs;

namespace Onboarding.Application.Fundos.Queries.GetCedenteTiposAtivos;

public sealed record GetCedenteTiposAtivosQuery(
    Guid CedenteId,
    int Page = 1,
    int PageSize = 20) : IQuery<PaginatedResult<RelCedenteTipoAtivoDto>>;
