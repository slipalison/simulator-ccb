using FluentValidation;

namespace Onboarding.Application.Fundos.Commands.UpdateFundoCedenteLimite;

/// <summary>
/// FluentValidation for UpdateFundoCedenteLimiteCommand.
/// </summary>
public sealed class UpdateFundoCedenteLimiteValidator
    : AbstractValidator<UpdateFundoCedenteLimiteCommand>
{
    public UpdateFundoCedenteLimiteValidator()
    {
        RuleFor(x => x.AssociationId)
            .NotEmpty().WithMessage("AssociationId is required.");

        RuleFor(x => x.LimitePercentual)
            .InclusiveBetween(0m, 100m)
            .When(x => x.LimitePercentual.HasValue)
            .WithMessage("LimitePercentual must be between 0 and 100.");

        RuleFor(x => x.LimiteValor)
            .GreaterThan(0m)
            .When(x => x.LimiteValor.HasValue)
            .WithMessage("LimiteValor must be greater than 0.");

        RuleFor(x => x)
            .Must(x => x.LimitePercentual.HasValue || x.LimiteValor.HasValue)
            .WithMessage("At least one of LimitePercentual or LimiteValor must be provided.");

        RuleFor(x => x.ActorSub)
            .NotEmpty().WithMessage("ActorSub is required.");

        RuleFor(x => x.ActorEmail)
            .NotEmpty().WithMessage("ActorEmail is required.");
    }
}
