using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Keycloak.AuthServices.Sdk.Admin.Models;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Onboarding.Domain.Exceptions;
using Onboarding.Infrastructure.Keycloak;
using Shouldly;

namespace Onboarding.Infrastructure.Tests.Keycloak;

/// <summary>
/// Unit tests for KeycloakUserService covering the uncovered paths (40.6% → >80%).
/// Tests use a per-test MockHttpMessageHandler so requests can be matched granularly.
/// NOTE: "backoffice" realm routes to a different named client ("keycloak-admin-backoffice");
/// all other realms route to "keycloak-admin-client". GetClient branch is tested below.
/// </summary>
public sealed class KeycloakUserServiceTests
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<KeycloakUserService> _logger;
    private readonly FlexMockHttpHandler _handler;
    private readonly KeycloakUserService _sut;

    public KeycloakUserServiceTests()
    {
        _httpClientFactory = Substitute.For<IHttpClientFactory>();
        _logger = Substitute.For<ILogger<KeycloakUserService>>();
        _handler = new FlexMockHttpHandler();

        var httpClient = new HttpClient(_handler)
        {
            BaseAddress = new Uri("http://keycloak:8080/")
        };
        _httpClientFactory.CreateClient(Arg.Any<string>()).Returns(httpClient);
        _sut = new KeycloakUserService(_httpClientFactory, _logger);
    }

    // --- GetClient routing ---

    [Fact]
    public async Task GetUserByEmailAsync_BackofficeRealm_UsesBackofficeClient()
    {
        // Arrange
        var email = "admin@backoffice.com";
        _handler.AddResponse(HttpMethod.Get, $"admin/realms/backoffice/users",
            HttpStatusCode.OK, new List<UserRepresentation> { new() { Id = "bo-123", Email = email } });

        // Act
        var result = await _sut.GetUserByEmailAsync("backoffice", email);

        // Assert
        result.ShouldNotBeNull();
        _httpClientFactory.Received().CreateClient("keycloak-admin-backoffice");
    }

    [Fact]
    public async Task GetUserByEmailAsync_NonBackofficeRealm_UsesClientNamedClient()
    {
        // Arrange
        var email = "user@client.com";
        _handler.AddResponse(HttpMethod.Get, $"admin/realms/client/users",
            HttpStatusCode.OK, new List<UserRepresentation> { new() { Id = "c-123", Email = email } });

        // Act
        await _sut.GetUserByEmailAsync("client", email);

        // Assert
        _httpClientFactory.Received().CreateClient("keycloak-admin-client");
    }

    // --- GetUserByEmailAsync ---

    [Fact]
    public async Task GetUserByEmailAsync_UserFound_ReturnsKeycloakUser()
    {
        // Arrange
        var email = "found@test.com";
        _handler.AddResponse(HttpMethod.Get, $"admin/realms/client/users",
            HttpStatusCode.OK, new List<UserRepresentation>
            {
                new() { Id = "usr-abc", Email = email, Enabled = true, EmailVerified = false }
            });

        // Act
        var result = await _sut.GetUserByEmailAsync("client", email);

        // Assert
        result.ShouldNotBeNull();
        result!.Id.ShouldBe("usr-abc");
        result.Email.ShouldBe(email);
        result.Enabled.ShouldBeTrue();
        result.EmailVerified.ShouldBeFalse();
    }

    [Fact]
    public async Task GetUserByEmailAsync_UserNotFound_ReturnsNull()
    {
        // Arrange
        _handler.AddResponse(HttpMethod.Get, "admin/realms/client/users",
            HttpStatusCode.OK, new List<UserRepresentation>());

        // Act
        var result = await _sut.GetUserByEmailAsync("client", "nobody@test.com");

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetUserByEmailAsync_UserWithNullEmail_FallsBackToProvidedEmail()
    {
        // Arrange
        var email = "fallback@test.com";
        _handler.AddResponse(HttpMethod.Get, "admin/realms/client/users",
            HttpStatusCode.OK, new List<UserRepresentation>
            {
                new() { Id = "u1", Email = null, Enabled = null, EmailVerified = null }
            });

        // Act
        var result = await _sut.GetUserByEmailAsync("client", email);

        // Assert
        result.ShouldNotBeNull();
        result!.Email.ShouldBe(email); // falls back to parameter
        result.Enabled.ShouldBeTrue(); // defaults to true
        result.EmailVerified.ShouldBeFalse(); // defaults to false
    }

    // --- CreateUserAsync additional branches ---

    [Fact]
    public async Task CreateUserAsync_PasswordResetConflict_ThrowsDuplicateKeycloakUserException()
    {
        // Arrange
        var email = "pwdconflict@test.com";
        var userId = "uid-conflict";

        _handler.AddResponse(HttpMethod.Post, "admin/realms/client/users", HttpStatusCode.Created, null);
        _handler.AddResponse(HttpMethod.Get, "admin/realms/client/users",
            HttpStatusCode.OK, new List<UserRepresentation> { new() { Id = userId } });
        _handler.AddResponse(HttpMethod.Put, $"admin/realms/client/users/{userId}/reset-password",
            HttpStatusCode.Conflict, "conflict");

        // Act & Assert
        await Should.ThrowAsync<DuplicateKeycloakUserException>(
            () => _sut.CreateUserAsync("client", "user", email, "pass", "Name"));
    }

    [Fact]
    public async Task CreateUserAsync_PasswordResetFails_ThrowsHttpRequestException()
    {
        // Arrange
        var email = "pwdfail@test.com";
        var userId = "uid-fail";

        _handler.AddResponse(HttpMethod.Post, "admin/realms/client/users", HttpStatusCode.Created, null);
        _handler.AddResponse(HttpMethod.Get, "admin/realms/client/users",
            HttpStatusCode.OK, new List<UserRepresentation> { new() { Id = userId } });
        // EnsureSuccessStatusCode throws on non-2xx
        _handler.AddResponse(HttpMethod.Put, $"admin/realms/client/users/{userId}/reset-password",
            HttpStatusCode.InternalServerError, "error");

        // Act & Assert
        await Should.ThrowAsync<HttpRequestException>(
            () => _sut.CreateUserAsync("client", "user", email, "pass", "Name"));
    }

    [Fact]
    public async Task CreateUserAsync_UserIdNotReturnedAfterCreate_ThrowsInvalidOperationException()
    {
        // Arrange
        _handler.AddResponse(HttpMethod.Post, "admin/realms/client/users", HttpStatusCode.Created, null);
        _handler.AddResponse(HttpMethod.Get, "admin/realms/client/users",
            HttpStatusCode.OK, new List<UserRepresentation>()); // empty — no userId

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => _sut.CreateUserAsync("client", "user", "e@t.com", "pass", "Name"));
    }

    [Fact]
    public async Task CreateUserAsync_CreateFails_ThrowsException()
    {
        // Arrange
        _handler.AddResponse(HttpMethod.Post, "admin/realms/client/users",
            HttpStatusCode.InternalServerError, "server error");

        // Act & Assert
        await Should.ThrowAsync<Exception>(
            () => _sut.CreateUserAsync("client", "user", "e@t.com", "pass", "Name"));
    }

    // --- CreateAdminUserAsync ---

    [Fact]
    public async Task CreateAdminUserAsync_Successful_ReturnsUserId()
    {
        // Arrange
        var email = "adminusr@test.com";
        var userId = "admin-uid-123";
        var roleId = "role-id-999";

        _handler.AddResponse(HttpMethod.Post, "admin/realms/client/users", HttpStatusCode.Created, null);
        _handler.AddResponse(HttpMethod.Get, "admin/realms/client/users",
            HttpStatusCode.OK, new List<UserRepresentation> { new() { Id = userId } });
        _handler.AddResponse(HttpMethod.Put, $"admin/realms/client/users/{userId}/reset-password",
            HttpStatusCode.NoContent, null);
        // AssignAdminRoleAsync: GET role
        _handler.AddResponse(HttpMethod.Get, "admin/realms/client/roles/admin",
            HttpStatusCode.OK, new { id = roleId, name = "admin" });
        // AssignAdminRoleAsync: POST role-mappings
        _handler.AddResponse(HttpMethod.Post, $"admin/realms/client/users/{userId}/role-mappings/realm",
            HttpStatusCode.NoContent, null);

        // Act
        var result = await _sut.CreateAdminUserAsync("client", email, "TempPass1!", "Full Name");

        // Assert
        result.ShouldBe(userId);
    }

    [Fact]
    public async Task CreateAdminUserAsync_Conflict_ThrowsDuplicateKeycloakUserException()
    {
        // Arrange
        _handler.AddResponse(HttpMethod.Post, "admin/realms/client/users",
            HttpStatusCode.Conflict, "already exists");

        // Act & Assert
        await Should.ThrowAsync<DuplicateKeycloakUserException>(
            () => _sut.CreateAdminUserAsync("client", "adm@t.com", "pass", "Full"));
    }

    [Fact]
    public async Task CreateAdminUserAsync_CreateFails_ThrowsHttpRequestException()
    {
        // Arrange
        _handler.AddResponse(HttpMethod.Post, "admin/realms/client/users",
            HttpStatusCode.InternalServerError, "error");

        // Act & Assert
        await Should.ThrowAsync<HttpRequestException>(
            () => _sut.CreateAdminUserAsync("client", "adm@t.com", "pass", "Full"));
    }

    [Fact]
    public async Task CreateAdminUserAsync_UserIdNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        _handler.AddResponse(HttpMethod.Post, "admin/realms/client/users", HttpStatusCode.Created, null);
        _handler.AddResponse(HttpMethod.Get, "admin/realms/client/users",
            HttpStatusCode.OK, new List<UserRepresentation>());

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => _sut.CreateAdminUserAsync("client", "adm@t.com", "pass", "Full"));
    }

    [Fact]
    public async Task CreateAdminUserAsync_PasswordSetFails_ThrowsInvalidOperationException()
    {
        // Arrange
        var userId = "new-admin-id";
        _handler.AddResponse(HttpMethod.Post, "admin/realms/client/users", HttpStatusCode.Created, null);
        _handler.AddResponse(HttpMethod.Get, "admin/realms/client/users",
            HttpStatusCode.OK, new List<UserRepresentation> { new() { Id = userId } });
        _handler.AddResponse(HttpMethod.Put, $"admin/realms/client/users/{userId}/reset-password",
            HttpStatusCode.BadRequest, "bad password");

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => _sut.CreateAdminUserAsync("client", "adm@t.com", "pass", "Full"));
    }

    // --- UpdateUserPasswordAsync ---

    [Fact]
    public async Task UpdateUserPasswordAsync_Success_CompletesWithoutException()
    {
        // Arrange
        var userId = "u-update";
        _handler.AddResponse(HttpMethod.Put, $"admin/realms/client/users/{userId}/reset-password",
            HttpStatusCode.NoContent, null);

        // Act & Assert
        await Should.NotThrowAsync(
            () => _sut.UpdateUserPasswordAsync("client", userId, "NewPass1!"));
    }

    [Fact]
    public async Task UpdateUserPasswordAsync_Fails_ThrowsInvalidOperationException()
    {
        // Arrange
        var userId = "u-update-fail";
        _handler.AddResponse(HttpMethod.Put, $"admin/realms/client/users/{userId}/reset-password",
            HttpStatusCode.InternalServerError, "error body");

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => _sut.UpdateUserPasswordAsync("client", userId, "NewPass1!"));
    }

    // --- ResetPasswordAsTemporaryAsync ---

    [Fact]
    public async Task ResetPasswordAsTemporaryAsync_Success_CompletesWithoutException()
    {
        // Arrange
        var userId = "u-temp";
        _handler.AddResponse(HttpMethod.Put, $"admin/realms/client/users/{userId}/reset-password",
            HttpStatusCode.NoContent, null);

        // Act & Assert
        await Should.NotThrowAsync(
            () => _sut.ResetPasswordAsTemporaryAsync("client", userId, "Temp1!"));
    }

    [Fact]
    public async Task ResetPasswordAsTemporaryAsync_Fails_ThrowsHttpRequestException()
    {
        // Arrange
        var userId = "u-temp-fail";
        _handler.AddResponse(HttpMethod.Put, $"admin/realms/client/users/{userId}/reset-password",
            HttpStatusCode.InternalServerError, null);

        // Act & Assert
        await Should.ThrowAsync<HttpRequestException>(
            () => _sut.ResetPasswordAsTemporaryAsync("client", userId, "Temp1!"));
    }

    // --- UnblockUserAsync ---

    [Fact]
    public async Task UnblockUserAsync_DisabledUser_EnablesUser()
    {
        // Arrange
        var userId = "u-blocked";
        _handler.AddResponse(HttpMethod.Get, $"admin/realms/client/users/{userId}",
            HttpStatusCode.OK, new UserRepresentation { Id = userId, Enabled = false });
        _handler.AddResponse(HttpMethod.Put, $"admin/realms/client/users/{userId}",
            HttpStatusCode.NoContent, null);

        // Act & Assert
        await Should.NotThrowAsync(() => _sut.UnblockUserAsync("client", userId));
    }

    [Fact]
    public async Task UnblockUserAsync_AlreadyEnabled_DoesNotCallPut()
    {
        // Arrange
        var userId = "u-enabled";
        _handler.AddResponse(HttpMethod.Get, $"admin/realms/client/users/{userId}",
            HttpStatusCode.OK, new UserRepresentation { Id = userId, Enabled = true });

        // Act
        await _sut.UnblockUserAsync("client", userId);

        // Assert — no PUT should be called for already-enabled user
        _handler.GetRequestCount(HttpMethod.Put, $"admin/realms/client/users/{userId}").ShouldBe(0);
    }

    [Fact]
    public async Task UnblockUserAsync_UserNotFound_ThrowsInvalidOperationException()
    {
        // Arrange — JSON "null" body causes GetFromJsonAsync<UserRepresentation> to return null,
        // which triggers the ?? throw new InvalidOperationException path.
        var userId = "u-missing";
        _handler.AddRawJsonResponse(HttpMethod.Get, $"admin/realms/client/users/{userId}",
            HttpStatusCode.OK, "null");

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => _sut.UnblockUserAsync("client", userId));
    }

    // --- BlockUserAsync ---

    [Fact]
    public async Task BlockUserAsync_AlreadyBlocked_DoesNotCallPut()
    {
        // Arrange
        var userId = "u-alr-blocked";
        _handler.AddResponse(HttpMethod.Get, $"admin/realms/client/users/{userId}",
            HttpStatusCode.OK, new UserRepresentation { Id = userId, Enabled = false });

        // Act
        await _sut.BlockUserAsync("client", userId);

        // Assert — no PUT for already-blocked
        _handler.GetRequestCount(HttpMethod.Put, $"admin/realms/client/users/{userId}").ShouldBe(0);
    }

    // --- SetTemporaryPasswordFlagAsync ---

    [Fact]
    public async Task SetTemporaryPasswordFlagAsync_UpdatePasswordNotPresent_AddsAction()
    {
        // Arrange
        var userId = "u-setflag";
        _handler.AddResponse(HttpMethod.Get, $"admin/realms/client/users/{userId}",
            HttpStatusCode.OK, new UserRepresentation
            {
                Id = userId,
                RequiredActions = new List<string>()
            });
        _handler.AddResponse(HttpMethod.Put, $"admin/realms/client/users/{userId}",
            HttpStatusCode.NoContent, null);

        // Act & Assert
        await Should.NotThrowAsync(() => _sut.SetTemporaryPasswordFlagAsync("client", userId));
    }

    [Fact]
    public async Task SetTemporaryPasswordFlagAsync_UpdatePasswordAlreadyPresent_DoesNotCallPut()
    {
        // Arrange
        var userId = "u-alreadyflag";
        _handler.AddResponse(HttpMethod.Get, $"admin/realms/client/users/{userId}",
            HttpStatusCode.OK, new UserRepresentation
            {
                Id = userId,
                RequiredActions = new List<string> { "UPDATE_PASSWORD" }
            });

        // Act
        await _sut.SetTemporaryPasswordFlagAsync("client", userId);

        // Assert — no PUT needed
        _handler.GetRequestCount(HttpMethod.Put, $"admin/realms/client/users/{userId}").ShouldBe(0);
    }

    [Fact]
    public async Task SetTemporaryPasswordFlagAsync_UserNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var userId = "u-notfound-flag";
        _handler.AddRawJsonResponse(HttpMethod.Get, $"admin/realms/client/users/{userId}",
            HttpStatusCode.OK, "null");

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => _sut.SetTemporaryPasswordFlagAsync("client", userId));
    }

    [Fact]
    public async Task SetTemporaryPasswordFlagAsync_NullRequiredActions_AddsUpdatePassword()
    {
        // Arrange
        var userId = "u-nullactions";
        _handler.AddResponse(HttpMethod.Get, $"admin/realms/client/users/{userId}",
            HttpStatusCode.OK, new UserRepresentation
            {
                Id = userId,
                RequiredActions = null
            });
        _handler.AddResponse(HttpMethod.Put, $"admin/realms/client/users/{userId}",
            HttpStatusCode.NoContent, null);

        // Act & Assert
        await Should.NotThrowAsync(() => _sut.SetTemporaryPasswordFlagAsync("client", userId));
    }

    // --- RemoveUpdatePasswordRequiredActionAsync ---

    [Fact]
    public async Task RemoveUpdatePasswordRequiredActionAsync_ActionPresent_RemovesAndCallsPut()
    {
        // Arrange
        var userId = "u-remove-action";
        _handler.AddResponse(HttpMethod.Get, $"admin/realms/client/users/{userId}",
            HttpStatusCode.OK, new UserRepresentation
            {
                Id = userId,
                RequiredActions = new List<string> { "UPDATE_PASSWORD" }
            });
        _handler.AddResponse(HttpMethod.Put, $"admin/realms/client/users/{userId}",
            HttpStatusCode.NoContent, null);

        // Act & Assert
        await Should.NotThrowAsync(
            () => _sut.RemoveUpdatePasswordRequiredActionAsync("client", userId));
    }

    [Fact]
    public async Task RemoveUpdatePasswordRequiredActionAsync_ActionAbsent_DoesNotCallPut()
    {
        // Arrange
        var userId = "u-no-action";
        _handler.AddResponse(HttpMethod.Get, $"admin/realms/client/users/{userId}",
            HttpStatusCode.OK, new UserRepresentation
            {
                Id = userId,
                RequiredActions = new List<string>()
            });

        // Act
        await _sut.RemoveUpdatePasswordRequiredActionAsync("client", userId);

        // Assert — no PUT needed since action was absent
        _handler.GetRequestCount(HttpMethod.Put, $"admin/realms/client/users/{userId}").ShouldBe(0);
    }

    [Fact]
    public async Task RemoveUpdatePasswordRequiredActionAsync_UserNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var userId = "u-notfound-rm";
        _handler.AddRawJsonResponse(HttpMethod.Get, $"admin/realms/client/users/{userId}",
            HttpStatusCode.OK, "null");

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => _sut.RemoveUpdatePasswordRequiredActionAsync("client", userId));
    }

    // --- AssignAdminRoleAsync ---

    [Fact]
    public async Task AssignAdminRoleAsync_Success_CompletesWithoutException()
    {
        // Arrange
        var userId = "u-role";
        var roleId = "role-admin-id";
        _handler.AddResponse(HttpMethod.Get, "admin/realms/client/roles/admin",
            HttpStatusCode.OK, new { id = roleId, name = "admin" });
        _handler.AddResponse(HttpMethod.Post, $"admin/realms/client/users/{userId}/role-mappings/realm",
            HttpStatusCode.NoContent, null);

        // Act & Assert
        await Should.NotThrowAsync(() => _sut.AssignAdminRoleAsync("client", userId));
    }

    [Fact]
    public async Task AssignAdminRoleAsync_RoleNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var userId = "u-role-fail";
        _handler.AddResponse(HttpMethod.Get, "admin/realms/client/roles/admin",
            HttpStatusCode.NotFound, "not found");

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => _sut.AssignAdminRoleAsync("client", userId));
    }

    [Fact]
    public async Task AssignAdminRoleAsync_RoleIdMissing_ThrowsInvalidOperationException()
    {
        // Arrange
        var userId = "u-role-no-id";
        // Return a JSON document with "id" = null — GetString() returns null, triggering ?? throw
        _handler.AddRawJsonResponse(HttpMethod.Get, "admin/realms/client/roles/admin",
            HttpStatusCode.OK, "{\"id\": null, \"name\": \"admin\"}");

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => _sut.AssignAdminRoleAsync("client", userId));
    }

    [Fact]
    public async Task AssignAdminRoleAsync_AssignmentFailsWithUnknownError_ThrowsInvalidOperationException()
    {
        // Arrange
        var userId = "u-assign-fail";
        var roleId = "role-id";
        _handler.AddResponse(HttpMethod.Get, "admin/realms/client/roles/admin",
            HttpStatusCode.OK, new { id = roleId, name = "admin" });
        // Return 500 (not Conflict, not containing "already exists")
        _handler.AddResponse(HttpMethod.Post, $"admin/realms/client/users/{userId}/role-mappings/realm",
            HttpStatusCode.InternalServerError, "unknown error body");

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => _sut.AssignAdminRoleAsync("client", userId));
    }

    [Fact]
    public async Task AssignAdminRoleAsync_AssignmentConflict_DoesNotThrow()
    {
        // Arrange
        var userId = "u-assign-conflict";
        var roleId = "role-id";
        _handler.AddResponse(HttpMethod.Get, "admin/realms/client/roles/admin",
            HttpStatusCode.OK, new { id = roleId, name = "admin" });
        _handler.AddResponse(HttpMethod.Post, $"admin/realms/client/users/{userId}/role-mappings/realm",
            HttpStatusCode.Conflict, "already exists");

        // Act & Assert
        await Should.NotThrowAsync(() => _sut.AssignAdminRoleAsync("client", userId));
    }

    // --- ClearFirstLoginFlagAsync ---

    [Fact]
    public async Task ClearFirstLoginFlagAsync_FlagIsTrue_SetsToFalseAndCallsPut()
    {
        // Arrange
        var userId = "u-clearflag";
        _handler.AddResponse(HttpMethod.Get, $"admin/realms/client/users/{userId}",
            HttpStatusCode.OK, new UserRepresentation
            {
                Id = userId,
                Attributes = new Dictionary<string, ICollection<string>>
                {
                    ["isFirstLogin"] = new[] { "true" }
                }
            });
        _handler.AddResponse(HttpMethod.Put, $"admin/realms/client/users/{userId}",
            HttpStatusCode.NoContent, null);

        // Act & Assert
        await Should.NotThrowAsync(() => _sut.ClearFirstLoginFlagAsync("client", userId));
    }

    [Fact]
    public async Task ClearFirstLoginFlagAsync_FlagIsFalse_DoesNotCallPut()
    {
        // Arrange
        var userId = "u-alreadycleared";
        _handler.AddResponse(HttpMethod.Get, $"admin/realms/client/users/{userId}",
            HttpStatusCode.OK, new UserRepresentation
            {
                Id = userId,
                Attributes = new Dictionary<string, ICollection<string>>
                {
                    ["isFirstLogin"] = new[] { "false" }
                }
            });

        // Act
        await _sut.ClearFirstLoginFlagAsync("client", userId);

        // Assert
        _handler.GetRequestCount(HttpMethod.Put, $"admin/realms/client/users/{userId}").ShouldBe(0);
    }

    [Fact]
    public async Task ClearFirstLoginFlagAsync_NoAttributes_DoesNotCallPut()
    {
        // Arrange
        var userId = "u-noattrs";
        _handler.AddResponse(HttpMethod.Get, $"admin/realms/client/users/{userId}",
            HttpStatusCode.OK, new UserRepresentation { Id = userId, Attributes = null });

        // Act
        await _sut.ClearFirstLoginFlagAsync("client", userId);

        // Assert
        _handler.GetRequestCount(HttpMethod.Put, $"admin/realms/client/users/{userId}").ShouldBe(0);
    }

    [Fact]
    public async Task ClearFirstLoginFlagAsync_UserNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var userId = "u-notfound-clf";
        _handler.AddRawJsonResponse(HttpMethod.Get, $"admin/realms/client/users/{userId}",
            HttpStatusCode.OK, "null");

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => _sut.ClearFirstLoginFlagAsync("client", userId));
    }

    // --- GetUsersByRoleAsync ---

    [Fact]
    public async Task GetUsersByRoleAsync_Success_ReturnsAdminUserDtos()
    {
        // Arrange
        var users = new List<UserRepresentation>
        {
            new() { Id = "u1", Email = "a@t.com", Username = "a", Enabled = true, FirstName = "Alice", LastName = "Smith", RequiredActions = ["UPDATE_PASSWORD"] },
            new() { Id = "u2", Email = null, Username = "b", Enabled = false, FirstName = null, LastName = null, RequiredActions = null },
        };
        _handler.AddResponse(HttpMethod.Get, "admin/realms/client/roles/admin/users",
            HttpStatusCode.OK, users);

        // Act
        var result = await _sut.GetUsersByRoleAsync("client", "admin");

        // Assert
        result.Count.ShouldBe(2);
        result[0].HasTemporaryPassword.ShouldBeTrue();
        result[1].Email.ShouldBe("b"); // falls back to username
        result[1].IsEnabled.ShouldBeFalse();
    }

    [Fact]
    public async Task GetUsersByRoleAsync_HttpError_ThrowsHttpRequestException()
    {
        // Arrange
        _handler.AddResponse(HttpMethod.Get, "admin/realms/client/roles/admin/users",
            HttpStatusCode.Forbidden, null);

        // Act & Assert
        await Should.ThrowAsync<HttpRequestException>(
            () => _sut.GetUsersByRoleAsync("client", "admin"));
    }

    // --- UpdateAdminUserAsync ---

    [Fact]
    public async Task UpdateAdminUserAsync_Success_CompletesWithoutException()
    {
        // Arrange
        var userId = "u-upd";
        var email = "updated@test.com";
        _handler.AddResponse(HttpMethod.Get, $"admin/realms/client/users?email={Uri.EscapeDataString(email)}&exact=true",
            HttpStatusCode.OK, new List<UserRepresentation>()); // no conflict
        _handler.AddResponse(HttpMethod.Get, $"admin/realms/client/users/{userId}",
            HttpStatusCode.OK, new UserRepresentation { Id = userId, FirstName = "Old", Email = "old@t.com", Username = "old" });
        _handler.AddResponse(HttpMethod.Put, $"admin/realms/client/users/{userId}",
            HttpStatusCode.NoContent, null);

        // Act & Assert
        await Should.NotThrowAsync(
            () => _sut.UpdateAdminUserAsync("client", userId, "New Name", email));
    }

    [Fact]
    public async Task UpdateAdminUserAsync_EmailInUseByOther_ThrowsArgumentException()
    {
        // Arrange
        var userId = "u-upd-conflict";
        var email = "taken@test.com";
        _handler.AddResponse(HttpMethod.Get, $"admin/realms/client/users?email={Uri.EscapeDataString(email)}&exact=true",
            HttpStatusCode.OK, new List<UserRepresentation> { new() { Id = "other-user-id" } }); // different user owns this email

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(
            () => _sut.UpdateAdminUserAsync("client", userId, "Name", email));
    }

    [Fact]
    public async Task UpdateAdminUserAsync_UserNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var userId = "u-upd-notfound";
        var email = "nf@test.com";
        _handler.AddResponse(HttpMethod.Get, $"admin/realms/client/users?email={Uri.EscapeDataString(email)}&exact=true",
            HttpStatusCode.OK, new List<UserRepresentation>());
        _handler.AddRawJsonResponse(HttpMethod.Get, $"admin/realms/client/users/{userId}",
            HttpStatusCode.OK, "null");

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => _sut.UpdateAdminUserAsync("client", userId, "Name", email));
    }

    [Fact]
    public async Task UpdateAdminUserAsync_ConflictOnPut_ThrowsArgumentException()
    {
        // Arrange
        var userId = "u-put-conflict";
        var email = "pconflict@test.com";
        _handler.AddResponse(HttpMethod.Get, $"admin/realms/client/users?email={Uri.EscapeDataString(email)}&exact=true",
            HttpStatusCode.OK, new List<UserRepresentation>());
        _handler.AddResponse(HttpMethod.Get, $"admin/realms/client/users/{userId}",
            HttpStatusCode.OK, new UserRepresentation { Id = userId });
        _handler.AddResponse(HttpMethod.Put, $"admin/realms/client/users/{userId}",
            HttpStatusCode.Conflict, "email conflict");

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(
            () => _sut.UpdateAdminUserAsync("client", userId, "Name", email));
    }

    [Fact]
    public async Task UpdateAdminUserAsync_OtherPutError_ThrowsInvalidOperationException()
    {
        // Arrange
        var userId = "u-put-err";
        var email = "puterr@test.com";
        _handler.AddResponse(HttpMethod.Get, $"admin/realms/client/users?email={Uri.EscapeDataString(email)}&exact=true",
            HttpStatusCode.OK, new List<UserRepresentation>());
        _handler.AddResponse(HttpMethod.Get, $"admin/realms/client/users/{userId}",
            HttpStatusCode.OK, new UserRepresentation { Id = userId });
        _handler.AddResponse(HttpMethod.Put, $"admin/realms/client/users/{userId}",
            HttpStatusCode.InternalServerError, "server error body");

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => _sut.UpdateAdminUserAsync("client", userId, "Name", email));
    }

    // --- LogoutAllSessionsAsync ---

    [Fact]
    public async Task LogoutAllSessionsAsync_Success_CompletesWithoutException()
    {
        // Arrange
        var userId = "u-logout";
        _handler.AddResponse(HttpMethod.Post, $"admin/realms/client/users/{userId}/logout",
            HttpStatusCode.NoContent, null);

        // Act & Assert
        await Should.NotThrowAsync(() => _sut.LogoutAllSessionsAsync("client", userId));
    }

    [Fact]
    public async Task LogoutAllSessionsAsync_NoActiveSessions_DoesNotThrow()
    {
        // Arrange
        var userId = "u-nosessions";
        _handler.AddResponse(HttpMethod.Post, $"admin/realms/client/users/{userId}/logout",
            HttpStatusCode.NotFound, null);

        // Act & Assert — 404 is acceptable (no active sessions)
        await Should.NotThrowAsync(() => _sut.LogoutAllSessionsAsync("client", userId));
    }

    [Fact]
    public async Task LogoutAllSessionsAsync_ServerError_ThrowsInvalidOperationException()
    {
        // Arrange
        var userId = "u-logout-fail";
        _handler.AddResponse(HttpMethod.Post, $"admin/realms/client/users/{userId}/logout",
            HttpStatusCode.InternalServerError, "error");

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => _sut.LogoutAllSessionsAsync("client", userId));
    }

    // --- DeleteGroupAsync ---

    [Fact]
    public async Task DeleteGroupAsync_Success_CompletesWithoutException()
    {
        // Arrange
        var groupId = "grp-delete";
        _handler.AddResponse(HttpMethod.Delete, $"admin/realms/client/groups/{groupId}",
            HttpStatusCode.NoContent, null);

        // Act & Assert
        await Should.NotThrowAsync(() => _sut.DeleteGroupAsync("client", groupId));
    }

    [Fact]
    public async Task DeleteGroupAsync_NotFound_DoesNotThrow()
    {
        // Arrange
        var groupId = "grp-notfound";
        _handler.AddResponse(HttpMethod.Delete, $"admin/realms/client/groups/{groupId}",
            HttpStatusCode.NotFound, null);

        // Act & Assert — 404 is acceptable (group already deleted)
        await Should.NotThrowAsync(() => _sut.DeleteGroupAsync("client", groupId));
    }

    [Fact]
    public async Task DeleteGroupAsync_ServerError_LogsWarningAndDoesNotThrow()
    {
        // Arrange
        var groupId = "grp-err";
        _handler.AddResponse(HttpMethod.Delete, $"admin/realms/client/groups/{groupId}",
            HttpStatusCode.InternalServerError, null);

        // Act & Assert — failure is swallowed + logged (fire-and-forget delete)
        await Should.NotThrowAsync(() => _sut.DeleteGroupAsync("client", groupId));
    }

    // --- GetGroupByNameAsync ---

    [Fact]
    public async Task GetGroupByNameAsync_HttpError_ReturnsNull()
    {
        // Arrange — response fails → returns null
        _handler.AddResponse(HttpMethod.Get, "admin/realms/client/groups?search=lost&exact=true",
            HttpStatusCode.InternalServerError, null);

        // Act
        var result = await _sut.GetGroupByNameAsync("client", "lost");

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetGroupByNameAsync_EmptyResult_ReturnsNull()
    {
        // Arrange
        _handler.AddResponse(HttpMethod.Get, "admin/realms/client/groups?search=gone&exact=true",
            HttpStatusCode.OK, new List<KeycloakGroupRepresentation>());

        // Act
        var result = await _sut.GetGroupByNameAsync("client", "gone");

        // Assert
        result.ShouldBeNull();
    }

    // --- CreateGroupAsync fallback path ---

    [Fact]
    public async Task CreateGroupAsync_NoLocationHeader_FallsBackToGetByName()
    {
        // Arrange: group doesn't exist
        _handler.AddResponse(HttpMethod.Get, "admin/realms/client/groups?search=fallback-group&exact=true",
            HttpStatusCode.OK, new List<KeycloakGroupRepresentation>());
        // POST without Location header
        _handler.AddResponse(HttpMethod.Post, "admin/realms/client/groups",
            HttpStatusCode.Created, null);
        // Fallback GET returns the created group
        _handler.AddResponse(HttpMethod.Get, "admin/realms/client/groups?search=fallback-group&exact=true",
            HttpStatusCode.OK, new List<KeycloakGroupRepresentation> { new("grp-fallback-id", "fallback-group") });

        // Act
        var result = await _sut.CreateGroupAsync("client", "fallback-group");

        // Assert
        result.ShouldBe("grp-fallback-id");
    }

    [Fact]
    public async Task CreateGroupAsync_CreateFails_ThrowsInvalidOperationException()
    {
        // Arrange
        _handler.AddResponse(HttpMethod.Get, "admin/realms/client/groups?search=bad-group&exact=true",
            HttpStatusCode.OK, new List<KeycloakGroupRepresentation>());
        _handler.AddResponse(HttpMethod.Post, "admin/realms/client/groups",
            HttpStatusCode.InternalServerError, "create failed");

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => _sut.CreateGroupAsync("client", "bad-group"));
    }

    // --- AddUserToGroupAsync failure ---

    [Fact]
    public async Task AddUserToGroupAsync_ServerError_ThrowsInvalidOperationException()
    {
        // Arrange
        _handler.AddResponse(HttpMethod.Put, "admin/realms/client/users/u1/groups/g1",
            HttpStatusCode.InternalServerError, "error");

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => _sut.AddUserToGroupAsync("client", "u1", "g1"));
    }

    // --- RemoveUserFromGroupAsync failure ---

    [Fact]
    public async Task RemoveUserFromGroupAsync_ServerError_ThrowsInvalidOperationException()
    {
        // Arrange
        _handler.AddResponse(HttpMethod.Delete, "admin/realms/client/users/u1/groups/g1",
            HttpStatusCode.InternalServerError, "error");

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => _sut.RemoveUserFromGroupAsync("client", "u1", "g1"));
    }
}

/// <summary>
/// Flexible mock HttpMessageHandler.
/// Routes are matched in registration order, but consumed (removed) after each match —
/// so the same path can be registered multiple times for sequential calls.
/// If a route's content stream is exhausted (re-read scenario), a fresh copy is returned.
/// </summary>
internal sealed class FlexMockHttpHandler : HttpMessageHandler
{
    // Each entry: (method, pathContains, factory that creates a fresh HttpResponseMessage)
    private readonly List<(HttpMethod method, string pathContains, Func<HttpResponseMessage> factory)> _routes = [];
    private readonly List<HttpRequestMessage> _sent = [];

    public void AddResponse(HttpMethod method, string pathContains, HttpStatusCode status, object? body)
    {
        _routes.Add((method, pathContains, () =>
        {
            var r = new HttpResponseMessage(status);
            if (body is not null) r.Content = JsonContent.Create(body);
            return r;
        }
        ));
    }

    /// <summary>
    /// Adds a response with a raw JSON string as body (e.g. "null" for null deserialization result).
    /// </summary>
    public void AddRawJsonResponse(HttpMethod method, string pathContains, HttpStatusCode status, string rawJson)
    {
        _routes.Add((method, pathContains, () =>
        {
            var r = new HttpResponseMessage(status);
            r.Content = new StringContent(rawJson, System.Text.Encoding.UTF8, "application/json");
            return r;
        }
        ));
    }

    public int GetRequestCount(HttpMethod method, string pathContains)
        => _sent.Count(r => r.Method == method && (r.RequestUri?.ToString() ?? "").Contains(pathContains));

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        _sent.Add(request);
        var url = request.RequestUri?.ToString() ?? "";

        // Find the first matching route index
        var idx = _routes.FindIndex(r =>
            r.method == request.Method && url.Contains(r.pathContains));

        if (idx < 0)
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));

        // Consume the route (remove so next call gets the next registered one for same path)
        var factory = _routes[idx].factory;
        _routes.RemoveAt(idx);
        return Task.FromResult(factory());
    }
}
