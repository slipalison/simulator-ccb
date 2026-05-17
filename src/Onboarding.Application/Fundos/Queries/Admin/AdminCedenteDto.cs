using Onboarding.Domain.Aggregates.CedenteAggregate;

namespace Onboarding.Application.Fundos.Queries.Admin;

/// <summary>
/// Cross-company admin DTO for Cedente listing (D-8).
/// Includes ClienteId + EmpresaNome from Company join — no company filter applied.
/// </summary>
public sealed record AdminCedenteDto(
    Guid Id,
    Guid ClienteId,
    string EmpresaNome,
    string Documento,
    string Nome,
    string? Email,
    string? Telefone,
    string? Endereco,
    CedenteTipo CedenteTipo,
    CedenteStatus Status,
    DateTimeOffset CreatedAt);
