using FluentValidation;
using Onboarding.Application.Admin.Commands;

namespace Onboarding.Application.Admin.Validators;

public sealed class UnblockUserCommandValidator : AbstractValidator<UnblockUserCommand>
{
    public UnblockUserCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");
    }
}
