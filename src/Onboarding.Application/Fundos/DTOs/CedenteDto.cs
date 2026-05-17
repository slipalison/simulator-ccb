using Onboarding.Domain.Aggregates.CedenteAggregate;

namespace Onboarding.Application.Fundos.DTOs;

/// <summary>
/// DTO for Cedente responses (CAD-14, CAD-15, CAD-16, CAD-17).
/// Documento is displayed as CPF/CNPJ string; CedenteTipo indicates PF or PJ.
/// </summary>
public sealed record CedenteDto(
    Guid Id,
    string Documento,
    string Nome,
    string? Email,
    string? Telefone,
    string? Endereco,
    CedenteTipo CedenteTipo,
    CedenteStatus Status,
    DateTimeOffset CreatedAt);