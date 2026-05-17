using Onboarding.Application.Common;
using Onboarding.Application.Fundos.DTOs;
using Onboarding.Domain.Repositories;

namespace Onboarding.Application.Fundos.Queries;

/// <summary>
/// Handler for paginated Fundo listing — company-scoped per D-01 (CAD-10).
/// </summary>
public sealed class ListFundoQueryHandler
    : IQueryHandler<ListFundoQuery, PaginatedResult<FundoDto>>
{
    private readonly IFundoRepository _repository;
    private readonly ICurrentCompanyService _currentCompanyService;

    public ListFundoQueryHandler(
        IFundoRepository repository,
        ICurrentCompanyService currentCompanyService)
    {
        _repository = repository;
        _currentCompanyService = currentCompanyService;
    }

    public async Task<PaginatedResult<FundoDto>> HandleAsync(
        ListFundoQuery query, CancellationToken ct = default)
    {
        var (items, totalCount) = await _repository.GetPagedByCompanyAsync(
            _currentCompanyService.CompanyId, query.Page, query.PageSize, query.Search, ct);

        var dtos = items.Select(f => new FundoDto(
            f.Id,
            f.Nome,
            f.Cnpj.Value,
            f.ConsultoriaFundoId,
            f.CustodianteId,
            f.TipoFundo,
            f.ClasseAnbima,
            f.Segmento,
            f.DataConstituicao,
            f.Status,
            f.CreatedAt)).ToList();

        return new PaginatedResult<FundoDto>(dtos, totalCount, query.Page, query.PageSize);
    }
}