using FluentValidation;
using Onboarding.Domain.Aggregates.TipoAtivoAggregate;

namespace Onboarding.Application.Fundos.Commands;

/// <summary>
/// FluentValidation for CreateTipoAtivoCommand (CAD-19, T-47-04).
/// TipoAtivo is global (D-03) — no company scope in uniqueness check.
/// </summary>
public sealed class CreateTipoAtivoCommandValidator
    : AbstractValidator<CreateTipoAtivoCommand>
{
    public CreateTipoAtivoCommandValidator()
    {
        RuleFor(x => x.Codigo)
            .NotEmpty().WithMessage("Codigo is required.");

        RuleFor(x => x.Descricao)
            .NotEmpty().WithMessage("Descricao is required.");

        RuleFor(x => x.Categoria)
            .IsInEnum().WithMessage("Invalid TipoAtivoCategoria.");
    }
}