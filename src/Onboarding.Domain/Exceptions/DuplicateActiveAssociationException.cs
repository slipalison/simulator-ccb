namespace Onboarding.Domain.Exceptions;

/// <summary>
/// Thrown when attempting to create an active association that already exists (REL-09, D-18).
/// Applies to FundoCedente (FundoId + CedenteId), CedenteTipoAtivo, and FundoTipoAtivo.
/// DB partial unique index is the authoritative gate for race conditions;
/// this exception is raised in the domain for the happy-path in-memory check.
/// </summary>
public sealed class DuplicateActiveAssociationException : DomainException
{
    public string AssociationType { get; }
    public string Key { get; }

    public DuplicateActiveAssociationException(string associationType, string key)
        : base($"An active '{associationType}' association already exists for '{key}'.")
    {
        AssociationType = associationType;
        Key = key;
    }
}
