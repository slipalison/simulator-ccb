using FluentValidation;

namespace Onboarding.Application.Fundos.Commands.TransitionFundoCedenteStatus;

/// <summary>
/// FluentValidation for TransitionFundoCedenteStatusCommand.
/// </summary>
public sealed class TransitionFundoCedenteStatusValidator
    : AbstractValidator<TransitionFundoCedenteStatusCommand>
{
    public TransitionFundoCedenteStatusValidator()
    {
        RuleFor(x => x.AssociationId)
            .NotEmpty().WithMessage("AssociationId is required.");

        RuleFor(x => x.NewStatus)
            .IsInEnum().WithMessage("Invalid NewStatus value.");

        RuleFor(x => x.ActorSub)
            .NotEmpty().WithMessage("ActorSub is required.");

        RuleFor(x => x.ActorEmail)
            .NotEmpty().WithMessage("ActorEmail is required.");
    }
}
