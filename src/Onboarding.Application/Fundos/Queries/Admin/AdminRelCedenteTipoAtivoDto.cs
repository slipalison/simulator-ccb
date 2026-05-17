using Onboarding.Domain.Aggregates.FundoCedenteAggregate;

namespace Onboarding.Application.Fundos.Queries.Admin;

/// <summary>
/// Cross-company admin DTO for CedenteTipoAtivo relationship listing (D-8).
/// Includes ClienteId + EmpresaNome from Cedente→Company join.
/// </summary>
public sealed record AdminRelCedenteTipoAtivoDto(
    Guid Id,
    Guid ClienteId,
    string EmpresaNome,
    Guid CedenteId,
    Guid TipoAtivoId,
    decimal? LimitePercentual,
    decimal? LimiteValor,
    DateTimeOffset DataInicio,
    DateTimeOffset? DataFim,
    RelationshipStatus Status,
    DateTimeOffset CreatedAt);
