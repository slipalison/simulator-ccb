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
public class GroupsClaimsTransformationTests
{
    private readonly GroupsClaimsTransformation _sut = new();

    // ── Helpers ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds a JwtSecurityToken via write+read round-trip.
    /// Arrays survive round-trip as JsonElement; plain strings survive as string.
    /// Use for array-based tests.
    /// </summary>
    private static JwtSecurityToken BuildJwtWithPayloadValue(string claimKey, object value)
    {
        // Serialize to JSON and parse back as JsonElement so the Payload dictionary
        // holds a JsonElement — exactly what the production code checks via `is JsonElement`.
        var json = JsonSerializer.Serialize(value);
        using var doc = JsonDocument.Parse(json);
        var element = doc.RootElement.Clone();   // clone survives `using`

        var claimsCollection = new Dictionary<string, object>
        {
            { claimKey, element }
        };

        var payload = new JwtPayload(
            issuer: "test-issuer",
            audience: "test-audience",
            claims: null,
            claimsCollection: claimsCollection,
            notBefore: null,
            expires: DateTime.UtcNow.AddHours(1),
            issuedAt: null);

        // JwtHeader uses a signing key only for write; for read-side tests we can supply
        // a minimal header by writing + re-reading the token string.
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("test-key-must-be-at-least-32-bytes!!"));
        var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(new JwtHeader(creds), payload);

        // Re-read via the handler so the Payload reflects actual JWT parsing semantics
        var handler = new JwtSecurityTokenHandler();
        var tokenString = handler.WriteToken(token);
        return handler.ReadJwtToken(tokenString);
    }

    /// <summary>
    /// Builds a JwtSecurityToken whose Payload directly holds a JsonElement of String kind
    /// WITHOUT a round-trip (which would convert the JsonElement back to a plain string).
    /// This is the only way to reach the JsonValueKind.String branch in the production code.
    /// </summary>
    private static JwtSecurityToken BuildJwtWithDirectJsonElement(string claimKey, JsonElement element)
    {
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("test-key-must-be-at-least-32-bytes!!"));
        var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        // Build payload with string claims, then manually inject the JsonElement
        var payload = new JwtPayload("test-issuer", "test-audience", null, null, DateTime.UtcNow.AddHours(1));
        payload[claimKey] = element;   // JwtPayload is a Dictionary<string, object>

        return new JwtSecurityToken(new JwtHeader(creds), payload);
    }

    private static ClaimsPrincipal PrincipalWithJwt(JwtSecurityToken jwt)
    {
        var identity = new ClaimsIdentity(new[] { new Claim("sub", "123") }, "Bearer");
        identity.BootstrapContext = jwt;
        return new ClaimsPrincipal(identity);
    }

    // ── Guard paths (already tested — retain to maintain regression net) ──────────

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

    // ── Missing coverage: JWT processing paths ─────────────────────────────────────

    [Fact]
    public async Task TransformAsync_AddsRoleClaims_WhenGroupsIsJsonArray()
    {
        // Arrange — Keycloak groups claim is a JSON array: ["admin-empresa", "viewer"]
        var jwt = BuildJwtWithPayloadValue("groups", new[] { "admin-empresa", "viewer" });
        var principal = PrincipalWithJwt(jwt);

        // Act
        var result = await _sut.TransformAsync(principal);

        // Assert — both groups mapped to ClaimTypes.Role
        result.IsInRole("admin-empresa").ShouldBeTrue();
        result.IsInRole("viewer").ShouldBeTrue();
    }

    [Fact]
    public async Task TransformAsync_AddsRoleClaim_WhenGroupsIsJsonElementString()
    {
        // The JsonValueKind.String branch is reached when the JwtPayload directly holds
        // a JsonElement of String kind (not via round-trip which converts strings to plain string).
        // We inject the JsonElement directly into the payload dictionary.
        using var doc = JsonDocument.Parse("\"admin-empresa\"");
        var stringElement = doc.RootElement.Clone();
        var jwt = BuildJwtWithDirectJsonElement("groups", stringElement);
        var principal = PrincipalWithJwt(jwt);

        // Act
        var result = await _sut.TransformAsync(principal);

        // Assert — JsonElement string IS added as role
        result.IsInRole("admin-empresa").ShouldBeTrue();
    }

    [Fact]
    public async Task TransformAsync_SkipsNullOrEmpty_GroupNamesInArray()
    {
        // Arrange — array contains null/empty entries that must be silently skipped
        var jwt = BuildJwtWithPayloadValue("groups", new string?[] { "viewer", null, "" });
        var principal = PrincipalWithJwt(jwt);

        // Act
        var result = await _sut.TransformAsync(principal);

        // Assert — only non-empty group added
        result.IsInRole("viewer").ShouldBeTrue();
        // Null and empty strings must not produce a claim with key ClaimTypes.Role
        var roleClaims = result.FindAll(ClaimTypes.Role).ToList();
        roleClaims.ShouldAllBe(c => !string.IsNullOrEmpty(c.Value));
    }

    [Fact]
    public async Task TransformAsync_DoesNotAddDuplicateRoleClaim_WhenGroupAlreadyPresent()
    {
        // Arrange — principal already has "viewer" role; token lists same group
        var jwt = BuildJwtWithPayloadValue("groups", new[] { "viewer" });
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "viewer") }, "Bearer");
        identity.BootstrapContext = jwt;
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = await _sut.TransformAsync(principal);

        // Assert — exactly one "viewer" claim, not two
        result.FindAll(ClaimTypes.Role)
              .Count(c => c.Value == "viewer")
              .ShouldBe(1);
    }

    [Fact]
    public async Task TransformAsync_ReturnsUnchangedPrincipal_WhenGroupsClaimAbsent()
    {
        // Arrange — JWT with no "groups" key in payload
        var jwt = BuildJwtWithPayloadValue("other_claim", "some_value");
        var principal = PrincipalWithJwt(jwt);
        var originalClaimCount = principal.Claims.Count();

        // Act
        var result = await _sut.TransformAsync(principal);

        // Assert — no new claims added
        result.Claims.Count().ShouldBe(originalClaimCount);
    }

    [Fact]
    public async Task TransformAsync_ReturnsUnchangedPrincipal_WhenGroupsArrayIsEmpty()
    {
        // Arrange — empty array → nothing to add
        var jwt = BuildJwtWithPayloadValue("groups", Array.Empty<string>());
        var identity = new ClaimsIdentity(new[] { new Claim("sub", "123") }, "Bearer");
        identity.BootstrapContext = jwt;
        var principal = new ClaimsPrincipal(identity);
        var claimsBefore = principal.Claims.ToList();

        // Act
        var result = await _sut.TransformAsync(principal);

        // Assert — role claims remain empty
        result.FindAll(ClaimTypes.Role).ShouldBeEmpty();
    }

    [Fact]
    public async Task TransformAsync_SkipsEmptyString_WhenGroupsIsJsonElementEmptyString()
    {
        // Single empty JsonElement string → IsNullOrEmpty guard skips it
        using var doc = JsonDocument.Parse("\"\"");
        var emptyElement = doc.RootElement.Clone();
        var jwt = BuildJwtWithDirectJsonElement("groups", emptyElement);
        var principal = PrincipalWithJwt(jwt);

        // Act
        var result = await _sut.TransformAsync(principal);

        // Assert — no role claims added
        result.FindAll(ClaimTypes.Role).ShouldBeEmpty();
    }

    [Fact]
    public async Task TransformAsync_AddsManyGroups_FromLargeArray()
    {
        // Arrange — multiple groups
        var groups = new[] { "admin-empresa", "viewer", "dashboard", "audit-read" };
        var jwt = BuildJwtWithPayloadValue("groups", groups);
        var principal = PrincipalWithJwt(jwt);

        // Act
        var result = await _sut.TransformAsync(principal);

        // Assert — each group mapped
        foreach (var g in groups)
            result.IsInRole(g).ShouldBeTrue();
    }
}
