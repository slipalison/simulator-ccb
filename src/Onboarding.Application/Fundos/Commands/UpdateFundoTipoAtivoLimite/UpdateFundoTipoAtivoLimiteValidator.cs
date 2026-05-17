using FluentValidation;

namespace Onboarding.Application.Fundos.Commands.UpdateFundoTipoAtivoLimite;

public sealed class UpdateFundoTipoAtivoLimiteValidator
    : AbstractValidator<UpdateFundoTipoAtivoLimiteCommand>
{
    public UpdateFundoTipoAtivoLimiteValidator()
    {
        RuleFor(x => x.AssociationId).NotEmpty();
        RuleFor(x => x.LimitePercentual).InclusiveBetween(0m, 100m).When(x => x.LimitePercentual.HasValue);
        RuleFor(x => x.LimiteValor).GreaterThan(0m).When(x => x.LimiteValor.HasValue);
        RuleFor(x => x)
            .Must(x => x.LimitePercentual.HasValue || x.LimiteValor.HasValue)
            .WithMessage("At least one of LimitePercentual or LimiteValor must be provided.");
        RuleFor(x => x.ActorSub).NotEmpty();
        RuleFor(x => x.ActorEmail).NotEmpty();
    }
}
