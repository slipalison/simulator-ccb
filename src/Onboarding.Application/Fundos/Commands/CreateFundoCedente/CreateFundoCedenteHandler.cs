using FluentValidation;
using Microsoft.Extensions.Logging;
using Onboarding.Application.Common;
using Onboarding.Application.Fundos.DTOs;
using Onboarding.Domain.Aggregates.Audit;
using Onboarding.Domain.Aggregates.FundoCedenteAggregate;
using Onboarding.Domain.Repositories;
using Onboarding.Domain.ValueObjects;

namespace Onboarding.Application.Fundos.Commands.CreateFundoCedente;

/// <summary>
/// Handler: create a FundoCedente association with REL-09 in-memory guard (D-18).
/// DB partial unique index is the authoritative race-condition gate; domain check is
/// defesa-em-profundidade for the happy-path (D-18).
/// </summary>
public sealed class CreateFundoCedenteHandler
    : ICommandHandler<CreateFundoCedenteCommand, RelFundoCedenteDto>
{
    private readonly IFundoCedenteAggregateRepository _repository;
    private readonly IFundoRepository _fundoRepository;
    private readonly ICedenteRepository _cedenteRepository;
    private readonly IAuditService _auditService;
    private readonly IValidator<CreateFundoCedenteCommand> _validator;
    private readonly ILogger<CreateFundoCedenteHandler> _logger;

    public CreateFundoCedenteHandler(
        IFundoCedenteAggregateRepository repository,
        IFundoRepository fundoRepository,
        ICedenteRepository cedenteRepository,
        IAuditService auditService,
        IValidator<CreateFundoCedenteCommand> validator,
        ILogger<CreateFundoCedenteHandler> logger)
    {
        _repository = repository;
        _fundoRepository = fundoRepository;
        _cedenteRepository = cedenteRepository;
        _auditService = auditService;
        _validator = validator;
        _logger = logger;
    }

    public async Task<RelFundoCedenteDto> HandleAsync(
        CreateFundoCedenteCommand command, CancellationToken ct = default)
    {
        // 1. Validate input
        var validation = await _validator.ValidateAsync(command, ct).ConfigureAwait(false);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        // 2. Verify parent Fundo exists (tenant guard in controller; here just FK check)
        var fundo = await _fundoRepository.GetByIdAsync(command.FundoId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Fundo with ID '{command.FundoId}' not found.");

        // 3. Verify parent Cedente exists
        var cedente = await _cedenteRepository.GetByIdAsync(command.CedenteId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Cedente with ID '{command.CedenteId}' not found.");

        // 4. REL-09 in-memory guard (D-18 defesa-em-profundidade)
        var existsActive = await _repository.ExistsActiveAsync(command.FundoId, command.CedenteId, ct)
            .ConfigureAwait(false);
        FundoCedenteAggregate.ActivateGuard(existsActive);

        // 5. Build value objects
        var limite = LimiteExposicao.Create(command.LimitePercentual, command.LimiteValor);
        var janela = JanelaVigencia.Create(command.DataInicio, command.DataFim);

        // 6. Create aggregate
        var association = FundoCedenteAggregate.Create(
            command.FundoId,
            command.CedenteId,
            limite,
            janela);

        // 7. Persist
        await _repository.AddAsync(association, ct).ConfigureAwait(false);

        _logger.LogInformation(
            "FundoCedente association {Id} created for Fundo {FundoId} / Cedente {CedenteId}",
            association.Id, command.FundoId, command.CedenteId);

        // 8. Audit (D-22)
        await _auditService.RecordAsync(
            actorSub: command.ActorSub,
            actorEmail: command.ActorEmail,
            action: ActionType.RelFundoCedenteCreated,
            targetUserId: association.Id,
            targetUserName: $"Fundo={command.FundoId}/Cedente={command.CedenteId}",
            details: $"FundoCedente created with status ATIVO",
            ct: ct).ConfigureAwait(false);

        return ToDto(association);
    }

    internal static RelFundoCedenteDto ToDto(FundoCedenteAggregate a) => new(
        a.Id,
        a.FundoId,
        a.CedenteId,
        a.Limite.Percentual,
        a.Limite.Valor,
        a.Janela.DataInicio,
        a.Janela.DataFim,
        a.Status,
        a.CreatedAt);
}
