using Onboarding.Application.Common;
using Onboarding.Domain.Repositories;

namespace Onboarding.Application.Fundos.Queries.GetFundoCedenteAllowedTransitions;

/// <summary>
/// Returns the allowed next statuses for a FundoCedente association (D-25).
/// Tenant-scoped via parent Fundo.ClienteId (D-5). Returns null on not-found / cross-tenant.
/// </summary>
public sealed class GetFundoCedenteAllowedTransitionsQueryHandler
    : IQueryHandler<GetFundoCedenteAllowedTransitionsQuery, IReadOnlyList<string>?>
{
    private readonly IFundoCedenteAggregateRepository _repository;
    private readonly IFundoRepository _fundoRepository;
    private readonly ICurrentCompanyService _currentCompanyService;

    public GetFundoCedenteAllowedTransitionsQueryHandler(
        IFundoCedenteAggregateRepository repository,
        IFundoRepository fundoRepository,
        ICurrentCompanyService currentCompanyService)
    {
        _repository = repository;
        _fundoRepository = fundoRepository;
        _currentCompanyService = currentCompanyService;
    }

    public async Task<IReadOnlyList<string>?> HandleAsync(
        GetFundoCedenteAllowedTransitionsQuery query, CancellationToken ct = default)
    {
        // Security: tenant guard via parent Fundo
        var fundo = await _fundoRepository.GetByIdAsync(query.FundoId, ct).ConfigureAwait(false);
        if (fundo is null || fundo.ClienteId != _currentCompanyService.CompanyId)
            return null;

        var association = await _repository.GetByIdAsync(query.AssociationId, ct).ConfigureAwait(false);
        if (association is null || association.FundoId != query.FundoId)
            return null;

        return association.GetAllowedNextStates();
    }
}
