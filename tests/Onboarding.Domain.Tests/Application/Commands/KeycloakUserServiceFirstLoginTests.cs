using Keycloak.AuthServices.Sdk.Admin;
using Keycloak.AuthServices.Sdk.Admin.Models;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Onboarding.Infrastructure.Keycloak;
using Shouldly;

namespace Onboarding.Domain.Tests.Application.Commands;

[Trait("Category", "Unit")]
public sealed class KeycloakUserServiceFirstLoginTests
{
    private readonly IKeycloakUserClient _keycloakUserClientMock = Substitute.For<IKeycloakUserClient>();
    private readonly IHttpClientFactory _httpClientFactoryMock = Substitute.For<IHttpClientFactory>();
    private readonly IConfiguration _configurationMock = Substitute.For<IConfiguration>();
    private readonly KeycloakUserService _sut;

    public KeycloakUserServiceFirstLoginTests()
    {
        _configurationMock["Keycloak:Realm"].Returns("onboarding");
        // Provide a dummy HttpClient so KeycloakUserService constructor does not fail
        _httpClientFactoryMock.CreateClient("keycloak-admin-api")
            .Returns(new HttpClient { BaseAddress = new Uri("http://localhost:8180/") });
        _sut = new KeycloakUserService(_keycloakUserClientMock, _httpClientFactoryMock, _configurationMock);
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

        _keycloakUserClientMock
            .GetUserAsync("backoffice", userId, cancellationToken: Arg.Any<CancellationToken>())
            .Returns(user);

        // Act
        await _sut.ClearFirstLoginFlagAsync("backoffice", userId);

        // Assert — UpdateUserAsync called with isFirstLogin = "false"
        await _keycloakUserClientMock.Received(1)
            .UpdateUserAsync(
                "backoffice",
                userId,
                Arg.Is<UserRepresentation>(u =>
                    u.Attributes != null &&
                    u.Attributes.ContainsKey("isFirstLogin") &&
                    u.Attributes["isFirstLogin"].First() == "false"),
                Arg.Any<CancellationToken>());
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

        _keycloakUserClientMock
            .GetUserAsync("backoffice", userId, cancellationToken: Arg.Any<CancellationToken>())
            .Returns(user);

        // Act
        await _sut.ClearFirstLoginFlagAsync("backoffice", userId);

        // Assert — UpdateUserAsync NOT called (idempotent no-op)
        await _keycloakUserClientMock.DidNotReceive()
            .UpdateUserAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<UserRepresentation>(),
                Arg.Any<CancellationToken>());
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

        _keycloakUserClientMock
            .GetUserAsync("backoffice", userId, cancellationToken: Arg.Any<CancellationToken>())
            .Returns(user);

        // Act
        await _sut.ClearFirstLoginFlagAsync("backoffice", userId);

        // Assert — UpdateUserAsync NOT called (idempotent no-op)
        await _keycloakUserClientMock.DidNotReceive()
            .UpdateUserAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<UserRepresentation>(),
                Arg.Any<CancellationToken>());
    }
}
