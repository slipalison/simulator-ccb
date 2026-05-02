using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Onboarding.API.Security;
using Shouldly;

namespace Onboarding.API.Tests.Security;

[Trait("Category", "Security")]
public class GroupsClaimsTransformationTests
{
    private readonly GroupsClaimsTransformation _sut = new();

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
}