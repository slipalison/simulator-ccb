using Onboarding.Domain.Aggregates.CustodianteAggregate;

namespace Onboarding.Application.Fundos.DTOs;

/// <summary>
/// DTO for Custodiante responses (CAD-05, CAD-06).
/// </summary>
public sealed record CustodianteDto(
    Guid Id,
    string RazaoSocial,
    string? CodigoInterno,
    string Cnpj,
    string? Email,
    string? Telefone,
    CustodianteStatus Status,
    DateTimeOffset CreatedAt);