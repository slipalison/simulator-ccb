using Onboarding.Domain.ValueObjects;
using Shouldly;

namespace Onboarding.Domain.Tests.ValueObjects;

public class EmailTests
{
    [Theory]
    [InlineData("user@example.com")]
    [InlineData("a@b.co")]
    public void Create_ValidEmail_ReturnsInstance(string raw)
    {
        var email = Email.Create(raw);

        email.ShouldNotBeNull();
        email.Value.ShouldBe(raw.ToLowerInvariant());
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("notanemail")]
    [InlineData("@domain.com")]
    public void Create_InvalidEmail_Throws(string? raw)
    {
        Should.Throw<ArgumentException>(() => Email.Create(raw!));
    }
}
