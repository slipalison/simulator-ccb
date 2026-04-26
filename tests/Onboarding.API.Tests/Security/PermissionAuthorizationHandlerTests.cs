using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Onboarding.API.Security;
using Onboarding.Application.Common;
using Onboarding.Infrastructure.Persistence;
using Shouldly;

namespace Onboarding.API.Tests.Security;

public class PermissionAuthorizationHandlerTests
{
    private readonly CurrentCompanyPermissionsService _permissionsService;

    public PermissionAuthorizationHandlerTests()
    {
        _permissionsService = new CurrentCompanyPermissionsService();
    }

    private ICurrentCompanyPermissionsService AsInterface() => _permissionsService;

    private static bool HasSucceededFor(AuthorizationHandlerContext context, IAuthorizationRequirement requirement)
    {
        // AuthorizationHandlerContext doesn't expose per-requirement success.
        // We check PendingRequirements — if the requirement is no longer pending, it succeeded.
        return !context.PendingRequirements.Contains(requirement);
    }

    [Fact]
    public async Task UserWithPermission_SatisfiesMatchingRequirement()
    {
        // Arrange
        _permissionsService.PermissionList = ["employees:read", "audit:read"];
        var handler = new PermissionAuthorizationHandler(AsInterface());
        var requirement = new PermissionRequirement("employees:read");
        var context = new AuthorizationHandlerContext([requirement], null!, null!);

        // Act
        await handler.HandleAsync(context);

        // Assert
        HasSucceededFor(context, requirement).ShouldBeTrue();
    }

    [Fact]
    public async Task UserWithoutPermission_FailsRequirement()
    {
        // Arrange
        _permissionsService.PermissionList = ["audit:read"];
        var handler = new PermissionAuthorizationHandler(AsInterface());
        var requirement = new PermissionRequirement("employees:read");
        var context = new AuthorizationHandlerContext([requirement], null!, null!);

        // Act
        await handler.HandleAsync(context);

        // Assert — requirement remains pending (handler didn't call Succeed)
        HasSucceededFor(context, requirement).ShouldBeFalse();
    }

    [Fact]
    public async Task CompanyOwner_HasAllPermissions_SatisfiesAnyRequirement()
    {
        // Arrange — PJ owner gets all 6 permissions (D-20)
        _permissionsService.PermissionList = Domain.Aggregates.EmployeeAggregate.Permissions.All.ToList();
        _permissionsService.IsCompanyOwnerFlag = true;
        var handler = new PermissionAuthorizationHandler(AsInterface());
        var requirement = new PermissionRequirement("employees:delete");
        var context = new AuthorizationHandlerContext([requirement], null!, null!);

        // Act
        await handler.HandleAsync(context);

        // Assert
        HasSucceededFor(context, requirement).ShouldBeTrue();
    }

    [Fact]
    public async Task UserWithEmptyPermissions_FailsAllRequirements()
    {
        // Arrange — unknown user gets empty permissions (D-11)
        _permissionsService.PermissionList = [];
        var handler = new PermissionAuthorizationHandler(AsInterface());
        var requirement = new PermissionRequirement("employees:read");
        var context = new AuthorizationHandlerContext([requirement], null!, null!);

        // Act
        await handler.HandleAsync(context);

        // Assert
        HasSucceededFor(context, requirement).ShouldBeFalse();
    }

    [Fact]
    public void CrossCompanyAccessPolicy_IsDefinedForAdminRole()
    {
        // CrossCompanyAccess uses [Authorize(Roles = "admin")] — NOT the permission handler.
        // This test verifies the constant is defined and distinct from permission strings.
        PermissionPolicies.CrossCompanyAccess.ShouldBe("CrossCompanyAccess");
        PermissionPolicies.CrossCompanyAccess.ShouldNotBeOneOf(
            Domain.Aggregates.EmployeeAggregate.Permissions.All);
    }
}