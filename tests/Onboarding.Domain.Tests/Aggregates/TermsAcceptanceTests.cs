using Onboarding.Domain.Aggregates.CompanyAggregate;
using Shouldly;

namespace Onboarding.Domain.Tests.Aggregates;

public class TermsAcceptanceTests
{
    [Fact]
    public void Create_validInput_setsAcceptedAtToUtcNow()
    {
        var before = DateTimeOffset.UtcNow;
        var terms = TermsAcceptance.Create("1.0", "192.168.1.1");

        terms.AcceptedAt.ShouldBeGreaterThanOrEqualTo(before);
    }

    [Fact]
    public void Create_validInput_setsTermsVersion()
    {
        var terms = TermsAcceptance.Create("1.0", "192.168.1.1");

        terms.TermsVersion.ShouldBe("1.0");
    }

    [Fact]
    public void Create_validInput_setsIpAddress()
    {
        var terms = TermsAcceptance.Create("1.0", "192.168.1.1");

        terms.IpAddress.ShouldBe("192.168.1.1");
    }

    [Fact]
    public void Create_emptyTermsVersion_throwsArgumentException()
    {
        Should.Throw<ArgumentException>(() =>
            TermsAcceptance.Create("", "192.168.1.1"));
    }

    [Fact]
    public void Create_nullTermsVersion_throwsArgumentException()
    {
        Should.Throw<ArgumentException>(() =>
            TermsAcceptance.Create(null!, "192.168.1.1"));
    }

    [Fact]
    public void Create_emptyIpAddress_throwsArgumentException()
    {
        Should.Throw<ArgumentException>(() =>
            TermsAcceptance.Create("1.0", ""));
    }

    [Fact]
    public void Create_nullIpAddress_throwsArgumentException()
    {
        Should.Throw<ArgumentException>(() =>
            TermsAcceptance.Create("1.0", null!));
    }

    [Fact]
    public void CurrentVersion_equals_1_0()
    {
        TermsAcceptance.CurrentVersion.ShouldBe("1.0");
    }
}