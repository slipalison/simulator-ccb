using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Onboarding.Infrastructure.Keycloak;
using Shouldly;

namespace Onboarding.Domain.Tests.Infrastructure;

public class KeycloakUserServiceGroupTests
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<KeycloakUserService> _logger;
    private readonly MockHttpMessageHandler _httpMessageHandler;
    private readonly KeycloakUserService _sut;

    public KeycloakUserServiceGroupTests()
    {
        _httpClientFactory = Substitute.For<IHttpClientFactory>();
        _logger = Substitute.For<ILogger<KeycloakUserService>>();
        _httpMessageHandler = new MockHttpMessageHandler();
        var httpClient = new HttpClient(_httpMessageHandler)
        {
            BaseAddress = new Uri("http://keycloak:8080/")
        };

        _httpClientFactory.CreateClient(Arg.Any<string>()).Returns(httpClient);
        _sut = new KeycloakUserService(_httpClientFactory, _logger);
    }

    // --- CreateGroupAsync ---

    [Fact]
    public async Task CreateGroupAsync_ShouldCreateAndReturnGroupId_WhenGroupDoesNotExist()
    {
        // Arrange: group doesn't exist (search returns empty), then we create it
        _httpMessageHandler.When(HttpMethod.Get, "admin/realms/client/groups?search=admin-empresa&exact=true")
            .Respond(HttpStatusCode.OK, new List<KeycloakGroupRepresentation>());

        _httpMessageHandler.When(HttpMethod.Post, "admin/realms/client/groups")
            .RespondWithHeaders(HttpStatusCode.Created, r =>
            {
                r.Headers.Location = new Uri("http://keycloak:8080/admin/realms/client/groups/group-uuid-123");
            });

        // Act
        var result = await _sut.CreateGroupAsync("client", "admin-empresa");

        // Assert
        result.ShouldBe("group-uuid-123");
    }

    [Fact]
    public async Task CreateGroupAsync_ShouldReturnExistingGroupId_WhenGroupAlreadyExists()
    {
        // Arrange: group already exists (search returns it)
        _httpMessageHandler.When(HttpMethod.Get, "admin/realms/client/groups?search=viewer&exact=true")
            .Respond(HttpStatusCode.OK, new List<KeycloakGroupRepresentation>
            {
                new("existing-group-id", "viewer")
            });

        // Act
        var result = await _sut.CreateGroupAsync("client", "viewer");

        // Assert
        result.ShouldBe("existing-group-id");
    }

    // --- AddUserToGroupAsync ---

    [Fact]
    public async Task AddUserToGroupAsync_ShouldCallPut_WhenSuccessful()
    {
        // Arrange
        _httpMessageHandler.When(HttpMethod.Put, "admin/realms/client/users/user-123/groups/group-456")
            .Respond(HttpStatusCode.NoContent);

        // Act
        await _sut.AddUserToGroupAsync("client", "user-123", "group-456");

        // Assert
        _httpMessageHandler.Verify(HttpMethod.Put, "admin/realms/client/users/user-123/groups/group-456");
    }

    [Fact]
    public async Task AddUserToGroupAsync_ShouldIgnoreConflict_WhenAlreadyMember()
    {
        // Arrange: 409 Conflict (already a member) should be idempotent
        _httpMessageHandler.When(HttpMethod.Put, "admin/realms/client/users/user-123/groups/group-456")
            .Respond(HttpStatusCode.Conflict);

        // Act & Assert — should not throw
        await Should.NotThrowAsync(() => _sut.AddUserToGroupAsync("client", "user-123", "group-456"));
    }

    // --- RemoveUserFromGroupAsync ---

    [Fact]
    public async Task RemoveUserFromGroupAsync_ShouldCallDelete_WhenSuccessful()
    {
        // Arrange
        _httpMessageHandler.When(HttpMethod.Delete, "admin/realms/client/users/user-123/groups/group-456")
            .Respond(HttpStatusCode.NoContent);

        // Act
        await _sut.RemoveUserFromGroupAsync("client", "user-123", "group-456");

        // Assert
        _httpMessageHandler.Verify(HttpMethod.Delete, "admin/realms/client/users/user-123/groups/group-456");
    }

    [Fact]
    public async Task RemoveUserFromGroupAsync_ShouldIgnoreNotFound_WhenNotMember()
    {
        // Arrange: 404 Not Found (not a member) should be idempotent
        _httpMessageHandler.When(HttpMethod.Delete, "admin/realms/client/users/user-123/groups/group-456")
            .Respond(HttpStatusCode.NotFound);

        // Act & Assert — should not throw
        await Should.NotThrowAsync(() => _sut.RemoveUserFromGroupAsync("client", "user-123", "group-456"));
    }

    // --- GetGroupByNameAsync ---

    [Fact]
    public async Task GetGroupByNameAsync_ShouldReturnGroupId_WhenGroupFound()
    {
        // Arrange
        _httpMessageHandler.When(HttpMethod.Get, "admin/realms/client/groups?search=dashboard&exact=true")
            .Respond(HttpStatusCode.OK, new List<KeycloakGroupRepresentation>
            {
                new("group-dashboard-uuid", "dashboard")
            });

        // Act
        var result = await _sut.GetGroupByNameAsync("client", "dashboard");

        // Assert
        result.ShouldBe("group-dashboard-uuid");
    }

    [Fact]
    public async Task GetGroupByNameAsync_ShouldReturnNull_WhenGroupNotFound()
    {
        // Arrange
        _httpMessageHandler.When(HttpMethod.Get, "admin/realms/client/groups?search=nonexistent&exact=true")
            .Respond(HttpStatusCode.OK, new List<KeycloakGroupRepresentation>());

        // Act
        var result = await _sut.GetGroupByNameAsync("client", "nonexistent");

        // Assert
        result.ShouldBeNull();
    }
}