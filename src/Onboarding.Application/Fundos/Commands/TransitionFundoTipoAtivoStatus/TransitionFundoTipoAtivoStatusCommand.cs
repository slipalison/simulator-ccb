using Onboarding.Domain.Aggregates.FundoCedenteAggregate;

namespace Onboarding.Application.Fundos.Commands.TransitionFundoTipoAtivoStatus;

public sealed record TransitionFundoTipoAtivoStatusCommand(
    Guid AssociationId,
    RelationshipStatus NewStatus,
    string ActorSub,
    string ActorEmail);
