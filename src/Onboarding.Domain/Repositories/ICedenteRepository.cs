using Onboarding.Domain.Aggregates.FundoAggregate;
using Onboarding.Domain.ValueObjects;

namespace Onboarding.Domain.Repositories;

/// <summary>
/// Repository contract for Cedente aggregate.
/// Company-scoped per D-01 — each company manages its own cedentes.
/// </summary>
public interface ICedenteRepository
{
    Task AddAsync(Cedente cedente, CancellationToken ct = default);
    Task SaveAsync(Cedente cedente, CancellationToken ct = default);
    Task<Cedente?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Checks if a Cedente with the given document (CPF or CNPJ) already exists
    /// within a company (CAD-18). Uses CedenteDocumento discriminated union.
    /// </summary>
    Task<bool> ExistsByDocumentoAsync(CedenteDocumento documento, Guid companyId, CancellationToken ct = default);

    /// <summary>
    /// Paginated query for cedentes within a company — isolation guarantee (D-01).
    /// </summary>
    Task<(IReadOnlyList<Cedente> Items, int TotalCount)> GetPagedByCompanyAsync(
        Guid companyId, int page, int pageSize, string? search, CancellationToken ct = default);
}