namespace Onboarding.Application.Fundos.Queries.GetFundoTipoAtivoAllowedTransitions;

/// <summary>
/// Query: get the allowed next statuses for a FundoTipoAtivo association (D-25).
/// FundoId is included to enforce tenant boundary (parent Fundo.ClienteId check).
/// Returns null result if aggregate is not found or caller does not own the parent Fundo.
/// </summary>
public sealed record GetFundoTipoAtivoAllowedTransitionsQuery(Guid FundoId, Guid AssociationId);
