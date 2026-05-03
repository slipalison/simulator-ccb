using Onboarding.Domain.Aggregates.TipoAtivoAggregate;

namespace Onboarding.Domain.Repositories;

/// <summary>
/// Repository contract for TipoAtivo aggregate.
/// Global scope (no companyId) per D-03/TEN-03 — TipoAtivo is a shared CVM catalog.
/// </summary>
public interface ITipoAtivoRepository
{
    Task AddAsync(TipoAtivo tipoAtivo, CancellationToken ct = default);
    Task SaveAsync(TipoAtivo tipoAtivo, CancellationToken ct = default);
    Task<TipoAtivo?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Checks if a TipoAtivo with the given codigo already exists (global uniqueness, CAD-22).
    /// </summary>
    Task<bool> ExistsByCodigoAsync(string codigo, CancellationToken ct = default);

    /// <summary>
    /// Paginated query for tipos de ativo — global scope, no company filter (TEN-03).
    /// </summary>
    Task<(IReadOnlyList<TipoAtivo> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, string? search, CancellationToken ct = default);
}