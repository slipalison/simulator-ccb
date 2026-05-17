using Onboarding.Domain.Aggregates.FundoCedenteAggregate;
using Onboarding.Domain.Exceptions;
using Onboarding.Domain.ValueObjects;
using Shouldly;

namespace Onboarding.Domain.Tests.Aggregates;

/// <summary>
/// Tests for FundoCedenteAggregate — invariants, state machine, and REL-09 guard (D-18, D-21, D-22).
/// </summary>
public class FundoCedenteAggregateTests
{
    private static readonly Guid FundoId = Guid.NewGuid();
    private static readonly Guid CedenteId = Guid.NewGuid();

    private static LimiteExposicao DefaultLimite()
        => LimiteExposicao.Create(50m, null);

    private static JanelaVigencia DefaultJanela()
        => JanelaVigencia.Create(DateTimeOffset.UtcNow);

    // === Factory ===

    [Fact]
    public void Create_WithValidArgs_ReturnsAtivoAggregate()
    {
        var agg = FundoCedenteAggregate.Create(FundoId, CedenteId, DefaultLimite(), DefaultJanela());

        agg.Id.ShouldNotBe(Guid.Empty);
        agg.FundoId.ShouldBe(FundoId);
        agg.CedenteId.ShouldBe(CedenteId);
        agg.Status.ShouldBe(RelationshipStatus.ATIVO);
        agg.CreatedAt.ShouldBeGreaterThan(DateTimeOffset.UtcNow.AddSeconds(-5));
    }

    [Fact]
    public void Create_EmptyFundoId_ThrowsArgumentException()
    {
        var ex = Should.Throw<ArgumentException>(() =>
            FundoCedenteAggregate.Create(Guid.Empty, CedenteId, DefaultLimite(), DefaultJanela()));
        ex.ParamName.ShouldBe("fundoId");
    }

    [Fact]
    public void Create_EmptyCedenteId_ThrowsArgumentException()
    {
        var ex = Should.Throw<ArgumentException>(() =>
            FundoCedenteAggregate.Create(FundoId, Guid.Empty, DefaultLimite(), DefaultJanela()));
        ex.ParamName.ShouldBe("cedenteId");
    }

    [Fact]
    public void Create_NullLimite_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            FundoCedenteAggregate.Create(FundoId, CedenteId, null!, DefaultJanela()));
    }

    [Fact]
    public void Create_NullJanela_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            FundoCedenteAggregate.Create(FundoId, CedenteId, DefaultLimite(), null!));
    }

    // === ActivateGuard (REL-09 in-memory) ===

    [Fact]
    public void ActivateGuard_NoExistingActive_DoesNotThrow()
    {
        Should.NotThrow(() => FundoCedenteAggregate.ActivateGuard(existsActiveForPair: false));
    }

    [Fact]
    public void ActivateGuard_ExistingActivePresent_ThrowsDuplicateActiveAssociation()
    {
        var ex = Should.Throw<DuplicateActiveAssociationException>(() =>
            FundoCedenteAggregate.ActivateGuard(existsActiveForPair: true));
        ex.AssociationType.ShouldBe("FundoCedente");
    }

    // === UpdateLimite ===

    [Fact]
    public void UpdateLimite_WhenAtivo_UpdatesSuccessfully()
    {
        var agg = FundoCedenteAggregate.Create(FundoId, CedenteId, DefaultLimite(), DefaultJanela());
        var newLimite = LimiteExposicao.Create(75m, 100000m);

        agg.UpdateLimite(newLimite);

        agg.Limite.Percentual.ShouldBe(75m);
        agg.Limite.Valor.ShouldBe(100000m);
    }

    [Fact]
    public void UpdateLimite_WhenHistorico_ThrowsInvalidStateTransition()
    {
        var agg = FundoCedenteAggregate.Create(FundoId, CedenteId, DefaultLimite(), DefaultJanela());
        agg.TransitionTo(RelationshipStatus.HISTORICO);

        Should.Throw<InvalidStateTransitionException>(() => agg.UpdateLimite(DefaultLimite()));
    }

    [Fact]
    public void UpdateLimite_NullLimite_ThrowsArgumentNullException()
    {
        var agg = FundoCedenteAggregate.Create(FundoId, CedenteId, DefaultLimite(), DefaultJanela());

        Should.Throw<ArgumentNullException>(() => agg.UpdateLimite(null!));
    }

    // === State machine (D-22) ===

    [Fact]
    public void TransitionTo_AtivoToInativo_Succeeds()
    {
        var agg = FundoCedenteAggregate.Create(FundoId, CedenteId, DefaultLimite(), DefaultJanela());

        agg.TransitionTo(RelationshipStatus.INATIVO);

        agg.Status.ShouldBe(RelationshipStatus.INATIVO);
    }

    [Fact]
    public void TransitionTo_InativoToAtivo_Succeeds()
    {
        var agg = FundoCedenteAggregate.Create(FundoId, CedenteId, DefaultLimite(), DefaultJanela());
        agg.TransitionTo(RelationshipStatus.INATIVO);

        agg.TransitionTo(RelationshipStatus.ATIVO);

        agg.Status.ShouldBe(RelationshipStatus.ATIVO);
    }

    [Fact]
    public void TransitionTo_AtivoToHistorico_Succeeds()
    {
        var agg = FundoCedenteAggregate.Create(FundoId, CedenteId, DefaultLimite(), DefaultJanela());

        agg.TransitionTo(RelationshipStatus.HISTORICO);

        agg.Status.ShouldBe(RelationshipStatus.HISTORICO);
    }

    [Fact]
    public void TransitionTo_InativoToHistorico_Succeeds()
    {
        var agg = FundoCedenteAggregate.Create(FundoId, CedenteId, DefaultLimite(), DefaultJanela());
        agg.TransitionTo(RelationshipStatus.INATIVO);

        agg.TransitionTo(RelationshipStatus.HISTORICO);

        agg.Status.ShouldBe(RelationshipStatus.HISTORICO);
    }

    [Fact]
    public void TransitionTo_HistoricoToAtivo_ThrowsInvalidStateTransition()
    {
        var agg = FundoCedenteAggregate.Create(FundoId, CedenteId, DefaultLimite(), DefaultJanela());
        agg.TransitionTo(RelationshipStatus.HISTORICO);

        var ex = Should.Throw<InvalidStateTransitionException>(() =>
            agg.TransitionTo(RelationshipStatus.ATIVO));
        ex.Message.ShouldContain("HISTORICO");
    }

    [Fact]
    public void TransitionTo_HistoricoToInativo_ThrowsInvalidStateTransition()
    {
        var agg = FundoCedenteAggregate.Create(FundoId, CedenteId, DefaultLimite(), DefaultJanela());
        agg.TransitionTo(RelationshipStatus.HISTORICO);

        Should.Throw<InvalidStateTransitionException>(() =>
            agg.TransitionTo(RelationshipStatus.INATIVO));
    }

    [Fact]
    public void TransitionTo_AtivoToAtivo_ThrowsInvalidStateTransition()
    {
        var agg = FundoCedenteAggregate.Create(FundoId, CedenteId, DefaultLimite(), DefaultJanela());

        Should.Throw<InvalidStateTransitionException>(() =>
            agg.TransitionTo(RelationshipStatus.ATIVO));
    }

    [Fact]
    public void CanTransitionTo_ValidTransitions_ReturnsTrue()
    {
        var agg = FundoCedenteAggregate.Create(FundoId, CedenteId, DefaultLimite(), DefaultJanela());

        agg.CanTransitionTo(RelationshipStatus.INATIVO).ShouldBeTrue();
        agg.CanTransitionTo(RelationshipStatus.HISTORICO).ShouldBeTrue();
        agg.CanTransitionTo(RelationshipStatus.ATIVO).ShouldBeFalse();
    }

    [Fact]
    public void CanTransitionTo_FromHistorico_ReturnsFalse()
    {
        var agg = FundoCedenteAggregate.Create(FundoId, CedenteId, DefaultLimite(), DefaultJanela());
        agg.TransitionTo(RelationshipStatus.HISTORICO);

        agg.CanTransitionTo(RelationshipStatus.ATIVO).ShouldBeFalse();
        agg.CanTransitionTo(RelationshipStatus.INATIVO).ShouldBeFalse();
        agg.CanTransitionTo(RelationshipStatus.HISTORICO).ShouldBeFalse();
    }
}
