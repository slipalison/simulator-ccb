using Onboarding.Domain.ValueObjects;
using Shouldly;

namespace Onboarding.Domain.Tests.ValueObjects;

/// <summary>
/// Tests for LimiteExposicao value object.
/// At least one of Percentual or Valor required (PLAN T-1 default rule).
/// </summary>
public class LimiteExposicaoTests
{
    [Fact]
    public void Create_WithPercentualOnly_Succeeds()
    {
        var limite = LimiteExposicao.Create(50m, null);

        limite.Percentual.ShouldBe(50m);
        limite.Valor.ShouldBeNull();
        limite.HasBothLimits.ShouldBeFalse();
    }

    [Fact]
    public void Create_WithValorOnly_Succeeds()
    {
        var limite = LimiteExposicao.Create(null, 100000m);

        limite.Percentual.ShouldBeNull();
        limite.Valor.ShouldBe(100000m);
    }

    [Fact]
    public void Create_WithBoth_Succeeds()
    {
        var limite = LimiteExposicao.Create(25m, 50000m);

        limite.Percentual.ShouldBe(25m);
        limite.Valor.ShouldBe(50000m);
        limite.HasBothLimits.ShouldBeTrue();
    }

    [Fact]
    public void Create_BothNull_ThrowsArgumentException()
    {
        var ex = Should.Throw<ArgumentException>(() =>
            LimiteExposicao.Create(null, null));
        ex.Message.ShouldContain("At least one");
    }

    [Fact]
    public void Create_PercentualAbove100_ThrowsArgumentException()
    {
        var ex = Should.Throw<ArgumentException>(() =>
            LimiteExposicao.Create(101m, null));
        ex.ParamName.ShouldBe("percentual");
    }

    [Fact]
    public void Create_PercentualNegative_ThrowsArgumentException()
    {
        var ex = Should.Throw<ArgumentException>(() =>
            LimiteExposicao.Create(-1m, null));
        ex.ParamName.ShouldBe("percentual");
    }

    [Fact]
    public void Create_PercentualZero_Succeeds()
    {
        var limite = LimiteExposicao.Create(0m, null);
        limite.Percentual.ShouldBe(0m);
    }

    [Fact]
    public void Create_PercentualHundred_Succeeds()
    {
        var limite = LimiteExposicao.Create(100m, null);
        limite.Percentual.ShouldBe(100m);
    }

    [Fact]
    public void Create_ValorZero_ThrowsArgumentException()
    {
        var ex = Should.Throw<ArgumentException>(() =>
            LimiteExposicao.Create(null, 0m));
        ex.ParamName.ShouldBe("valor");
    }

    [Fact]
    public void Create_ValorNegative_ThrowsArgumentException()
    {
        var ex = Should.Throw<ArgumentException>(() =>
            LimiteExposicao.Create(null, -100m));
        ex.ParamName.ShouldBe("valor");
    }

    [Fact]
    public void FromPercentual_CreatesWithPercentualOnly()
    {
        var limite = LimiteExposicao.FromPercentual(40m);
        limite.Percentual.ShouldBe(40m);
        limite.Valor.ShouldBeNull();
    }

    [Fact]
    public void FromValor_CreatesWithValorOnly()
    {
        var limite = LimiteExposicao.FromValor(75000m);
        limite.Valor.ShouldBe(75000m);
        limite.Percentual.ShouldBeNull();
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        var l1 = LimiteExposicao.Create(50m, 100000m);
        var l2 = LimiteExposicao.Create(50m, 100000m);
        l1.ShouldBe(l2);
    }

    [Fact]
    public void ToString_PercentualOnly_ContainsPercent()
    {
        var limite = LimiteExposicao.FromPercentual(30m);
        limite.ToString().ShouldContain("%");
    }

    [Fact]
    public void ToString_ValorOnly_ContainsR()
    {
        var limite = LimiteExposicao.FromValor(50000m);
        limite.ToString().ShouldContain("R$");
    }

    [Fact]
    public void ToString_BothLimits_ContainsPercentAndR()
    {
        var limite = LimiteExposicao.Create(20m, 30000m);
        var str = limite.ToString();
        str.ShouldContain("%");
        str.ShouldContain("R$");
    }
}
