using Onboarding.Domain.Aggregates.FundoAggregate;

namespace Onboarding.Domain.Repositories;

/// <summary>
/// Repository contract for ConsultoriaFundo aggregate.
/// Company-scoped per D-01 — each company manages its own consultorias.
/// </summary>
public interface IConsultoriaFundoRepository
{
    Task AddAsync(ConsultoriaFundo consultoria, CancellationToken ct = default);
    Task SaveAsync(ConsultoriaFundo consultoria, CancellationToken ct = default);
    Task<ConsultoriaFundo?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Checks if a ConsultoriaFundo with the given CNPJ already exists within a company (D-01/CAD-04).
    /// </summary>
    Task<bool> ExistsByCnpjAsync(string cnpj, Guid companyId, CancellationToken ct = default);

    /// <summary>
    /// Paginated query for consultorias within a company — isolation guarantee (D-01).
    /// </summary>
    Task<(IReadOnlyList<ConsultoriaFundo> Items, int TotalCount)> GetPagedByCompanyAsync(
        Guid companyId, int page, int pageSize, string? search, CancellationToken ct = default);
}