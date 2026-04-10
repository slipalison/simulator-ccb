using Onboarding.Domain.Aggregates.ClientAggregate;

namespace Onboarding.Domain.Repositories;

public interface IClientRepository
{
    Task AddAsync(Client client, CancellationToken ct = default);
    Task SaveAsync(Client client, CancellationToken ct = default);
    Task<Client?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default);
    Task<bool> ExistsByCpfAsync(string cpf, CancellationToken ct = default);
    Task<bool> ExistsByCnpjAsync(string cnpj, CancellationToken ct = default);
    // Added Phase 5: compensation step — delete row if Keycloak user creation fails (REG-06)
    Task DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Returns the client with the given email address, or null if not found.
    /// Used by GET /api/clients/me to look up the authenticated user's profile (D-07, D-08).
    /// Email is normalized to lowercase before querying (matches Email value object behavior).
    /// </summary>
    Task<Client?> GetByEmailAsync(string email, CancellationToken ct = default);

    /// <summary>
    /// Returns the client with the given Keycloak user ID (sub claim), or null if not found.
    /// Used by GET /api/clients/me to look up profile without exposing email in JWT (LGPD).
    /// </summary>
    Task<Client?> GetByKeycloakSubAsync(string keycloakSub, CancellationToken ct = default);

    /// <summary>
    /// Returns the client with the given name, or null if not found.
    /// Fallback for profile lookup when Keycloak sub claim is not available in the token.
    /// </summary>
    Task<Client?> GetByNameAsync(string name, CancellationToken ct = default);

    /// <summary>
    /// Returns a paginated list of clients with optional search and status filters.
    /// Used by admin paginated user listing (ADMIN-01).
    /// </summary>
    Task<(IReadOnlyList<Client> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, string? search, string? status, CancellationToken ct = default);
}
