using FluentValidation;
using Microsoft.Extensions.Logging;
using Onboarding.Application.Common;
using Onboarding.Application.Fundos.Commands.CreateCedenteTipoAtivo;
using Onboarding.Application.Fundos.DTOs;
using Onboarding.Domain.Aggregates.Audit;
using Onboarding.Domain.Aggregates.CedenteTipoAtivoAggregate;
using Onboarding.Domain.Repositories;
using Onboarding.Domain.ValueObjects;

namespace Onboarding.Application.Fundos.Commands.UpdateCedenteTipoAtivoLimite;

public sealed class UpdateCedenteTipoAtivoLimiteHandler
    : ICommandHandler<UpdateCedenteTipoAtivoLimiteCommand, RelCedenteTipoAtivoDto>
{
    private readonly ICedenteTipoAtivoAggregateRepository _repository;
    private readonly IAuditService _auditService;
    private readonly IValidator<UpdateCedenteTipoAtivoLimiteCommand> _validator;
    private readonly ILogger<UpdateCedenteTipoAtivoLimiteHandler> _logger;

    public UpdateCedenteTipoAtivoLimiteHandler(
        ICedenteTipoAtivoAggregateRepository repository,
        IAuditService auditService,
        IValidator<UpdateCedenteTipoAtivoLimiteCommand> validator,
        ILogger<UpdateCedenteTipoAtivoLimiteHandler> logger)
    {
        _repository = repository;
        _auditService = auditService;
        _validator = validator;
        _logger = logger;
    }

    public async Task<RelCedenteTipoAtivoDto> HandleAsync(
        UpdateCedenteTipoAtivoLimiteCommand command, CancellationToken ct = default)
    {
        var validation = await _validator.ValidateAsync(command, ct).ConfigureAwait(false);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var association = await _repository.GetByIdAsync(command.AssociationId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException(
                $"CedenteTipoAtivo association with ID '{command.AssociationId}' not found.");

        var limite = LimiteExposicao.Create(command.LimitePercentual, command.LimiteValor);
        association.UpdateLimite(limite);

        await _repository.SaveAsync(association, ct).ConfigureAwait(false);

        _logger.LogInformation("CedenteTipoAtivo {Id} limits updated", command.AssociationId);

        await _auditService.RecordAsync(
            command.ActorSub, command.ActorEmail,
            ActionType.RelCedenteTipoAtivoLimiteUpdated,
            association.Id, $"Cedente={association.CedenteId}/TipoAtivo={association.TipoAtivoId}",
            $"Limits updated: Percentual={command.LimitePercentual}, Valor={command.LimiteValor}",
            ct: ct).ConfigureAwait(false);

        return CreateCedenteTipoAtivoHandler.ToDto(association);
    }
}
