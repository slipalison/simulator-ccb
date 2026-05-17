namespace Onboarding.Application.Fundos.Commands.CreateCedenteTipoAtivo;

/// <summary>
/// Command: create a new CedenteTipoAtivo association (Phase 50, D-21, D-22).
/// Symmetric with CreateFundoCedenteCommand pattern.
/// </summary>
public sealed record CreateCedenteTipoAtivoCommand(
    Guid CedenteId,
    Guid TipoAtivoId,
    decimal? LimitePercentual,
    decimal? LimiteValor,
    DateTimeOffset DataInicio,
    DateTimeOffset? DataFim,
    string ActorSub,
    string ActorEmail);
