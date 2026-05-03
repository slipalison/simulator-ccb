using Microsoft.Extensions.Logging;
using Onboarding.Application.Common;
using Onboarding.Application.Fundos.DTOs;
using Onboarding.Domain.Aggregates.Audit;
using Onboarding.Domain.Repositories;

namespace Onboarding.Application.Fundos.Commands;

/// <summary>
/// Handler: update an existing Custodiante with audit logging (ADM-04).
/// </summary>
public sealed class UpdateCustodianteCommandHandler
    : ICommandHandler<UpdateCustodianteCommand, CustodianteDto>
{
    private readonly ICustodianteRepository _repository;
    private readonly IAuditService _auditService;
    private readonly ILogger<UpdateCustodianteCommandHandler> _logger;

    public UpdateCustodianteCommandHandler(
        ICustodianteRepository repository,
        IAuditService auditService,
        ILogger<UpdateCustodianteCommandHandler> logger)
    {
        _repository = repository;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<CustodianteDto> HandleAsync(
        UpdateCustodianteCommand command, CancellationToken ct = default)
    {
        var custodiante = await _repository.GetByIdAsync(command.Id, ct)
            ?? throw new KeyNotFoundException($"Custodiante with ID '{command.Id}' not found.");

        custodiante.Update(
            command.RazaoSocial,
            command.CodigoInterno,
            command.Email,
            command.Telefone,
            command.Status);

        await _repository.SaveAsync(custodiante, ct);
        _logger.LogInformation("Custodiante {Id} updated", command.Id);

        await _auditService.RecordAsync(
            actorSub: command.ActorSub,
            actorEmail: command.ActorEmail,
            action: ActionType.CustodianteUpdated,
            targetUserId: custodiante.Id,
            targetUserName: command.RazaoSocial,
            details: $"Custodiante updated",
            ct: ct);

        return new CustodianteDto(
            custodiante.Id,
            custodiante.RazaoSocial,
            custodiante.CodigoInterno,
            custodiante.Cnpj.Value,
            custodiante.Email?.Value,
            custodiante.Telefone?.Value,
            custodiante.Status,
            custodiante.CreatedAt);
    }
}