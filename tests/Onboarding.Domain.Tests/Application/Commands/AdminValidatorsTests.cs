using FluentValidation;
using Onboarding.Application.Admin.Commands;
using Onboarding.Application.Admin.Validators;
using Shouldly;

namespace Onboarding.Domain.Tests.Application.Commands;

[Trait("Category", "Unit")]
public class AdminValidatorsTests
{
    [Fact]
    public async Task CreateAdminCommandValidator_ValidCommand_Passes()
    {
        var validator = new CreateAdminCommandValidator();
        var command = new CreateAdminCommand("John Doe", "john@test.com", "sub", "creator@test.com", null);

        var result = await validator.ValidateAsync(command);

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("", "test@test.com", "Full name is required.")]
    [InlineData("A", "test@test.com", "Full name must be at least 2 characters.")]
    [InlineData("John Doe", "", "Email is required.")]
    [InlineData("John Doe", "not-an-email", "Invalid email format.")]
    public async Task CreateAdminCommandValidator_InvalidInputs_Fail(string fullName, string email, string expectedError)
    {
        var validator = new CreateAdminCommandValidator();
        var command = new CreateAdminCommand(fullName, email, "sub", "creator@test.com", null);

        var result = await validator.ValidateAsync(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains(expectedError) || e.ErrorMessage == expectedError);
    }

    [Fact]
    public async Task ForcePasswordChangeCommandValidator_ValidCommand_Passes()
    {
        var validator = new ForcePasswordChangeCommandValidator();
        var command = new ForcePasswordChangeCommand("user-123", "admin@test.com", "Str0ng!Pass", null);

        var result = await validator.ValidateAsync(command);

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("", "admin@test.com", "Str0ng!Pass", "User ID is required.")]
    [InlineData("user-123", "", "Str0ng!Pass", "Admin email is required.")]
    [InlineData("user-123", "bad-email", "Str0ng!Pass", "Invalid email format.")]
    [InlineData("user-123", "admin@test.com", "", "Password is required.")]
    [InlineData("user-123", "admin@test.com", "short1!", "Password must be at least 8 characters.")]
    [InlineData("user-123", "admin@test.com", "nouppercase1!", "Password must contain at least one uppercase letter.")]
    [InlineData("user-123", "admin@test.com", "NOLOWERCASE1!", "Password must contain at least one lowercase letter.")]
    [InlineData("user-123", "admin@test.com", "NoDigitsHere!", "Password must contain at least one digit.")]
    [InlineData("user-123", "admin@test.com", "NoSpecialChar1", "Password must contain at least one special character")]
    public async Task ForcePasswordChangeCommandValidator_InvalidInputs_Fail(string userId, string email, string password, string expectedError)
    {
        var validator = new ForcePasswordChangeCommandValidator();
        var command = new ForcePasswordChangeCommand(userId, email, password, null);

        var result = await validator.ValidateAsync(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains(expectedError));
    }

    [Fact]
    public async Task ToggleAdministratorStatusCommandValidator_ValidCommand_Passes()
    {
        var validator = new ToggleAdministratorStatusCommandValidator();
        var command = new ToggleAdministratorStatusCommand(
            "target-id", "Admin", true, "Reason", "actor-id", "actor@test.com", null);

        var result = await validator.ValidateAsync(command);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public async Task ToggleAdministratorStatusCommandValidator_Fails_WhenSelfToggle()
    {
        var validator = new ToggleAdministratorStatusCommandValidator();
        var command = new ToggleAdministratorStatusCommand(
            "same-id", "Admin", false, null, "same-id", "actor@test.com", null);

        var result = await validator.ValidateAsync(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("cannot change their own"));
    }

    [Fact]
    public async Task UpdateAdministratorCommandValidator_ValidCommand_Passes()
    {
        var validator = new UpdateAdministratorCommandValidator();
        var command = new UpdateAdministratorCommand(
            "target-id", "New Name", "new@test.com", "actor-id", "actor@test.com", null);

        var result = await validator.ValidateAsync(command);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public async Task UpdateAdministratorCommandValidator_Fails_WhenSelfEdit()
    {
        var validator = new UpdateAdministratorCommandValidator();
        var command = new UpdateAdministratorCommand(
            "same-id", "Name", "email@test.com", "same-id", "actor@test.com", null);

        var result = await validator.ValidateAsync(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("cannot edit their own"));
    }

    [Theory]
    [InlineData("", "Name", "email@test.com", "actor", "actor@test.com")]
    [InlineData("id", "", "email@test.com", "actor", "actor@test.com")]
    [InlineData("id", "Name", "bad-email", "actor", "actor@test.com")]
    public async Task UpdateAdministratorCommandValidator_Fails_InvalidData(string targetId, string name, string email, string actorSub, string actorEmail)
    {
        var validator = new UpdateAdministratorCommandValidator();
        var command = new UpdateAdministratorCommand(targetId, name, email, actorSub, actorEmail, null);

        var result = await validator.ValidateAsync(command);

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public async Task ResetAdministratorPasswordCommandValidator_ValidCommand_Passes()
    {
        var validator = new ResetAdministratorPasswordCommandValidator();
        var command = new ResetAdministratorPasswordCommand(
            "target-id", "Admin", "actor-id", "actor@test.com", null);

        var result = await validator.ValidateAsync(command);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public async Task ResetAdministratorPasswordCommandValidator_Fails_WhenSelfReset()
    {
        var validator = new ResetAdministratorPasswordCommandValidator();
        var command = new ResetAdministratorPasswordCommand(
            "same-id", "Admin", "same-id", "actor@test.com", null);

        var result = await validator.ValidateAsync(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("cannot reset their own"));
    }

    [Theory]
    [InlineData("", "Admin", "actor", "actor@test.com")]
    [InlineData("id", "Admin", "", "actor@test.com")]
    public async Task ResetAdministratorPasswordCommandValidator_Fails_EmptyFields(string targetId, string targetUserName, string actorSub, string actorEmail)
    {
        var validator = new ResetAdministratorPasswordCommandValidator();
        var command = new ResetAdministratorPasswordCommand(targetId, targetUserName, actorSub, actorEmail, null);

        var result = await validator.ValidateAsync(command);

        result.IsValid.ShouldBeFalse();
    }
}