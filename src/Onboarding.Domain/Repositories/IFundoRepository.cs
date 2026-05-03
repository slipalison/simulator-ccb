using Onboarding.Domain.Aggregates.FundoAggregate;

namespace Onboarding.Domain.Repositories;

/// <summary>
/// Repository contract for Fundo aggregate.
/// CNPJ uniqueness is company-scoped per CAD-12 (HasQueryFilter in Infrastructure).
/// </summary>
public interface IFundoRepository
{
    Task AddAsync(Fundo fundo, CancellationToken ct = default);
    Task SaveAsync(Fundo fundo, CancellationToken ct = default);
    Task<Fundo?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Checks if a Fundo with the given CNPJ already exists within a company (CAD-12).
    /// </summary>
    Task<bool> ExistsByCnpjAsync(string cnpj, Guid companyId, CancellationToken ct = default);

    /// <summary>
    /// Paginated query for fundos within a company — isolation guarantee (D-01).
    /// </summary>
    Task<(IReadOnlyList<Fundo> Items, int TotalCount)> GetPagedByCompanyAsync(
        Guid companyId, int page, int pageSize, string? search, CancellationToken ct = default);
}