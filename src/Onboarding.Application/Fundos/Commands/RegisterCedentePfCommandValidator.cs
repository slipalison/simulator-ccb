using FluentValidation;
using Onboarding.Domain.ValueObjects;

namespace Onboarding.Application.Fundos.Commands;

/// <summary>
/// FluentValidation for RegisterCedentePfCommand (CAD-14, T-47-01).
/// CPF validated via Cpf.Create() — catches invalid check digits and format.
/// </summary>
public sealed class RegisterCedentePfCommandValidator
    : AbstractValidator<RegisterCedentePfCommand>
{
    public RegisterCedentePfCommandValidator()
    {
        RuleFor(x => x.Cpf)
            .NotEmpty().WithMessage("CPF is required.")
            .Must(BeValidCpf).WithMessage("Invalid CPF.");

        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("Nome is required.");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Invalid email format.")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));
    }

    private static bool BeValidCpf(string cpf)
    {
        try
        {
            Cpf.Create(cpf);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}