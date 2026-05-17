using Microsoft.Extensions.Logging;
using Onboarding.Application.Common;
using Onboarding.Application.Fundos.DTOs;
using Onboarding.Domain.Aggregates.Audit;
using Onboarding.Domain.Aggregates.ConsultoriaFundoAggregate;
using Onboarding.Domain.Exceptions;
using Onboarding.Domain.Repositories;

namespace Onboarding.Application.Fundos.Commands;

/// <summary>
/// Handler: register a new ConsultoriaFundo with CNPJ validation, uniqueness check (CAD-04→409),
/// and audit logging (ADM-04).
/// </summary>
public sealed class RegisterConsultoriaFundoCommandHandler
    : ICommandHandler<RegisterConsultoriaFundoCommand, ConsultoriaFundoDto>
{
    private readonly IConsultoriaFundoRepository _repository;
    private readonly ICurrentCompanyService _currentCompanyService;
    private readonly IAuditService _auditService;
    private readonly ILogger<RegisterConsultoriaFundoCommandHandler> _logger;

    public RegisterConsultoriaFundoCommandHandler(
        IConsultoriaFundoRepository repository,
        ICurrentCompanyService currentCompanyService,
        IAuditService auditService,
        ILogger<RegisterConsultoriaFundoCommandHandler> logger)
    {
        _repository = repository;
        _currentCompanyService = currentCompanyService;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<ConsultoriaFundoDto> HandleAsync(
        RegisterConsultoriaFundoCommand command, CancellationToken ct = default)
    {
        var companyId = _currentCompanyService.CompanyId;

        // 1. Check duplicate CNPJ (CAD-04→409, T-47-01)
        if (await _repository.ExistsByCnpjAsync(command.Cnpj, companyId, ct))
            throw new DuplicateEntityException("ConsultoriaFundo", command.Cnpj);

        // 2. Create via domain factory method
        var consultoria = ConsultoriaFundo.Register(
            command.RazaoSocial,
            command.Cnpj,
            companyId,
            command.NomeFantasia,
            command.Email,
            command.Telefone);

        // 3. Persist
        await _repository.AddAsync(consultoria, ct);
        _logger.LogInformation("ConsultoriaFundo {Id} created with CNPJ {Cnpj}", consultoria.Id, command.Cnpj);

        // 4. Audit (ADM-04)
        await _auditService.RecordAsync(
            actorSub: command.ActorSub,
            actorEmail: command.ActorEmail,
            action: ActionType.ConsultoriaCreated,
            targetUserId: consultoria.Id,
            targetUserName: command.RazaoSocial,
            details: $"ConsultoriaFundo created with CNPJ {command.Cnpj}",
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