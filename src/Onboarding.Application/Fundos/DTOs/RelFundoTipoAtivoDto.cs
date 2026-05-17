using Onboarding.Domain.Aggregates.FundoCedenteAggregate;

namespace Onboarding.Application.Fundos.DTOs;

/// <summary>
/// Read DTO for FundoTipoAtivoAggregate (Phase 50, D-21).
/// </summary>
public sealed record RelFundoTipoAtivoDto(
    Guid Id,
    Guid FundoId,
    Guid TipoAtivoId,
    decimal? LimitePercentual,
    decimal? LimiteValor,
    DateTimeOffset DataInicio,
    DateTimeOffset? DataFim,
    RelationshipStatus Status,
    DateTimeOffset CreatedAt);
