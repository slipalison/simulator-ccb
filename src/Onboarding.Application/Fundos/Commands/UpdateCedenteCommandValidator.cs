using FluentValidation;
using Onboarding.Domain.Aggregates.CedenteAggregate;

namespace Onboarding.Application.Fundos.Commands;

/// <summary>
/// FluentValidation for UpdateCedenteCommand (CAD-17).
/// </summary>
public sealed class UpdateCedenteCommandValidator
    : AbstractValidator<UpdateCedenteCommand>
{
    public UpdateCedenteCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Id is required.");

        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("Nome is required.");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Invalid CedenteStatus.");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Invalid email format.")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));
    }
}