using FluentValidation;
using Onboarding.Application.Auth.Commands;
using Onboarding.Application.Auth.Validators;
using Shouldly;

namespace Onboarding.API.Tests.AdminAuth;

/// <summary>
/// RED/GREEN unit tests for AdminLoginCommand validation (Task 1).
/// </summary>
public class AdminLoginValidationTests
{
    private readonly IValidator<AdminLoginCommand> _validator = new AdminLoginCommandValidator();

    [Fact]
    public void ValidEmailAndPassword_Valid_PassesValidation()
    {
        var command = new AdminLoginCommand("admin@example.com", "SecureP@ss123");
        var result = _validator.Validate(command);
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void EmptyEmail_Invalid_FailsValidation()
    {
        var command = new AdminLoginCommand(string.Empty, "SecureP@ss123");
        var result = _validator.Validate(command);
        result.IsValid.ShouldBeFalse("Email should be required");
        result.Errors.ShouldContain(e => e.PropertyName == "Email");
    }

    [Fact]
    public void InvalidEmail_Invalid_FailsValidation()
    {
        var command = new AdminLoginCommand("not-an-email", "SecureP@ss123");
        var result = _validator.Validate(command);
        result.IsValid.ShouldBeFalse("Email must be valid format");
        result.Errors.ShouldContain(e => e.PropertyName == "Email");
    }

    [Fact]
    public void EmptyPassword_Invalid_FailsValidation()
    {
        var command = new AdminLoginCommand("admin@example.com", string.Empty);
        var result = _validator.Validate(command);
        result.IsValid.ShouldBeFalse("Password should be required");
        result.Errors.ShouldContain(e => e.PropertyName == "Password");
    }
}
