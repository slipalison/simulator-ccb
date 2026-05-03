using Onboarding.Domain.Aggregates.ConsultoriaFundoAggregate;

namespace Onboarding.Application.Fundos.DTOs;

/// <summary>
/// DTO for ConsultoriaFundo responses (CAD-01, CAD-02).
/// </summary>
public sealed record ConsultoriaFundoDto(
    Guid Id,
    string RazaoSocial,
    string? NomeFantasia,
    string Cnpj,
    string? Email,
    string? Telefone,
    ConsultoriaFundoStatus Status,
    DateTimeOffset CreatedAt);