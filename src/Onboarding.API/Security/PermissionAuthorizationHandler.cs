using Microsoft.AspNetCore.Authorization;
using Onboarding.Application.Common;

namespace Onboarding.API.Security;

/// <summary>
/// Authorization requirement that specifies a permission string (e.g., "employees:read") (D-08).
/// Evaluated by PermissionAuthorizationHandler against ICurrentCompanyPermissionsService.
/// </summary>
public class PermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }
    public PermissionRequirement(string permission) => Permission = permission;
}

/// <summary>
/// Authorization handler that checks if the current user has the required permission (D-08).
/// Reads resolved permissions from ICurrentCompanyPermissionsService (set by ClientClaimsMiddleware).
/// Permissions come from DB AccessGroup lookup, NOT directly from JWT groups claim (T-39-07).
/// </summary>
public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly ICurrentCompanyPermissionsService _permissionsService;

    public PermissionAuthorizationHandler(ICurrentCompanyPermissionsService permissionsService)
    {
        _permissionsService = permissionsService;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (_permissionsService.Permissions.Contains(requirement.Permission))
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}