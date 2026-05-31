using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.DependencyInjection;
using Onboarding.Application.Common;

namespace Onboarding.Infrastructure.Dispatch;

/// <summary>
/// Resolves <c>IValidator&lt;T&gt;</c> from <see cref="IServiceProvider"/> and runs validation.
/// If no validator is registered for <typeparamref name="T"/> the result is a valid no-op
/// <see cref="ValidationResult"/> so callers without validators are not broken.
///
/// D-61: validation is explicit in the controller; this runner is the single injection point
/// replacing N individual <c>IValidator&lt;T&gt;</c> fields.
/// </summary>
internal sealed class ValidationRunner(IServiceProvider sp) : IValidationRunner
{
    public Task<ValidationResult> Validate<T>(T instance, CancellationToken ct = default)
    {
        var validator = sp.GetService<IValidator<T>>();
        if (validator is null)
            return Task.FromResult(new ValidationResult());

        return validator.ValidateAsync(instance, ct);
    }
}
