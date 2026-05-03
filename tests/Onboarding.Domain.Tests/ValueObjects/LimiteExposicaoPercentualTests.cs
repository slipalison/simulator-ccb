using Onboarding.Domain.ValueObjects;
using Shouldly;

namespace Onboarding.Domain.Tests.ValueObjects;

public class LimiteExposicaoPercentualTests
{
    [Fact]
    public void Create_SentinelMinusOne_ReturnsUnlimited()
    {
        var limite = LimiteExposicaoPercentual.Create(-1);

        limite.Value.ShouldBe(-1m);
        limite.IsUnlimited.ShouldBeTrue();
    }

    [Fact]
    public void Create_ZeroPercent_Succeeds()
    {
        var limite = LimiteExposicaoPercentual.Create(0);

        limite.Value.ShouldBe(0m);
        limite.IsUnlimited.ShouldBeFalse();
    }

    [Fact]
    public void Create_FiftyPercent_Succeeds()
    {
        var limite = LimiteExposicaoPercentual.Create(50);

        limite.Value.ShouldBe(50m);
        limite.IsUnlimited.ShouldBeFalse();
    }

    [Fact]
    public void Create_HundredPercent_Succeeds()
    {
        var limite = LimiteExposicaoPercentual.Create(100);

        limite.Value.ShouldBe(100m);
        limite.IsUnlimited.ShouldBeFalse();
    }

    [Fact]
    public void Create_BelowMinusOne_Throws()
    {
        Should.Throw<ArgumentException>(() => LimiteExposicaoPercentual.Create(-2));
    }

    [Fact]
    public void Create_AboveHundred_Throws()
    {
        Should.Throw<ArgumentException>(() => LimiteExposicaoPercentual.Create(101));
    }

    [Fact]
    public void Create_FractionalValue_Throws()
    {
        Should.Throw<ArgumentException>(() => LimiteExposicaoPercentual.Create(-0.5m));
    }

    [Fact]
    public void TwoLimiteWithSameValue_AreEqual()
    {
        var a = LimiteExposicaoPercentual.Create(50);
        var b = LimiteExposicaoPercentual.Create(50);

        a.ShouldBe(b);
        (a == b).ShouldBeTrue();
    }

    [Fact]
    public void TwoLimiteDifferentValues_AreNotEqual()
    {
        var a = LimiteExposicaoPercentual.Create(50);
        var b = LimiteExposicaoPercentual.Create(100);

        a.ShouldNotBe(b);
        (a != b).ShouldBeTrue();
    }
}