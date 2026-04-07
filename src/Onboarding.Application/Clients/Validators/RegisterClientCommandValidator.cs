using FluentValidation;
using Onboarding.Application.Clients.Commands;

namespace Onboarding.Application.Clients.Validators;

public sealed class RegisterClientCommandValidator
    : AbstractValidator<RegisterClientCommand>
{
    public RegisterClientCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotNull().WithMessage("Email is required.")
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email format is invalid.")
            .MaximumLength(320).WithMessage("Email must be at most 320 characters.");

        RuleFor(x => x.Phone)
            .NotNull().WithMessage("Phone is required.")
            .NotEmpty().WithMessage("Phone is required.")
            .MaximumLength(20).WithMessage("Phone must be at most 20 characters.");

        RuleFor(x => x.Password)
            .NotNull().WithMessage("Password is required.")
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.");

        // Exatamente um documento deve ser fornecido
        RuleFor(x => x)
            .Must(x => (x.Cpf is not null) != (x.Cnpj is not null))
            .WithMessage("Provide either CPF (Pessoa Física) or CNPJ (Pessoa Jurídica), not both.")
            .OverridePropertyName("DocumentType");

        // Regras específicas para PF (CPF presente)
        When(x => x.Cpf is not null, () =>
        {
            RuleFor(x => x.Nome)
                .NotEmpty().WithMessage("Nome is required for Pessoa Física.")
                .MaximumLength(200).WithMessage("Nome must be at most 200 characters.");

            RuleFor(x => x.Cpf!)
                .Must(IsValidCpfStructure)
                .WithMessage("CPF must contain 11 digits.");
        });

        // Regras específicas para PJ (CNPJ presente)
        When(x => x.Cnpj is not null, () =>
        {
            RuleFor(x => x.RazaoSocial)
                .NotEmpty().WithMessage("Razão Social is required for Pessoa Jurídica.")
                .MaximumLength(200).WithMessage("Razão Social must be at most 200 characters.");

            RuleFor(x => x.Cnpj!)
                .Must(IsValidCnpjStructure)
                .WithMessage("CNPJ must contain 14 alphanumeric characters.");
        });
    }

    // Structural format check only — deep check-digit validation is in the domain value object
    private static bool IsValidCpfStructure(string? cpf) =>
        cpf is not null &&
        System.Text.RegularExpressions.Regex.IsMatch(
            cpf.Replace(".", "").Replace("-", ""), @"^\d{11}$");

    // Supports both numeric CNPJ (current) and alphanumeric format (July 2026 — REG-04)
    private static bool IsValidCnpjStructure(string? cnpj) =>
        cnpj is not null &&
        System.Text.RegularExpressions.Regex.IsMatch(
            cnpj.Replace(".", "").Replace("/", "").Replace("-", ""), @"^[A-Z0-9]{14}$",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
}
