namespace Onboarding.Application.Common;

/// <summary>
/// Application-layer abstraction over the Keycloak Admin API.
/// Infrastructure implements this with KeycloakUserService (Keycloak.AuthServices.Sdk).
/// Keeping this in Application allows unit-testing the command handler without SDK dependencies.
/// </summary>
public interface IKeycloakUserService
{
    /// <summary>
    /// Creates a user in Keycloak realm "onboarding". Returns the Keycloak user ID (UUID string).
    /// Throws on any HTTP error from the Admin API.
    /// </summary>
    Task<string> CreateUserAsync(
        string username,
        string email,
        string password,
        string firstName,
        CancellationToken ct = default);

    /// <summary>
    /// Creates an admin user in Keycloak with a temporary password and admin role.
    /// Returns the Keycloak user ID (UUID string).
    /// </summary>
    Task<string> CreateAdminUserAsync(
        string email,
        string temporaryPassword,
        string fullName,
        CancellationToken ct = default);

    /// <summary>
    /// Deletes a Keycloak user by email address. Used as compensation step if app_db
    /// persist fails after Keycloak user was already created (Phase 5 rollback path).
    /// No-op if no user with the given email exists.
    /// </summary>
    Task DeleteUserByEmailAsync(string email, CancellationToken ct = default);

    /// <summary>
    /// Checks if a user exists in Keycloak by email address.
    /// Used by forgot password flow to determine whether to send a reset email.
    /// Returns false if no user found (without throwing).
    /// </summary>
    Task<bool> UserExistsByEmailAsync(string email, CancellationToken ct = default);

    /// <summary>
    /// Gets a Keycloak user by email address. Returns null if not found.
    /// </summary>
    Task<KeycloakUser?> GetUserByEmailAsync(string email, CancellationToken ct = default);

    /// <summary>
    /// Gets a Keycloak user by ID. Returns null if not found.
    /// </summary>
    Task<KeycloakUserDetails?> GetUserByIdAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Updates the password for a Keycloak user by their ID.
    /// </summary>
    Task UpdateUserPasswordAsync(string userId, string newPassword, CancellationToken ct = default);

    /// <summary>
    /// Sets the temporary password flag and UPDATE_PASSWORD required action for a user.
    /// </summary>
    Task SetTemporaryPasswordFlagAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Removes the UPDATE_PASSWORD required action from a user after they change their password.
    /// </summary>
    Task RemoveUpdatePasswordRequiredActionAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Assigns the admin role to a user.
    /// </summary>
    Task AssignAdminRoleAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Blocks a user in Keycloak by setting Enabled = false.
    /// Fetches the full UserRepresentation, modifies Enabled, and sends it back.
    /// </summary>
    Task BlockUserAsync(string keycloakUserId, CancellationToken ct = default);

    /// <summary>
    /// Unblocks a user in Keycloak by setting Enabled = true.
    /// </summary>
    Task UnblockUserAsync(string keycloakUserId, CancellationToken ct = default);

    /// <summary>
    /// Retorna todos os usuarios com a role especificada no Keycloak.
    /// Fonte: GET /admin/realms/{realm}/roles/{roleName}/users
    /// Inclui ativos (Enabled=true) e bloqueados (Enabled=false).
    /// HasTemporaryPassword deriva da presenca de "UPDATE_PASSWORD" nos requiredActions.
    /// </summary>
    Task<IReadOnlyList<AdminUserDto>> GetUsersByRoleAsync(string roleName, CancellationToken ct = default);

    /// <summary>
    /// Clears the isFirstLogin user attribute in Keycloak (sets to "false"). Idempotent:
    /// if the attribute is absent or already "false", the method is a no-op.
    /// </summary>
    Task ClearFirstLoginFlagAsync(string userId, CancellationToken ct = default);
}

/// <summary>
/// Minimal representation of a Keycloak user.
/// </summary>
public sealed record KeycloakUser(string Id, string Email, bool Enabled = true, bool EmailVerified = true);

/// <summary>
/// Extended Keycloak user details including required actions.
/// </summary>
public sealed record KeycloakUserDetails(string Id, string Email, bool Enabled, bool EmailVerified, IReadOnlyList<string> RequiredActions);
