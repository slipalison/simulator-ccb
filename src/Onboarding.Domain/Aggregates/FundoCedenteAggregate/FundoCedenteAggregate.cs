using Onboarding.Domain.Common;
using Onboarding.Domain.Exceptions;
using Onboarding.Domain.ValueObjects;

namespace Onboarding.Domain.Aggregates.FundoCedenteAggregate;

/// <summary>
/// Standalone relationship aggregate between Fundo and Cedente (D-21).
/// Carries exposure limits (LimiteExposicao VO), date window (JanelaVigencia VO), and Status.
/// REL-09: at most one ATIVO association per (FundoId, CedenteId) pair — enforced
/// in-memory by ActivateGuard() and at DB level by partial unique index (D-18).
/// Tenant scoping comes via parent Fundo.ClienteId / Cedente.ClienteId — no ClienteId here (D-5).
/// </summary>
public sealed class FundoCedenteAggregate : Entity<Guid>
{
    public Guid FundoId { get; private set; }
    public Guid CedenteId { get; private set; }
    public LimiteExposicao Limite { get; private set; } = default!;
    public JanelaVigencia Janela { get; private set; } = default!;
    public RelationshipStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private FundoCedenteAggregate() { }

    /// <summary>
    /// Factory method — creates a new FundoCedente association with ATIVO status.
    /// FundoId and CedenteId must not be empty.
    /// </summary>
    public static FundoCedenteAggregate Create(
        Guid fundoId,
        Guid cedenteId,
        LimiteExposicao limite,
        JanelaVigencia janela)
    {
        if (fundoId == Guid.Empty)
            throw new ArgumentException("FundoId is required.", nameof(fundoId));
        if (cedenteId == Guid.Empty)
            throw new ArgumentException("CedenteId is required.", nameof(cedenteId));
        if (limite is null)
            throw new ArgumentNullException(nameof(limite));
        if (janela is null)
            throw new ArgumentNullException(nameof(janela));

        return new FundoCedenteAggregate
        {
            Id = Guid.NewGuid(),
            FundoId = fundoId,
            CedenteId = cedenteId,
            Limite = limite,
            Janela = janela,
            Status = RelationshipStatus.ATIVO,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// In-memory REL-09 guard (D-18, defesa-em-profundidade).
    /// Call before persisting a new ATIVO association to detect duplicates from the repository check.
    /// The DB partial unique index is the authoritative race-condition gate.
    /// </summary>
    public static void ActivateGuard(bool existsActiveForPair)
    {
        if (existsActiveForPair)
            throw new DuplicateActiveAssociationException(
                "FundoCedente", "FundoId+CedenteId pair already has an active association.");
    }

    /// <summary>
    /// Updates exposure limits. Status must not be HISTORICO.
    /// </summary>
    public void UpdateLimite(LimiteExposicao limite)
    {
        if (Status == RelationshipStatus.HISTORICO)
            throw new InvalidStateTransitionException(
                $"Cannot update limits on a HISTORICO FundoCedente association (Id={Id}).");
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
                $"FundoCedente cannot transition from {Status} to {newStatus}.");

        Status = newStatus;
    }

    /// <summary>
    /// Returns true if the transition from current Status to newStatus is valid.
    /// HISTORICO is terminal — no transitions out.
    /// </summary>
    public bool CanTransitionTo(RelationshipStatus newStatus)
    {
        if (Status == RelationshipStatus.HISTORICO)
            return false; // terminal

        return newStatus switch
        {
            RelationshipStatus.ATIVO => Status == RelationshipStatus.INATIVO,
            RelationshipStatus.INATIVO => Status == RelationshipStatus.ATIVO,
            RelationshipStatus.HISTORICO => true, // any → HISTORICO
            _ => false
        };
    }

    /// <summary>
    /// Returns the list of valid next statuses from the current Status.
    /// Single source of truth — delegates to CanTransitionTo (D-25).
    /// </summary>
    public IReadOnlyList<string> GetAllowedNextStates()
    {
        var all = (RelationshipStatus[])Enum.GetValues(typeof(RelationshipStatus));
        var result = new List<string>(all.Length);
        foreach (var candidate in all)
        {
            if (CanTransitionTo(candidate))
                result.Add(candidate.ToString());
        }
        return result;
    }
}
