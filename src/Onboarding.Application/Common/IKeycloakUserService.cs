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
}
