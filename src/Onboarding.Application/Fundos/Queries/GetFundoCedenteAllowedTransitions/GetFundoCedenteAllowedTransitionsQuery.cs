namespace Onboarding.Application.Fundos.Queries.GetFundoCedenteAllowedTransitions;

/// <summary>
/// Query: get the allowed next statuses for a FundoCedente association (D-25).
/// FundoId is included to enforce tenant boundary (parent Fundo.ClienteId check).
/// Returns null result if aggregate is not found or caller does not own the parent Fundo.
/// </summary>
public sealed record GetFundoCedenteAllowedTransitionsQuery(Guid FundoId, Guid AssociationId);
