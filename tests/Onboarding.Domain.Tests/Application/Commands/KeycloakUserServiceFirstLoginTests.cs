using System.Net;
using System.Text.Json;
using Keycloak.AuthServices.Sdk.Admin;
using Keycloak.AuthServices.Sdk.Admin.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Onboarding.Infrastructure.Keycloak;
using Shouldly;

namespace Onboarding.Domain.Tests.Application.Commands;

[Trait("Category", "Unit")]
public sealed class KeycloakUserServiceFirstLoginTests
{
    private readonly IHttpClientFactory _httpClientFactoryMock = Substitute.For<IHttpClientFactory>();
    private readonly IConfiguration _configurationMock = Substitute.For<IConfiguration>();
    private readonly ILogger<KeycloakUserService> _loggerMock = Substitute.For<ILogger<KeycloakUserService>>();
    private readonly KeycloakUserService _sut;
    private readonly FakeHttpMessageHandler _httpHandler = new();

    public KeycloakUserServiceFirstLoginTests()
    {
        _configurationMock["Keycloak:Realm"].Returns("onboarding");

        var httpClient = new HttpClient(_httpHandler) { BaseAddress = new Uri("http://localhost:8180/") };

        _httpClientFactoryMock.CreateClient("keycloak-admin-backoffice")
            .Returns(httpClient);
        _httpClientFactoryMock.CreateClient("keycloak-admin-client")
            .Returns(httpClient);

        _sut = new KeycloakUserService(_httpClientFactoryMock, _loggerMock);
    }

    [Fact]
    public async Task ClearFirstLoginFlagAsync_WhenAttributeTrue_CallsUpdateWithFalse()
    {
        // Arrange
        const string userId = "user-uuid-123";
        var user = new UserRepresentation
        {
            Id = userId,
            Attributes = new Dictionary<string, ICollection<string>>
            {
                ["isFirstLogin"] = new[] { "true" }
            }
        };

        _httpHandler.Responses[$"http://localhost:8180/admin/realms/backoffice/users/{userId}"] =
            () => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(user)) };

        // Act
        await _sut.ClearFirstLoginFlagAsync("backoffice", userId);

        // Assert — PUT called with isFirstLogin = "false"
        var putRequest = _httpHandler.Requests.FirstOrDefault(r => r.Method == HttpMethod.Put);
        putRequest.ShouldNotBeNull();
        putRequest.RequestUri!.ToString().ShouldContain($"/users/{userId}");

        var body = await putRequest.Content!.ReadAsStringAsync();
        var updatedUser = JsonSerializer.Deserialize<UserRepresentation>(body);
        updatedUser!.Attributes!["isFirstLogin"].First().ShouldBe("false");
    }

    [Fact]
    public async Task ClearFirstLoginFlagAsync_WhenAttributeAbsent_IsNoOp()
    {
        // Arrange
        const string userId = "user-uuid-456";
        var user = new UserRepresentation
        {
            Id = userId,
            Attributes = null
        };

        _httpHandler.Responses[$"http://localhost:8180/admin/realms/backoffice/users/{userId}"] =
            () => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(user)) };

        // Act
        await _sut.ClearFirstLoginFlagAsync("backoffice", userId);

        // Assert — PUT NOT called
        _httpHandler.Requests.Any(r => r.Method == HttpMethod.Put).ShouldBeFalse();
    }

    [Fact]
    public async Task ClearFirstLoginFlagAsync_WhenAttributeAlreadyFalse_IsNoOp()
    {
        // Arrange
        const string userId = "user-uuid-789";
        var user = new UserRepresentation
        {
            Id = userId,
            Attributes = new Dictionary<string, ICollection<string>>
            {
                ["isFirstLogin"] = new[] { "false" }
            }
        };

        _httpHandler.Responses[$"http://localhost:8180/admin/realms/backoffice/users/{userId}"] =
            () => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(user)) };

        // Act
        await _sut.ClearFirstLoginFlagAsync("backoffice", userId);

        // Assert — PUT NOT called
        _httpHandler.Requests.Any(r => r.Method == HttpMethod.Put).ShouldBeFalse();
    }

    private class FakeHttpMessageHandler : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = new();
        public Dictionary<string, Func<HttpResponseMessage>> Responses { get; } = new();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            if (Responses.TryGetValue(request.RequestUri!.ToString(), out var responseFunc))
                return Task.FromResult(responseFunc());

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }
}
