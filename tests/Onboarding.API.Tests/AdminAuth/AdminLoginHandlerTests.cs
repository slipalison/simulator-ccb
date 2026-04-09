using Microsoft.Extensions.Logging;
using NSubstitute;
using Onboarding.Application.Auth.Commands;
using Onboarding.Application.Auth.DTOs;
using Onboarding.Application.Common;
using Onboarding.Infrastructure.Keycloak;
using Shouldly;

namespace Onboarding.API.Tests.AdminAuth;

/// <summary>
/// Unit tests for AdminLoginCommandHandler (Task 2).
/// </summary>
public class AdminLoginHandlerTests
{
    private readonly IKeycloakTokenService _tokenService = Substitute.For<IKeycloakTokenService>();
    private readonly ILogger<AdminLoginCommandHandler> _logger = Substitute.For<ILogger<AdminLoginCommandHandler>>();

    [Fact]
    public async Task HandleAsync_ValidAdminCredentials_ReturnsSessionResponse()
    {
        // Arrange
        _tokenService.ExchangePasswordAsync("admin@test.com", "SecureP@ss123", Arg.Any<CancellationToken>())
            .Returns(new TokenResponse("fake-access-token", "fake-refresh-token", 300, "Bearer", 1800, "openid"));

        var handler = new AdminLoginCommandHandler(_tokenService, _logger);
        var command = new AdminLoginCommand("admin@test.com", "SecureP@ss123");

        // Act — note: this uses a real JWT, so we need a token with "role" claims
        // For unit tests, we mock the token service but the handler decodes the JWT.
        // Since we can't easily create a JWT with resource_access in unit tests,
        // we test with a token that has flat "role" claims.
        var adminJwt = CreateAdminJwt("admin@test.com", "Admin User");
        _tokenService.ExchangePasswordAsync("admin@test.com", "SecureP@ss123", Arg.Any<CancellationToken>())
            .Returns(new TokenResponse(adminJwt, "fake-refresh-token", 300, "Bearer", 1800, "openid"));

        var result = await handler.HandleAsync(command);

        // Assert
        result.ShouldNotBeNull();
        result.AdminEmail.ShouldBe("admin@test.com");
        result.AdminName.ShouldBe("Admin User");
        result.RefreshToken.ShouldBe("fake-refresh-token");
        result.RefreshExpiresIn.ShouldBe(1800);
    }

    [Fact]
    public async Task HandleAsync_NonAdminUser_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var nonAdminJwt = CreateNonAdminJwt("user@test.com");
        _tokenService.ExchangePasswordAsync("user@test.com", "SecureP@ss123", Arg.Any<CancellationToken>())
            .Returns(new TokenResponse(nonAdminJwt, "fake-refresh-token", 300, "Bearer", 1800, "openid"));

        var handler = new AdminLoginCommandHandler(_tokenService, _logger);
        var command = new AdminLoginCommand("user@test.com", "SecureP@ss123");

        // Act & Assert
        await Should.ThrowAsync<UnauthorizedAccessException>(async () => await handler.HandleAsync(command));
    }

    [Fact]
    public async Task HandleAsync_InvalidCredentials_ThrowsKeycloakAuthException()
    {
        // Arrange
        _tokenService
            .When(x => x.ExchangePasswordAsync("admin@test.com", "wrong-password", Arg.Any<CancellationToken>()))
            .Do(x => throw new KeycloakAuthException("Invalid credentials."));

        var handler = new AdminLoginCommandHandler(_tokenService, _logger);
        var command = new AdminLoginCommand("admin@test.com", "wrong-password");

        // Act & Assert
        await Should.ThrowAsync<KeycloakAuthException>(async () => await handler.HandleAsync(command));
    }

    /// <summary>
    /// Creates a fake JWT with admin role claims (mimics Keycloak token structure).
    /// </summary>
    private static string CreateAdminJwt(string email, string name)
    {
        var claims = new List<System.Security.Claims.Claim>
        {
            new("email", email),
            new("name", name),
            new("sub", Guid.NewGuid().ToString()),
            new("role", "admin"),
            new("role", "user"),
        };

        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            issuer: "http://localhost",
            audience: "http://localhost",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1));

        return new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Creates a fake JWT WITHOUT admin role — simulates a regular user.
    /// </summary>
    private static string CreateNonAdminJwt(string email)
    {
        var claims = new List<System.Security.Claims.Claim>
        {
            new("email", email),
            new("sub", Guid.NewGuid().ToString()),
            new("role", "user"),
        };

        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            issuer: "http://localhost",
            audience: "http://localhost",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1));

        return new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
    }
}
