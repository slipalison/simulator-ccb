namespace Onboarding.Application.Fundos.Commands.UpdateCedenteTipoAtivoLimite;

public sealed record UpdateCedenteTipoAtivoLimiteCommand(
    Guid AssociationId,
    decimal? LimitePercentual,
    decimal? LimiteValor,
    string ActorSub,
    string ActorEmail);
