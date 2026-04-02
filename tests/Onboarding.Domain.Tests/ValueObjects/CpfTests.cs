using Onboarding.Domain.ValueObjects;
using Shouldly;

namespace Onboarding.Domain.Tests.ValueObjects;

public class CpfTests
{
    [Theory]
    [InlineData("529.982.247-25")]
    [InlineData("52998224725")]
    public void Create_ValidCpf_ReturnsInstance(string raw)
    {
        var cpf = Cpf.Create(raw);

        cpf.ShouldNotBeNull();
        cpf.Value.ShouldBe("52998224725");
    }

    [Theory]
    [InlineData("000.000.000-00")]
    [InlineData("111.111.111-11")]
    [InlineData("529.982.247-26")]
    [InlineData("123")]
    [InlineData("")]
    [InlineData(null)]
    public void Create_InvalidCpf_Throws(string? raw)
    {
        Should.Throw<ArgumentException>(() => Cpf.Create(raw!));
    }

    [Fact]
    public void TwoCpfsWithSameValue_AreEqual()
    {
        var cpf1 = Cpf.Create("529.982.247-25");
        var cpf2 = Cpf.Create("529.982.247-25");

        cpf1.ShouldBe(cpf2);
        (cpf1 == cpf2).ShouldBeTrue();
    }
}
