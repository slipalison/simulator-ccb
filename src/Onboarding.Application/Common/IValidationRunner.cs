using FluentValidation.Results;

namespace Onboarding.Application.Common;

/// <summary>
/// Resolves <c>IValidator&lt;T&gt;</c> from the DI container and runs validation.
/// D-61: validation stays explicit in the controller (not moved to the dispatcher).
/// Missing validator → returns a valid no-op <see cref="ValidationResult"/> (safe default).
/// </summary>
public interface IValidationRunner
{
    /// <summary>
    /// Validates <paramref name="instance"/> using the registered <c>IValidator&lt;T&gt;</c>.
    /// If no validator is registered for <typeparamref name="T"/> the result is always valid.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <param name="instance">The instance to validate.</param>
    /// <param name="ct">Cancellation token propagated to the validator.</param>
    Task<ValidationResult> Validate<T>(T instance, CancellationToken ct = default);
}
