using Onboarding.Domain.Aggregates.FundoCedenteAggregate;

namespace Onboarding.Domain.Repositories;

/// <summary>
/// Repository interface for the standalone FundoCedenteAggregate (Phase 50, D-21).
/// Distinct from the legacy FundoCedente join entity owned by Fundo aggregate.
/// </summary>
public interface IFundoCedenteAggregateRepository
{
    Task AddAsync(FundoCedenteAggregate association, CancellationToken ct = default);
    Task SaveAsync(FundoCedenteAggregate association, CancellationToken ct = default);
    Task<FundoCedenteAggregate?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Returns true if an ATIVO association already exists for the given (FundoId, CedenteId) pair.
    /// Used for in-memory REL-09 check (D-18 defesa-em-profundidade).
    /// </summary>
    Task<bool> ExistsActiveAsync(Guid fundoId, Guid cedenteId, CancellationToken ct = default);

    Task<(IReadOnlyList<FundoCedenteAggregate> Items, int TotalCount)> GetPagedByFundoAsync(
        Guid fundoId, int page, int pageSize, CancellationToken ct = default);
}
