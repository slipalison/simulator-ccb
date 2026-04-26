using FluentValidation;
using Onboarding.Domain.ValueObjects;

namespace Onboarding.Application.Companies.Commands;

/// <summary>
/// Validator for RegisterEmployeeCommand — ensures CPF is valid, email format is correct, 
/// and all required fields are present (MGMT-01).
/// </summary>
public sealed class RegisterEmployeeCommandValidator : AbstractValidator<RegisterEmployeeCommand>
{
    public RegisterEmployeeCommandValidator()
    {
        RuleFor(x => x.Nome).NotEmpty().WithMessage("Nome is required.");
        RuleFor(x => x.Cpf).NotEmpty().WithMessage("CPF is required.")
            .Must(BeValidCpf).WithMessage("Invalid CPF format or check digits.");
        RuleFor(x => x.Email).NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.");
        RuleFor(x => x.Phone).NotEmpty().WithMessage("Phone is required.");
        RuleFor(x => x.CompanyId).NotEmpty().WithMessage("CompanyId is required.");
    }

    private static bool BeValidCpf(string cpf)
    {
        try
        {
            Cpf.Create(cpf);
            return true;
        }
        catch
        {
            return false;
        }
    }
}