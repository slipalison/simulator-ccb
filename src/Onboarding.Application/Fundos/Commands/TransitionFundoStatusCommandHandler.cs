using Microsoft.Extensions.Logging;
using Onboarding.Application.Common;
using Onboarding.Application.Fundos.DTOs;
using Onboarding.Domain.Aggregates.Audit;
using Onboarding.Domain.Aggregates.FundoAggregate;
using Onboarding.Domain.Exceptions;
using Onboarding.Domain.Repositories;

namespace Onboarding.Application.Fundos.Commands;

/// <summary>
/// Handler: transition Fundo status per state machine (CAD-13, D-02).
/// Calls fundo.TransitionTo() which enforces valid transitions and throws
/// InvalidStateTransitionException for invalid ones.
/// </summary>
public sealed class TransitionFundoStatusCommandHandler
    : ICommandHandler<TransitionFundoStatusCommand, FundoDto>
{
    private readonly IFundoRepository _repository;
    private readonly IAuditService _auditService;
    private readonly ILogger<TransitionFundoStatusCommandHandler> _logger;

    public TransitionFundoStatusCommandHandler(
        IFundoRepository repository,
        IAuditService auditService,
        ILogger<TransitionFundoStatusCommandHandler> logger)
    {
        _repository = repository;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<FundoDto> HandleAsync(
        TransitionFundoStatusCommand command, CancellationToken ct = default)
    {
        var fundo = await _repository.GetByIdAsync(command.FundoId, ct)
            ?? throw new KeyNotFoundException($"Fundo with ID '{command.FundoId}' not found.");

        var previousStatus = fundo.Status;

        // Domain enforces state machine — throws InvalidStateTransitionException for invalid transitions
        fundo.TransitionTo(command.NewStatus);

        await _repository.SaveAsync(fundo, ct);
        _logger.LogInformation("Fundo {Id} status transitioned from {From} to {To}",
            fundo.Id, previousStatus, command.NewStatus);

        await _auditService.RecordAsync(
            actorSub: command.ActorSub,
            actorEmail: command.ActorEmail,
            action: ActionType.FundoStatusChanged,
            targetUserId: fundo.Id,
            targetUserName: fundo.Nome,
            details: $"Fundo status changed from {previousStatus} to {command.NewStatus}",
            ct: ct);

        return new FundoDto(
            fundo.Id,
            fundo.Nome,
            fundo.Cnpj.Value,
            fundo.ConsultoriaFundoId,
            fundo.CustodianteId,
            fundo.TipoFundo,
            fundo.ClasseAnbima,
            fundo.Segmento,
            fundo.DataConstituicao,
            fundo.Status,
            fundo.CreatedAt);
    }
}