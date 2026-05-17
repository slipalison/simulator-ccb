using Onboarding.Domain.Aggregates.FundoCedenteAggregate;

namespace Onboarding.Application.Fundos.DTOs;

/// <summary>
/// Read DTO for CedenteTipoAtivoAggregate (Phase 50, D-21).
/// </summary>
public sealed record RelCedenteTipoAtivoDto(
    Guid Id,
    Guid CedenteId,
    Guid TipoAtivoId,
    decimal? LimitePercentual,
    decimal? LimiteValor,
    DateTimeOffset DataInicio,
    DateTimeOffset? DataFim,
    RelationshipStatus Status,
    DateTimeOffset CreatedAt);
