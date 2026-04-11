using Onboarding.Domain.Aggregates.PasswordReset;
using Shouldly;
using System.Reflection;

namespace Onboarding.Domain.Tests.Aggregates.PasswordReset;

/// <summary>
/// Unit tests for PasswordResetToken entity.
/// Tests creation, expiration, and usage state.
/// </summary>
public class PasswordResetTokenTests
{
    [Fact]
    public void Create_ValidToken_IsCreatedWithCorrectState()
    {
        // Act
        var token = PasswordResetToken.Create("user@example.com");

        // Assert
        token.Email.ShouldBe("user@example.com");
        token.Token.ShouldNotBeNullOrEmpty();
        token.IsUsed.ShouldBeFalse();
        token.IsExpired.ShouldBeFalse();
        token.IsValid.ShouldBeTrue();
        token.CreatedAt.ShouldNotBe(default);
        token.ExpiresAt.ShouldBeGreaterThan(token.CreatedAt);
    }

    [Fact]
    public void Create_GeneratesUniqueTokens()
    {
        var token1 = PasswordResetToken.Create("user1@example.com");
        var token2 = PasswordResetToken.Create("user2@example.com");

        token1.Token.ShouldNotBe(token2.Token);
        token1.Id.ShouldNotBe(token2.Id);
    }

    [Fact]
    public void Create_SetsExpirationTo15Minutes()
    {
        var beforeCreate = DateTime.UtcNow;
        var token = PasswordResetToken.Create("user@example.com");
        var afterCreate = DateTime.UtcNow;

        var expectedMin = beforeCreate.AddMinutes(15);
        var expectedMax = afterCreate.AddMinutes(15);

        token.ExpiresAt.ShouldBeGreaterThanOrEqualTo(expectedMin);
        token.ExpiresAt.ShouldBeLessThanOrEqualTo(expectedMax);
    }

    [Fact]
    public void MarkAsUsed_SetsIsUsedToTrue()
    {
        var token = PasswordResetToken.Create("user@example.com");

        token.MarkAsUsed();

        token.IsUsed.ShouldBeTrue();
        token.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void IsValid_AfterMarkAsUsed_ReturnsFalse()
    {
        var token = PasswordResetToken.Create("user@example.com");
        token.MarkAsUsed();

        token.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void IsExpired_WhenPastExpiration_ReturnsTrue()
    {
        // Arrange - create token and set ExpiresAt to the past via reflection
        var token = PasswordResetToken.Create("user@example.com");
        var expiresAtProp = typeof(PasswordResetToken).GetProperty(nameof(PasswordResetToken.ExpiresAt),
            BindingFlags.Public | BindingFlags.Instance)!;
        expiresAtProp.SetValue(token, DateTime.UtcNow.AddMinutes(-30));

        // Act & Assert
        token.IsExpired.ShouldBeTrue();
        token.IsValid.ShouldBeFalse();
    }
}
