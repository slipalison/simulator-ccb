using Onboarding.Domain.Common;

namespace Onboarding.Domain.Aggregates.TipoAtivoAggregate;

/// <summary>
/// TipoAtivo aggregate root — global asset type catalog (D-03/TEN-03).
/// NO ClientId — this entity is shared across all companies.
/// </summary>
public sealed class TipoAtivo : Entity<Guid>
{
    public string Codigo { get; private set; } = default!;
    public string Descricao { get; private set; } = default!;
    public TipoAtivoCategoria Categoria { get; private set; }
    public string? Subcategoria { get; private set; }
    public TipoAtivoStatus Status { get; private set; }
    public int OrdemExibicao { get; private set; }

    private TipoAtivo() { }

    /// <summary>
    /// Factory method — creates a new TipoAtivo with ATIVO status.
    /// Global entity per D-03/TEN-03 — no ClientId.
    /// </summary>
    public static TipoAtivo Register(
        string codigo,
        string descricao,
        TipoAtivoCategoria categoria,
        string? subcategoria = null,
        int ordemExibicao = 0)
    {
        if (string.IsNullOrWhiteSpace(codigo))
            throw new ArgumentException("Codigo is required.", nameof(codigo));
        if (string.IsNullOrWhiteSpace(descricao))
            throw new ArgumentException("Descricao is required.", nameof(descricao));

        return new TipoAtivo
        {
            Id = Guid.NewGuid(),
            Codigo = codigo,
            Descricao = descricao,
            Categoria = categoria,
            Subcategoria = subcategoria,
            Status = TipoAtivoStatus.ATIVO,
            OrdemExibicao = ordemExibicao
        };
    }

    /// <summary>
    /// Updates TipoAtivo data. Descricao is required.
    /// </summary>
    public void Update(string descricao, string? subcategoria, TipoAtivoStatus status, int ordemExibicao)
    {
        if (string.IsNullOrWhiteSpace(descricao))
            throw new ArgumentException("Descricao is required.", nameof(descricao));

        Descricao = descricao;
        Subcategoria = subcategoria;
        Status = status;
        OrdemExibicao = ordemExibicao;
    }
}