using FluentValidation;
using Microsoft.Extensions.Logging;
using Onboarding.Application.Common;
using Onboarding.Application.Fundos.Commands.CreateFundoCedente;
using Onboarding.Application.Fundos.DTOs;
using Onboarding.Domain.Aggregates.Audit;
using Onboarding.Domain.Repositories;
using Onboarding.Domain.ValueObjects;

namespace Onboarding.Application.Fundos.Commands.UpdateFundoCedenteLimite;

/// <summary>
/// Handler: update exposure limits on a FundoCedente association.
/// Domain enforces that HISTORICO associations cannot be updated (D-22).
/// </summary>
public sealed class UpdateFundoCedenteLimiteHandler
    : ICommandHandler<UpdateFundoCedenteLimiteCommand, RelFundoCedenteDto>
{
    private readonly IFundoCedenteAggregateRepository _repository;
    private readonly IAuditService _auditService;
    private readonly IValidator<UpdateFundoCedenteLimiteCommand> _validator;
    private readonly ILogger<UpdateFundoCedenteLimiteHandler> _logger;

    public UpdateFundoCedenteLimiteHandler(
        IFundoCedenteAggregateRepository repository,
        IAuditService auditService,
        IValidator<UpdateFundoCedenteLimiteCommand> validator,
        ILogger<UpdateFundoCedenteLimiteHandler> logger)
    {
        _repository = repository;
        _auditService = auditService;
        _validator = validator;
        _logger = logger;
    }

    public async Task<RelFundoCedenteDto> HandleAsync(
        UpdateFundoCedenteLimiteCommand command, CancellationToken ct = default)
    {
        var validation = await _validator.ValidateAsync(command, ct).ConfigureAwait(false);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var association = await _repository.GetByIdAsync(command.AssociationId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException(
                $"FundoCedente association with ID '{command.AssociationId}' not found.");

        var limite = LimiteExposicao.Create(command.LimitePercentual, command.LimiteValor);

        // Domain enforces: HISTORICO associations cannot be updated (throws InvalidStateTransitionException)
        association.UpdateLimite(limite);

        await _repository.SaveAsync(association, ct).ConfigureAwait(false);

        _logger.LogInformation(
            "FundoCedente {Id} limits updated", command.AssociationId);

        await _auditService.RecordAsync(
            actorSub: command.ActorSub,
            actorEmail: command.ActorEmail,
            action: ActionType.RelFundoCedenteLimiteUpdated,
            targetUserId: association.Id,
            targetUserName: $"Fundo={association.FundoId}/Cedente={association.CedenteId}",
            details: $"Limits updated: Percentual={command.LimitePercentual}, Valor={command.LimiteValor}",
            ct: ct).ConfigureAwait(false);

        return CreateFundoCedenteHandler.ToDto(association);
    }
}
