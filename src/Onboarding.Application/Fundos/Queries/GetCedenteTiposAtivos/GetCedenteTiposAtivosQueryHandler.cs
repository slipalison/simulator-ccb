using Onboarding.Application.Common;
using Onboarding.Application.Fundos.Commands.CreateCedenteTipoAtivo;
using Onboarding.Application.Fundos.DTOs;
using Onboarding.Domain.Repositories;

namespace Onboarding.Application.Fundos.Queries.GetCedenteTiposAtivos;

public sealed class GetCedenteTiposAtivosQueryHandler
    : IQueryHandler<GetCedenteTiposAtivosQuery, PaginatedResult<RelCedenteTipoAtivoDto>>
{
    private readonly ICedenteTipoAtivoAggregateRepository _repository;

    public GetCedenteTiposAtivosQueryHandler(ICedenteTipoAtivoAggregateRepository repository)
        => _repository = repository;

    public async Task<PaginatedResult<RelCedenteTipoAtivoDto>> HandleAsync(
        GetCedenteTiposAtivosQuery query, CancellationToken ct = default)
    {
        var (items, totalCount) = await _repository
            .GetPagedByCedenteAsync(query.CedenteId, query.Page, query.PageSize, ct)
            .ConfigureAwait(false);

        var dtos = items.Select(CreateCedenteTipoAtivoHandler.ToDto).ToList();
        return new PaginatedResult<RelCedenteTipoAtivoDto>(dtos, totalCount, query.Page, query.PageSize);
    }
}
