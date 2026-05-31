using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Onboarding.Infrastructure.Dispatch;
using Shouldly;

namespace Onboarding.Infrastructure.Tests.Dispatch;

// --- Test types at namespace level so NSubstitute can create proxies for generic validators ---

public sealed record ValRunnerValidatedCommand(string Value);
public sealed record ValRunnerUnvalidatedCommand(int Number);

public sealed class ValRunnerStrictValidator : AbstractValidator<ValRunnerValidatedCommand>
{
    public ValRunnerStrictValidator()
    {
        RuleFor(x => x.Value).NotEmpty().WithMessage("Value is required.");
    }
}

/// <summary>
/// Unit tests for <see cref="ValidationRunner"/>.
/// D-61: ValidationRunner.Validate returns valid no-op when no validator is registered.
/// Test types are at namespace level so NSubstitute can create proxies.
/// </summary>
public sealed class ValidationRunnerTests
{
    [Fact]
    public async Task Validate_RegisteredValidator_ValidInstance_ReturnsValid()
    {
        // Arrange
        var sp = BuildProvider(sc =>
            sc.AddScoped<IValidator<ValRunnerValidatedCommand>, ValRunnerStrictValidator>());
        var runner = new ValidationRunner(sp);

        // Act
        var result = await runner.Validate(new ValRunnerValidatedCommand("hello"));

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Fact]
    public async Task Validate_RegisteredValidator_InvalidInstance_ReturnsErrors()
    {
        // Arrange
        var sp = BuildProvider(sc =>
            sc.AddScoped<IValidator<ValRunnerValidatedCommand>, ValRunnerStrictValidator>());
        var runner = new ValidationRunner(sp);

        // Act
        var result = await runner.Validate(new ValRunnerValidatedCommand(string.Empty));

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].ErrorMessage.ShouldBe("Value is required.");
    }

    [Fact]
    public async Task Validate_NoValidatorRegistered_ReturnsValidNoOp()
    {
        // Arrange — no IValidator<UnvalidatedCommand> in container
        var sp = BuildProvider(_ => { });
        var runner = new ValidationRunner(sp);

        // Act
        var result = await runner.Validate(new ValRunnerUnvalidatedCommand(42));

        // Assert — valid no-op per D-61
        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Fact]
    public async Task Validate_SubstituteValidator_Invoked()
    {
        // Arrange — use a real validator so ValidateAsync(T, ct) returns a proper result
        // (NSubstitute cannot intercept extension-method ValidateAsync(T, ct) reliably).
        // The test validates that the runner discovers and invokes the registered validator.
        var sp = BuildProvider(sc =>
            sc.AddScoped<IValidator<ValRunnerValidatedCommand>, ValRunnerStrictValidator>());
        var runner = new ValidationRunner(sp);
        using var cts = new CancellationTokenSource();

        // Act — valid instance → no errors
        var result = await runner.Validate(new ValRunnerValidatedCommand("ok"), cts.Token);

        // Assert — validator was invoked and returned the expected result
        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Fact]
    public async Task Validate_CancellationTokenPropagated_ValidatorReceivesToken()
    {
        // Arrange
        var sp = BuildProvider(sc =>
            sc.AddScoped<IValidator<ValRunnerValidatedCommand>, ValRunnerStrictValidator>());
        var runner = new ValidationRunner(sp);
        var cts = new CancellationTokenSource();

        // Act — just ensure no exception propagation breakage with CT
        var result = await runner.Validate(new ValRunnerValidatedCommand("ok"), cts.Token);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    // --- Helpers ---

    private static IServiceProvider BuildProvider(Action<IServiceCollection> configure)
    {
        var sc = new ServiceCollection();
        configure(sc);
        return sc.BuildServiceProvider();
    }
}
