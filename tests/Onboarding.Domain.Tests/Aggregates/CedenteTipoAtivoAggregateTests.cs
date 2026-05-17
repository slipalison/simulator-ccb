using Onboarding.Domain.Aggregates.CedenteTipoAtivoAggregate;
using Onboarding.Domain.Aggregates.FundoCedenteAggregate;
using Onboarding.Domain.Exceptions;
using Onboarding.Domain.ValueObjects;
using Shouldly;

namespace Onboarding.Domain.Tests.Aggregates;

/// <summary>
/// Tests for CedenteTipoAtivoAggregate — symmetric shape with FundoCedenteAggregate (D-21).
/// </summary>
public class CedenteTipoAtivoAggregateTests
{
    private static readonly Guid CedenteId = Guid.NewGuid();
    private static readonly Guid TipoAtivoId = Guid.NewGuid();

    private static LimiteExposicao DefaultLimite() => LimiteExposicao.Create(null, 50000m);
    private static JanelaVigencia DefaultJanela() => JanelaVigencia.Create(DateTimeOffset.UtcNow);

    [Fact]
    public void Create_WithValidArgs_ReturnsAtivoAggregate()
    {
        var agg = CedenteTipoAtivoAggregate.Create(CedenteId, TipoAtivoId, DefaultLimite(), DefaultJanela());

        agg.Id.ShouldNotBe(Guid.Empty);
        agg.CedenteId.ShouldBe(CedenteId);
        agg.TipoAtivoId.ShouldBe(TipoAtivoId);
        agg.Status.ShouldBe(RelationshipStatus.ATIVO);
    }

    [Fact]
    public void Create_EmptyCedenteId_ThrowsArgumentException()
    {
        var ex = Should.Throw<ArgumentException>(() =>
            CedenteTipoAtivoAggregate.Create(Guid.Empty, TipoAtivoId, DefaultLimite(), DefaultJanela()));
        ex.ParamName.ShouldBe("cedenteId");
    }

    [Fact]
    public void Create_EmptyTipoAtivoId_ThrowsArgumentException()
    {
        var ex = Should.Throw<ArgumentException>(() =>
            CedenteTipoAtivoAggregate.Create(CedenteId, Guid.Empty, DefaultLimite(), DefaultJanela()));
        ex.ParamName.ShouldBe("tipoAtivoId");
    }

    [Fact]
    public void ActivateGuard_ExistingActive_ThrowsDuplicateActiveAssociation()
    {
        var ex = Should.Throw<DuplicateActiveAssociationException>(() =>
            CedenteTipoAtivoAggregate.ActivateGuard(existsActiveForPair: true));
        ex.AssociationType.ShouldBe("CedenteTipoAtivo");
    }

    [Fact]
    public void ActivateGuard_NoExisting_DoesNotThrow()
    {
        Should.NotThrow(() => CedenteTipoAtivoAggregate.ActivateGuard(existsActiveForPair: false));
    }

    [Fact]
    public void UpdateLimite_WhenAtivo_UpdatesSuccessfully()
    {
        var agg = CedenteTipoAtivoAggregate.Create(CedenteId, TipoAtivoId, DefaultLimite(), DefaultJanela());
        var newLimite = LimiteExposicao.Create(20m, 75000m);

        agg.UpdateLimite(newLimite);

        agg.Limite.Percentual.ShouldBe(20m);
        agg.Limite.Valor.ShouldBe(75000m);
    }

    [Fact]
    public void UpdateLimite_WhenHistorico_ThrowsInvalidStateTransition()
    {
        var agg = CedenteTipoAtivoAggregate.Create(CedenteId, TipoAtivoId, DefaultLimite(), DefaultJanela());
        agg.TransitionTo(RelationshipStatus.HISTORICO);

        Should.Throw<InvalidStateTransitionException>(() => agg.UpdateLimite(DefaultLimite()));
    }

    [Fact]
    public void TransitionTo_AtivoToInativo_Succeeds()
    {
        var agg = CedenteTipoAtivoAggregate.Create(CedenteId, TipoAtivoId, DefaultLimite(), DefaultJanela());

        agg.TransitionTo(RelationshipStatus.INATIVO);

        agg.Status.ShouldBe(RelationshipStatus.INATIVO);
    }

    [Fact]
    public void TransitionTo_InativoToAtivo_Succeeds()
    {
        var agg = CedenteTipoAtivoAggregate.Create(CedenteId, TipoAtivoId, DefaultLimite(), DefaultJanela());
        agg.TransitionTo(RelationshipStatus.INATIVO);

        agg.TransitionTo(RelationshipStatus.ATIVO);

        agg.Status.ShouldBe(RelationshipStatus.ATIVO);
    }

    [Fact]
    public void TransitionTo_HistoricoIsTerminal_ThrowsInvalidStateTransition()
    {
        var agg = CedenteTipoAtivoAggregate.Create(CedenteId, TipoAtivoId, DefaultLimite(), DefaultJanela());
        agg.TransitionTo(RelationshipStatus.HISTORICO);

        Should.Throw<InvalidStateTransitionException>(() =>
            agg.TransitionTo(RelationshipStatus.ATIVO));
    }

    [Fact]
    public void CanTransitionTo_FromHistorico_AllReturnFalse()
    {
        var agg = CedenteTipoAtivoAggregate.Create(CedenteId, TipoAtivoId, DefaultLimite(), DefaultJanela());
        agg.TransitionTo(RelationshipStatus.HISTORICO);

        agg.CanTransitionTo(RelationshipStatus.ATIVO).ShouldBeFalse();
        agg.CanTransitionTo(RelationshipStatus.INATIVO).ShouldBeFalse();
        agg.CanTransitionTo(RelationshipStatus.HISTORICO).ShouldBeFalse();
    }
}
