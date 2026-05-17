using Onboarding.Application.Common;
using Onboarding.Application.Fundos.Commands.CreateFundoCedente;
using Onboarding.Application.Fundos.DTOs;
using Onboarding.Domain.Repositories;

namespace Onboarding.Application.Fundos.Queries.GetFundoCedentes;

/// <summary>
/// Query handler: paginated list of FundoCedente associations for a given FundoId.
/// Repository uses AsNoTracking for read performance (PRIO 2).
/// </summary>
public sealed class GetFundoCedentesQueryHandler
    : IQueryHandler<GetFundoCedentesQuery, PaginatedResult<RelFundoCedenteDto>>
{
    private readonly IFundoCedenteAggregateRepository _repository;

    public GetFundoCedentesQueryHandler(IFundoCedenteAggregateRepository repository)
        => _repository = repository;

    public async Task<PaginatedResult<RelFundoCedenteDto>> HandleAsync(
        GetFundoCedentesQuery query, CancellationToken ct = default)
    {
        var (items, totalCount) = await _repository
            .GetPagedByFundoAsync(query.FundoId, query.Page, query.PageSize, ct)
            .ConfigureAwait(false);

        var dtos = items.Select(CreateFundoCedenteHandler.ToDto).ToList();
        return new PaginatedResult<RelFundoCedenteDto>(dtos, totalCount, query.Page, query.PageSize);
    }
}
