using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Testing;
using NSubstitute;
using Onboarding.API.Tests.Authentication;
using Onboarding.Application.Admin.DTOs;
using Onboarding.Domain.Aggregates.ClientAggregate;
using Onboarding.Domain.Repositories;
using Shouldly;

namespace Onboarding.API.Tests.Admin;

/// <summary>
/// Authorization tests for admin endpoints — verifies [Authorize(Roles = "admin")] pipeline.
/// Tests: 403 for non-admin, 401 for unauthenticated, 200 for admin.
/// </summary>
[Collection(WebAppFactoryCollection.Name)]
public sealed class AdminAuthorizationTests : IAsyncLifetime
{
    private AdminTestFactory? _factory;
    private HttpClient? _adminClient;
    private HttpClient? _nonAdminClient;
    private HttpClient? _unauthenticatedClient;

    public Task InitializeAsync()
    {
        _factory = new AdminTestFactory();

        _adminClient = _factory.CreateClient();
        _adminClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", FakeJwtTokenHelper.GenerateAdminJwt());

        _nonAdminClient = _factory.CreateClient();
        _nonAdminClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", FakeJwtTokenHelper.GenerateNonAdminJwt());

        _unauthenticatedClient = _factory.CreateClient();

        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _adminClient?.Dispose();
        _nonAdminClient?.Dispose();
        _unauthenticatedClient?.Dispose();
        if (_factory is not null)
            await _factory.DisposeAsync();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetAdminUsers_WithAdminToken_ReturnsOk()
    {
        // Arrange — mock returns empty page
        _factory!.AdminRepositoryMock
            .GetPagedAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((new List<Client>().AsReadOnly(), 0));

        // Act
        var response = await _adminClient!.GetAsync("/api/admin/users?page=1&pageSize=10");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetAdminUsers_WithNonAdminToken_ReturnsForbidden()
    {
        // Act
        var response = await _nonAdminClient!.GetAsync("/api/admin/users?page=1&pageSize=10");

        // Assert — 403 Forbidden for non-admin
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetAdminUsers_WithoutToken_ReturnsUnauthorized()
    {
        // Act
        var response = await _unauthenticatedClient!.GetAsync("/api/admin/users?page=1&pageSize=10");

        // Assert — 401 Unauthorized
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task BlockUser_WithNonAdminToken_ReturnsForbidden()
    {
        // Act
        var response = await _nonAdminClient!.PostAsync("/api/admin/users/00000000-0000-0000-0000-000000000001/block", null);

        // Assert — 403 Forbidden for non-admin
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetAdministrators_WithNonAdminToken_ReturnsForbidden()
    {
        // Act
        var response = await _nonAdminClient!.GetAsync("/api/admin/administrators");

        // Assert — 403 Forbidden for non-admin
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetAdministrators_WithAdminToken_ReturnsOk()
    {
        // Act
        var response = await _adminClient!.GetAsync("/api/admin/administrators");

        // Assert — 200 OK for admin (may return empty list)
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task CreateAdmin_WithNonAdminToken_ReturnsForbidden()
    {
        // Act
        var content = new StringContent("{\"fullName\":\"Test\",\"email\":\"test@test.com\"}", System.Text.Encoding.UTF8, "application/json");
        var response = await _nonAdminClient!.PostAsync("/api/admin/administrators", content);

        // Assert — 403 Forbidden for non-admin
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task CreateAdmin_OldRoute_ReturnsMethodNotAllowed()
    {
        // Act — old route POST /api/admin/users should return 405 (GET /api/admin/users still exists for client listing)
        var content = new StringContent("{\"fullName\":\"Test\",\"email\":\"test@test.com\"}", System.Text.Encoding.UTF8, "application/json");
        var response = await _adminClient!.PostAsync("/api/admin/users", content);

        // Assert — 405 Method Not Allowed (route exists but only for GET)
        response.StatusCode.ShouldBe(HttpStatusCode.MethodNotAllowed);
    }
}
