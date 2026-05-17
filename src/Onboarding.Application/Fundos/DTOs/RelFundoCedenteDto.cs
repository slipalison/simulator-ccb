using Onboarding.Domain.Aggregates.FundoCedenteAggregate;

namespace Onboarding.Application.Fundos.DTOs;

/// <summary>
/// Read DTO for FundoCedenteAggregate — standalone relationship aggregate (Phase 50, D-21).
/// Distinct from the legacy FundoCedenteDto (owned entity inside Fundo aggregate).
/// </summary>
public sealed record RelFundoCedenteDto(
    Guid Id,
    Guid FundoId,
    Guid CedenteId,
    decimal? LimitePercentual,
    decimal? LimiteValor,
    DateTimeOffset DataInicio,
    DateTimeOffset? DataFim,
    RelationshipStatus Status,
    DateTimeOffset CreatedAt);
