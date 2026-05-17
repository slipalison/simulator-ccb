using Onboarding.Domain.Aggregates.FundoCedenteAggregate;

namespace Onboarding.Application.Fundos.Commands.TransitionFundoCedenteStatus;

/// <summary>
/// Command: transition a FundoCedente association's Status (D-22 state-machine action).
/// Body: { AssociationId, NewStatus }. ActorSub/ActorEmail for audit trail.
/// Domain enforces valid transitions; HISTORICO is terminal.
/// </summary>
public sealed record TransitionFundoCedenteStatusCommand(
    Guid AssociationId,
    RelationshipStatus NewStatus,
    string ActorSub,
    string ActorEmail);
