using Onboarding.Domain.Aggregates.FundoAggregate;

namespace Onboarding.Domain.Repositories;

/// <summary>
/// Repository contract for Custodiante aggregate.
/// Company-scoped per D-01 — each company manages its own custodiantes.
/// </summary>
public interface ICustodianteRepository
{
    Task AddAsync(Custodiante custodiante, CancellationToken ct = default);
    Task SaveAsync(Custodiante custodiante, CancellationToken ct = default);
    Task<Custodiante?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Checks if a Custodiante with the given CNPJ already exists within a company (CAD-08).
    /// </summary>
    Task<bool> ExistsByCnpjAsync(string cnpj, Guid companyId, CancellationToken ct = default);

    /// <summary>
    /// Paginated query for custodiantes within a company — isolation guarantee (D-01).
    /// </summary>
    Task<(IReadOnlyList<Custodiante> Items, int TotalCount)> GetPagedByCompanyAsync(
        Guid companyId, int page, int pageSize, string? search, CancellationToken ct = default);
}