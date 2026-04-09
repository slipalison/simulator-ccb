using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Onboarding.Application.Auth.DTOs;
using Onboarding.Application.Common;
using Onboarding.Infrastructure.Keycloak;
using Shouldly;

namespace Onboarding.API.Tests.AdminAuth;

/// <summary>
/// Endpoint tests for AdminAuthController (Task 3).
/// Tests login/logout/me endpoints with cookie handling.
/// </summary>
public class AdminAuthEndpointTests : IClassFixture<AdminAuthTestFactory>
{
    private readonly AdminAuthTestFactory _factory;

    public AdminAuthEndpointTests(AdminAuthTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_ValidAdminCredentials_Returns200WithSetCookieHeader()
    {
        // Arrange
        var adminJwt = CreateAdminJwt("admin@test.com", "Admin User");
        _factory.TokenServiceMock
            .ExchangePasswordAsync("admin@test.com", "SecureP@ss123", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new TokenResponse(adminJwt, "fake-refresh-token", 300, "Bearer", 1800, "openid")));

        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/admin/auth/login",
            new { email = "admin@test.com", password = "SecureP@ss123" });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var cookies = response.Headers.GetValues("Set-Cookie").ToList();
        cookies.ShouldNotBeEmpty();
        cookies.Any(c => c.Contains("adminRefreshToken=")).ShouldBeTrue();
        cookies.Any(c => c.Contains("httponly", StringComparison.OrdinalIgnoreCase)).ShouldBeTrue();
        cookies.Any(c => c.Contains("path=/api/admin", StringComparison.OrdinalIgnoreCase)).ShouldBeTrue();
        cookies.Any(c => c.Contains("samesite=strict", StringComparison.OrdinalIgnoreCase)).ShouldBeTrue();
        // Secure should NOT be present since we configured Secure=false for tests
        cookies.Any(c => c.Contains("secure", StringComparison.OrdinalIgnoreCase)).ShouldBeFalse();

        var body = await response.Content.ReadFromJsonAsync<object>();
        body.ShouldNotBeNull();
    }

    [Fact]
    public async Task Login_NonAdminUser_Returns403()
    {
        // Arrange
        var nonAdminJwt = CreateNonAdminJwt("user@test.com");
        _factory.TokenServiceMock
            .ExchangePasswordAsync("user@test.com", "SecureP@ss123", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new TokenResponse(nonAdminJwt, "fake-refresh-token", 300, "Bearer", 1800, "openid")));

        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/admin/auth/login",
            new { email = "user@test.com", password = "SecureP@ss123" });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Login_InvalidCredentials_Returns401()
    {
        // Arrange
        _factory.TokenServiceMock
            .When(x => x.ExchangePasswordAsync("admin@test.com", "wrong", Arg.Any<CancellationToken>()))
            .Do(x => throw new KeycloakAuthException("Invalid credentials."));

        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/admin/auth/login",
            new { email = "admin@test.com", password = "wrong" });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_InvalidEmail_Returns422()
    {
        // Arrange
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/admin/auth/login",
            new { email = "not-an-email", password = "SecureP@ss123" });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Logout_WithAuthenticatedRequest_Returns204()
    {
        // Arrange — login first to get cookie
        var adminJwt = CreateAdminJwt("admin@test.com", "Admin User");
        _factory.TokenServiceMock
            .ExchangePasswordAsync("admin@test.com", "SecureP@ss123", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new TokenResponse(adminJwt, "fake-refresh-token", 300, "Bearer", 1800, "openid")));

        var client = _factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/admin/auth/login",
            new { email = "admin@test.com", password = "SecureP@ss123" });
        loginResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var cookies = loginResponse.Headers.GetValues("Set-Cookie").ToList();
        var adminCookie = cookies.First(c => c.Contains("adminRefreshToken="));
        client.DefaultRequestHeaders.Add("Cookie", adminCookie);

        // Act
        var logoutResponse = await client.PostAsync("/api/admin/auth/logout", null);

        // Assert
        logoutResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task GetMe_ValidCookie_ReturnsAdminInfo()
    {
        // Arrange — login first
        var adminJwt = CreateAdminJwt("admin@test.com", "Admin User");
        var refreshedJwt = CreateAdminJwt("admin@test.com", "Admin User");
        _factory.TokenServiceMock
            .ExchangePasswordAsync("admin@test.com", "SecureP@ss123", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new TokenResponse(adminJwt, "fake-refresh-token", 300, "Bearer", 1800, "openid")));
        _factory.TokenServiceMock
            .RefreshTokenAsync("fake-refresh-token", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new TokenResponse(refreshedJwt, "new-refresh-token", 300, "Bearer", 1800, "openid")));

        var client = _factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/admin/auth/login",
            new { email = "admin@test.com", password = "SecureP@ss123" });
        loginResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var cookies = loginResponse.Headers.GetValues("Set-Cookie").ToList();
        var adminCookie = cookies.First(c => c.Contains("adminRefreshToken="));
        client.DefaultRequestHeaders.Add("Cookie", adminCookie);

        // Act
        var meResponse = await client.GetAsync("/api/admin/auth/me");

        // Assert
        meResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await meResponse.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        body.ShouldNotBeNull();
        body["adminEmail"].ShouldBe("admin@test.com");
        body["adminName"].ShouldBe("Admin User");
    }

    [Fact]
    public async Task GetMe_NoCookie_Returns401()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/admin/auth/me");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    /// <summary>Creates a fake JWT with admin role claims.</summary>
    private static string CreateAdminJwt(string email, string name)
    {
        var claims = new List<Claim>
        {
            new("email", email),
            new("name", name),
            new("sub", Guid.NewGuid().ToString()),
            new("role", "admin"),
        };

        var token = new JwtSecurityToken(
            issuer: "http://localhost",
            audience: "http://localhost",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>Creates a fake JWT WITHOUT admin role.</summary>
    private static string CreateNonAdminJwt(string email)
    {
        var claims = new List<Claim>
        {
            new("email", email),
            new("sub", Guid.NewGuid().ToString()),
            new("role", "user"),
        };

        var token = new JwtSecurityToken(
            issuer: "http://localhost",
            audience: "http://localhost",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
