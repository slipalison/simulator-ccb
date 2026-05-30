using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.Tokens;
using Onboarding.API.Security;
using Shouldly;

namespace Onboarding.API.Tests.Security;

[Trait("Category", "Security")]
public class RealmRolesClaimsTransformationTests
{
    private readonly RealmRolesClaimsTransformation _sut = new();

    // ── Helper: builds a JwtSecurityToken whose Payload["realm_access"] is a JsonElement ──
    // The production code does: bootstrapContext.Payload.TryGetValue("realm_access", out var v)
    // and then v is JsonElement (true when the token is parsed from a JWT string with JSON object).
    private static JwtSecurityToken BuildJwtWithRealmAccess(string[]? roles = null,
        bool omitRealmAccess = false,
        bool omitRolesKey = false)
    {
        Dictionary<string, object> claimsCollection;

        if (omitRealmAccess)
        {
            // No realm_access key at all
            claimsCollection = new Dictionary<string, object>
            {
                { "other_claim", "value" }
            };
        }
        else if (omitRolesKey)
        {
            // realm_access present but no "roles" key inside
            var realmAccessJson = JsonSerializer.Serialize(new { some_other_key = "x" });
            using var realmAccessDoc = JsonDocument.Parse(realmAccessJson);
            claimsCollection = new Dictionary<string, object>
            {
                { "realm_access", realmAccessDoc.RootElement.Clone() }
            };
        }
        else
        {
            // Normal case: realm_access: { roles: [...] }
            var rolesPayload = roles ?? Array.Empty<string>();
            var realmAccessJson = JsonSerializer.Serialize(new { roles = rolesPayload });
            using var realmAccessDoc = JsonDocument.Parse(realmAccessJson);
            claimsCollection = new Dictionary<string, object>
            {
                { "realm_access", realmAccessDoc.RootElement.Clone() }
            };
        }

        var payload = new JwtPayload(
            issuer: "test-issuer",
            audience: "test-audience",
            claims: null,
            claimsCollection: claimsCollection,
            notBefore: null,
            expires: DateTime.UtcNow.AddHours(1),
            issuedAt: null);

        var signingKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes("test-key-must-be-at-least-32-bytes!!"));
        var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(new JwtHeader(creds), payload);

        var handler = new JwtSecurityTokenHandler();
        return handler.ReadJwtToken(handler.WriteToken(token));
    }

    private static ClaimsPrincipal PrincipalWithJwt(JwtSecurityToken jwt)
    {
        var identity = new ClaimsIdentity(new[] { new Claim("sub", "u-123") }, "Bearer");
        identity.BootstrapContext = jwt;
        return new ClaimsPrincipal(identity);
    }

    // ── Guard paths (already tested — retain for regression net) ─────────────────

    [Fact]
    public async Task TransformAsync_ReturnsSamePrincipal_WhenNullIdentity()
    {
        var principal = new ClaimsPrincipal();

        var result = await _sut.TransformAsync(principal);

        result.ShouldBe(principal);
    }

    [Fact]
    public async Task TransformAsync_ReturnsSamePrincipal_WhenNoBootstrapContext()
    {
        var identity = new ClaimsIdentity(new[] { new Claim("sub", "123") }, "Bearer");
        var principal = new ClaimsPrincipal(identity);

        var result = await _sut.TransformAsync(principal);

        result.ShouldBe(principal);
    }

    [Fact]
    public async Task TransformAsync_ReturnsSamePrincipal_WhenBootstrapContextIsNotJwtSecurityToken()
    {
        var identity = new ClaimsIdentity(new[] { new Claim("sub", "123") }, "Bearer");
        identity.BootstrapContext = "not-a-jwt-token";
        var principal = new ClaimsPrincipal(identity);

        var result = await _sut.TransformAsync(principal);

        result.ShouldBe(principal);
    }

    [Fact]
    public async Task TransformAsync_ReturnsSamePrincipal_WhenGenericIdentity()
    {
        var identity = new System.Security.Principal.GenericIdentity("test-user");
        var principal = new ClaimsPrincipal(identity);

        var result = await _sut.TransformAsync(principal);

        result.ShouldBe(principal);
    }

    // ── Missing coverage: JWT realm_access.roles processing ───────────────────────

    [Fact]
    public async Task TransformAsync_AddsRoleClaims_WhenRealmAccessRolesPresent()
    {
        // Arrange — typical Keycloak JWT: realm_access: { roles: ["admin", "user"] }
        var jwt = BuildJwtWithRealmAccess(new[] { "admin", "user" });
        var principal = PrincipalWithJwt(jwt);

        // Act
        var result = await _sut.TransformAsync(principal);

        // Assert — both roles added as ClaimTypes.Role
        result.IsInRole("admin").ShouldBeTrue();
        result.IsInRole("user").ShouldBeTrue();
    }

    [Fact]
    public async Task TransformAsync_AddsSingleRole_WhenOnlyOneRoleInArray()
    {
        var jwt = BuildJwtWithRealmAccess(new[] { "admin" });
        var principal = PrincipalWithJwt(jwt);

        var result = await _sut.TransformAsync(principal);

        result.IsInRole("admin").ShouldBeTrue();
    }

    [Fact]
    public async Task TransformAsync_DoesNotAddDuplicateRoleClaim_WhenRoleAlreadyPresent()
    {
        // Arrange — principal already has "admin" role; JWT lists same role
        var jwt = BuildJwtWithRealmAccess(new[] { "admin" });
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "admin") }, "Bearer");
        identity.BootstrapContext = jwt;
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = await _sut.TransformAsync(principal);

        // Assert — exactly one "admin" claim
        result.FindAll(ClaimTypes.Role)
              .Count(c => c.Value == "admin")
              .ShouldBe(1);
    }

    [Fact]
    public async Task TransformAsync_ReturnsUnchangedPrincipal_WhenRealmAccessAbsent()
    {
        // Arrange — JWT with no realm_access key
        var jwt = BuildJwtWithRealmAccess(omitRealmAccess: true);
        var principal = PrincipalWithJwt(jwt);
        var claimsBefore = principal.Claims.Count();

        // Act
        var result = await _sut.TransformAsync(principal);

        // Assert — no extra claims added
        result.Claims.Count().ShouldBe(claimsBefore);
    }

    [Fact]
    public async Task TransformAsync_ReturnsUnchangedPrincipal_WhenRolesKeyAbsentInRealmAccess()
    {
        // Arrange — realm_access exists but has no "roles" property
        var jwt = BuildJwtWithRealmAccess(omitRolesKey: true);
        var principal = PrincipalWithJwt(jwt);
        var claimsBefore = principal.Claims.Count();

        // Act
        var result = await _sut.TransformAsync(principal);

        // Assert — no role claims added
        result.FindAll(ClaimTypes.Role).ShouldBeEmpty();
    }

    [Fact]
    public async Task TransformAsync_ReturnsUnchangedPrincipal_WhenRolesArrayIsEmpty()
    {
        // Arrange — roles: []
        var jwt = BuildJwtWithRealmAccess(Array.Empty<string>());
        var identity = new ClaimsIdentity(new[] { new Claim("sub", "u-1") }, "Bearer");
        identity.BootstrapContext = jwt;
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = await _sut.TransformAsync(principal);

        // Assert
        result.FindAll(ClaimTypes.Role).ShouldBeEmpty();
    }

    [Fact]
    public async Task TransformAsync_SkipsNullOrEmpty_RoleNamesInArray()
    {
        // Arrange — null/empty strings must be filtered (IsNullOrEmpty guard in code)
        // JSON null in array becomes JsonElement with ValueKind=Null; GetString() returns null.
        // We use an explicit non-empty set and verify only it lands; we can't easily inject
        // null into the array via serialization since null gets dropped. Use empty string instead.
        var jwt = BuildJwtWithRealmAccess(new[] { "admin", "" });
        var principal = PrincipalWithJwt(jwt);

        // Act
        var result = await _sut.TransformAsync(principal);

        // Assert — "admin" added, empty string skipped
        result.IsInRole("admin").ShouldBeTrue();
        result.FindAll(ClaimTypes.Role).ShouldAllBe(c => !string.IsNullOrEmpty(c.Value));
    }

    [Fact]
    public async Task TransformAsync_AddsManyRoles_FromLargeRolesArray()
    {
        var roles = new[] { "admin", "user", "viewer", "report-reader", "offline_access" };
        var jwt = BuildJwtWithRealmAccess(roles);
        var principal = PrincipalWithJwt(jwt);

        var result = await _sut.TransformAsync(principal);

        foreach (var r in roles)
            result.IsInRole(r).ShouldBeTrue();
    }
}
