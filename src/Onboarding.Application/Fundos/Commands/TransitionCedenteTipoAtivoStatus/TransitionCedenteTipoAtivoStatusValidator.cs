using FluentValidation;

namespace Onboarding.Application.Fundos.Commands.TransitionCedenteTipoAtivoStatus;

public sealed class TransitionCedenteTipoAtivoStatusValidator
    : AbstractValidator<TransitionCedenteTipoAtivoStatusCommand>
{
    public TransitionCedenteTipoAtivoStatusValidator()
    {
        RuleFor(x => x.AssociationId).NotEmpty();
        RuleFor(x => x.NewStatus).IsInEnum();
        RuleFor(x => x.ActorSub).NotEmpty();
        RuleFor(x => x.ActorEmail).NotEmpty();
    }
}
