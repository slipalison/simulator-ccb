using FluentValidation;
using Onboarding.Domain.ValueObjects;

namespace Onboarding.Application.Companies.Commands;

/// <summary>
/// FluentValidation for RegisterCompanyCommand (T-38-01, REG-02).
/// Password policy matches Keycloak: 8+ chars, 1 upper, 1 lower, 1 digit, 1 special.
/// CNPJ validated via Cnpj.Create() — catches invalid check digits and format.
/// Terms acceptance is mandatory (D-12).
/// </summary>
public sealed class RegisterCompanyCommandValidator : AbstractValidator<RegisterCompanyCommand>
{
    public RegisterCompanyCommandValidator()
    {
        RuleFor(x => x.RazaoSocial)
            .NotEmpty().WithMessage("Razão Social is required.");

        RuleFor(x => x.Cnpj)
            .NotEmpty().WithMessage("CNPJ is required.")
            .Must(BeValidCnpj).WithMessage("Invalid CNPJ.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Phone is required.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .Must(HaveUppercase).WithMessage("Password must contain at least one uppercase letter.")
            .Must(HaveLowercase).WithMessage("Password must contain at least one lowercase letter.")
            .Must(HaveDigit).WithMessage("Password must contain at least one digit.")
            .Must(HaveSpecialChar).WithMessage("Password must contain at least one special character.");

        RuleFor(x => x.TermsAccepted)
            .Must(term => term == true).WithMessage("Terms acceptance is required.");

        RuleFor(x => x.TermsVersion)
            .NotEmpty().WithMessage("Terms version is required.");
    }

    private static bool BeValidCnpj(string cnpj)
    {
        try
        {
            Cnpj.Create(cnpj);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static bool HaveUppercase(string password) =>
        password.Any(char.IsUpper);

    private static bool HaveLowercase(string password) =>
        password.Any(char.IsLower);

    private static bool HaveDigit(string password) =>
        password.Any(char.IsDigit);

    private static bool HaveSpecialChar(string password) =>
        password.Any(c => !char.IsLetterOrDigit(c));
}