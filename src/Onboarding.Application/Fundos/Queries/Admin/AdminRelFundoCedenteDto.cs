using Onboarding.Domain.Aggregates.FundoCedenteAggregate;

namespace Onboarding.Application.Fundos.Queries.Admin;

/// <summary>
/// Cross-company admin DTO for FundoCedente relationship listing (D-8).
/// Includes ClienteId + EmpresaNome from Fundo→Company join.
/// </summary>
public sealed record AdminRelFundoCedenteDto(
    Guid Id,
    Guid ClienteId,
    string EmpresaNome,
    Guid FundoId,
    string FundoNome,
    Guid CedenteId,
    decimal? LimitePercentual,
    decimal? LimiteValor,
    DateTimeOffset DataInicio,
    DateTimeOffset? DataFim,
    RelationshipStatus Status,
    DateTimeOffset CreatedAt);
