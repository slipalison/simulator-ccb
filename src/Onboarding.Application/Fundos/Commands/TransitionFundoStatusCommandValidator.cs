using FluentValidation;
using Onboarding.Domain.Aggregates.FundoAggregate;

namespace Onboarding.Application.Fundos.Commands;

/// <summary>
/// FluentValidation for TransitionFundoStatusCommand (CAD-13).
/// Validates FundoId is non-empty and NewStatus is a valid FundoStatus enum value.
/// Domain layer (FundoStatusValidator) enforces the actual transition rules.
/// </summary>
public sealed class TransitionFundoStatusCommandValidator
    : AbstractValidator<TransitionFundoStatusCommand>
{
    public TransitionFundoStatusCommandValidator()
    {
        RuleFor(x => x.FundoId)
            .NotEmpty().WithMessage("FundoId is required.");

        RuleFor(x => x.NewStatus)
            .IsInEnum().WithMessage("Invalid FundoStatus value.");
    }
}