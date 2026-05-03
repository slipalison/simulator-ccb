using Onboarding.Domain.Aggregates.TipoAtivoAggregate;

namespace Onboarding.Application.Fundos.Commands;

/// <summary>
/// Command: update TipoAtivo fields (CAD-21).
/// </summary>
public sealed record UpdateTipoAtivoCommand(
    Guid Id,
    string Descricao,
    string? Subcategoria,
    TipoAtivoStatus Status,
    int OrdemExibicao);