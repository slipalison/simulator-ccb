namespace Onboarding.Domain.Exceptions;

/// <summary>
/// Base exception for all domain business rule violations.
/// Inheritors carry domain-specific context (entity name, state, key values).
/// Controllers translate these to appropriate HTTP status codes — messages are never
/// exposed directly to API consumers (T-45-04).
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
    protected DomainException(string message, Exception inner) : base(message, inner) { }
}