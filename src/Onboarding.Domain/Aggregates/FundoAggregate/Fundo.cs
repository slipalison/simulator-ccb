using Onboarding.Domain.Common;
using Onboarding.Domain.Exceptions;
using Onboarding.Domain.ValueObjects;

namespace Onboarding.Domain.Aggregates.FundoAggregate;

/// <summary>
/// Fundo aggregate root — represents an investment fund with state machine lifecycle (D-02),
/// managed FundoCedente collection (D-08), and FundoTipoAtivo join records.
/// </summary>
public sealed class Fundo : Entity<Guid>
{
    public Guid ClienteId { get; private set; }
    public Cnpj Cnpj { get; private set; } = default!;
    public string Nome { get; private set; } = default!;
    public Guid ConsultoriaFundoId { get; private set; }
    public Guid CustodianteId { get; private set; }
    public TipoFundo TipoFundo { get; private set; }
    public string? ClasseAnbima { get; private set; }
    public string? Segmento { get; private set; }
    public DateTimeOffset? DataConstituicao { get; private set; }
    public FundoStatus Status { get; private set; }
    public List<FundoCedente> Cedentes { get; private set; } = [];
    public List<FundoTipoAtivo> TiposAtivo { get; private set; } = [];
    public DateTimeOffset CreatedAt { get; private set; }

    private Fundo() { }

    /// <summary>
    /// Factory method — creates a new Fundo with RASCUNHO status.
    /// </summary>
    public static Fundo Register(
        string nome,
        string cnpj,
        Guid clientId,
        Guid consultoriaFundoId,
        Guid custodianteId,
        TipoFundo tipoFundo,
        string? classeAnbima = null,
        string? segmento = null,
        DateTimeOffset? dataConstituicao = null)
    {
        if (clientId == Guid.Empty)
            throw new ArgumentException("ClientId is required.", nameof(clientId));
        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("Nome is required.", nameof(nome));

        return new Fundo
        {
            Id = Guid.NewGuid(),
            Nome = nome,
            Cnpj = Cnpj.Create(cnpj),
            ClienteId = clientId,
            ConsultoriaFundoId = consultoriaFundoId,
            CustodianteId = custodianteId,
            TipoFundo = tipoFundo,
            ClasseAnbima = classeAnbima,
            Segmento = segmento,
            DataConstituicao = dataConstituicao,
            Status = FundoStatus.RASCUNHO,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// State machine transition (D-02, D-07). Throws InvalidStateTransitionException for invalid transitions.
    /// </summary>
    public void TransitionTo(FundoStatus newStatus)
    {
        if (!FundoStatusValidator.CanTransitionTo(Status, newStatus))
            throw new InvalidStateTransitionException(nameof(Fundo), Status, newStatus);

        Status = newStatus;
    }

    /// <summary>
    /// Updates mutable Fundo data. Nome is required.
    /// </summary>
    public void Update(string nome, string? classeAnbima, string? segmento, DateTimeOffset? dataConstituicao)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("Nome is required.", nameof(nome));

        Nome = nome;
        ClasseAnbima = classeAnbima;
        Segmento = segmento;
        DataConstituicao = dataConstituicao;
    }

    // === FundoCedente management (D-08, REL-09) ===

    /// <summary>
    /// Adds a FundoCedente association. Rejects if an active association for the same CedenteId
    /// already exists (REL-09: at most one active per Fundo-Cedente pair).
    /// </summary>
    public void AddCedente(
        Guid cedenteId,
        LimiteExposicaoPercentual limitePercentual,
        decimal? limiteValor,
        DateTimeOffset dataInicio,
        DateTimeOffset? dataFim)
    {
        if (Cedentes.Any(c => c.CedenteId == cedenteId && c.Status == FundoCedenteStatus.ATIVO))
            throw new DuplicateEntityException("FundoCedente", cedenteId.ToString());

        var fc = FundoCedente.Create(Id, cedenteId, limitePercentual, limiteValor, dataInicio, dataFim);
        Cedentes.Add(fc);
    }

    /// <summary>
    /// Updates an existing FundoCedente association's details.
    /// </summary>
    public void UpdateCedente(
        Guid cedenteId,
        LimiteExposicaoPercentual limitePercentual,
        decimal? limiteValor,
        DateTimeOffset dataInicio,
        DateTimeOffset? dataFim,
        FundoCedenteStatus status)
    {
        var fc = Cedentes.FirstOrDefault(c => c.CedenteId == cedenteId)
            ?? throw new InvalidOperationException($"FundoCedente with CedenteId '{cedenteId}' not found.");

        fc.UpdateDetails(limitePercentual, limiteValor, dataInicio, dataFim, status);
    }

    /// <summary>
    /// Removes a FundoCedente association by CedenteId. Idempotent — does nothing if not found.
    /// </summary>
    public void RemoveCedente(Guid cedenteId)
    {
        var fc = Cedentes.FirstOrDefault(c => c.CedenteId == cedenteId);
        if (fc is not null)
            Cedentes.Remove(fc);
    }

    // === FundoTipoAtivo management ===

    /// <summary>
    /// Adds a FundoTipoAtivo join record. Rejects duplicates.
    /// </summary>
    public void AddTipoAtivo(Guid tipoAtivoId)
    {
        if (TiposAtivo.Any(t => t.TipoAtivoId == tipoAtivoId))
            throw new DuplicateEntityException("FundoTipoAtivo", tipoAtivoId.ToString());

        TiposAtivo.Add(FundoTipoAtivo.Create(Id, tipoAtivoId));
    }

    /// <summary>
    /// Removes a FundoTipoAtivo join record. Idempotent — does nothing if not found.
    /// </summary>
    public void RemoveTipoAtivo(Guid tipoAtivoId)
    {
        var ta = TiposAtivo.FirstOrDefault(t => t.TipoAtivoId == tipoAtivoId);
        if (ta is not null)
            TiposAtivo.Remove(ta);
    }
}