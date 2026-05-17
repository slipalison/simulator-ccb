using Onboarding.Domain.Aggregates.CedenteTipoAtivoAggregate;

namespace Onboarding.Domain.Repositories;

/// <summary>
/// Repository interface for the standalone CedenteTipoAtivoAggregate (Phase 50, D-21).
/// Distinct from the legacy CedenteTipoAtivo join entity owned by Cedente aggregate.
/// </summary>
public interface ICedenteTipoAtivoAggregateRepository
{
    Task AddAsync(CedenteTipoAtivoAggregate association, CancellationToken ct = default);
    Task SaveAsync(CedenteTipoAtivoAggregate association, CancellationToken ct = default);
    Task<CedenteTipoAtivoAggregate?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Returns true if an ATIVO association already exists for the given (CedenteId, TipoAtivoId) pair.
    /// Used for in-memory duplicate check analogous to REL-09.
    /// </summary>
    Task<bool> ExistsActiveAsync(Guid cedenteId, Guid tipoAtivoId, CancellationToken ct = default);

    Task<(IReadOnlyList<CedenteTipoAtivoAggregate> Items, int TotalCount)> GetPagedByCedenteAsync(
        Guid cedenteId, int page, int pageSize, CancellationToken ct = default);
}
