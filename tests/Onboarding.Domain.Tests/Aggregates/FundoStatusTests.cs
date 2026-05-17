using Onboarding.Domain.Aggregates.FundoAggregate;
using Shouldly;

namespace Onboarding.Domain.Tests.Aggregates;

public class FundoStatusTests
{
    // Valid transitions per D-02
    [Fact]
    public void CanTransitionTo_RascunhoToAtivo_ReturnsTrue()
    {
        FundoStatusValidator.CanTransitionTo(FundoStatus.RASCUNHO, FundoStatus.ATIVO).ShouldBeTrue();
    }

    [Fact]
    public void CanTransitionTo_AtivoToSuspenso_ReturnsTrue()
    {
        FundoStatusValidator.CanTransitionTo(FundoStatus.ATIVO, FundoStatus.SUSPENSO).ShouldBeTrue();
    }

    [Fact]
    public void CanTransitionTo_SuspensoToAtivo_ReturnsTrue()
    {
        FundoStatusValidator.CanTransitionTo(FundoStatus.SUSPENSO, FundoStatus.ATIVO).ShouldBeTrue();
    }

    [Fact]
    public void CanTransitionTo_AtivoToEmLiquidacao_ReturnsTrue()
    {
        FundoStatusValidator.CanTransitionTo(FundoStatus.ATIVO, FundoStatus.EM_LIQUIDACAO).ShouldBeTrue();
    }

    [Fact]
    public void CanTransitionTo_EmLiquidacaoToEncerrado_ReturnsTrue()
    {
        FundoStatusValidator.CanTransitionTo(FundoStatus.EM_LIQUIDACAO, FundoStatus.ENCERRADO).ShouldBeTrue();
    }

    // Invalid transitions
    [Fact]
    public void CanTransitionTo_EmLiquidacaoToAtivo_ReturnsFalse()
    {
        // D-02: EM_LIQUIDACAO can only go forward to ENCERRADO
        FundoStatusValidator.CanTransitionTo(FundoStatus.EM_LIQUIDACAO, FundoStatus.ATIVO).ShouldBeFalse();
    }

    [Fact]
    public void CanTransitionTo_EncerradoToAtivo_ReturnsFalse()
    {
        FundoStatusValidator.CanTransitionTo(FundoStatus.ENCERRADO, FundoStatus.ATIVO).ShouldBeFalse();
    }

    [Fact]
    public void CanTransitionTo_RascunhoToSuspenso_ReturnsFalse()
    {
        FundoStatusValidator.CanTransitionTo(FundoStatus.RASCUNHO, FundoStatus.SUSPENSO).ShouldBeFalse();
    }

    [Fact]
    public void CanTransitionTo_RascunhoToEmLiquidacao_ReturnsFalse()
    {
        FundoStatusValidator.CanTransitionTo(FundoStatus.RASCUNHO, FundoStatus.EM_LIQUIDACAO).ShouldBeFalse();
    }

    [Fact]
    public void CanTransitionTo_RascunhoToEncerrado_ReturnsFalse()
    {
        FundoStatusValidator.CanTransitionTo(FundoStatus.RASCUNHO, FundoStatus.ENCERRADO).ShouldBeFalse();
    }

    [Fact]
    public void CanTransitionTo_SameStatus_ReturnsFalse()
    {
        FundoStatusValidator.CanTransitionTo(FundoStatus.ATIVO, FundoStatus.ATIVO).ShouldBeFalse();
    }

    [Fact]
    public void CanTransitionTo_SuspensoToEmLiquidacao_ReturnsFalse()
    {
        FundoStatusValidator.CanTransitionTo(FundoStatus.SUSPENSO, FundoStatus.EM_LIQUIDACAO).ShouldBeFalse();
    }

    [Fact]
    public void CanTransitionTo_EncerradoToAny_ReturnsFalse()
    {
        FundoStatusValidator.CanTransitionTo(FundoStatus.ENCERRADO, FundoStatus.RASCUNHO).ShouldBeFalse();
        FundoStatusValidator.CanTransitionTo(FundoStatus.ENCERRADO, FundoStatus.SUSPENSO).ShouldBeFalse();
        FundoStatusValidator.CanTransitionTo(FundoStatus.ENCERRADO, FundoStatus.EM_LIQUIDACAO).ShouldBeFalse();
    }

    [Fact]
    public void FundoStatus_HasExactlyFiveValues()
    {
        var values = Enum.GetValues<FundoStatus>();
        values.Length.ShouldBe(5);
    }
}