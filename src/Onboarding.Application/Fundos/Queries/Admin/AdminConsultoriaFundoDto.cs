using Onboarding.Domain.Aggregates.ConsultoriaFundoAggregate;

namespace Onboarding.Application.Fundos.Queries.Admin;

/// <summary>
/// Cross-company admin DTO for ConsultoriaFundo listing (D-8).
/// Includes ClienteId + EmpresaNome from Company join — no company filter applied.
/// </summary>
public sealed record AdminConsultoriaFundoDto(
    Guid Id,
    Guid ClienteId,
    string EmpresaNome,
    string RazaoSocial,
    string? NomeFantasia,
    string Cnpj,
    string? Email,
    string? Telefone,
    ConsultoriaFundoStatus Status,
    DateTimeOffset CreatedAt);
