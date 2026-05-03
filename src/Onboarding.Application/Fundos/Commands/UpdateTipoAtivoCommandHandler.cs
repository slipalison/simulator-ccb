using Microsoft.Extensions.Logging;
using Onboarding.Application.Common;
using Onboarding.Application.Fundos.DTOs;
using Onboarding.Domain.Aggregates.Audit;
using Onboarding.Domain.Repositories;

namespace Onboarding.Application.Fundos.Commands;

/// <summary>
/// Handler: update an existing TipoAtivo with audit logging (ADM-04).
/// No ICurrentCompanyService — TipoAtivo is a global entity per D-03.
/// </summary>
public sealed class UpdateTipoAtivoCommandHandler
    : ICommandHandler<UpdateTipoAtivoCommand, TipoAtivoDto>
{
    private readonly ITipoAtivoRepository _repository;
    private readonly IAuditService _auditService;
    private readonly ILogger<UpdateTipoAtivoCommandHandler> _logger;

    public UpdateTipoAtivoCommandHandler(
        ITipoAtivoRepository repository,
        IAuditService auditService,
        ILogger<UpdateTipoAtivoCommandHandler> logger)
    {
        _repository = repository;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<TipoAtivoDto> HandleAsync(
        UpdateTipoAtivoCommand command, CancellationToken ct = default)
    {
        var tipoAtivo = await _repository.GetByIdAsync(command.Id, ct)
            ?? throw new KeyNotFoundException($"TipoAtivo with ID '{command.Id}' not found.");

        tipoAtivo.Update(
            command.Descricao,
            command.Subcategoria,
            command.Status,
            command.OrdemExibicao);

        await _repository.SaveAsync(tipoAtivo, ct);
        _logger.LogInformation("TipoAtivo {Id} updated", command.Id);

        await _auditService.RecordAsync(
            actorSub: "",
            actorEmail: "",
            action: ActionType.TipoAtivoUpdated,
            targetUserId: tipoAtivo.Id,
            targetUserName: command.Descricao,
            details: $"TipoAtivo updated",
            ct: ct);

        return new TipoAtivoDto(
            tipoAtivo.Id,
            tipoAtivo.Codigo,
            tipoAtivo.Descricao,
            tipoAtivo.Categoria,
            tipoAtivo.Subcategoria,
            tipoAtivo.Status,
            tipoAtivo.OrdemExibicao);
    }
}