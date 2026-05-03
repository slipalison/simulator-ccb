using Microsoft.Extensions.Logging;
using Onboarding.Application.Common;
using Onboarding.Application.Fundos.DTOs;
using Onboarding.Domain.Aggregates.Audit;
using Onboarding.Domain.Aggregates.TipoAtivoAggregate;
using Onboarding.Domain.Exceptions;
using Onboarding.Domain.Repositories;

namespace Onboarding.Application.Fundos.Commands;

/// <summary>
/// Handler: create a new TipoAtivo with global codigo uniqueness check (CAD-22→409),
/// and audit logging (ADM-04).
/// No ICurrentCompanyService — TipoAtivo is a global entity per D-03.
/// </summary>
public sealed class CreateTipoAtivoCommandHandler
    : ICommandHandler<CreateTipoAtivoCommand, TipoAtivoDto>
{
    private readonly ITipoAtivoRepository _repository;
    private readonly IAuditService _auditService;
    private readonly ILogger<CreateTipoAtivoCommandHandler> _logger;

    public CreateTipoAtivoCommandHandler(
        ITipoAtivoRepository repository,
        IAuditService auditService,
        ILogger<CreateTipoAtivoCommandHandler> logger)
    {
        _repository = repository;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<TipoAtivoDto> HandleAsync(
        CreateTipoAtivoCommand command, CancellationToken ct = default)
    {
        // 1. Check duplicate codigo globally (CAD-22→409, T-47-04)
        if (await _repository.ExistsByCodigoAsync(command.Codigo, ct))
            throw new DuplicateEntityException("TipoAtivo", command.Codigo);

        // 2. Create via domain factory method
        var tipoAtivo = TipoAtivo.Register(
            command.Codigo,
            command.Descricao,
            command.Categoria,
            command.Subcategoria,
            command.OrdemExibicao);

        // 3. Persist
        await _repository.AddAsync(tipoAtivo, ct);
        _logger.LogInformation("TipoAtivo {Id} created with codigo {Codigo}", tipoAtivo.Id, command.Codigo);

        // 4. Audit (ADM-04)
        await _auditService.RecordAsync(
            actorSub: "",
            actorEmail: "",
            action: ActionType.TipoAtivoCreated,
            targetUserId: tipoAtivo.Id,
            targetUserName: command.Descricao,
            details: $"TipoAtivo created with codigo {command.Codigo}",
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