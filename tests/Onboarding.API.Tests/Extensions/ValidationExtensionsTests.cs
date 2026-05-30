using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Onboarding.API.Extensions;
using Shouldly;

namespace Onboarding.API.Tests.Extensions;

/// <summary>
/// Unit tests for <see cref="ValidationExtensions"/> — the shared DRY-01 validation helper.
/// Coverage gate: new file post-boundary, must reach 80%.
/// </summary>
public class ValidationExtensionsTests
{
    // ─── ToValidationProblem(ValidationResult) ───────────────────────────

    [Fact]
    public void ToValidationProblem_ValidationResult_WithSingleError_ReturnsProblemDetailsWithOneField()
    {
        var failures = new List<ValidationFailure> { new("Email", "Email is required.") };
        var result = new ValidationResult(failures);

        var problem = result.ToValidationProblem();

        problem.ShouldNotBeNull();
        problem.Errors.ShouldContainKey("Email");
        problem.Errors["Email"].ShouldContain("Email is required.");
    }

    [Fact]
    public void ToValidationProblem_ValidationResult_WithMultipleErrorsSameField_GroupsUnderSameKey()
    {
        var failures = new List<ValidationFailure>
        {
            new("Password", "Too short."),
            new("Password", "Must contain a digit.")
        };
        var result = new ValidationResult(failures);

        var problem = result.ToValidationProblem();

        problem.Errors["Password"].Length.ShouldBe(2);
        problem.Errors["Password"].ShouldContain("Too short.");
        problem.Errors["Password"].ShouldContain("Must contain a digit.");
    }

    [Fact]
    public void ToValidationProblem_ValidationResult_WithMultipleFields_ProducesDistinctKeys()
    {
        var failures = new List<ValidationFailure>
        {
            new("Email", "Invalid email."),
            new("Password", "Too short.")
        };
        var result = new ValidationResult(failures);

        var problem = result.ToValidationProblem();

        problem.Errors.Count.ShouldBe(2);
        problem.Errors.ShouldContainKey("Email");
        problem.Errors.ShouldContainKey("Password");
    }

    [Fact]
    public void ToValidationProblem_ValidationResult_Empty_ReturnsEmptyErrors()
    {
        var result = new ValidationResult(new List<ValidationFailure>());

        var problem = result.ToValidationProblem();

        problem.Errors.ShouldBeEmpty();
    }

    [Fact]
    public void ToValidationProblem_ValidationResult_ReturnsValidationProblemDetailsType()
    {
        var result = new ValidationResult(new List<ValidationFailure> { new("X", "err") });

        var problem = result.ToValidationProblem();

        problem.ShouldBeOfType<ValidationProblemDetails>();
    }

    // ─── ToValidationProblem(ValidationException) ────────────────────────

    [Fact]
    public void ToValidationProblem_ValidationException_WithSingleError_ReturnsProblemDetailsWithOneField()
    {
        var failures = new List<ValidationFailure> { new("Name", "Name is required.") };
        var ex = new ValidationException(failures);

        var problem = ex.ToValidationProblem();

        problem.ShouldNotBeNull();
        problem.Errors.ShouldContainKey("Name");
        problem.Errors["Name"].ShouldContain("Name is required.");
    }

    [Fact]
    public void ToValidationProblem_ValidationException_WithMultipleErrorsSameField_GroupsUnderSameKey()
    {
        var failures = new List<ValidationFailure>
        {
            new("Cnpj", "CNPJ is required."),
            new("Cnpj", "CNPJ format is invalid.")
        };
        var ex = new ValidationException(failures);

        var problem = ex.ToValidationProblem();

        problem.Errors["Cnpj"].Length.ShouldBe(2);
    }

    [Fact]
    public void ToValidationProblem_ValidationException_ReturnsValidationProblemDetailsType()
    {
        var ex = new ValidationException(new List<ValidationFailure>
        {
            new("Field", "Error.")
        });

        var result = ex.ToValidationProblem();

        result.ShouldBeOfType<ValidationProblemDetails>();
    }
}
