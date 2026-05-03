using FluentValidation;
using Onboarding.Domain.ValueObjects;

namespace Onboarding.Application.Fundos.Commands;

/// <summary>
/// FluentValidation for RegisterCedentePjCommand (CAD-15, T-47-01).
/// CNPJ validated via Cnpj.Create() — catches invalid check digits and format.
/// </summary>
public sealed class RegisterCedentePjCommandValidator
    : AbstractValidator<RegisterCedentePjCommand>
{
    public RegisterCedentePjCommandValidator()
    {
        RuleFor(x => x.Cnpj)
            .NotEmpty().WithMessage("CNPJ is required.")
            .Must(BeValidCnpj).WithMessage("Invalid CNPJ.");

        RuleFor(x => x.RazaoSocial)
            .NotEmpty().WithMessage("Razão Social is required.");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Invalid email format.")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));
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