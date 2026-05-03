using Microsoft.Extensions.Logging;
using Onboarding.Application.Common;
using Onboarding.Application.Fundos.DTOs;
using Onboarding.Domain.Aggregates.Audit;
using Onboarding.Domain.Repositories;

namespace Onboarding.Application.Fundos.Commands;

/// <summary>
/// Handler: update Fundo mutable data (CAD-11).
/// DATA update only — status transitions use TransitionFundoStatusCommand.
/// </summary>
public sealed class UpdateFundoCommandHandler
    : ICommandHandler<UpdateFundoCommand, FundoDto>
{
    private readonly IFundoRepository _repository;
    private readonly IAuditService _auditService;
    private readonly ILogger<UpdateFundoCommandHandler> _logger;

    public UpdateFundoCommandHandler(
        IFundoRepository repository,
        IAuditService auditService,
        ILogger<UpdateFundoCommandHandler> logger)
    {
        _repository = repository;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<FundoDto> HandleAsync(
        UpdateFundoCommand command, CancellationToken ct = default)
    {
        var fundo = await _repository.GetByIdAsync(command.Id, ct)
            ?? throw new KeyNotFoundException($"Fundo with ID '{command.Id}' not found.");

        fundo.Update(
            command.Nome,
            command.ClasseAnbima,
            command.Segmento,
            command.DataConstituicao);

        await _repository.SaveAsync(fundo, ct);
        _logger.LogInformation("Fundo {Id} updated", command.Id);

        await _auditService.RecordAsync(
            actorSub: "",
            actorEmail: "",
            action: ActionType.FundoUpdated,
            targetUserId: fundo.Id,
            targetUserName: command.Nome,
            details: $"Fundo updated",
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