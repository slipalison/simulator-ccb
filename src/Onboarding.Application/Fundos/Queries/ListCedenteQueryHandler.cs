using Onboarding.Application.Common;
using Onboarding.Application.Fundos.DTOs;
using Onboarding.Domain.Aggregates.CedenteAggregate;
using Onboarding.Domain.Repositories;

namespace Onboarding.Application.Fundos.Queries;

/// <summary>
/// Handler for paginated Cedente listing — company-scoped per D-01 (CAD-16).
/// Maps Documento discriminated union (PF=Cpf, PJ=Cnpj) to string display.
/// </summary>
public sealed class ListCedenteQueryHandler
    : IQueryHandler<ListCedenteQuery, PaginatedResult<CedenteDto>>
{
    private readonly ICedenteRepository _repository;
    private readonly ICurrentCompanyService _currentCompanyService;

    public ListCedenteQueryHandler(
        ICedenteRepository repository,
        ICurrentCompanyService currentCompanyService)
    {
        _repository = repository;
        _currentCompanyService = currentCompanyService;
    }

    public async Task<PaginatedResult<CedenteDto>> HandleAsync(
        ListCedenteQuery query, CancellationToken ct = default)
    {
        var (items, totalCount) = await _repository.GetPagedByCompanyAsync(
            _currentCompanyService.CompanyId, query.Page, query.PageSize, query.Search, ct);

        var dtos = items.Select(c => new CedenteDto(
            c.Id,
            c.Documento.Match(
                pf => pf.Cpf.Value,
                pj => pj.Cnpj.Value),
            c.Nome,
            c.Email?.Value,
            c.Telefone?.Value,
            c.Endereco,
            c.Documento.IsPf ? CedenteTipo.PF : CedenteTipo.PJ,
            c.Status,
            c.CreatedAt)).ToList();

        return new PaginatedResult<CedenteDto>(dtos, totalCount, query.Page, query.PageSize);
    }
}