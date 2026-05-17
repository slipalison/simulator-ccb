using Microsoft.Extensions.Logging;
using Onboarding.Application.Common;
using Onboarding.Application.Fundos.DTOs;
using Onboarding.Domain.Aggregates.Audit;
using Onboarding.Domain.Repositories;

namespace Onboarding.Application.Fundos.Commands;

/// <summary>
/// Handler: update an existing ConsultoriaFundo with audit logging (ADM-04).
/// </summary>
public sealed class UpdateConsultoriaFundoCommandHandler
    : ICommandHandler<UpdateConsultoriaFundoCommand, ConsultoriaFundoDto>
{
    private readonly IConsultoriaFundoRepository _repository;
    private readonly ICurrentCompanyService _currentCompanyService;
    private readonly IAuditService _auditService;
    private readonly ILogger<UpdateConsultoriaFundoCommandHandler> _logger;

    public UpdateConsultoriaFundoCommandHandler(
        IConsultoriaFundoRepository repository,
        ICurrentCompanyService currentCompanyService,
        IAuditService auditService,
        ILogger<UpdateConsultoriaFundoCommandHandler> logger)
    {
        _repository = repository;
        _currentCompanyService = currentCompanyService;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<ConsultoriaFundoDto> HandleAsync(
        UpdateConsultoriaFundoCommand command, CancellationToken ct = default)
    {
        var consultoria = await _repository.GetByIdAsync(command.Id, ct);
        if (consultoria is null || consultoria.ClienteId != _currentCompanyService.CompanyId)
            throw new KeyNotFoundException($"ConsultoriaFundo with ID '{command.Id}' not found.");

        consultoria.Update(
            command.RazaoSocial,
            command.NomeFantasia,
            command.Email,
            command.Telefone,
            command.Status);

        await _repository.SaveAsync(consultoria, ct);
        _logger.LogInformation("ConsultoriaFundo {Id} updated", command.Id);

        await _auditService.RecordAsync(
            actorSub: command.ActorSub,
            actorEmail: command.ActorEmail,
            action: ActionType.ConsultoriaUpdated,
            targetUserId: consultoria.Id,
            targetUserName: command.RazaoSocial,
            details: $"ConsultoriaFundo updated",
            ct: ct);

        return new ConsultoriaFundoDto(
            consultoria.Id,
            consultoria.RazaoSocial,
            consultoria.NomeFantasia,
            consultoria.Cnpj.Value,
            consultoria.Email?.Value,
            consultoria.Telefone?.Value,
            consultoria.Status,
            consultoria.CreatedAt);
    }
}