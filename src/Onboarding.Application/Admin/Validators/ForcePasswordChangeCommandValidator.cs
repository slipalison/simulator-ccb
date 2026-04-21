using FluentValidation;
using Onboarding.Application.Admin.Commands;

namespace Onboarding.Application.Admin.Validators;

public sealed class ForcePasswordChangeCommandValidator : AbstractValidator<ForcePasswordChangeCommand>
{
    public ForcePasswordChangeCommandValidator()
    {
        RuleFor(x => x.KeycloakUserId)
            .NotEmpty().WithMessage("User ID is required.");

        RuleFor(x => x.AdminEmail)
            .NotEmpty().WithMessage("Admin email is required.")
            .EmailAddress().WithMessage("Invalid email format.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches(@"\d").WithMessage("Password must contain at least one digit.")
            .Matches(@"[!@#$%^&*]").WithMessage("Password must contain at least one special character (!@#$%^&*).");
    }
}
