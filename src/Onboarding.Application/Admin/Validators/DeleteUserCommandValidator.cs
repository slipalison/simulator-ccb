using FluentValidation;
using Onboarding.Application.Admin.Commands;

namespace Onboarding.Application.Admin.Validators;

public sealed class DeleteUserCommandValidator : AbstractValidator<DeleteUserCommand>
{
    public DeleteUserCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");

        RuleFor(x => x.ConfirmEmail)
            .NotEmpty().WithMessage("ConfirmEmail is required.")
            .EmailAddress().WithMessage("ConfirmEmail format is invalid.");
    }
}
