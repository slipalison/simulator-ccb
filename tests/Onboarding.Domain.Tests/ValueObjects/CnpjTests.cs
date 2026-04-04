using Onboarding.Domain.ValueObjects;
using Shouldly;

namespace Onboarding.Domain.Tests.ValueObjects;

public class CnpjTests
{
    [Theory]
    [InlineData("11.222.333/0001-81")]
    [InlineData("11222333000181")]
    public void Create_ValidNumericCnpj_ReturnsInstance(string raw)
    {
        var cnpj = Cnpj.Create(raw);

        cnpj.ShouldNotBeNull();
        cnpj.Value.ShouldBe("11222333000181");
    }

    // NOTE: The alphanumeric CNPJ format (letters A-Z in positions 1-8) takes effect
    // in July 2026. The ASCII-48 algorithm is backward-compatible with purely numeric
    // CNPJs, so we use a numeric CNPJ here to verify the algorithm path.
    // TODO: Add a true alphanumeric CNPJ test case once the July 2026 format is
    // officially published by Receita Federal with verified sample values.
    [Theory]
    [InlineData("11.222.333/0001-81")]
    [InlineData("XJ.15M.ED4/63DH-74")]
    public void Create_ValidAlphanumericCnpj_ReturnsInstance(string raw)
    {
        // Using a numeric CNPJ to test the ASCII-48 code path (numeric chars
        // pass through the same algorithm as alphanumeric ones).
        var cnpj = Cnpj.Create(raw);

        cnpj.ShouldNotBeNull();
    }

    [Theory]
    [InlineData("00.000.000/0000-00")]
    [InlineData("11.222.333/0001-82")]
    [InlineData("")]
    [InlineData(null)]
    public void Create_InvalidCnpj_Throws(string? raw)
    {
        Should.Throw<ArgumentException>(() => Cnpj.Create(raw!));
    }

    [Fact]
    public void TwoCnpjsWithSameValue_AreEqual()
    {
        var cnpj1 = Cnpj.Create("11.222.333/0001-81");
        var cnpj2 = Cnpj.Create("11222333000181");

        cnpj1.ShouldBe(cnpj2);
        (cnpj1 == cnpj2).ShouldBeTrue();
    }
}
