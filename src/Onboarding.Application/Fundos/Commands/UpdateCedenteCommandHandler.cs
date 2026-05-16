using Microsoft.Extensions.Logging;
using Onboarding.Application.Common;
using Onboarding.Application.Fundos.DTOs;
using Onboarding.Domain.Aggregates.Audit;
using Onboarding.Domain.Aggregates.CedenteAggregate;
using Onboarding.Domain.Repositories;

namespace Onboarding.Application.Fundos.Commands;

/// <summary>
/// Handler: update an existing Cedente with audit logging (ADM-04).
/// </summary>
public sealed class UpdateCedenteCommandHandler
    : ICommandHandler<UpdateCedenteCommand, CedenteDto>
{
    private readonly ICedenteRepository _repository;
    private readonly ICurrentCompanyService _currentCompanyService;
    private readonly IAuditService _auditService;
    private readonly ILogger<UpdateCedenteCommandHandler> _logger;

    public UpdateCedenteCommandHandler(
        ICedenteRepository repository,
        ICurrentCompanyService currentCompanyService,
        IAuditService auditService,
        ILogger<UpdateCedenteCommandHandler> logger)
    {
        _repository = repository;
        _currentCompanyService = currentCompanyService;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<CedenteDto> HandleAsync(
        UpdateCedenteCommand command, CancellationToken ct = default)
    {
        var cedente = await _repository.GetByIdAsync(command.Id, ct);
        if (cedente is null || cedente.ClienteId != _currentCompanyService.CompanyId)
            throw new KeyNotFoundException($"Cedente with ID '{command.Id}' not found.");

        cedente.Update(
            command.Nome,
            command.Email,
            command.Telefone,
            command.Endereco,
            command.Status);

        await _repository.SaveAsync(cedente, ct);
        _logger.LogInformation("Cedente {Id} updated", command.Id);

        await _auditService.RecordAsync(
            actorSub: command.ActorSub,
            actorEmail: command.ActorEmail,
            action: ActionType.CedenteUpdated,
            targetUserId: cedente.Id,
            targetUserName: command.Nome,
            details: $"Cedente updated",
            ct: ct);

        return new CedenteDto(
            cedente.Id,
            cedente.Documento.Match(
                pf => pf.Cpf.Value,
                pj => pj.Cnpj.Value),
            cedente.Nome,
            cedente.Email?.Value,
            cedente.Telefone?.Value,
            cedente.Endereco,
            cedente.Documento.IsPf ? CedenteTipo.PF : CedenteTipo.PJ,
            cedente.Status,
            cedente.CreatedAt);
    }
}