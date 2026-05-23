using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Onboarding.API.Controllers;
using Onboarding.Application.Common;
using Onboarding.Domain.Aggregates.EmployeeAggregate;
using Shouldly;

namespace Onboarding.API.Tests.Controllers;

/// <summary>
/// Unit tests for PermissionsController.GetPermissions() — verifies that the endpoint
/// correctly reflects the permissions already resolved by ClientClaimsMiddleware into
/// ICurrentCompanyPermissionsService (frontend-client-fundos W-arch fix, round 4).
///
/// Strategy: mock ICurrentCompanyPermissionsService with pre-populated state that mirrors
/// what ClientClaimsMiddleware would have set. No real DB or Keycloak required.
/// Authorization ([Authorize]) enforcement is tested via integration tests further below.
/// </summary>
public sealed class AuthControllerPermissionsTests
{
    // -------------------------------------------------------------------------
    // Helper: build SUT with given permissions state
    // -------------------------------------------------------------------------

    private static PermissionsController BuildSut(
        Guid companyId,
        IReadOnlyList<string> permissions,
        bool isOwner = false)
    {
        var service = Substitute.For<ICurrentCompanyPermissionsService>();
        service.CompanyId.Returns(companyId);
        service.Permissions.Returns(permissions);
        service.IsCompanyOwner.Returns(isOwner);

        var controller = new PermissionsController(service);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        return controller;
    }

    private static string[] ExtractPermissions(IActionResult result)
    {
        var ok = result.ShouldBeOfType<OkObjectResult>();
        var json = JsonSerializer.Serialize(ok.Value);
        using var doc = JsonDocument.Parse(json);
        var found = doc.RootElement.TryGetProperty("Permissions", out var perms)
            || doc.RootElement.TryGetProperty("permissions", out perms);
        found.ShouldBeTrue("Permissions property must exist in /auth/permissions response");
        return perms.EnumerateArray().Select(e => e.GetString()!).ToArray();
    }

    private static string? ExtractCompanyId(IActionResult result)
    {
        var ok = result.ShouldBeOfType<OkObjectResult>();
        var json = JsonSerializer.Serialize(ok.Value);
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.TryGetProperty("CompanyId", out var cid)
            || doc.RootElement.TryGetProperty("companyId", out cid))
        {
            return cid.ValueKind == JsonValueKind.Null ? null : cid.GetString();
        }
        return null;
    }

    // =========================================================================
    // Company owner (PJ) — all permissions
    // =========================================================================

    [Fact]
    public void GetPermissions_CompanyOwnerPj_ReturnsAllTenPermissions()
    {
        // Arrange — middleware would have set Permissions.All for a PJ owner
        var companyId = Guid.NewGuid();
        var sut = BuildSut(companyId, Permissions.All, isOwner: true);

        // Act
        var result = sut.GetPermissions();

        // Assert
        var perms = ExtractPermissions(result);
        perms.Length.ShouldBe(Permissions.All.Length);
        perms.ShouldContain(Permissions.FundsRead);
        perms.ShouldContain(Permissions.FundsWrite);
        perms.ShouldContain(Permissions.FundsDelete);
        perms.ShouldContain(Permissions.FundsManage);
        perms.ShouldContain(Permissions.EmployeesRead);
        perms.ShouldContain(Permissions.EmployeesWrite);
        perms.ShouldContain(Permissions.EmployeesDelete);
        perms.ShouldContain(Permissions.AuditRead);
        perms.ShouldContain(Permissions.DashboardAccess);
        perms.ShouldContain(Permissions.AccessGroupsManage);
    }

    [Fact]
    public void GetPermissions_CompanyOwnerPj_CompanyIdPresentInResponse()
    {
        var companyId = Guid.NewGuid();
        var sut = BuildSut(companyId, Permissions.All, isOwner: true);

        var result = sut.GetPermissions();

        var cid = ExtractCompanyId(result);
        cid.ShouldBe(companyId.ToString());
    }

    // =========================================================================
    // Employee with admin-empresa AccessGroup — all permissions (same as PJ owner)
    // =========================================================================

    [Fact]
    public void GetPermissions_EmployeeAdminEmpresa_ReturnsAllPermissions()
    {
        // Arrange — admin-empresa group has Permissions.All (see AccessGroup.CreateDefaultGroups)
        var companyId = Guid.NewGuid();
        var sut = BuildSut(companyId, Permissions.All, isOwner: false);

        var result = sut.GetPermissions();

        var perms = ExtractPermissions(result);
        perms.Length.ShouldBe(10);
    }

    // =========================================================================
    // Employee with viewer AccessGroup — employees:read + audit:read + funds:read
    // =========================================================================

    [Fact]
    public void GetPermissions_EmployeeViewer_ReturnsThreeReadPermissions()
    {
        // Arrange — viewer group: EmployeesRead + AuditRead + FundsRead (D-08)
        var viewerPerms = new[] { Permissions.EmployeesRead, Permissions.AuditRead, Permissions.FundsRead };
        var sut = BuildSut(Guid.NewGuid(), viewerPerms);

        var result = sut.GetPermissions();

        var perms = ExtractPermissions(result);
        perms.Length.ShouldBe(3);
        perms.ShouldContain(Permissions.EmployeesRead);
        perms.ShouldContain(Permissions.AuditRead);
        perms.ShouldContain(Permissions.FundsRead);
        perms.ShouldNotContain(Permissions.FundsWrite);
        perms.ShouldNotContain(Permissions.FundsDelete);
        perms.ShouldNotContain(Permissions.FundsManage);
    }

    // =========================================================================
    // Employee with dashboard AccessGroup — dashboard:access only
    // =========================================================================

    [Fact]
    public void GetPermissions_EmployeeDashboard_ReturnsOnePermission()
    {
        // Arrange — dashboard group: DashboardAccess only (D-08)
        var dashboardPerms = new[] { Permissions.DashboardAccess };
        var sut = BuildSut(Guid.NewGuid(), dashboardPerms);

        var result = sut.GetPermissions();

        var perms = ExtractPermissions(result);
        perms.Length.ShouldBe(1);
        perms.ShouldContain(Permissions.DashboardAccess);
        perms.ShouldNotContain(Permissions.FundsRead);
    }

    // =========================================================================
    // Custom AccessGroup with arbitrary subset of permissions
    // =========================================================================

    [Fact]
    public void GetPermissions_EmployeeCustomGroup_ReturnsSubsetPermissions()
    {
        // Arrange — custom group with funds:read + funds:write only
        var customPerms = new[] { Permissions.FundsRead, Permissions.FundsWrite };
        var sut = BuildSut(Guid.NewGuid(), customPerms);

        var result = sut.GetPermissions();

        var perms = ExtractPermissions(result);
        perms.Length.ShouldBe(2);
        perms.ShouldContain(Permissions.FundsRead);
        perms.ShouldContain(Permissions.FundsWrite);
        perms.ShouldNotContain(Permissions.FundsDelete);
        perms.ShouldNotContain(Permissions.FundsManage);
    }

    // =========================================================================
    // Bearer with valid token but sub not in DB — empty permissions
    // =========================================================================

    [Fact]
    public void GetPermissions_SubNotInDb_ReturnsEmptyPermissionsAndNullCompanyId()
    {
        // Arrange — ClientClaimsMiddleware "no-match" path: CompanyId = Guid.Empty, Permissions = []
        var sut = BuildSut(Guid.Empty, []);

        var result = sut.GetPermissions();

        var perms = ExtractPermissions(result);
        perms.ShouldBeEmpty();
        var cid = ExtractCompanyId(result);
        cid.ShouldBeNull();
    }
}

/// <summary>
/// Integration tests for GET /api/auth/permissions using WebApplicationFactory.
/// Verifies [Authorize(AuthenticationSchemes = "BearerClient")] enforcement.
/// </summary>
public sealed class AuthControllerPermissionsIntegrationTests : IClassFixture<PermissionsTestApiFactory>
{
    private readonly PermissionsTestApiFactory _factory;

    public AuthControllerPermissionsIntegrationTests(PermissionsTestApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetPermissions_WithoutBearer_Returns401()
    {
        var client = _factory.CreateClient();
        // No Authorization header — should be rejected by [Authorize]
        var response = await client.GetAsync("/api/auth/permissions");
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetPermissions_WithValidBearer_Returns200WithPermissionsArray()
    {
        // Arrange — generate a valid fake JWT (BearerClient scheme is configured in test factory)
        var token = Authentication.FakeJwtTokenHelper.GenerateFakeJwt("test@example.com", Guid.NewGuid().ToString());
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync("/api/auth/permissions");

        // Assert — 200 with a permissions array (may be empty if sub not in mock DB)
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        var found = doc.RootElement.TryGetProperty("permissions", out var perms)
            || doc.RootElement.TryGetProperty("Permissions", out perms);
        found.ShouldBeTrue("response must contain permissions array");
        perms.ValueKind.ShouldBe(JsonValueKind.Array);
    }
}
