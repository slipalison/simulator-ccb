using Onboarding.Domain.Aggregates.CustodianteAggregate;

namespace Onboarding.Application.Fundos.Queries.Admin;

/// <summary>
/// Cross-company admin DTO for Custodiante listing (D-8).
/// Includes ClienteId + EmpresaNome from Company join — no company filter applied.
/// </summary>
public sealed record AdminCustodianteDto(
    Guid Id,
    Guid ClienteId,
    string EmpresaNome,
    string RazaoSocial,
    string? CodigoInterno,
    string Cnpj,
    string? Email,
    string? Telefone,
    CustodianteStatus Status,
    DateTimeOffset CreatedAt);
