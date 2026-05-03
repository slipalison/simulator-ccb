using Onboarding.Domain.Aggregates.CedenteAggregate;

namespace Onboarding.Application.Fundos.Commands;

/// <summary>
/// Command: update Cedente data (CAD-17).
/// </summary>
public sealed record UpdateCedenteCommand(
    Guid Id,
    string Nome,
    string? Email,
    string? Telefone,
    string? Endereco,
    CedenteStatus Status);