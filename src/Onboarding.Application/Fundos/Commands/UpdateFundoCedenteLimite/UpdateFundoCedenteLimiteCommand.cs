namespace Onboarding.Application.Fundos.Commands.UpdateFundoCedenteLimite;

/// <summary>
/// Command: update exposure limits on an existing FundoCedente association (D-21, D-22).
/// ActorSub/ActorEmail mandatory for audit trail.
/// </summary>
public sealed record UpdateFundoCedenteLimiteCommand(
    Guid AssociationId,
    decimal? LimitePercentual,
    decimal? LimiteValor,
    string ActorSub,
    string ActorEmail);
