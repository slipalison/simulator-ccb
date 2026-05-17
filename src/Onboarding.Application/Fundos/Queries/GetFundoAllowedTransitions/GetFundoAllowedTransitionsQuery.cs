namespace Onboarding.Application.Fundos.Queries.GetFundoAllowedTransitions;

/// <summary>
/// Query: get the allowed next statuses for a Fundo identified by id (D-25).
/// Returns null result if the aggregate is not found or belongs to another tenant.
/// </summary>
public sealed record GetFundoAllowedTransitionsQuery(Guid FundoId);
