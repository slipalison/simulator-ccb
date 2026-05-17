using Onboarding.Application.Common;
using Onboarding.Application.Fundos.Commands.CreateFundoTipoAtivo;
using Onboarding.Application.Fundos.DTOs;
using Onboarding.Domain.Repositories;

namespace Onboarding.Application.Fundos.Queries.GetFundoTiposAtivos;

public sealed class GetFundoTiposAtivosQueryHandler
    : IQueryHandler<GetFundoTiposAtivosQuery, PaginatedResult<RelFundoTipoAtivoDto>>
{
    private readonly IFundoTipoAtivoAggregateRepository _repository;

    public GetFundoTiposAtivosQueryHandler(IFundoTipoAtivoAggregateRepository repository)
        => _repository = repository;

    public async Task<PaginatedResult<RelFundoTipoAtivoDto>> HandleAsync(
        GetFundoTiposAtivosQuery query, CancellationToken ct = default)
    {
        var (items, totalCount) = await _repository
            .GetPagedByFundoAsync(query.FundoId, query.Page, query.PageSize, ct)
            .ConfigureAwait(false);

        var dtos = items.Select(CreateFundoTipoAtivoHandler.ToDto).ToList();
        return new PaginatedResult<RelFundoTipoAtivoDto>(dtos, totalCount, query.Page, query.PageSize);
    }
}
