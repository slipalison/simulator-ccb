using Onboarding.Domain.Aggregates.FundoCedenteAggregate;
using Onboarding.Domain.Common;
using Onboarding.Domain.Exceptions;
using Onboarding.Domain.ValueObjects;

namespace Onboarding.Domain.Aggregates.FundoTipoAtivoAggregate;

/// <summary>
/// Standalone relationship aggregate between Fundo and TipoAtivo (D-21).
/// Carries exposure limits (LimiteExposicao VO), date window (JanelaVigencia VO), and Status.
/// Partial unique index (FundoId, TipoAtivoId) WHERE Status=ATIVO enforced at DB level.
/// Tenant scoping via parent Fundo.ClienteId — no ClienteId here (D-5).
/// </summary>
public sealed class FundoTipoAtivoAggregate : Entity<Guid>
{
    public Guid FundoId { get; private set; }
    public Guid TipoAtivoId { get; private set; }
    public LimiteExposicao Limite { get; private set; } = default!;
    public JanelaVigencia Janela { get; private set; } = default!;
    public RelationshipStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private FundoTipoAtivoAggregate() { }

    /// <summary>
    /// Factory method — creates a new FundoTipoAtivo association with ATIVO status.
    /// </summary>
    public static FundoTipoAtivoAggregate Create(
        Guid fundoId,
        Guid tipoAtivoId,
        LimiteExposicao limite,
        JanelaVigencia janela)
    {
        if (fundoId == Guid.Empty)
            throw new ArgumentException("FundoId is required.", nameof(fundoId));
        if (tipoAtivoId == Guid.Empty)
            throw new ArgumentException("TipoAtivoId is required.", nameof(tipoAtivoId));
        if (limite is null)
            throw new ArgumentNullException(nameof(limite));
        if (janela is null)
            throw new ArgumentNullException(nameof(janela));

        return new FundoTipoAtivoAggregate
        {
            Id = Guid.NewGuid(),
            FundoId = fundoId,
            TipoAtivoId = tipoAtivoId,
            Limite = limite,
            Janela = janela,
            Status = RelationshipStatus.ATIVO,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// In-memory guard for duplicate active association.
    /// DB partial unique index is the authoritative race-condition gate.
    /// </summary>
    public static void ActivateGuard(bool existsActiveForPair)
    {
        if (existsActiveForPair)
            throw new DuplicateActiveAssociationException(
                "FundoTipoAtivo", "FundoId+TipoAtivoId pair already has an active association.");
    }

    /// <summary>
    /// Updates exposure limits. Status must not be HISTORICO.
    /// </summary>
    public void UpdateLimite(LimiteExposicao limite)
    {
        if (Status == RelationshipStatus.HISTORICO)
            throw new InvalidStateTransitionException(
                $"Cannot update limits on a HISTORICO FundoTipoAtivo association (Id={Id}).");
        if (limite is null)
            throw new ArgumentNullException(nameof(limite));

        Limite = limite;
    }

    /// <summary>
    /// Transitions Status per state machine (D-22).
    /// Valid: ATIVO ↔ INATIVO, any → HISTORICO. HISTORICO is terminal.
    /// </summary>
    public void TransitionTo(RelationshipStatus newStatus)
    {
        if (!CanTransitionTo(newStatus))
            throw new InvalidStateTransitionException(
                $"FundoTipoAtivo cannot transition from {Status} to {newStatus}.");

        Status = newStatus;
    }

    /// <summary>
    /// Returns true if the transition from current Status to newStatus is valid.
    /// </summary>
    public bool CanTransitionTo(RelationshipStatus newStatus)
    {
        if (Status == RelationshipStatus.HISTORICO)
            return false;

        return newStatus switch
        {
            RelationshipStatus.ATIVO => Status == RelationshipStatus.INATIVO,
            RelationshipStatus.INATIVO => Status == RelationshipStatus.ATIVO,
            RelationshipStatus.HISTORICO => true,
            _ => false
        };
    }
}
