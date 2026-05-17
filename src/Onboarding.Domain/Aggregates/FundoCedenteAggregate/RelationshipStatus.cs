namespace Onboarding.Domain.Aggregates.FundoCedenteAggregate;

/// <summary>
/// Status of a relationship aggregate (FundoCedente, CedenteTipoAtivo, FundoTipoAtivo).
/// D-19: Status is explicit — NOT derived from date window.
/// State machine (D-22): ATIVO ↔ INATIVO, any → HISTORICO, HISTORICO is terminal.
/// </summary>
public enum RelationshipStatus
{
    ATIVO = 1,
    INATIVO = 2,
    HISTORICO = 3
}
