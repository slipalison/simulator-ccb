using Onboarding.Domain.Aggregates.CompanyAggregate;

namespace Onboarding.Domain.Repositories;

/// <summary>
/// Repository contract for Company aggregate.
/// Replaces IClientRepository (D-19).
/// </summary>
public interface ICompanyRepository
{
    Task AddAsync(Company company, CancellationToken ct = default);
    Task SaveAsync(Company company, CancellationToken ct = default);
    Task<Company?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Checks if a company with the given CNPJ already exists (REG-02).
    /// </summary>
    Task<bool> ExistsByCnpjAsync(string cnpj, CancellationToken ct = default);

    Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default);
    Task<Company?> GetByKeycloakSubAsync(string keycloakSub, CancellationToken ct = default);
    Task<Company?> GetByEmailAsync(string email, CancellationToken ct = default);

    /// <summary>
    /// Compensation step — delete row if Keycloak user creation fails (REG-06).
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Paginated query for admin listing (ADMIN-01).
    /// </summary>
    Task<(IReadOnlyList<Company> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, string? search, string? status, CancellationToken ct = default);
}