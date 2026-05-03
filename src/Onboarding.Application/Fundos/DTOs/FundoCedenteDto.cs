using Onboarding.Domain.Aggregates.FundoAggregate;

namespace Onboarding.Application.Fundos.DTOs;

/// <summary>
/// DTO for FundoCedente association responses (REL-01, REL-02, REL-03).
/// Includes exposure limits (sentinel -1 = unlimited per D-04).
/// </summary>
public sealed record FundoCedenteDto(
    Guid CedenteId,
    decimal LimiteExposicaoPercentual,
    decimal? LimiteExposicaoValor,
    DateTimeOffset DataInicio,
    DateTimeOffset? DataFim,
    FundoCedenteStatus Status);