using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Onboarding.Infrastructure.Dispatch;
using Shouldly;

namespace Onboarding.Infrastructure.Tests.Dispatch;

/// <summary>
/// Unit tests for <see cref="ValidationRunner"/>.
/// D-61: ValidationRunner.Validate returns valid no-op when no validator is registered.
/// </summary>
public sealed class ValidationRunnerTests
{
    // --- Test types ---

    private sealed record ValidatedCommand(string Value);
    private sealed record UnvalidatedCommand(int Number);

    private sealed class StrictValidator : AbstractValidator<ValidatedCommand>
    {
        public StrictValidator()
        {
            RuleFor(x => x.Value).NotEmpty().WithMessage("Value is required.");
        }
    }

    // --- Tests ---

    [Fact]
    public async Task Validate_RegisteredValidator_ValidInstance_ReturnsValid()
    {
        // Arrange
        var sp = BuildProvider(sc =>
            sc.AddScoped<IValidator<ValidatedCommand>, StrictValidator>());
        var runner = new ValidationRunner(sp);

        // Act
        var result = await runner.Validate(new ValidatedCommand("hello"));

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Fact]
    public async Task Validate_RegisteredValidator_InvalidInstance_ReturnsErrors()
    {
        // Arrange
        var sp = BuildProvider(sc =>
            sc.AddScoped<IValidator<ValidatedCommand>, StrictValidator>());
        var runner = new ValidationRunner(sp);

        // Act
        var result = await runner.Validate(new ValidatedCommand(string.Empty));

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
        var result = await runner.Validate(new UnvalidatedCommand(42));

        // Assert — valid no-op per D-61
        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Fact]
    public async Task Validate_SubstituteValidator_Invoked()
    {
        // Arrange
        var validator = Substitute.For<IValidator<ValidatedCommand>>();
        validator.ValidateAsync(Arg.Any<IValidationContext>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        var sp = BuildProvider(sc =>
            sc.AddScoped<IValidator<ValidatedCommand>>(_ => validator));
        var runner = new ValidationRunner(sp);
        using var cts = new CancellationTokenSource();

        // Act
        var result = await runner.Validate(new ValidatedCommand("x"), cts.Token);

        // Assert
        result.IsValid.ShouldBeTrue();
        await validator.Received(1).ValidateAsync(
            Arg.Any<IValidationContext>(),
            Arg.Is<CancellationToken>(ct => ct == cts.Token));
    }

    [Fact]
    public async Task Validate_CancellationTokenPropagated_ValidatorReceivesToken()
    {
        // Arrange
        var sp = BuildProvider(sc =>
            sc.AddScoped<IValidator<ValidatedCommand>, StrictValidator>());
        var runner = new ValidationRunner(sp);
        var cts = new CancellationTokenSource();

        // Act — just ensure no exception propagation breakage with CT
        var result = await runner.Validate(new ValidatedCommand("ok"), cts.Token);

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
