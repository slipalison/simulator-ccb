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
    /// Updates the password for a Keycloak user by their ID.
    /// </summary>
    Task UpdateUserPasswordAsync(string userId, string newPassword, CancellationToken ct = default);
}

/// <summary>
/// Minimal representation of a Keycloak user.
/// </summary>
public sealed record KeycloakUser(string Id, string Email);
