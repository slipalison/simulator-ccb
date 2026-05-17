using FluentValidation;
using Onboarding.Domain.Aggregates.ConsultoriaFundoAggregate;

namespace Onboarding.Application.Fundos.Commands;

/// <summary>
/// FluentValidation for UpdateConsultoriaFundoCommand (CAD-03).
/// </summary>
public sealed class UpdateConsultoriaFundoCommandValidator
    : AbstractValidator<UpdateConsultoriaFundoCommand>
{
    public UpdateConsultoriaFundoCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Id is required.");

        RuleFor(x => x.RazaoSocial)
            .NotEmpty().WithMessage("Razão Social is required.");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Invalid ConsultoriaFundo status.");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Invalid email format.")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));
    }
}