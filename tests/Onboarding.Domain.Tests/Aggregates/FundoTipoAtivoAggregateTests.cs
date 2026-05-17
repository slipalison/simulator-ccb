using Onboarding.Domain.Aggregates.FundoCedenteAggregate;
using Onboarding.Domain.Aggregates.FundoTipoAtivoAggregate;
using Onboarding.Domain.Exceptions;
using Onboarding.Domain.ValueObjects;
using Shouldly;

namespace Onboarding.Domain.Tests.Aggregates;

/// <summary>
/// Tests for FundoTipoAtivoAggregate — symmetric shape with FundoCedenteAggregate (D-21).
/// </summary>
public class FundoTipoAtivoAggregateTests
{
    private static readonly Guid FundoId = Guid.NewGuid();
    private static readonly Guid TipoAtivoId = Guid.NewGuid();

    private static LimiteExposicao DefaultLimite() => LimiteExposicao.Create(30m, null);
    private static JanelaVigencia DefaultJanela() => JanelaVigencia.Create(DateTimeOffset.UtcNow);

    [Fact]
    public void Create_WithValidArgs_ReturnsAtivoAggregate()
    {
        var agg = FundoTipoAtivoAggregate.Create(FundoId, TipoAtivoId, DefaultLimite(), DefaultJanela());

        agg.Id.ShouldNotBe(Guid.Empty);
        agg.FundoId.ShouldBe(FundoId);
        agg.TipoAtivoId.ShouldBe(TipoAtivoId);
        agg.Status.ShouldBe(RelationshipStatus.ATIVO);
    }

    [Fact]
    public void Create_EmptyFundoId_ThrowsArgumentException()
    {
        var ex = Should.Throw<ArgumentException>(() =>
            FundoTipoAtivoAggregate.Create(Guid.Empty, TipoAtivoId, DefaultLimite(), DefaultJanela()));
        ex.ParamName.ShouldBe("fundoId");
    }

    [Fact]
    public void Create_EmptyTipoAtivoId_ThrowsArgumentException()
    {
        var ex = Should.Throw<ArgumentException>(() =>
            FundoTipoAtivoAggregate.Create(FundoId, Guid.Empty, DefaultLimite(), DefaultJanela()));
        ex.ParamName.ShouldBe("tipoAtivoId");
    }

    [Fact]
    public void ActivateGuard_ExistingActive_ThrowsDuplicateActiveAssociation()
    {
        var ex = Should.Throw<DuplicateActiveAssociationException>(() =>
            FundoTipoAtivoAggregate.ActivateGuard(existsActiveForPair: true));
        ex.AssociationType.ShouldBe("FundoTipoAtivo");
    }

    [Fact]
    public void ActivateGuard_NoExisting_DoesNotThrow()
    {
        Should.NotThrow(() => FundoTipoAtivoAggregate.ActivateGuard(existsActiveForPair: false));
    }

    [Fact]
    public void UpdateLimite_WhenAtivo_UpdatesSuccessfully()
    {
        var agg = FundoTipoAtivoAggregate.Create(FundoId, TipoAtivoId, DefaultLimite(), DefaultJanela());
        var newLimite = LimiteExposicao.Create(null, 200000m);

        agg.UpdateLimite(newLimite);

        agg.Limite.Valor.ShouldBe(200000m);
        agg.Limite.Percentual.ShouldBeNull();
    }

    [Fact]
    public void UpdateLimite_WhenHistorico_ThrowsInvalidStateTransition()
    {
        var agg = FundoTipoAtivoAggregate.Create(FundoId, TipoAtivoId, DefaultLimite(), DefaultJanela());
        agg.TransitionTo(RelationshipStatus.HISTORICO);

        Should.Throw<InvalidStateTransitionException>(() => agg.UpdateLimite(DefaultLimite()));
    }

    [Fact]
    public void TransitionTo_AtivoToInativo_Succeeds()
    {
        var agg = FundoTipoAtivoAggregate.Create(FundoId, TipoAtivoId, DefaultLimite(), DefaultJanela());

        agg.TransitionTo(RelationshipStatus.INATIVO);

        agg.Status.ShouldBe(RelationshipStatus.INATIVO);
    }

    [Fact]
    public void TransitionTo_InativoToAtivo_Succeeds()
    {
        var agg = FundoTipoAtivoAggregate.Create(FundoId, TipoAtivoId, DefaultLimite(), DefaultJanela());
        agg.TransitionTo(RelationshipStatus.INATIVO);

        agg.TransitionTo(RelationshipStatus.ATIVO);

        agg.Status.ShouldBe(RelationshipStatus.ATIVO);
    }

    [Fact]
    public void TransitionTo_HistoricoIsTerminal_ThrowsInvalidStateTransition()
    {
        var agg = FundoTipoAtivoAggregate.Create(FundoId, TipoAtivoId, DefaultLimite(), DefaultJanela());
        agg.TransitionTo(RelationshipStatus.HISTORICO);

        Should.Throw<InvalidStateTransitionException>(() =>
            agg.TransitionTo(RelationshipStatus.ATIVO));
    }

    [Fact]
    public void CanTransitionTo_FromHistorico_AllReturnFalse()
    {
        var agg = FundoTipoAtivoAggregate.Create(FundoId, TipoAtivoId, DefaultLimite(), DefaultJanela());
        agg.TransitionTo(RelationshipStatus.HISTORICO);

        agg.CanTransitionTo(RelationshipStatus.ATIVO).ShouldBeFalse();
        agg.CanTransitionTo(RelationshipStatus.INATIVO).ShouldBeFalse();
        agg.CanTransitionTo(RelationshipStatus.HISTORICO).ShouldBeFalse();
    }
}
