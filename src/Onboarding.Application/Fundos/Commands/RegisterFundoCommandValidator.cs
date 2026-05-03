using FluentValidation;
using Onboarding.Domain.Aggregates.FundoAggregate;
using Onboarding.Domain.ValueObjects;

namespace Onboarding.Application.Fundos.Commands;

/// <summary>
/// FluentValidation for RegisterFundoCommand (CAD-09, T-47-01).
/// CNPJ validated via Cnpj.Create() — catches invalid check digits and format.
/// </summary>
public sealed class RegisterFundoCommandValidator
    : AbstractValidator<RegisterFundoCommand>
{
    public RegisterFundoCommandValidator()
    {
        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("Nome is required.");

        RuleFor(x => x.Cnpj)
            .NotEmpty().WithMessage("CNPJ is required.")
            .Must(BeValidCnpj).WithMessage("Invalid CNPJ.");

        RuleFor(x => x.ConsultoriaFundoId)
            .NotEmpty().WithMessage("ConsultoriaFundoId is required.");

        RuleFor(x => x.CustodianteId)
            .NotEmpty().WithMessage("CustodianteId is required.");

        RuleFor(x => x.TipoFundo)
            .IsInEnum().WithMessage("Invalid TipoFundo.");
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