namespace Onboarding.Domain.Exceptions;

/// <summary>
/// Thrown when a registration attempt conflicts with an existing client record.
/// The message is intentionally generic — callers (controllers) must NOT propagate
/// this message to HTTP responses to avoid information leakage (SEC-08).
/// </summary>
public sealed class DuplicateClientException : Exception
{
    public DuplicateClientException(string message) : base(message) { }
    public DuplicateClientException(string message, Exception inner) : base(message, inner) { }
}
