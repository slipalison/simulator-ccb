using Onboarding.Domain.Aggregates.TipoAtivoAggregate;

namespace Onboarding.Application.Fundos.DTOs;

/// <summary>
/// DTO for TipoAtivo responses (CAD-19, CAD-20).
/// Global entity per D-03 — no company scope.
/// </summary>
public sealed record TipoAtivoDto(
    Guid Id,
    string Codigo,
    string Descricao,
    TipoAtivoCategoria Categoria,
    string? Subcategoria,
    TipoAtivoStatus Status,
    int OrdemExibicao);