using Onboarding.Domain.Aggregates.CustodianteAggregate;

namespace Onboarding.Application.Fundos.Commands;

/// <summary>
/// Command: update Custodiante fields (CAD-07).
/// </summary>
public sealed record UpdateCustodianteCommand(
    Guid Id,
    string RazaoSocial,
    string? CodigoInterno,
    string? Email,
    string? Telefone,
    CustodianteStatus Status);