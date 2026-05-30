using Microsoft.AspNetCore.Mvc;

namespace Onboarding.API.Extensions;

/// <summary>
/// Shared validation-to-ProblemDetails helpers used by all API controllers.
/// DRY-01 fix: previously duplicated in FundosController, AdminUserController,
/// CedenteTiposAtivosController, FundoCedentesController, FundoTiposAtivosController.
/// </summary>
public static class ValidationExtensions
{
    /// <summary>
    /// Converts a FluentValidation <see cref="FluentValidation.Results.ValidationResult"/>
    /// to a <see cref="ValidationProblemDetails"/> suitable for a 422 response.
    /// Used by controllers that run validation manually before dispatching commands.
    /// </summary>
    public static ValidationProblemDetails ToValidationProblem(
        this FluentValidation.Results.ValidationResult result)
        => new(result.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));

    /// <summary>
    /// Converts a <see cref="FluentValidation.ValidationException"/> (thrown by validators
    /// called inline in handlers) to a <see cref="ValidationProblemDetails"/> for a 422 response.
    /// Used by the relationship controllers (FundoCedentes, FundoTiposAtivos, CedenteTiposAtivos).
    /// </summary>
    public static ValidationProblemDetails ToValidationProblem(
        this FluentValidation.ValidationException ex)
        => new(ex.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
}
