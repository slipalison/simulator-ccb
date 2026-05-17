namespace Onboarding.Application.Fundos.Commands.UpdateFundoTipoAtivoLimite;

public sealed record UpdateFundoTipoAtivoLimiteCommand(
    Guid AssociationId,
    decimal? LimitePercentual,
    decimal? LimiteValor,
    string ActorSub,
    string ActorEmail);
