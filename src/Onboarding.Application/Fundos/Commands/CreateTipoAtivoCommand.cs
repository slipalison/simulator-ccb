using Onboarding.Domain.Aggregates.TipoAtivoAggregate;

namespace Onboarding.Application.Fundos.Commands;

/// <summary>
/// Command: create a new TipoAtivo (CAD-19).
/// Global entity per D-03/TEN-03 — no CompanyId.
/// </summary>
public sealed record CreateTipoAtivoCommand(
    string Codigo,
    string Descricao,
    TipoAtivoCategoria Categoria,
    string? Subcategoria,
    int OrdemExibicao = 0,
    string ActorSub = "",
    string ActorEmail = "");