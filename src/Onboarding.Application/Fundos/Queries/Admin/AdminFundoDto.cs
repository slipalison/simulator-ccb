using Onboarding.Domain.Aggregates.FundoAggregate;

namespace Onboarding.Application.Fundos.Queries.Admin;

/// <summary>
/// Cross-company admin DTO for Fundo listing (D-8).
/// Includes ClienteId + EmpresaNome from Company join — no company filter applied.
/// </summary>
public sealed record AdminFundoDto(
    Guid Id,
    Guid ClienteId,
    string EmpresaNome,
    string Nome,
    string Cnpj,
    Guid ConsultoriaFundoId,
    Guid CustodianteId,
    TipoFundo TipoFundo,
    string? ClasseAnbima,
    string? Segmento,
    DateTimeOffset? DataConstituicao,
    FundoStatus Status,
    DateTimeOffset CreatedAt);
