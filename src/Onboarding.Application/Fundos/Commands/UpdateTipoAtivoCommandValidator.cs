using FluentValidation;
using Onboarding.Domain.Aggregates.TipoAtivoAggregate;

namespace Onboarding.Application.Fundos.Commands;

/// <summary>
/// FluentValidation for UpdateTipoAtivoCommand (CAD-21).
/// </summary>
public sealed class UpdateTipoAtivoCommandValidator
    : AbstractValidator<UpdateTipoAtivoCommand>
{
    public UpdateTipoAtivoCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Id is required.");

        RuleFor(x => x.Descricao)
            .NotEmpty().WithMessage("Descricao is required.");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Invalid TipoAtivoStatus.");
    }
}