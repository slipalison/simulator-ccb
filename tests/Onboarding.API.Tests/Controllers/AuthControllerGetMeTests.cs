using System.Text.Json;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Onboarding.API.Controllers;
using Onboarding.API.Tests.Authentication;
using Onboarding.Application.Auth.Commands;
using Onboarding.Application.Auth.DTOs;
using Onboarding.Application.Common;
using Onboarding.Domain.Aggregates.CompanyAggregate;
using Onboarding.Domain.Aggregates.EmployeeAggregate;
using Onboarding.Domain.Repositories;
using Onboarding.Infrastructure.Keycloak;
using Shouldly;
using static Onboarding.Domain.Aggregates.CompanyAggregate.TermsAcceptance;

namespace Onboarding.API.Tests.Controllers;

/// <summary>
/// Unit tests for AuthController.GetMe() — Phase 55 refactor (D-60..D-63).
/// Controller now uses ICommandDispatcher + IValidationRunner (3 ctor deps).
/// Repos passed directly to GetMe() [FromServices] params.
/// </summary>
public sealed class AuthControllerGetMeTests
{
    // -------------------------------------------------------------------------
    // Shared mocks
    // -------------------------------------------------------------------------

    private readonly ICommandDispatcher _commands = Substitute.For<ICommandDispatcher>();
    private readonly IValidationRunner _validation = Substitute.For<IValidationRunner>();
    private readonly ICompanyRepository _companyRepo = Substitute.For<ICompanyRepository>();
    private readonly IEmployeeRepository _employeeRepo = Substitute.For<IEmployeeRepository>();
    private readonly IAccessGroupRepository _accessGroupRepo = Substitute.For<IAccessGroupRepository>();

    private static readonly string KnownSub = Guid.NewGuid().ToString();
    private static readonly string FakeAccessToken = FakeJwtTokenHelper.GenerateFakeJwt("user@test.com", KnownSub);
    private static readonly TokenResponse FakeTokens = new(
        FakeAccessToken, "refresh-xyz", 300, "Bearer", 1800, "openid");

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private AuthController BuildSut()
    {
        // Default: validation passes
        _validation.Validate(Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        var sut = new AuthController(
            _commands,
            _validation,
            Substitute.For<ILogger<AuthController>>());

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Cookie = "refreshToken=valid-token";

        sut.ControllerContext = new ControllerContext { HttpContext = httpContext };
        return sut;
    }

    private static IReadOnlyList<string> ExtractPermissions(IActionResult result)
    {
        var ok = result.ShouldBeOfType<OkObjectResult>();
        var json = JsonSerializer.Serialize(ok.Value);
        using var doc = JsonDocument.Parse(json);
        var found = doc.RootElement.TryGetProperty("Permissions", out var perms)
            || doc.RootElement.TryGetProperty("permissions", out perms);
        found.ShouldBeTrue("permissions property must exist in /auth/me response");
        var list = new List<string>();
        foreach (var item in perms.EnumerateArray())
            list.Add(item.GetString()!);
        return list;
    }

    private void StubRefreshSuccess()
    {
        _commands.Send<TokenResponse>(Arg.Any<RefreshTokenCommand>(), Arg.Any<CancellationToken>())
            .Returns(FakeTokens);
    }

    // =========================================================================
    // GetMe — permissions[] populated
    // =========================================================================

    [Fact]
    public async Task GetMe_CompanyOwner_ReturnsAllPermissions()
    {
        // Arrange
        StubRefreshSuccess();

        var terms = TermsAcceptance.Create("1.0", "127.0.0.1");
        var company = Company.Register("Empresa Teste", "11222333000181", "owner@test.com", "11999999999", terms);
        company.SetKeycloakUserId(KnownSub);
        _companyRepo.GetByKeycloakSubAsync(KnownSub, Arg.Any<CancellationToken>())
            .Returns(company);

        var sut = BuildSut();

        // Act
        var result = await sut.GetMe(_companyRepo, _employeeRepo, _accessGroupRepo, CancellationToken.None);

        // Assert
        var permissions = ExtractPermissions(result);
        permissions.ShouldContain(Permissions.FundsRead);
        permissions.ShouldContain(Permissions.FundsWrite);
        permissions.ShouldContain(Permissions.FundsDelete);
        permissions.ShouldContain(Permissions.FundsManage);
        permissions.ShouldContain(Permissions.EmployeesRead);
        permissions.ShouldContain(Permissions.AccessGroupsManage);
        permissions.Count.ShouldBe(Permissions.All.Length);
    }

    [Fact]
    public async Task GetMe_EmployeeWithFundsReadOnly_ReturnsFundsReadPermission()
    {
        // Arrange
        StubRefreshSuccess();

        _companyRepo.GetByKeycloakSubAsync(KnownSub, Arg.Any<CancellationToken>())
            .Returns((Company?)null);

        var companyId = Guid.NewGuid();
        var accessGroupId = Guid.NewGuid();
        var viewerGroup = AccessGroup.Create(companyId, "viewer", [Permissions.FundsRead]);

        var employee = Employee.Register("João Viewer", "12345678909", "viewer@test.com", "11888888888", companyId, accessGroupId);
        employee.SetKeycloakUserId(KnownSub);

        _employeeRepo.GetByKeycloakSubAsync(KnownSub, Arg.Any<CancellationToken>())
            .Returns(employee);
        _accessGroupRepo.GetByIdAsync(accessGroupId, Arg.Any<CancellationToken>())
            .Returns(viewerGroup);

        var sut = BuildSut();

        // Act
        var result = await sut.GetMe(_companyRepo, _employeeRepo, _accessGroupRepo, CancellationToken.None);

        // Assert
        var permissions = ExtractPermissions(result);
        permissions.ShouldContain(Permissions.FundsRead);
        permissions.ShouldNotContain(Permissions.FundsWrite);
        permissions.ShouldNotContain(Permissions.FundsDelete);
        permissions.ShouldNotContain(Permissions.FundsManage);
        permissions.Count.ShouldBe(1);
    }

    [Fact]
    public async Task GetMe_NoCompanyAndNoEmployee_ReturnsEmptyPermissions()
    {
        // Arrange
        StubRefreshSuccess();

        _companyRepo.GetByKeycloakSubAsync(KnownSub, Arg.Any<CancellationToken>())
            .Returns((Company?)null);
        _employeeRepo.GetByKeycloakSubAsync(KnownSub, Arg.Any<CancellationToken>())
            .Returns((Employee?)null);

        var sut = BuildSut();

        // Act
        var result = await sut.GetMe(_companyRepo, _employeeRepo, _accessGroupRepo, CancellationToken.None);

        // Assert
        var permissions = ExtractPermissions(result);
        permissions.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetMe_ResponseShape_ContainsAllExpectedFields()
    {
        // Arrange
        StubRefreshSuccess();
        _companyRepo.GetByKeycloakSubAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Company?)null);
        _employeeRepo.GetByKeycloakSubAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Employee?)null);

        var sut = BuildSut();

        // Act
        var result = await sut.GetMe(_companyRepo, _employeeRepo, _accessGroupRepo, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        var json = JsonSerializer.Serialize(ok.Value);
        using var doc = JsonDocument.Parse(json);

        (doc.RootElement.TryGetProperty("AccessToken", out _) || doc.RootElement.TryGetProperty("accessToken", out _))
            .ShouldBeTrue("accessToken field missing");
        (doc.RootElement.TryGetProperty("ExpiresIn", out _) || doc.RootElement.TryGetProperty("expiresIn", out _))
            .ShouldBeTrue("expiresIn field missing");
        (doc.RootElement.TryGetProperty("TokenType", out _) || doc.RootElement.TryGetProperty("tokenType", out _))
            .ShouldBeTrue("tokenType field missing");
        (doc.RootElement.TryGetProperty("Scope", out _) || doc.RootElement.TryGetProperty("scope", out _))
            .ShouldBeTrue("scope field missing");
        (doc.RootElement.TryGetProperty("Permissions", out _) || doc.RootElement.TryGetProperty("permissions", out _))
            .ShouldBeTrue("permissions field missing");
    }

    // =========================================================================
    // GetMe — session validation paths
    // =========================================================================

    [Fact]
    public async Task GetMe_WithoutCookie_Returns401()
    {
        // Arrange — no cookie
        _validation.Validate(Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        var sut = new AuthController(
            _commands,
            _validation,
            Substitute.For<ILogger<AuthController>>());
        sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext() // no cookie
        };

        // Act
        var result = await sut.GetMe(_companyRepo, _employeeRepo, _accessGroupRepo, CancellationToken.None);

        // Assert
        var unauthorized = result.ShouldBeOfType<UnauthorizedObjectResult>();
        unauthorized.StatusCode.ShouldBe(StatusCodes.Status401Unauthorized);
    }

    [Fact]
    public async Task GetMe_ExpiredRefreshToken_Returns401AndClearsCookie()
    {
        // Arrange
        _commands.Send<TokenResponse>(Arg.Any<RefreshTokenCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new KeycloakAuthException("expired"));

        var sut = BuildSut();

        // Act
        var result = await sut.GetMe(_companyRepo, _employeeRepo, _accessGroupRepo, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<UnauthorizedObjectResult>()
            .StatusCode.ShouldBe(StatusCodes.Status401Unauthorized);
    }

    [Fact]
    public async Task GetMe_EmployeeWithNoAccessGroup_ReturnsEmptyPermissions()
    {
        // Arrange
        StubRefreshSuccess();

        _companyRepo.GetByKeycloakSubAsync(KnownSub, Arg.Any<CancellationToken>())
            .Returns((Company?)null);

        var companyId = Guid.NewGuid();
        var accessGroupId = Guid.NewGuid();
        var employee = Employee.Register("Orphan Employee", "98765432100", "orphan@test.com", "11777777777", companyId, accessGroupId);
        employee.SetKeycloakUserId(KnownSub);

        _employeeRepo.GetByKeycloakSubAsync(KnownSub, Arg.Any<CancellationToken>())
            .Returns(employee);
        _accessGroupRepo.GetByIdAsync(accessGroupId, Arg.Any<CancellationToken>())
            .Returns((AccessGroup?)null); // access group deleted

        var sut = BuildSut();

        // Act
        var result = await sut.GetMe(_companyRepo, _employeeRepo, _accessGroupRepo, CancellationToken.None);

        // Assert
        var permissions = ExtractPermissions(result);
        permissions.ShouldBeEmpty();
    }
}
