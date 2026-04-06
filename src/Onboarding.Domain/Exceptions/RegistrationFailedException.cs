namespace Onboarding.Domain.Exceptions;

/// <summary>
/// Thrown when the Keycloak user creation fails after the client was already persisted to app_db.
/// The compensation (DeleteAsync) is called before this is thrown.
/// The InnerException contains the original Keycloak error for observability (logs), but it
/// must NOT be propagated to the HTTP response body.
/// </summary>
public sealed class RegistrationFailedException : Exception
{
    public RegistrationFailedException(string message) : base(message) { }
    public RegistrationFailedException(string message, Exception inner) : base(message, inner) { }
}
