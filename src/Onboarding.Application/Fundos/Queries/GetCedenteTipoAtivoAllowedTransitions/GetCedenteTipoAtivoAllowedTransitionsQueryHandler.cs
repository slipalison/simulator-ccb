using Onboarding.Application.Common;
using Onboarding.Domain.Repositories;

namespace Onboarding.Application.Fundos.Queries.GetCedenteTipoAtivoAllowedTransitions;

/// <summary>
/// Returns the allowed next statuses for a CedenteTipoAtivo association (D-25).
/// Tenant-scoped via parent Cedente.ClienteId (D-5). Returns null on not-found / cross-tenant.
/// </summary>
public sealed class GetCedenteTipoAtivoAllowedTransitionsQueryHandler
    : IQueryHandler<GetCedenteTipoAtivoAllowedTransitionsQuery, IReadOnlyList<string>?>
{
    private readonly ICedenteTipoAtivoAggregateRepository _repository;
    private readonly ICedenteRepository _cedenteRepository;
    private readonly ICurrentCompanyService _currentCompanyService;

    public GetCedenteTipoAtivoAllowedTransitionsQueryHandler(
        ICedenteTipoAtivoAggregateRepository repository,
        ICedenteRepository cedenteRepository,
        ICurrentCompanyService currentCompanyService)
    {
        _repository = repository;
        _cedenteRepository = cedenteRepository;
        _currentCompanyService = currentCompanyService;
    }

    public async Task<IReadOnlyList<string>?> HandleAsync(
        GetCedenteTipoAtivoAllowedTransitionsQuery query, CancellationToken ct = default)
    {
        // Security: tenant guard via parent Cedente
        var cedente = await _cedenteRepository.GetByIdAsync(query.CedenteId, ct).ConfigureAwait(false);
        if (cedente is null || cedente.ClienteId != _currentCompanyService.CompanyId)
            return null;

        var association = await _repository.GetByIdAsync(query.AssociationId, ct).ConfigureAwait(false);
        if (association is null || association.CedenteId != query.CedenteId)
            return null;

        return association.GetAllowedNextStates();
    }
}
