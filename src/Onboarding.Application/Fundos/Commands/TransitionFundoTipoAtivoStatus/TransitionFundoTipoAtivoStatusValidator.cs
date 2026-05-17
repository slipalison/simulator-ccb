using FluentValidation;

namespace Onboarding.Application.Fundos.Commands.TransitionFundoTipoAtivoStatus;

public sealed class TransitionFundoTipoAtivoStatusValidator
    : AbstractValidator<TransitionFundoTipoAtivoStatusCommand>
{
    public TransitionFundoTipoAtivoStatusValidator()
    {
        RuleFor(x => x.AssociationId).NotEmpty();
        RuleFor(x => x.NewStatus).IsInEnum();
        RuleFor(x => x.ActorSub).NotEmpty();
        RuleFor(x => x.ActorEmail).NotEmpty();
    }
}
