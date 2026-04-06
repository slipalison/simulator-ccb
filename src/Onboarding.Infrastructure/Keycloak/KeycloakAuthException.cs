namespace Onboarding.Infrastructure.Keycloak;

/// <summary>
/// Thrown when Keycloak rejects authentication (invalid credentials or expired token).
/// Mapped to HTTP 401 in AuthController — message is generic (SEC-08).
/// </summary>
public sealed class KeycloakAuthException : Exception
{
    public KeycloakAuthException(string message) : base(message) { }
}
