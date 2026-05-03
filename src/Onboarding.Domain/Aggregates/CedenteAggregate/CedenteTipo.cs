namespace Onboarding.Domain.Aggregates.CedenteAggregate;

/// <summary>
/// Indicates whether a Cedente is a natural person (PF) or legal entity (PJ).
/// Used in the Cedente aggregate to determine document validation rules.
/// </summary>
public enum CedenteTipo
{
    PF = 1,
    PJ = 2
}