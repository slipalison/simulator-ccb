using Onboarding.Application.Common;
using Onboarding.Application.Fundos.DTOs;
using Onboarding.Domain.Repositories;

namespace Onboarding.Application.Fundos.Queries;

/// <summary>
/// Handler for paginated ConsultoriaFundo listing — company-scoped per D-01 (CAD-02).
/// </summary>
public sealed class ListConsultoriaFundoQueryHandler
    : IQueryHandler<ListConsultoriaFundoQuery, PaginatedResult<ConsultoriaFundoDto>>
{
    private readonly IConsultoriaFundoRepository _repository;
    private readonly ICurrentCompanyService _currentCompanyService;

    public ListConsultoriaFundoQueryHandler(
        IConsultoriaFundoRepository repository,
        ICurrentCompanyService currentCompanyService)
    {
        _repository = repository;
        _currentCompanyService = currentCompanyService;
    }

    public async Task<PaginatedResult<ConsultoriaFundoDto>> HandleAsync(
        ListConsultoriaFundoQuery query, CancellationToken ct = default)
    {
        var (items, totalCount) = await _repository.GetPagedByCompanyAsync(
            _currentCompanyService.CompanyId, query.Page, query.PageSize, query.Search, ct);

        var dtos = items.Select(c => new ConsultoriaFundoDto(
            c.Id,
            c.RazaoSocial,
            c.NomeFantasia,
            c.Cnpj.Value,
            c.Email?.Value,
            c.Telefone?.Value,
            c.Status,
            c.CreatedAt)).ToList();

        return new PaginatedResult<ConsultoriaFundoDto>(dtos, totalCount, query.Page, query.PageSize);
    }
}