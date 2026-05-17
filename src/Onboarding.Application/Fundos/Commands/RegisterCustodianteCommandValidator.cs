using FluentValidation;
using Onboarding.Domain.ValueObjects;

namespace Onboarding.Application.Fundos.Commands;

/// <summary>
/// FluentValidation for RegisterCustodianteCommand (CAD-05, T-47-01).
/// CNPJ validated via Cnpj.Create() — catches invalid check digits and format.
/// </summary>
public sealed class RegisterCustodianteCommandValidator
    : AbstractValidator<RegisterCustodianteCommand>
{
    public RegisterCustodianteCommandValidator()
    {
        RuleFor(x => x.RazaoSocial)
            .NotEmpty().WithMessage("Razão Social is required.");

        RuleFor(x => x.Cnpj)
            .NotEmpty().WithMessage("CNPJ is required.")
            .Must(BeValidCnpj).WithMessage("Invalid CNPJ.");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Invalid email format.")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.Telefone)
            .NotEmpty().WithMessage("Telefone must not be empty when provided.")
            .When(x => !string.IsNullOrWhiteSpace(x.Telefone));
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
}