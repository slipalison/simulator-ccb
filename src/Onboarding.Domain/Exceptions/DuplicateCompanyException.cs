namespace Onboarding.Domain.Exceptions;

/// <summary>
/// Thrown when a registration attempt conflicts with an existing company record.
/// The message is intentionally generic — callers (controllers) must NOT propagate
/// this message to HTTP responses to avoid information leakage (SEC-08).
/// </summary>
public sealed class DuplicateCompanyException : Exception
{
    public DuplicateCompanyException(string message) : base(message) { }
    public DuplicateCompanyException(string message, Exception inner) : base(message, inner) { }
}