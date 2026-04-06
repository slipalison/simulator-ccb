using FluentValidation;
using Onboarding.Application.Auth.Commands;

namespace Onboarding.Application.Auth.Validators;

public sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("refresh_token is required.");
    }
}
