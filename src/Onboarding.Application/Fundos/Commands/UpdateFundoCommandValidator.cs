using FluentValidation;

namespace Onboarding.Application.Fundos.Commands;

/// <summary>
/// FluentValidation for UpdateFundoCommand (CAD-11).
/// </summary>
public sealed class UpdateFundoCommandValidator
    : AbstractValidator<UpdateFundoCommand>
{
    public UpdateFundoCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Id is required.");

        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("Nome is required.");
    }
}