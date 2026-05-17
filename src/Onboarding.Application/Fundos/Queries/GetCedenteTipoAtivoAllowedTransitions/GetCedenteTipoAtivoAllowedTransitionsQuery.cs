namespace Onboarding.Application.Fundos.Queries.GetCedenteTipoAtivoAllowedTransitions;

/// <summary>
/// Query: get the allowed next statuses for a CedenteTipoAtivo association (D-25).
/// CedenteId is included to enforce tenant boundary (parent Cedente.ClienteId check).
/// Returns null result if aggregate is not found or caller does not own the parent Cedente.
/// </summary>
public sealed record GetCedenteTipoAtivoAllowedTransitionsQuery(Guid CedenteId, Guid AssociationId);
