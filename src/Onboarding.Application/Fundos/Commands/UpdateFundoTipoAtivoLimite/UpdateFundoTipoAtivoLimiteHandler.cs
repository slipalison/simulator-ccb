using FluentValidation;
using Microsoft.Extensions.Logging;
using Onboarding.Application.Common;
using Onboarding.Application.Fundos.Commands.CreateFundoTipoAtivo;
using Onboarding.Application.Fundos.DTOs;
using Onboarding.Domain.Aggregates.Audit;
using Onboarding.Domain.Aggregates.FundoTipoAtivoAggregate;
using Onboarding.Domain.Repositories;
using Onboarding.Domain.ValueObjects;

namespace Onboarding.Application.Fundos.Commands.UpdateFundoTipoAtivoLimite;

public sealed class UpdateFundoTipoAtivoLimiteHandler
    : ICommandHandler<UpdateFundoTipoAtivoLimiteCommand, RelFundoTipoAtivoDto>
{
    private readonly IFundoTipoAtivoAggregateRepository _repository;
    private readonly IAuditService _auditService;
    private readonly IValidator<UpdateFundoTipoAtivoLimiteCommand> _validator;
    private readonly ILogger<UpdateFundoTipoAtivoLimiteHandler> _logger;

    public UpdateFundoTipoAtivoLimiteHandler(
        IFundoTipoAtivoAggregateRepository repository,
        IAuditService auditService,
        IValidator<UpdateFundoTipoAtivoLimiteCommand> validator,
        ILogger<UpdateFundoTipoAtivoLimiteHandler> logger)
    {
        _repository = repository;
        _auditService = auditService;
        _validator = validator;
        _logger = logger;
    }

    public async Task<RelFundoTipoAtivoDto> HandleAsync(
        UpdateFundoTipoAtivoLimiteCommand command, CancellationToken ct = default)
    {
        var validation = await _validator.ValidateAsync(command, ct).ConfigureAwait(false);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var association = await _repository.GetByIdAsync(command.AssociationId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException(
                $"FundoTipoAtivo association with ID '{command.AssociationId}' not found.");

        var limite = LimiteExposicao.Create(command.LimitePercentual, command.LimiteValor);
        association.UpdateLimite(limite);

        await _repository.SaveAsync(association, ct).ConfigureAwait(false);

        _logger.LogInformation("FundoTipoAtivo {Id} limits updated", command.AssociationId);

        await _auditService.RecordAsync(
            command.ActorSub, command.ActorEmail,
            ActionType.RelFundoTipoAtivoLimiteUpdated,
            association.Id, $"Fundo={association.FundoId}/TipoAtivo={association.TipoAtivoId}",
            $"Limits updated: Percentual={command.LimitePercentual}, Valor={command.LimiteValor}",
            ct: ct).ConfigureAwait(false);

        return CreateFundoTipoAtivoHandler.ToDto(association);
    }
}
