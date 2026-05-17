using Onboarding.Domain.Aggregates.FundoTipoAtivoAggregate;

namespace Onboarding.Domain.Repositories;

/// <summary>
/// Repository interface for the standalone FundoTipoAtivoAggregate (Phase 50, D-21).
/// Distinct from the legacy FundoTipoAtivo join entity owned by Fundo aggregate.
/// </summary>
public interface IFundoTipoAtivoAggregateRepository
{
    Task AddAsync(FundoTipoAtivoAggregate association, CancellationToken ct = default);
    Task SaveAsync(FundoTipoAtivoAggregate association, CancellationToken ct = default);
    Task<FundoTipoAtivoAggregate?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Returns true if an ATIVO association already exists for the given (FundoId, TipoAtivoId) pair.
    /// Used for in-memory duplicate check analogous to REL-09.
    /// </summary>
    Task<bool> ExistsActiveAsync(Guid fundoId, Guid tipoAtivoId, CancellationToken ct = default);

    Task<(IReadOnlyList<FundoTipoAtivoAggregate> Items, int TotalCount)> GetPagedByFundoAsync(
        Guid fundoId, int page, int pageSize, CancellationToken ct = default);
}
