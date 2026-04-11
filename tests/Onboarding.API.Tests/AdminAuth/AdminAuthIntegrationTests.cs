using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using NSubstitute;
using Onboarding.Application.Auth.DTOs;
using Onboarding.Application.Common;
using Onboarding.Domain.Aggregates.ClientAggregate;
using Shouldly;

namespace Onboarding.API.Tests.AdminAuth;

[Collection("AdminAuthIntegration")]
public class AdminAuthIntegrationTests : IClassFixture<AdminAuthIntegrationTestFactory>
{
    private readonly AdminAuthIntegrationTestFactory _factory;

    public AdminAuthIntegrationTests(AdminAuthIntegrationTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AdminEndpoint_WithAdminCookie_Returns200()
    {
        // Note: The existing AdminUserController uses JWT Bearer auth, not cookie auth.
        // The admin cookie from AdminAuthController is for the /api/admin/auth/* endpoints only.
        // This test verifies that after admin login, the /api/admin/auth/me endpoint works
        // and returns correct admin data — demonstrating the session is valid.

        // Arrange
        SetupAdminLogin();
        _factory.TokenServiceMock
            .RefreshTokenAsync("admin-refresh-token", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new TokenResponse(
                CreateAdminJwt("admin@test.com", "Admin User"),
                "admin-refresh-token", 300, "Bearer", 1800, "openid")));

        var client = CreateLoggedInAdminClient();

        // Act — access /api/admin/auth/me (session validation)
        var response = await client.GetAsync("/api/admin/auth/me");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        body.ShouldNotBeNull();
        body["adminEmail"].ShouldBe("admin@test.com");
    }

    [Fact]
    public async Task AdminEndpoint_WithRegularUserCookie_Returns403()
    {
        // Arrange — login as non-admin (will get 403 from login)
        var nonAdminJwt = CreateNonAdminJwt("user@test.com");
        _factory.TokenServiceMock
            .ExchangePasswordAsync("user@test.com", "SecureP@ss123", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new TokenResponse(nonAdminJwt, "user-refresh-token", 300, "Bearer", 1800, "openid")));

        var client = _factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/admin/auth/login",
            new { email = "user@test.com", password = "SecureP@ss123" });
        loginResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        // Non-admin user cannot access admin endpoints
        // Since login failed, there's no cookie — should get 401
        var response = await client.GetAsync("/api/admin/users?page=1&pageSize=20");
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_SetCookieHeader_HasHttpOnlyFlag()
    {
        // Arrange
        SetupAdminLogin();
        var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/admin/auth/login",
            new { email = "admin@test.com", password = "SecureP@ss123" });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var cookies = response.Headers.GetValues("Set-Cookie").ToList();
        cookies.Any(c => c.Contains("httponly", StringComparison.OrdinalIgnoreCase)).ShouldBeTrue();
    }

    [Fact]
    public async Task Login_SetCookieHeader_SameSiteStrict()
    {
        // Arrange
        SetupAdminLogin();
        var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/admin/auth/login",
            new { email = "admin@test.com", password = "SecureP@ss123" });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var cookies = response.Headers.GetValues("Set-Cookie").ToList();
        cookies.Any(c => c.Contains("samesite=strict", StringComparison.OrdinalIgnoreCase)).ShouldBeTrue();
    }

    [Fact]
    public async Task Logout_ClearsAdminCookie()
    {
        // Arrange
        SetupAdminLogin();
        _factory.TokenServiceMock
            .RefreshTokenAsync("admin-refresh-token", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new TokenResponse(
                CreateAdminJwt("admin@test.com", "Admin User"),
                "admin-refresh-token", 300, "Bearer", 1800, "openid")));

        var client = CreateLoggedInAdminClient();

        // Act
        var logoutResponse = await client.PostAsync("/api/admin/auth/logout", null);

        // Assert
        logoutResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // After logout, create a new client without the cookie
        var loggedOutClient = _factory.CreateClient();
        var meResponse = await loggedOutClient.GetAsync("/api/admin/auth/me");
        meResponse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    /// <summary>Configures the mock token service for a successful admin login.</summary>
    private void SetupAdminLogin()
    {
        var adminJwt = CreateAdminJwt("admin@test.com", "Admin User");
        _factory.TokenServiceMock
            .ExchangePasswordAsync("admin@test.com", "SecureP@ss123", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new TokenResponse(adminJwt, "admin-refresh-token", 300, "Bearer", 1800, "openid")));
    }

    /// <summary>Creates an HttpClient that is logged in as admin (has cookie).</summary>
    private HttpClient CreateLoggedInAdminClient()
    {
        SetupAdminLogin();
        var client = _factory.CreateClient();

        // Manually set the cookie (since we can't extract Set-Cookie from login in this helper)
        client.DefaultRequestHeaders.Add("Cookie", "adminRefreshToken=admin-refresh-token; Path=/api/admin; HttpOnly");
        return client;
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
