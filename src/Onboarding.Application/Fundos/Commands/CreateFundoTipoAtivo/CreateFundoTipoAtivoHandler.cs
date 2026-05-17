using FluentValidation;
using Microsoft.Extensions.Logging;
using Onboarding.Application.Common;
using Onboarding.Application.Fundos.DTOs;
using Onboarding.Domain.Aggregates.Audit;
using Onboarding.Domain.Aggregates.FundoAggregate;
using Onboarding.Domain.Aggregates.FundoTipoAtivoAggregate;
using Onboarding.Domain.Repositories;
using Onboarding.Domain.ValueObjects;

namespace Onboarding.Application.Fundos.Commands.CreateFundoTipoAtivo;

public sealed class CreateFundoTipoAtivoHandler
    : ICommandHandler<CreateFundoTipoAtivoCommand, RelFundoTipoAtivoDto>
{
    private readonly IFundoTipoAtivoAggregateRepository _repository;
    private readonly IFundoRepository _fundoRepository;
    private readonly ITipoAtivoRepository _tipoAtivoRepository;
    private readonly IAuditService _auditService;
    private readonly IValidator<CreateFundoTipoAtivoCommand> _validator;
    private readonly ILogger<CreateFundoTipoAtivoHandler> _logger;

    public CreateFundoTipoAtivoHandler(
        IFundoTipoAtivoAggregateRepository repository,
        IFundoRepository fundoRepository,
        ITipoAtivoRepository tipoAtivoRepository,
        IAuditService auditService,
        IValidator<CreateFundoTipoAtivoCommand> validator,
        ILogger<CreateFundoTipoAtivoHandler> logger)
    {
        _repository = repository;
        _fundoRepository = fundoRepository;
        _tipoAtivoRepository = tipoAtivoRepository;
        _auditService = auditService;
        _validator = validator;
        _logger = logger;
    }

    public async Task<RelFundoTipoAtivoDto> HandleAsync(
        CreateFundoTipoAtivoCommand command, CancellationToken ct = default)
    {
        var validation = await _validator.ValidateAsync(command, ct).ConfigureAwait(false);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        _ = await _fundoRepository.GetByIdAsync(command.FundoId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Fundo with ID '{command.FundoId}' not found.");

        _ = await _tipoAtivoRepository.GetByIdAsync(command.TipoAtivoId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"TipoAtivo with ID '{command.TipoAtivoId}' not found.");

        var existsActive = await _repository
            .ExistsActiveAsync(command.FundoId, command.TipoAtivoId, ct)
            .ConfigureAwait(false);
        FundoTipoAtivoAggregate.ActivateGuard(existsActive);

        var limite = LimiteExposicao.Create(command.LimitePercentual, command.LimiteValor);
        var janela = JanelaVigencia.Create(command.DataInicio, command.DataFim);

        var association = FundoTipoAtivoAggregate.Create(
            command.FundoId, command.TipoAtivoId, limite, janela);

        await _repository.AddAsync(association, ct).ConfigureAwait(false);

        _logger.LogInformation(
            "FundoTipoAtivo {Id} created for Fundo {FundoId} / TipoAtivo {TipoAtivoId}",
            association.Id, command.FundoId, command.TipoAtivoId);

        await _auditService.RecordAsync(
            command.ActorSub, command.ActorEmail,
            ActionType.RelFundoTipoAtivoCreated,
            association.Id, $"Fundo={command.FundoId}/TipoAtivo={command.TipoAtivoId}",
            "FundoTipoAtivo created with status ATIVO",
            ct: ct).ConfigureAwait(false);

        return ToDto(association);
    }

    internal static RelFundoTipoAtivoDto ToDto(FundoTipoAtivoAggregate a) => new(
        a.Id, a.FundoId, a.TipoAtivoId,
        a.Limite.Percentual, a.Limite.Valor,
        a.Janela.DataInicio, a.Janela.DataFim,
        a.Status, a.CreatedAt);
}
