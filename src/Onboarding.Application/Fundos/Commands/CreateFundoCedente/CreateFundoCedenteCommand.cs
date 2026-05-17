namespace Onboarding.Application.Fundos.Commands.CreateFundoCedente;

/// <summary>
/// Command: create a new FundoCedente association (Phase 50, D-18, D-21, D-22).
/// REL-09: at most one ATIVO per (FundoId, CedenteId) — handler checks before persist.
/// ActorSub/ActorEmail mandatory for audit trail (D-22).
/// </summary>
public sealed record CreateFundoCedenteCommand(
    Guid FundoId,
    Guid CedenteId,
    decimal? LimitePercentual,
    decimal? LimiteValor,
    DateTimeOffset DataInicio,
    DateTimeOffset? DataFim,
    string ActorSub,
    string ActorEmail);
