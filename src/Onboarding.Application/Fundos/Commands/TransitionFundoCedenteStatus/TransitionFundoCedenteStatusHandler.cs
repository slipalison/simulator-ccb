using FluentValidation;
using Microsoft.Extensions.Logging;
using Onboarding.Application.Common;
using Onboarding.Application.Fundos.Commands.CreateFundoCedente;
using Onboarding.Application.Fundos.DTOs;
using Onboarding.Domain.Aggregates.Audit;
using Onboarding.Domain.Repositories;

namespace Onboarding.Application.Fundos.Commands.TransitionFundoCedenteStatus;

/// <summary>
/// Handler: state-machine status transition for FundoCedente (D-22).
/// Domain enforces valid transitions; HISTORICO is terminal.
/// Writes AdminAuditLog for every transition (D-22).
/// </summary>
public sealed class TransitionFundoCedenteStatusHandler
    : ICommandHandler<TransitionFundoCedenteStatusCommand, RelFundoCedenteDto>
{
    private readonly IFundoCedenteAggregateRepository _repository;
    private readonly IAuditService _auditService;
    private readonly IValidator<TransitionFundoCedenteStatusCommand> _validator;
    private readonly ILogger<TransitionFundoCedenteStatusHandler> _logger;

    public TransitionFundoCedenteStatusHandler(
        IFundoCedenteAggregateRepository repository,
        IAuditService auditService,
        IValidator<TransitionFundoCedenteStatusCommand> validator,
        ILogger<TransitionFundoCedenteStatusHandler> logger)
    {
        _repository = repository;
        _auditService = auditService;
        _validator = validator;
        _logger = logger;
    }

    public async Task<RelFundoCedenteDto> HandleAsync(
        TransitionFundoCedenteStatusCommand command, CancellationToken ct = default)
    {
        var validation = await _validator.ValidateAsync(command, ct).ConfigureAwait(false);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var association = await _repository.GetByIdAsync(command.AssociationId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException(
                $"FundoCedente association with ID '{command.AssociationId}' not found.");

        var previousStatus = association.Status;

        // Domain enforces state machine — throws InvalidStateTransitionException for invalid transitions
        association.TransitionTo(command.NewStatus);

        await _repository.SaveAsync(association, ct).ConfigureAwait(false);

        _logger.LogInformation(
            "FundoCedente {Id} status transitioned from {From} to {To}",
            command.AssociationId, previousStatus, command.NewStatus);

        await _auditService.RecordAsync(
            actorSub: command.ActorSub,
            actorEmail: command.ActorEmail,
            action: ActionType.RelFundoCedenteStatusChanged,
            targetUserId: association.Id,
            targetUserName: $"Fundo={association.FundoId}/Cedente={association.CedenteId}",
            details: $"Status changed from {previousStatus} to {command.NewStatus}",
            entityType: "FundoCedente",
            entityId: association.Id,
            ct: ct).ConfigureAwait(false);

        return CreateFundoCedenteHandler.ToDto(association);
    }
}
