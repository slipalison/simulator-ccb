using Onboarding.Domain.Aggregates.FundoAggregate;

namespace Onboarding.Application.Fundos.Commands;

/// <summary>
/// Command: transition Fundo status per state machine (CAD-13, D-02).
/// Invalid transitions throw InvalidStateTransitionException from the domain layer.
/// </summary>
public sealed record TransitionFundoStatusCommand(
    Guid FundoId,
    FundoStatus NewStatus,
    string ActorSub,
    string ActorEmail);