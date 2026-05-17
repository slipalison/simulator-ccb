using FluentValidation;
using Onboarding.Domain.Aggregates.CustodianteAggregate;

namespace Onboarding.Application.Fundos.Commands;

/// <summary>
/// FluentValidation for UpdateCustodianteCommand (CAD-07).
/// </summary>
public sealed class UpdateCustodianteCommandValidator
    : AbstractValidator<UpdateCustodianteCommand>
{
    public UpdateCustodianteCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Id is required.");

        RuleFor(x => x.RazaoSocial)
            .NotEmpty().WithMessage("Razão Social is required.");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Invalid Custodiante status.");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Invalid email format.")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));
    }
}