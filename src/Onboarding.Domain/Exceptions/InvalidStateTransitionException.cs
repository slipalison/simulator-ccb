using Onboarding.Domain.Aggregates.FundoAggregate;

namespace Onboarding.Domain.Exceptions;

/// <summary>
/// Thrown when a FundoStatus transition violates the state machine rules (D-02).
/// Carries the entity name, from-status, and to-status for observability.
/// </summary>
public sealed class InvalidStateTransitionException : DomainException
{
    public string EntityName { get; }
    public FundoStatus From { get; }
    public FundoStatus To { get; }

    public InvalidStateTransitionException(string entityName, FundoStatus from, FundoStatus to)
        : base($"'{entityName}' cannot transition from {from} to {to}.")
    {
        EntityName = entityName;
        From = from;
        To = to;
    }

    public InvalidStateTransitionException(string message)
        : base(message)
    {
        EntityName = string.Empty;
        From = default;
        To = default;
    }
}