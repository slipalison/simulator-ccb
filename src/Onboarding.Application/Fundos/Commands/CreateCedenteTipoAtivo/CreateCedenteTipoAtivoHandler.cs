using FluentValidation;
using Microsoft.Extensions.Logging;
using Onboarding.Application.Common;
using Onboarding.Application.Fundos.DTOs;
using Onboarding.Domain.Aggregates.Audit;
using Onboarding.Domain.Aggregates.CedenteTipoAtivoAggregate;
using Onboarding.Domain.Repositories;
using Onboarding.Domain.ValueObjects;

namespace Onboarding.Application.Fundos.Commands.CreateCedenteTipoAtivo;

/// <summary>
/// Handler: create a CedenteTipoAtivo association with in-memory duplicate guard.
/// </summary>
public sealed class CreateCedenteTipoAtivoHandler
    : ICommandHandler<CreateCedenteTipoAtivoCommand, RelCedenteTipoAtivoDto>
{
    private readonly ICedenteTipoAtivoAggregateRepository _repository;
    private readonly ICedenteRepository _cedenteRepository;
    private readonly ITipoAtivoRepository _tipoAtivoRepository;
    private readonly IAuditService _auditService;
    private readonly IValidator<CreateCedenteTipoAtivoCommand> _validator;
    private readonly ILogger<CreateCedenteTipoAtivoHandler> _logger;

    public CreateCedenteTipoAtivoHandler(
        ICedenteTipoAtivoAggregateRepository repository,
        ICedenteRepository cedenteRepository,
        ITipoAtivoRepository tipoAtivoRepository,
        IAuditService auditService,
        IValidator<CreateCedenteTipoAtivoCommand> validator,
        ILogger<CreateCedenteTipoAtivoHandler> logger)
    {
        _repository = repository;
        _cedenteRepository = cedenteRepository;
        _tipoAtivoRepository = tipoAtivoRepository;
        _auditService = auditService;
        _validator = validator;
        _logger = logger;
    }

    public async Task<RelCedenteTipoAtivoDto> HandleAsync(
        CreateCedenteTipoAtivoCommand command, CancellationToken ct = default)
    {
        var validation = await _validator.ValidateAsync(command, ct).ConfigureAwait(false);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        // Verify parent entities exist
        _ = await _cedenteRepository.GetByIdAsync(command.CedenteId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Cedente with ID '{command.CedenteId}' not found.");

        _ = await _tipoAtivoRepository.GetByIdAsync(command.TipoAtivoId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"TipoAtivo with ID '{command.TipoAtivoId}' not found.");

        // In-memory duplicate guard
        var existsActive = await _repository
            .ExistsActiveAsync(command.CedenteId, command.TipoAtivoId, ct)
            .ConfigureAwait(false);
        CedenteTipoAtivoAggregate.ActivateGuard(existsActive);

        var limite = LimiteExposicao.Create(command.LimitePercentual, command.LimiteValor);
        var janela = JanelaVigencia.Create(command.DataInicio, command.DataFim);

        var association = CedenteTipoAtivoAggregate.Create(
            command.CedenteId, command.TipoAtivoId, limite, janela);

        await _repository.AddAsync(association, ct).ConfigureAwait(false);

        _logger.LogInformation(
            "CedenteTipoAtivo {Id} created for Cedente {CedenteId} / TipoAtivo {TipoAtivoId}",
            association.Id, command.CedenteId, command.TipoAtivoId);

        await _auditService.RecordAsync(
            actorSub: command.ActorSub,
            actorEmail: command.ActorEmail,
            action: ActionType.RelCedenteTipoAtivoCreated,
            targetUserId: association.Id,
            targetUserName: $"Cedente={command.CedenteId}/TipoAtivo={command.TipoAtivoId}",
            details: "CedenteTipoAtivo created with status ATIVO",
            ct: ct).ConfigureAwait(false);

        return ToDto(association);
    }

    internal static RelCedenteTipoAtivoDto ToDto(CedenteTipoAtivoAggregate a) => new(
        a.Id, a.CedenteId, a.TipoAtivoId,
        a.Limite.Percentual, a.Limite.Valor,
        a.Janela.DataInicio, a.Janela.DataFim,
        a.Status, a.CreatedAt);
}
