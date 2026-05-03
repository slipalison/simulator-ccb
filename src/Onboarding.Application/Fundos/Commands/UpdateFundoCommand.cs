namespace Onboarding.Application.Fundos.Commands;

/// <summary>
/// Command: update Fundo mutable data (CAD-11).
/// Status transitions are handled separately via TransitionFundoStatusCommand.
/// </summary>
public sealed record UpdateFundoCommand(
    Guid Id,
    string Nome,
    string? ClasseAnbima,
    string? Segmento,
    DateTimeOffset? DataConstituicao,
    string ActorSub,
    string ActorEmail);