using Onboarding.Domain.ValueObjects;
using Shouldly;

namespace Onboarding.Domain.Tests.ValueObjects;

public class PhoneNumberTests
{
    [Theory]
    [InlineData("+55 (11) 99999-8888")]
    [InlineData("11999998888")]
    public void Create_ValidPhone_ReturnsInstance(string raw)
    {
        var phone = PhoneNumber.Create(raw);

        phone.ShouldNotBeNull();
        phone.Value.ShouldBe("11999998888");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("123")]
    public void Create_InvalidPhone_Throws(string? raw)
    {
        Should.Throw<ArgumentException>(() => PhoneNumber.Create(raw!));
    }
}
