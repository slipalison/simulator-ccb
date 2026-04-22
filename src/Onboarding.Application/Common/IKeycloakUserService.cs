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
    Task<string> CreateUserAsync(string targetRealm, string username, string email, string password, string firstName, CancellationToken ct = default);

    Task<string> CreateAdminUserAsync(string targetRealm, string email, string temporaryPassword, string fullName, CancellationToken ct = default);

    Task DeleteUserByEmailAsync(string targetRealm, string email, CancellationToken ct = default);

    Task<bool> UserExistsByEmailAsync(string targetRealm, string email, CancellationToken ct = default);

    Task<KeycloakUser?> GetUserByEmailAsync(string targetRealm, string email, CancellationToken ct = default);

    Task<KeycloakUserDetails?> GetUserByIdAsync(string targetRealm, string userId, CancellationToken ct = default);

    Task UpdateUserPasswordAsync(string targetRealm, string userId, string newPassword, CancellationToken ct = default);

    /// <summary>
    /// Resets a user's password as temporary in a single atomic Keycloak call
    /// (PUT reset-password with temporary=true). The user will be forced to change
    /// their password on next login. Use this instead of UpdateUserPasswordAsync +
    /// SetTemporaryPasswordFlagAsync to avoid the two-call gap.
    /// </summary>
    Task ResetPasswordAsTemporaryAsync(string targetRealm, string userId, string newPassword, CancellationToken ct = default);

    Task SetTemporaryPasswordFlagAsync(string targetRealm, string userId, CancellationToken ct = default);

    Task RemoveUpdatePasswordRequiredActionAsync(string targetRealm, string userId, CancellationToken ct = default);

    Task AssignAdminRoleAsync(string targetRealm, string userId, CancellationToken ct = default);

    Task BlockUserAsync(string targetRealm, string keycloakUserId, CancellationToken ct = default);

    Task UnblockUserAsync(string targetRealm, string keycloakUserId, CancellationToken ct = default);

    Task<IReadOnlyList<AdminUserDto>> GetUsersByRoleAsync(string targetRealm, string roleName, CancellationToken ct = default);

    Task ClearFirstLoginFlagAsync(string targetRealm, string userId, CancellationToken ct = default);

    /// <summary>
    /// Updates an admin's display name and email in Keycloak.
    /// Throws InvalidOperationException if the target user is not found.
    /// Throws ArgumentException (409-equivalent) if the new email is already taken.
    /// </summary>
    Task UpdateAdminUserAsync(string targetRealm, string userId, string fullName, string email, CancellationToken ct = default);

    /// <summary>
    /// Immediately revokes all active sessions of a Keycloak user (POST /users/{id}/logout).
    /// Call after disabling an account to ensure the user cannot continue with an existing token.
    /// </summary>
    Task LogoutAllSessionsAsync(string targetRealm, string userId, CancellationToken ct = default);
}

/// <summary>
/// Minimal representation of a Keycloak user.
/// </summary>
public sealed record KeycloakUser(string Id, string Email, bool Enabled = true, bool EmailVerified = true);

/// <summary>
/// Extended Keycloak user details including required actions and display name.
/// </summary>
public sealed record KeycloakUserDetails(string Id, string Email, bool Enabled, bool EmailVerified, IReadOnlyList<string> RequiredActions, string FullName = "");
