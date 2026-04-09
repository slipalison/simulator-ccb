using FluentValidation.TestHelper;
using Onboarding.Application.Admin.Commands;
using Onboarding.Application.Admin.Validators;
using Shouldly;

namespace Onboarding.Domain.Tests.Application;

public class UpdateUserCommandValidatorTests
{
    private readonly UpdateUserCommandValidator _validator = new();

    [Fact]
    public void ValidCommand_ShouldPass()
    {
        // Arrange
        var command = new UpdateUserCommand(
            Guid.NewGuid(), "John Doe", null, "john@example.com", "11999999999", "admin-sub", "admin@example.com");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void EmptyName_ShouldFail(string? name)
    {
        // Arrange
        var command = new UpdateUserCommand(
            Guid.NewGuid(), name!, null, "john@example.com", "11999999999", "admin-sub", "admin@example.com");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void NameOver200Chars_ShouldFail()
    {
        // Arrange
        var command = new UpdateUserCommand(
            Guid.NewGuid(), new string('A', 201), null, "john@example.com", "11999999999", "admin-sub", "admin@example.com");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void InvalidEmail_ShouldFail()
    {
        // Arrange
        var command = new UpdateUserCommand(
            Guid.NewGuid(), "John", null, "not-an-email", "11999999999", "admin-sub", "admin@example.com");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void RazaoSocialOver300Chars_ShouldFail()
    {
        // Arrange
        var command = new UpdateUserCommand(
            Guid.NewGuid(), "John", new string('B', 301), "john@example.com", "11999999999", "admin-sub", "admin@example.com");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RazaoSocial);
    }
}

public class BlockUserCommandValidatorTests
{
    private readonly BlockUserCommandValidator _validator = new();

    [Fact]
    public void ValidUserId_ShouldPass()
    {
        var command = new BlockUserCommand(Guid.NewGuid());
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyUserId_ShouldFail()
    {
        var command = new BlockUserCommand(Guid.Empty);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }
}

public class UnblockUserCommandValidatorTests
{
    private readonly UnblockUserCommandValidator _validator = new();

    [Fact]
    public void ValidUserId_ShouldPass()
    {
        var command = new UnblockUserCommand(Guid.NewGuid());
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyUserId_ShouldFail()
    {
        var command = new UnblockUserCommand(Guid.Empty);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }
}

public class DeleteUserCommandValidatorTests
{
    private readonly DeleteUserCommandValidator _validator = new();

    [Fact]
    public void ValidCommand_ShouldPass()
    {
        var command = new DeleteUserCommand(Guid.NewGuid(), "user@example.com");
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyConfirmEmail_ShouldFail()
    {
        var command = new DeleteUserCommand(Guid.NewGuid(), "");
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ConfirmEmail);
    }

    [Fact]
    public void InvalidConfirmEmailFormat_ShouldFail()
    {
        var command = new DeleteUserCommand(Guid.NewGuid(), "not-an-email");
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ConfirmEmail);
    }

    [Fact]
    public void EmptyUserId_ShouldFail()
    {
        var command = new DeleteUserCommand(Guid.Empty, "user@example.com");
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }
}
