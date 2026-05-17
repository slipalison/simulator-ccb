using FluentValidation;

namespace Onboarding.Application.Fundos.Commands.CreateFundoTipoAtivo;

public sealed class CreateFundoTipoAtivoValidator
    : AbstractValidator<CreateFundoTipoAtivoCommand>
{
    public CreateFundoTipoAtivoValidator()
    {
        RuleFor(x => x.FundoId).NotEmpty().WithMessage("FundoId is required.");
        RuleFor(x => x.TipoAtivoId).NotEmpty().WithMessage("TipoAtivoId is required.");

        RuleFor(x => x.LimitePercentual)
            .InclusiveBetween(0m, 100m)
            .When(x => x.LimitePercentual.HasValue);

        RuleFor(x => x.LimiteValor)
            .GreaterThan(0m)
            .When(x => x.LimiteValor.HasValue);

        RuleFor(x => x)
            .Must(x => x.LimitePercentual.HasValue || x.LimiteValor.HasValue)
            .WithMessage("At least one of LimitePercentual or LimiteValor must be provided.");

        RuleFor(x => x.DataInicio).NotEmpty();

        RuleFor(x => x.DataFim)
            .GreaterThan(x => x.DataInicio)
            .When(x => x.DataFim.HasValue);

        RuleFor(x => x.ActorSub).NotEmpty();
        RuleFor(x => x.ActorEmail).NotEmpty();
    }
}
