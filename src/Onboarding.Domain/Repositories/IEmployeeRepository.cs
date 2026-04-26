using Onboarding.Domain.Aggregates.EmployeeAggregate;

namespace Onboarding.Domain.Repositories;

/// <summary>
/// Repository contract for Employee aggregate.
/// </summary>
public interface IEmployeeRepository
{
    Task AddAsync(Employee employee, CancellationToken ct = default);
    Task SaveAsync(Employee employee, CancellationToken ct = default);
    Task<Employee?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Employee?> GetByKeycloakSubAsync(string keycloakSub, CancellationToken ct = default);
    Task<bool> ExistsByCpfAsync(string cpf, CancellationToken ct = default);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default);

    /// <summary>
    /// Paginated employee listing scoped to a company — isolation guarantee (D-17).
    /// </summary>
    Task<(IReadOnlyList<Employee> Items, int TotalCount)> GetPagedByCompanyAsync(
        Guid companyId, int page, int pageSize, string? search, string? status, CancellationToken ct = default);

    Task DeleteAsync(Guid id, CancellationToken ct = default);
}