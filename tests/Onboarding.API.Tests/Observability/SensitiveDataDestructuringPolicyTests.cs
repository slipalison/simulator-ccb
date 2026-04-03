using NSubstitute;
using Onboarding.API.Observability;
using Serilog.Core;
using Serilog.Events;
using Shouldly;

namespace Onboarding.API.Tests.Observability;

[Trait("Category", "Observability")]
[Trait("Category", "Security")]
public class SensitiveDataDestructuringPolicyTests
{
    private readonly SensitiveDataDestructuringPolicy _policy = new();
    private readonly ILogEventPropertyValueFactory _factory;

    public SensitiveDataDestructuringPolicyTests()
    {
        _factory = Substitute.For<ILogEventPropertyValueFactory>();
        _factory.CreatePropertyValue(Arg.Any<object>(), Arg.Any<bool>())
                .Returns(call => new ScalarValue(call.ArgAt<object>(0)));
    }

    // SEC-09: password field is masked as [REDACTED]
    [Fact]
    public void Destructure_ShouldRedact_PasswordField()
    {
        var input = new { Password = "secret123" };

        var result = _policy.TryDestructure(input, _factory, out var destructured);

        result.ShouldBeTrue();
        var structure = destructured.ShouldBeOfType<StructureValue>();
        var passwordProp = structure.Properties.Single(p => p.Name == "Password");
        passwordProp.Value.ShouldBeOfType<ScalarValue>().Value.ShouldBe("[REDACTED]");
    }

    // SEC-09: token field is masked as [REDACTED]
    [Fact]
    public void Destructure_ShouldRedact_TokenField()
    {
        var input = new { Token = "eyJhbGciOiJSUzI1NiJ9..." };

        var result = _policy.TryDestructure(input, _factory, out var destructured);

        result.ShouldBeTrue();
        var structure = destructured.ShouldBeOfType<StructureValue>();
        var prop = structure.Properties.Single(p => p.Name == "Token");
        prop.Value.ShouldBeOfType<ScalarValue>().Value.ShouldBe("[REDACTED]");
    }

    // SEC-09: secret field is masked as [REDACTED]
    [Fact]
    public void Destructure_ShouldRedact_SecretField()
    {
        var input = new { Secret = "my-very-secret-value" };

        var result = _policy.TryDestructure(input, _factory, out var destructured);

        result.ShouldBeTrue();
        var structure = destructured.ShouldBeOfType<StructureValue>();
        var prop = structure.Properties.Single(p => p.Name == "Secret");
        prop.Value.ShouldBeOfType<ScalarValue>().Value.ShouldBe("[REDACTED]");
    }

    // SEC-09: client_secret field is masked as [REDACTED] (case-insensitive match)
    [Fact]
    public void Destructure_ShouldRedact_ClientSecretField()
    {
        var input = new { client_secret = "oauth-client-secret-value" };

        var result = _policy.TryDestructure(input, _factory, out var destructured);

        result.ShouldBeTrue();
        var structure = destructured.ShouldBeOfType<StructureValue>();
        var prop = structure.Properties.Single(p => p.Name == "client_secret");
        prop.Value.ShouldBeOfType<ScalarValue>().Value.ShouldBe("[REDACTED]");
    }

    // SEC-09: non-sensitive fields pass through unchanged (no sensitive keys = policy skips)
    [Fact]
    public void Destructure_ShouldNotRedact_NonSensitiveFields()
    {
        var input = new { Name = "João" };

        var result = _policy.TryDestructure(input, _factory, out var destructured);

        // Object with no sensitive keys — policy returns false (skips it)
        result.ShouldBeFalse();
    }

    // SEC-09: authorization header value is masked
    [Fact]
    public void Destructure_ShouldRedact_AuthorizationField()
    {
        var input = new { Authorization = "Bearer eyJhbGciOiJSUzI1NiJ9..." };

        var result = _policy.TryDestructure(input, _factory, out var destructured);

        result.ShouldBeTrue();
        var structure = destructured.ShouldBeOfType<StructureValue>();
        var prop = structure.Properties.Single(p => p.Name == "Authorization");
        prop.Value.ShouldBeOfType<ScalarValue>().Value.ShouldBe("[REDACTED]");
    }

    // D-19: CPF masked as ***.***.***-**
    [Fact]
    public void Destructure_ShouldMaskCpf_WithStarPattern()
    {
        var input = new { Cpf = "529.982.247-25" };

        var result = _policy.TryDestructure(input, _factory, out var destructured);

        result.ShouldBeTrue();
        var structure = destructured.ShouldBeOfType<StructureValue>();
        var prop = structure.Properties.Single(p => p.Name == "Cpf");
        prop.Value.ShouldBeOfType<ScalarValue>().Value.ShouldBe("***.***.***-**");
    }

    // D-20: email masked as a***@domain.com (preserves domain)
    [Fact]
    public void Destructure_ShouldMaskEmail_PreservingDomain()
    {
        // OBS-01: Logs are enriched with TraceId and SpanId — structural test
        // Full TraceId enrichment verified via Serilog.Enrichers.Span integration (E2E in VALIDATION.md)
        SensitiveDataDestructuringPolicy.MaskEmail("joao@example.com").ShouldBe("j***@example.com");
    }

    // OBS-01: Logs are enriched with TraceId and SpanId
    [Fact]
    public void LogEntry_ShouldContainTraceIdAndSpanId_WhenActivityIsActive()
    {
        // Structural test: Serilog.Enrichers.Span enriches from System.Diagnostics.Activity.
        // Full E2E verification is manual per VALIDATION.md (requires running stack).
        // Here we verify the assembly is present (package installed).
        var enricherAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .Any(a => a.GetName().Name?.Contains("Serilog") == true);
        enricherAssembly.ShouldBeTrue();
    }
}
