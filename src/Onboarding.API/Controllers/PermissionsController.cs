using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Onboarding.Application.Common;

namespace Onboarding.API.Controllers;

/// <summary>
/// GET /api/auth/permissions — Bearer-only endpoint returning the caller's resolved permissions.
///
/// W-arch fix (frontend-client-fundos round 4): the BFF uses Authorization: Bearer &lt;token&gt;
/// directly (no cookie). /api/auth/me only validates via the httpOnly refreshToken cookie
/// so it was unreachable from BFF flows. This endpoint accepts a raw Bearer token via the
/// BearerClient scheme and returns the permissions already resolved by ClientClaimsMiddleware.
///
/// ClientClaimsMiddleware runs before UseAuthorization (see Program.cs middleware order):
///   UseClientSession → UseAuthentication → UseClientClaims → UseAuthorization
/// The middleware detects the Authorization header, authenticates "BearerClient", resolves the
/// sub → Company/Employee → permissions chain, and populates ICurrentCompanyPermissionsService
/// before this action executes. The handler is therefore a simple read.
///
/// Security (D-5, D-12):
/// - [Authorize] enforces authentication before the action body runs.
/// - AuthenticationSchemes = "BearerClient" rejects cookies and admin tokens.
/// - Permissions are user-scoped: sub-to-company binding prevents cross-tenant leaks.
/// - No PII in response (permissions are enum strings, no email/sub/company name).
/// </summary>
[ApiController]
[Route("api/auth")]
public sealed class PermissionsController : ControllerBase
{
    private readonly ICurrentCompanyPermissionsService _permissionsService;

    public PermissionsController(ICurrentCompanyPermissionsService permissionsService)
    {
        _permissionsService = permissionsService;
    }

    /// <summary>GET /api/auth/permissions — returns resolved permissions for the Bearer token caller.</summary>
    [HttpGet("permissions")]
    [Authorize(AuthenticationSchemes = "BearerClient")]
    [ProducesResponseType(typeof(PermissionsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult GetPermissions()
    {
        // ClientClaimsMiddleware has already resolved sub → permissions before this executes.
        // No DB call needed here — just read the populated service.
        var response = new PermissionsResponse(
            Permissions: _permissionsService.Permissions.ToArray(),
            AccessGroup: null,  // not tracked at this layer; permissions list is the contract
            CompanyId: _permissionsService.CompanyId == Guid.Empty
                ? null
                : _permissionsService.CompanyId.ToString());

        return Ok(response);
    }
}

/// <summary>
/// Response DTO for GET /api/auth/permissions.
/// AccessGroup is informational only (null when sub maps to a PJ owner).
/// CompanyId is null when no company/employee was resolved (empty permissions path).
/// </summary>
public sealed record PermissionsResponse(
    string[] Permissions,
    string? AccessGroup,
    string? CompanyId);
