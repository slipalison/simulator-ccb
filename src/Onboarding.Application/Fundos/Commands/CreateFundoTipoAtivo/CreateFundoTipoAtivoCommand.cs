namespace Onboarding.Application.Fundos.Commands.CreateFundoTipoAtivo;

public sealed record CreateFundoTipoAtivoCommand(
    Guid FundoId,
    Guid TipoAtivoId,
    decimal? LimitePercentual,
    decimal? LimiteValor,
    DateTimeOffset DataInicio,
    DateTimeOffset? DataFim,
    string ActorSub,
    string ActorEmail);
