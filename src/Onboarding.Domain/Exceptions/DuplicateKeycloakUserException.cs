namespace Onboarding.Domain.Exceptions;

/// <summary>
/// Thrown when attempting to create a Keycloak user that already exists
/// (duplicate email, username, or other unique identifier).
/// Maps to HTTP 409 Conflict in the API layer.
/// </summary>
public sealed class DuplicateKeycloakUserException : Exception
{
    public DuplicateKeycloakUserException(string message) : base(message) { }
    public DuplicateKeycloakUserException(string message, Exception inner) : base(message, inner) { }
}
