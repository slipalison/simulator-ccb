using Onboarding.Application.Common;
using Onboarding.Application.Fundos.DTOs;
using Onboarding.Domain.Repositories;

namespace Onboarding.Application.Fundos.Queries;

/// <summary>
/// Handler for paginated Custodiante listing — company-scoped per D-01 (CAD-06).
/// </summary>
public sealed class ListCustodianteQueryHandler
    : IQueryHandler<ListCustodianteQuery, PaginatedResult<CustodianteDto>>
{
    private readonly ICustodianteRepository _repository;
    private readonly ICurrentCompanyService _currentCompanyService;

    public ListCustodianteQueryHandler(
        ICustodianteRepository repository,
        ICurrentCompanyService currentCompanyService)
    {
        _repository = repository;
        _currentCompanyService = currentCompanyService;
    }

    public async Task<PaginatedResult<CustodianteDto>> HandleAsync(
        ListCustodianteQuery query, CancellationToken ct = default)
    {
        var (items, totalCount) = await _repository.GetPagedByCompanyAsync(
            _currentCompanyService.CompanyId, query.Page, query.PageSize, query.Search, ct);

        var dtos = items.Select(c => new CustodianteDto(
            c.Id,
            c.RazaoSocial,
            c.CodigoInterno,
            c.Cnpj.Value,
            c.Email?.Value,
            c.Telefone?.Value,
            c.Status,
            c.CreatedAt)).ToList();

        return new PaginatedResult<CustodianteDto>(dtos, totalCount, query.Page, query.PageSize);
    }
}