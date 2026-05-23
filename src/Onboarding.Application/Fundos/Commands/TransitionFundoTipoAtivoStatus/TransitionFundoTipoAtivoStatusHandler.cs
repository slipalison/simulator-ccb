using FluentValidation;
using Microsoft.Extensions.Logging;
using Onboarding.Application.Common;
using Onboarding.Application.Fundos.Commands.CreateFundoTipoAtivo;
using Onboarding.Application.Fundos.DTOs;
using Onboarding.Domain.Aggregates.Audit;
using Onboarding.Domain.Aggregates.FundoTipoAtivoAggregate;
using Onboarding.Domain.Repositories;

namespace Onboarding.Application.Fundos.Commands.TransitionFundoTipoAtivoStatus;

public sealed class TransitionFundoTipoAtivoStatusHandler
    : ICommandHandler<TransitionFundoTipoAtivoStatusCommand, RelFundoTipoAtivoDto>
{
    private readonly IFundoTipoAtivoAggregateRepository _repository;
    private readonly IAuditService _auditService;
    private readonly IValidator<TransitionFundoTipoAtivoStatusCommand> _validator;
    private readonly ILogger<TransitionFundoTipoAtivoStatusHandler> _logger;

    public TransitionFundoTipoAtivoStatusHandler(
        IFundoTipoAtivoAggregateRepository repository,
        IAuditService auditService,
        IValidator<TransitionFundoTipoAtivoStatusCommand> validator,
        ILogger<TransitionFundoTipoAtivoStatusHandler> logger)
    {
        _repository = repository;
        _auditService = auditService;
        _validator = validator;
        _logger = logger;
    }

    public async Task<RelFundoTipoAtivoDto> HandleAsync(
        TransitionFundoTipoAtivoStatusCommand command, CancellationToken ct = default)
    {
        var validation = await _validator.ValidateAsync(command, ct).ConfigureAwait(false);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var association = await _repository.GetByIdAsync(command.AssociationId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException(
                $"FundoTipoAtivo association with ID '{command.AssociationId}' not found.");

        var previousStatus = association.Status;
        association.TransitionTo(command.NewStatus);

        await _repository.SaveAsync(association, ct).ConfigureAwait(false);

        _logger.LogInformation("FundoTipoAtivo {Id} status: {From} → {To}",
            command.AssociationId, previousStatus, command.NewStatus);

        await _auditService.RecordAsync(
            command.ActorSub, command.ActorEmail,
            ActionType.RelFundoTipoAtivoStatusChanged,
            association.Id, $"Fundo={association.FundoId}/TipoAtivo={association.TipoAtivoId}",
            $"Status changed from {previousStatus} to {command.NewStatus}",
            entityType: "FundoTipoAtivo",
            entityId: association.Id,
            ct: ct).ConfigureAwait(false);

        return CreateFundoTipoAtivoHandler.ToDto(association);
    }
}
