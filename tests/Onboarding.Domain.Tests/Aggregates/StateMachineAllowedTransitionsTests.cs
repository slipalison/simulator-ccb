using Onboarding.Domain.Aggregates.CedenteTipoAtivoAggregate;
using Onboarding.Domain.Aggregates.FundoCedenteAggregate;
using Onboarding.Domain.Aggregates.FundoAggregate;
using Onboarding.Domain.Aggregates.FundoTipoAtivoAggregate;
using Onboarding.Domain.ValueObjects;
using Shouldly;

namespace Onboarding.Domain.Tests.Aggregates;

/// <summary>
/// Parity tests: GetAllowedNextStates() output must match what TransitionTo() actually accepts (D-25).
/// For each aggregate + each source status, verify that:
///   - every string returned by GetAllowedNextStates() is a status that TransitionTo() does NOT throw for
///   - every status NOT in GetAllowedNextStates() causes TransitionTo() to throw
/// </summary>
public class StateMachineAllowedTransitionsTests
{
    // =========================================================================
    // Fundo — 5 statuses
    // =========================================================================

    [Theory]
    [InlineData(FundoStatus.RASCUNHO, new[] { "ATIVO" })]
    [InlineData(FundoStatus.ATIVO, new[] { "SUSPENSO", "EM_LIQUIDACAO" })]
    [InlineData(FundoStatus.SUSPENSO, new[] { "ATIVO" })]
    [InlineData(FundoStatus.EM_LIQUIDACAO, new[] { "ENCERRADO" })]
    [InlineData(FundoStatus.ENCERRADO, new string[0])]
    public void Fundo_GetAllowedNextStates_returnsExpectedSet(
        FundoStatus currentStatus, string[] expectedStates)
    {
        var fundo = MakeFundo(currentStatus);

        var allowed = fundo.GetAllowedNextStates();

        allowed.ShouldBe(expectedStates.OrderBy(s => s), ignoreOrder: true);
    }

    [Theory]
    [InlineData(FundoStatus.RASCUNHO)]
    [InlineData(FundoStatus.ATIVO)]
    [InlineData(FundoStatus.SUSPENSO)]
    [InlineData(FundoStatus.EM_LIQUIDACAO)]
    [InlineData(FundoStatus.ENCERRADO)]
    public void Fundo_GetAllowedNextStates_parityWithTransitionTo(FundoStatus currentStatus)
    {
        var allowed = MakeFundo(currentStatus).GetAllowedNextStates().ToHashSet();
        var all = (FundoStatus[])Enum.GetValues(typeof(FundoStatus));

        foreach (var target in all)
        {
            var targetStr = target.ToString();
            if (allowed.Contains(targetStr))
            {
                // Transition must succeed
                var fundo = MakeFundo(currentStatus);
                Should.NotThrow(() => fundo.TransitionTo(target));
            }
            else
            {
                // Transition must throw
                var fundo = MakeFundo(currentStatus);
                Should.Throw<Exception>(() => fundo.TransitionTo(target));
            }
        }
    }

    // =========================================================================
    // FundoCedenteAggregate (RelationshipStatus)
    // =========================================================================

    [Theory]
    [InlineData(RelationshipStatus.ATIVO, new[] { "INATIVO", "HISTORICO" })]
    [InlineData(RelationshipStatus.INATIVO, new[] { "ATIVO", "HISTORICO" })]
    [InlineData(RelationshipStatus.HISTORICO, new string[0])]
    public void FundoCedente_GetAllowedNextStates_returnsExpectedSet(
        RelationshipStatus currentStatus, string[] expectedStates)
    {
        var assoc = MakeFundoCedente(currentStatus);

        var allowed = assoc.GetAllowedNextStates();

        allowed.ShouldBe(expectedStates.OrderBy(s => s), ignoreOrder: true);
    }

    [Theory]
    [InlineData(RelationshipStatus.ATIVO)]
    [InlineData(RelationshipStatus.INATIVO)]
    [InlineData(RelationshipStatus.HISTORICO)]
    public void FundoCedente_GetAllowedNextStates_parityWithTransitionTo(RelationshipStatus currentStatus)
    {
        var allowed = MakeFundoCedente(currentStatus).GetAllowedNextStates().ToHashSet();
        var all = (RelationshipStatus[])Enum.GetValues(typeof(RelationshipStatus));

        foreach (var target in all)
        {
            var targetStr = target.ToString();
            if (allowed.Contains(targetStr))
            {
                var assoc = MakeFundoCedente(currentStatus);
                Should.NotThrow(() => assoc.TransitionTo(target));
            }
            else
            {
                var assoc = MakeFundoCedente(currentStatus);
                Should.Throw<Exception>(() => assoc.TransitionTo(target));
            }
        }
    }

    // =========================================================================
    // FundoTipoAtivoAggregate (RelationshipStatus)
    // =========================================================================

    [Theory]
    [InlineData(RelationshipStatus.ATIVO, new[] { "INATIVO", "HISTORICO" })]
    [InlineData(RelationshipStatus.INATIVO, new[] { "ATIVO", "HISTORICO" })]
    [InlineData(RelationshipStatus.HISTORICO, new string[0])]
    public void FundoTipoAtivo_GetAllowedNextStates_returnsExpectedSet(
        RelationshipStatus currentStatus, string[] expectedStates)
    {
        var assoc = MakeFundoTipoAtivo(currentStatus);

        var allowed = assoc.GetAllowedNextStates();

        allowed.ShouldBe(expectedStates.OrderBy(s => s), ignoreOrder: true);
    }

    [Theory]
    [InlineData(RelationshipStatus.ATIVO)]
    [InlineData(RelationshipStatus.INATIVO)]
    [InlineData(RelationshipStatus.HISTORICO)]
    public void FundoTipoAtivo_GetAllowedNextStates_parityWithTransitionTo(RelationshipStatus currentStatus)
    {
        var allowed = MakeFundoTipoAtivo(currentStatus).GetAllowedNextStates().ToHashSet();
        var all = (RelationshipStatus[])Enum.GetValues(typeof(RelationshipStatus));

        foreach (var target in all)
        {
            var targetStr = target.ToString();
            if (allowed.Contains(targetStr))
            {
                var assoc = MakeFundoTipoAtivo(currentStatus);
                Should.NotThrow(() => assoc.TransitionTo(target));
            }
            else
            {
                var assoc = MakeFundoTipoAtivo(currentStatus);
                Should.Throw<Exception>(() => assoc.TransitionTo(target));
            }
        }
    }

    // =========================================================================
    // CedenteTipoAtivoAggregate (RelationshipStatus)
    // =========================================================================

    [Theory]
    [InlineData(RelationshipStatus.ATIVO, new[] { "INATIVO", "HISTORICO" })]
    [InlineData(RelationshipStatus.INATIVO, new[] { "ATIVO", "HISTORICO" })]
    [InlineData(RelationshipStatus.HISTORICO, new string[0])]
    public void CedenteTipoAtivo_GetAllowedNextStates_returnsExpectedSet(
        RelationshipStatus currentStatus, string[] expectedStates)
    {
        var assoc = MakeCedenteTipoAtivo(currentStatus);

        var allowed = assoc.GetAllowedNextStates();

        allowed.ShouldBe(expectedStates.OrderBy(s => s), ignoreOrder: true);
    }

    [Theory]
    [InlineData(RelationshipStatus.ATIVO)]
    [InlineData(RelationshipStatus.INATIVO)]
    [InlineData(RelationshipStatus.HISTORICO)]
    public void CedenteTipoAtivo_GetAllowedNextStates_parityWithTransitionTo(RelationshipStatus currentStatus)
    {
        var allowed = MakeCedenteTipoAtivo(currentStatus).GetAllowedNextStates().ToHashSet();
        var all = (RelationshipStatus[])Enum.GetValues(typeof(RelationshipStatus));

        foreach (var target in all)
        {
            var targetStr = target.ToString();
            if (allowed.Contains(targetStr))
            {
                var assoc = MakeCedenteTipoAtivo(currentStatus);
                Should.NotThrow(() => assoc.TransitionTo(target));
            }
            else
            {
                var assoc = MakeCedenteTipoAtivo(currentStatus);
                Should.Throw<Exception>(() => assoc.TransitionTo(target));
            }
        }
    }

    // =========================================================================
    // Helpers — build aggregates with a specific status via valid factory path then transitions
    // =========================================================================

    private static Domain.Aggregates.FundoAggregate.Fundo MakeFundo(FundoStatus status)
    {
        var fundo = Domain.Aggregates.FundoAggregate.Fundo.Register(
            "Fundo Teste", "11222333000181", Guid.NewGuid(),
            Guid.NewGuid(), Guid.NewGuid(), TipoFundo.Multimercado);

        // fundo starts at RASCUNHO; navigate to desired status via valid transitions
        ApplyFundoStatus(fundo, status);
        return fundo;
    }

    private static void ApplyFundoStatus(Domain.Aggregates.FundoAggregate.Fundo fundo, FundoStatus target)
    {
        if (fundo.Status == target) return;

        // RASCUNHO → ATIVO → SUSPENSO / EM_LIQUIDACAO → ENCERRADO
        switch (target)
        {
            case FundoStatus.ATIVO:
                fundo.TransitionTo(FundoStatus.ATIVO);
                break;
            case FundoStatus.SUSPENSO:
                fundo.TransitionTo(FundoStatus.ATIVO);
                fundo.TransitionTo(FundoStatus.SUSPENSO);
                break;
            case FundoStatus.EM_LIQUIDACAO:
                fundo.TransitionTo(FundoStatus.ATIVO);
                fundo.TransitionTo(FundoStatus.EM_LIQUIDACAO);
                break;
            case FundoStatus.ENCERRADO:
                fundo.TransitionTo(FundoStatus.ATIVO);
                fundo.TransitionTo(FundoStatus.EM_LIQUIDACAO);
                fundo.TransitionTo(FundoStatus.ENCERRADO);
                break;
                // RASCUNHO is the initial state — no action needed
        }
    }

    private static LimiteExposicao MakeLimite() =>
        LimiteExposicao.FromPercentual(50m);

    private static JanelaVigencia MakeJanela() =>
        JanelaVigencia.Create(DateTimeOffset.UtcNow, null);

    private static FundoCedenteAggregate MakeFundoCedente(RelationshipStatus status)
    {
        var assoc = FundoCedenteAggregate.Create(
            Guid.NewGuid(), Guid.NewGuid(), MakeLimite(), MakeJanela());
        // starts ATIVO
        ApplyRelStatus(assoc.TransitionTo, assoc.Status, status);
        return assoc;
    }

    private static FundoTipoAtivoAggregate MakeFundoTipoAtivo(RelationshipStatus status)
    {
        var assoc = FundoTipoAtivoAggregate.Create(
            Guid.NewGuid(), Guid.NewGuid(), MakeLimite(), MakeJanela());
        ApplyRelStatus(assoc.TransitionTo, assoc.Status, status);
        return assoc;
    }

    private static CedenteTipoAtivoAggregate MakeCedenteTipoAtivo(RelationshipStatus status)
    {
        var assoc = CedenteTipoAtivoAggregate.Create(
            Guid.NewGuid(), Guid.NewGuid(), MakeLimite(), MakeJanela());
        ApplyRelStatus(assoc.TransitionTo, assoc.Status, status);
        return assoc;
    }

    private static void ApplyRelStatus(
        Action<RelationshipStatus> transitionTo,
        RelationshipStatus current,
        RelationshipStatus target)
    {
        if (current == target) return; // already ATIVO

        switch (target)
        {
            case RelationshipStatus.INATIVO:
                transitionTo(RelationshipStatus.INATIVO);
                break;
            case RelationshipStatus.HISTORICO:
                transitionTo(RelationshipStatus.HISTORICO);
                break;
                // ATIVO is the initial state
        }
    }
}
