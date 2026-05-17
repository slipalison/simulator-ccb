using Onboarding.Domain.ValueObjects;
using Shouldly;

namespace Onboarding.Domain.Tests.ValueObjects;

/// <summary>
/// Tests for JanelaVigencia value object (D-20).
/// Half-open [DataInicio, DataFim) with DataFim nullable.
/// </summary>
public class JanelaVigenciaTests
{
    [Fact]
    public void Create_WithOnlyDataInicio_IsInfinite()
    {
        var inicio = DateTimeOffset.UtcNow;

        var janela = JanelaVigencia.Create(inicio);

        janela.DataInicio.ShouldBe(inicio);
        janela.DataFim.ShouldBeNull();
        janela.IsInfinite.ShouldBeTrue();
    }

    [Fact]
    public void Create_WithDataFimAfterDataInicio_Succeeds()
    {
        var inicio = DateTimeOffset.UtcNow;
        var fim = inicio.AddYears(1);

        var janela = JanelaVigencia.Create(inicio, fim);

        janela.DataFim.ShouldBe(fim);
        janela.IsInfinite.ShouldBeFalse();
    }

    [Fact]
    public void Create_DataFimEqualToDataInicio_ThrowsArgumentException()
    {
        var inicio = DateTimeOffset.UtcNow;

        var ex = Should.Throw<ArgumentException>(() =>
            JanelaVigencia.Create(inicio, inicio));
        ex.ParamName.ShouldBe("dataFim");
    }

    [Fact]
    public void Create_DataFimBeforeDataInicio_ThrowsArgumentException()
    {
        var inicio = DateTimeOffset.UtcNow;
        var fim = inicio.AddDays(-1);

        var ex = Should.Throw<ArgumentException>(() =>
            JanelaVigencia.Create(inicio, fim));
        ex.ParamName.ShouldBe("dataFim");
    }

    [Fact]
    public void IsActiveAt_WhenInsideWindow_ReturnsTrue()
    {
        var inicio = DateTimeOffset.UtcNow.AddDays(-1);
        var fim = DateTimeOffset.UtcNow.AddDays(1);
        var janela = JanelaVigencia.Create(inicio, fim);

        janela.IsActiveAt(DateTimeOffset.UtcNow).ShouldBeTrue();
    }

    [Fact]
    public void IsActiveAt_WhenBeforeDataInicio_ReturnsFalse()
    {
        var inicio = DateTimeOffset.UtcNow.AddDays(1);
        var janela = JanelaVigencia.Create(inicio);

        janela.IsActiveAt(DateTimeOffset.UtcNow).ShouldBeFalse();
    }

    [Fact]
    public void IsActiveAt_WhenAtDataFim_ReturnsFalseHalfOpen()
    {
        var inicio = DateTimeOffset.UtcNow.AddDays(-1);
        var fim = DateTimeOffset.UtcNow;
        var janela = JanelaVigencia.Create(inicio, fim);

        // Half-open: DataFim itself is excluded
        janela.IsActiveAt(fim).ShouldBeFalse();
    }

    [Fact]
    public void IsActiveAt_InfiniteWindow_AfterStart_ReturnsTrue()
    {
        var inicio = DateTimeOffset.UtcNow.AddDays(-10);
        var janela = JanelaVigencia.Create(inicio);

        janela.IsActiveAt(DateTimeOffset.UtcNow).ShouldBeTrue();
        janela.IsActiveAt(DateTimeOffset.UtcNow.AddYears(100)).ShouldBeTrue();
    }

    [Fact]
    public void ToString_InfiniteWindow_ContainsInfinitySymbol()
    {
        var janela = JanelaVigencia.Create(DateTimeOffset.UtcNow);
        janela.ToString().ShouldContain("∞");
    }

    [Fact]
    public void ToString_FiniteWindow_ContainsBothDates()
    {
        var inicio = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var fim = new DateTimeOffset(2027, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var janela = JanelaVigencia.Create(inicio, fim);

        var str = janela.ToString();
        str.ShouldContain("[");
        str.ShouldContain(")");
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        var inicio = DateTimeOffset.UtcNow;
        var fim = inicio.AddYears(1);

        var j1 = JanelaVigencia.Create(inicio, fim);
        var j2 = JanelaVigencia.Create(inicio, fim);

        j1.ShouldBe(j2);
    }
}
