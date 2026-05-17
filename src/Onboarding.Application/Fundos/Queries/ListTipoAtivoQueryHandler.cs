using Onboarding.Application.Common;
using Onboarding.Application.Fundos.DTOs;
using Onboarding.Domain.Repositories;

namespace Onboarding.Application.Fundos.Queries;

/// <summary>
/// Handler for paginated TipoAtivo listing — global scope, NO company filter per D-03/TEN-03 (CAD-20).
/// Does NOT inject ICurrentCompanyService (global entity).
/// </summary>
public sealed class ListTipoAtivoQueryHandler
    : IQueryHandler<ListTipoAtivoQuery, PaginatedResult<TipoAtivoDto>>
{
    private readonly ITipoAtivoRepository _repository;

    public ListTipoAtivoQueryHandler(ITipoAtivoRepository repository)
    {
        _repository = repository;
    }

    public async Task<PaginatedResult<TipoAtivoDto>> HandleAsync(
        ListTipoAtivoQuery query, CancellationToken ct = default)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(
            query.Page, query.PageSize, query.Search, ct);

        var dtos = items.Select(t => new TipoAtivoDto(
            t.Id,
            t.Codigo,
            t.Descricao,
            t.Categoria,
            t.Subcategoria,
            t.Status,
            t.OrdemExibicao)).ToList();

        return new PaginatedResult<TipoAtivoDto>(dtos, totalCount, query.Page, query.PageSize);
    }
}