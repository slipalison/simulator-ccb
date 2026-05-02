using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Onboarding.Application.Common;
using Onboarding.Domain.Aggregates.EmployeeAggregate;
using Onboarding.Domain.Repositories;
using Onboarding.Infrastructure.Persistence;

namespace Onboarding.API.Middleware;

/// <summary>
/// Client claims middleware — resolves JWT sub → Company/Employee → CompanyId + Permissions
/// for each request on BearerClient scheme (D-09, D-10, D-11).
///
/// Runs AFTER UseAuthentication (needs authenticated user) and BEFORE UseAuthorization
/// (sets permissions that authorization policies will check).
///
/// Because no DefaultScheme is configured, UseAuthentication() does not auto-authenticate.
/// This middleware explicitly calls AuthenticateAsync("BearerClient") when an Authorization
/// header is present but the user is not yet authenticated, ensuring permissions are
/// resolved before UseAuthorization runs.
///
/// Flow per request:
/// 1. Skip /api/admin routes (backoffice handles own auth)
/// 2. Force BearerClient authentication if Authorization header present but user not yet authenticated
/// 3. Skip unauthenticated users
/// 4. Read JWT "sub" claim
/// 5. Look up Company by KeycloakUserId == sub
///    - If found (PJ owner): set all permissions + IsCompanyOwner=true
/// 6. If no company, look up Employee by KeycloakUserId == sub
///    - If found: look up AccessGroup → set permissions from AccessGroup.Permissions
/// 7. If nothing found: CompanyId = Guid.Empty → HasQueryFilter returns empty → 403
/// </summary>
public static class ClientClaimsMiddleware
{
    public static IApplicationBuilder UseClientClaims(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            // Skip admin routes — those use backoffice auth (BearerBackoffice scheme)
            if (context.Request.Path.StartsWithSegments("/api/admin"))
            {
                await next();
                return;
            }

            // Force BearerClient authentication if not already done.
            // Without DefaultScheme, UseAuthentication() does not auto-authenticate,
            // so we must explicitly authenticate here before checking IsAuthenticated.
            if (context.User.Identity?.IsAuthenticated != true
                && context.Request.Headers.ContainsKey("Authorization"))
            {
                var result = await context.AuthenticateAsync("BearerClient");
                if (result.Succeeded && result.Principal != null)
                {
                    context.User = result.Principal;
                }
            }

            // Skip if user is NOT authenticated
            if (context.User.Identity?.IsAuthenticated != true)
            {
                await next();
                return;
            }

            var logger = context.RequestServices.GetRequiredService<ILoggerFactory>()
                .CreateLogger("ClientClaimsMiddleware");

            var companyService = (CurrentCompanyService)context.RequestServices
                .GetRequiredService<ICurrentCompanyService>();
            var permissionsService = (CurrentCompanyPermissionsService)context.RequestServices
                .GetRequiredService<ICurrentCompanyPermissionsService>();
            var companyRepository = context.RequestServices.GetRequiredService<ICompanyRepository>();
            var employeeRepository = context.RequestServices.GetRequiredService<IEmployeeRepository>();
            var accessGroupRepository = context.RequestServices.GetRequiredService<IAccessGroupRepository>();

            // Read JWT "sub" claim — the Keycloak user ID
            var sub = context.User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(sub))
            {
                logger.LogDebug("ClientClaimsMiddleware: no sub claim on {Path}", context.Request.Path);
                await next();
                return;
            }

            // Try Company lookup first (PJ owner — D-20)
            var company = await companyRepository.GetByKeycloakSubAsync(sub, context.RequestAborted);

            if (company != null)
            {
                // PJ owner: gets all permissions regardless of JWT groups claim (D-20)
                companyService.CompanyId = company.Id;
                permissionsService.CompanyId = company.Id;
                permissionsService.IsCompanyOwnerFlag = true;
                permissionsService.PermissionList = Permissions.All.ToList();

                logger.LogInformation(
                    "ClientClaimsMiddleware: PJ owner resolved for sub={Sub}, companyId={CompanyId}",
                    sub, company.Id);

                await next();
                return;
            }

            // Try Employee lookup (D-09)
            var employee = await employeeRepository.GetByKeycloakSubAsync(sub, context.RequestAborted);

            if (employee != null)
            {
                var accessGroup = await accessGroupRepository.GetByIdAsync(employee.AccessGroupId, context.RequestAborted);

                companyService.CompanyId = employee.CompanyId;
                permissionsService.CompanyId = employee.CompanyId;
                permissionsService.IsCompanyOwnerFlag = false;
                permissionsService.PermissionList = accessGroup?.Permissions.ToList() ?? [];

                logger.LogInformation(
                    "ClientClaimsMiddleware: Employee resolved for sub={Sub}, companyId={CompanyId}, accessGroup={AccessGroup}",
                    sub, employee.CompanyId, accessGroup?.Name ?? "null");

                await next();
                return;
            }

            // No company or employee found → CompanyId remains Guid.Empty → HasQueryFilter returns empty → 403
            logger.LogWarning(
                "ClientClaimsMiddleware: no Company or Employee found for sub={Sub} — access denied", sub);

            await next();
        });
    }
}