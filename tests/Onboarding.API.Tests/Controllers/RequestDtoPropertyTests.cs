using Onboarding.API.Controllers;
using Shouldly;

namespace Onboarding.API.Tests.Controllers;

/// <summary>
/// Covers the property getters of request DTO records defined in AuthController and
/// AdminUserController that are not exercised by endpoint integration tests.
///
/// RefreshTokenRequest.RefreshToken — the refresh endpoint reads from httpOnly cookie
/// directly, so this DTO's getter is never called by the existing integration flow.
/// ForcePasswordChangeRequest.NewPassword — the ForcePasswordChange endpoint does
/// use this property; this test ensures the getter is covered in the unit pass.
///
/// These are post-boundary files (D-2 enforces 80%+) and coverlet reports them
/// as 0% line because only the positional record getter is instrumented.
/// </summary>
[Trait("Category", "Unit")]
public sealed class RequestDtoPropertyTests
{
    [Fact]
    public void RefreshTokenRequest_GetRefreshToken_ReturnsValue()
    {
        // Arrange + Act
        var dto = new RefreshTokenRequest("my-refresh-token");

        // Assert
        dto.RefreshToken.ShouldBe("my-refresh-token");
    }

    [Fact]
    public void RefreshTokenRequest_GetRefreshToken_NullValue_ReturnsNull()
    {
        // Arrange + Act
        var dto = new RefreshTokenRequest(null);

        // Assert
        dto.RefreshToken.ShouldBeNull();
    }

    [Fact]
    public void ForcePasswordChangeRequest_GetNewPassword_ReturnsValue()
    {
        // Arrange + Act
        var dto = new ForcePasswordChangeRequest("NewS3cur3P@ss!");

        // Assert
        dto.NewPassword.ShouldBe("NewS3cur3P@ss!");
    }
}
