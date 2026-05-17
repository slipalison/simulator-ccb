using Onboarding.Domain.Aggregates.FundoAggregate;

namespace Onboarding.Application.Fundos.DTOs;

/// <summary>
/// DTO for Fundo responses (CAD-09, CAD-10, CAD-11).
/// Status reflects state machine position (D-02).
/// </summary>
public sealed record FundoDto(
    Guid Id,
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