using Onboarding.Domain.Aggregates.FundoCedenteAggregate;

namespace Onboarding.Application.Fundos.Commands.TransitionCedenteTipoAtivoStatus;

public sealed record TransitionCedenteTipoAtivoStatusCommand(
    Guid AssociationId,
    RelationshipStatus NewStatus,
    string ActorSub,
    string ActorEmail);
