using System.Net;
using System.Net.Http.Json;
using Keycloak.AuthServices.Sdk.Admin.Models;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Onboarding.Domain.Exceptions;
using Onboarding.Infrastructure.Keycloak;
using Shouldly;

namespace Onboarding.Domain.Tests.Infrastructure;

public class KeycloakUserServiceTests
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly MockHttpMessageHandler _httpMessageHandler;
    private readonly KeycloakUserService _sut;

    public KeycloakUserServiceTests()
    {
        _httpClientFactory = Substitute.For<IHttpClientFactory>();
        _httpMessageHandler = new MockHttpMessageHandler();
        var httpClient = new HttpClient(_httpMessageHandler)
        {
            BaseAddress = new Uri("http://keycloak:8080/")
        };

        _httpClientFactory.CreateClient(Arg.Any<string>()).Returns(httpClient);
        _sut = new KeycloakUserService(_httpClientFactory);
    }

    [Fact]
    public async Task CreateUserAsync_ShouldReturnUserId_WhenSuccessful()
    {
        // Arrange
        var email = "test@example.com";
        var expectedUserId = "user-123";
        
        // Mock POST (create user)
        _httpMessageHandler.When(HttpMethod.Post, "admin/realms/client/users")
            .Respond(HttpStatusCode.Created);

        // Mock GET (fetch user ID)
        _httpMessageHandler.When(HttpMethod.Get, $"admin/realms/client/users?email={Uri.EscapeDataString(email)}&exact=true")
            .Respond(HttpStatusCode.OK, new List<UserRepresentation> { new() { Id = expectedUserId } });

        // Mock PUT (reset password)
        _httpMessageHandler.When(HttpMethod.Put, $"admin/realms/client/users/{expectedUserId}/reset-password")
            .Respond(HttpStatusCode.NoContent);

        // Act
        var result = await _sut.CreateUserAsync("client", "username", email, "password", "firstName");

        // Assert
        result.ShouldBe(expectedUserId);
    }

    [Fact]
    public async Task CreateUserAsync_ShouldThrowDuplicateKeycloakUserException_WhenKeycloakReturnsConflict()
    {
        // Arrange
        _httpMessageHandler.When(HttpMethod.Post, "admin/realms/client/users")
            .Respond(HttpStatusCode.Conflict, "User already exists");

        // Act & Assert
        await Should.ThrowAsync<DuplicateKeycloakUserException>(() => 
            _sut.CreateUserAsync("client", "username", "test@example.com", "password", "firstName"));
    }

    [Fact]
    public async Task DeleteUserByEmailAsync_ShouldCallDelete_WhenUserExists()
    {
        // Arrange
        var email = "test@example.com";
        var userId = "user-123";
        
        _httpMessageHandler.When(HttpMethod.Get, $"admin/realms/client/users?email={Uri.EscapeDataString(email)}&exact=true")
            .Respond(HttpStatusCode.OK, new List<UserRepresentation> { new() { Id = userId } });

        _httpMessageHandler.When(HttpMethod.Delete, $"admin/realms/client/users/{userId}")
            .Respond(HttpStatusCode.NoContent);

        // Act
        await _sut.DeleteUserByEmailAsync("client", email);

        // Assert
        _httpMessageHandler.Verify(HttpMethod.Delete, $"admin/realms/client/users/{userId}");
    }

    [Fact]
    public async Task UserExistsByEmailAsync_ShouldReturnTrue_WhenUserFound()
    {
        // Arrange
        var email = "test@example.com";
        _httpMessageHandler.When(HttpMethod.Get, $"admin/realms/client/users?email={Uri.EscapeDataString(email)}&exact=true")
            .Respond(HttpStatusCode.OK, new List<UserRepresentation> { new() { Id = "123" } });

        // Act
        var result = await _sut.UserExistsByEmailAsync("client", email);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task GetUserByIdAsync_ShouldReturnUser_WhenFound()
    {
        // Arrange
        var userId = "user-123";
        _httpMessageHandler.When(HttpMethod.Get, $"admin/realms/client/users/{userId}")
            .Respond(HttpStatusCode.OK, new UserRepresentation { Id = userId, Email = "test@example.com", Enabled = true });

        // Act
        var result = await _sut.GetUserByIdAsync("client", userId);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(userId);
    }

    // REG-06: BlockUserAsync (Phase 6 scope)
    [Fact]
    public async Task BlockUserAsync_ShouldSetEnabledToFalse_WhenUserIsEnabled()
    {
        // Arrange
        var userId = "user-123";
        _httpMessageHandler.When(HttpMethod.Get, $"admin/realms/client/users/{userId}")
            .Respond(HttpStatusCode.OK, new UserRepresentation { Id = userId, Enabled = true });

        _httpMessageHandler.When(HttpMethod.Put, $"admin/realms/client/users/{userId}")
            .Respond(HttpStatusCode.NoContent);

        // Act
        await _sut.BlockUserAsync("client", userId);

        // Assert
        _httpMessageHandler.Verify(HttpMethod.Put, $"admin/realms/client/users/{userId}");
    }
}

public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly List<(Func<HttpRequestMessage, bool> predicate, HttpResponseMessage response)> _responses = [];
    private readonly List<HttpRequestMessage> _sentRequests = [];

    public RequestBuilder When(HttpMethod method, string path)
    {
        return new RequestBuilder(this, method, path);
    }

    public void Verify(HttpMethod method, string path)
    {
        _sentRequests.Any(r => r.Method == method && r.RequestUri!.PathAndQuery.Contains(path)).ShouldBeTrue();
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        _sentRequests.Add(request);
        var match = _responses.FirstOrDefault(r => r.predicate(request));
        if (match.response != null) return match.response;
        
        return new HttpResponseMessage(HttpStatusCode.NotFound);
    }

    public class RequestBuilder
    {
        private readonly MockHttpMessageHandler _handler;
        private readonly HttpMethod _method;
        private readonly string _path;

        public RequestBuilder(MockHttpMessageHandler handler, HttpMethod method, string path)
        {
            _handler = handler;
            _method = method;
            _path = path;
        }

        public void Respond(HttpStatusCode code, object? body = null)
        {
            var response = new HttpResponseMessage(code);
            if (body != null)
            {
                response.Content = JsonContent.Create(body);
            }
            _handler._responses.Add((r => r.Method == _method && r.RequestUri!.PathAndQuery.Contains(_path), response));
        }
    }
}
