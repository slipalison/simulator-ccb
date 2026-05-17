using FluentValidation;

namespace Onboarding.Application.Fundos.Commands.CreateFundoCedente;

/// <summary>
/// FluentValidation for CreateFundoCedenteCommand.
/// At least one of LimitePercentual or LimiteValor required (PLAN T-1 rule, D-21).
/// </summary>
public sealed class CreateFundoCedenteValidator
    : AbstractValidator<CreateFundoCedenteCommand>
{
    public CreateFundoCedenteValidator()
    {
        RuleFor(x => x.FundoId)
            .NotEmpty().WithMessage("FundoId is required.");

        RuleFor(x => x.CedenteId)
            .NotEmpty().WithMessage("CedenteId is required.");

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

        RuleFor(x => x.DataInicio)
            .NotEmpty().WithMessage("DataInicio is required.");

        RuleFor(x => x.DataFim)
            .GreaterThan(x => x.DataInicio)
            .When(x => x.DataFim.HasValue)
            .WithMessage("DataFim must be after DataInicio.");

        RuleFor(x => x.ActorSub)
            .NotEmpty().WithMessage("ActorSub is required.");

        RuleFor(x => x.ActorEmail)
            .NotEmpty().WithMessage("ActorEmail is required.");
    }
}
