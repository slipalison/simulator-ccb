using FluentValidation;
using Onboarding.Application.Admin.Commands;

namespace Onboarding.Application.Admin.Validators;

public sealed class BlockUserCommandValidator : AbstractValidator<BlockUserCommand>
{
    public BlockUserCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");
    }
}
