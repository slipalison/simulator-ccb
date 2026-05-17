using FluentValidation;
using Microsoft.Extensions.Logging;
using Onboarding.Application.Common;
using Onboarding.Application.Fundos.Commands.CreateCedenteTipoAtivo;
using Onboarding.Application.Fundos.DTOs;
using Onboarding.Domain.Aggregates.Audit;
using Onboarding.Domain.Aggregates.CedenteTipoAtivoAggregate;
using Onboarding.Domain.Repositories;

namespace Onboarding.Application.Fundos.Commands.TransitionCedenteTipoAtivoStatus;

public sealed class TransitionCedenteTipoAtivoStatusHandler
    : ICommandHandler<TransitionCedenteTipoAtivoStatusCommand, RelCedenteTipoAtivoDto>
{
    private readonly ICedenteTipoAtivoAggregateRepository _repository;
    private readonly IAuditService _auditService;
    private readonly IValidator<TransitionCedenteTipoAtivoStatusCommand> _validator;
    private readonly ILogger<TransitionCedenteTipoAtivoStatusHandler> _logger;

    public TransitionCedenteTipoAtivoStatusHandler(
        ICedenteTipoAtivoAggregateRepository repository,
        IAuditService auditService,
        IValidator<TransitionCedenteTipoAtivoStatusCommand> validator,
        ILogger<TransitionCedenteTipoAtivoStatusHandler> logger)
    {
        _repository = repository;
        _auditService = auditService;
        _validator = validator;
        _logger = logger;
    }

    public async Task<RelCedenteTipoAtivoDto> HandleAsync(
        TransitionCedenteTipoAtivoStatusCommand command, CancellationToken ct = default)
    {
        var validation = await _validator.ValidateAsync(command, ct).ConfigureAwait(false);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var association = await _repository.GetByIdAsync(command.AssociationId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException(
                $"CedenteTipoAtivo association with ID '{command.AssociationId}' not found.");

        var previousStatus = association.Status;
        association.TransitionTo(command.NewStatus);

        await _repository.SaveAsync(association, ct).ConfigureAwait(false);

        _logger.LogInformation("CedenteTipoAtivo {Id} status: {From} → {To}",
            command.AssociationId, previousStatus, command.NewStatus);

        await _auditService.RecordAsync(
            command.ActorSub, command.ActorEmail,
            ActionType.RelCedenteTipoAtivoStatusChanged,
            association.Id, $"Cedente={association.CedenteId}/TipoAtivo={association.TipoAtivoId}",
            $"Status changed from {previousStatus} to {command.NewStatus}",
            ct: ct).ConfigureAwait(false);

        return CreateCedenteTipoAtivoHandler.ToDto(association);
    }
}
