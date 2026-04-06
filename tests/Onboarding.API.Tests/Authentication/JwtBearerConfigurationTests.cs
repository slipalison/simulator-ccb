using Shouldly;

namespace Onboarding.API.Tests.Authentication;

/// <summary>
/// RED stubs for AUTH-02: JWT Bearer configuration validates tokens issued by Keycloak.
/// Test name matches VALIDATION.md task 06-01-01: JwtBearerConfigurationTests.
/// </summary>
public class JwtBearerConfigurationTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public Task JwtBearer_Should_BeConfiguredWithKeycloakAuthority()
    {
        // RED stub — implement when AddJwtBearer is wired in Plan 02
        true.ShouldBeFalse("RED stub — not implemented yet");
        return Task.CompletedTask;
    }

    [Fact]
    [Trait("Category", "Unit")]
    public Task JwtBearer_Should_HaveValidateAudienceDisabled()
    {
        // RED stub — D-05: ValidateAudience = false required for Keycloak ROPC tokens
        true.ShouldBeFalse("RED stub — not implemented yet");
        return Task.CompletedTask;
    }

    [Fact]
    [Trait("Category", "Unit")]
    public Task JwtBearer_Should_HaveMapInboundClaimsFalse()
    {
        // RED stub — D-04: MapInboundClaims = false required so "email" claim stays as "email"
        true.ShouldBeFalse("RED stub — not implemented yet");
        return Task.CompletedTask;
    }
}
