using Microsoft.Extensions.Logging;
using Onboarding.Application.Common;
using Onboarding.Application.Fundos.DTOs;
using Onboarding.Domain.Aggregates.Audit;
using Onboarding.Domain.Aggregates.CustodianteAggregate;
using Onboarding.Domain.Exceptions;
using Onboarding.Domain.Repositories;

namespace Onboarding.Application.Fundos.Commands;

/// <summary>
/// Handler: register a new Custodiante with CNPJ validation, uniqueness check (CAD-08→409),
/// and audit logging (ADM-04).
/// </summary>
public sealed class RegisterCustodianteCommandHandler
    : ICommandHandler<RegisterCustodianteCommand, CustodianteDto>
{
    private readonly ICustodianteRepository _repository;
    private readonly ICurrentCompanyService _currentCompanyService;
    private readonly IAuditService _auditService;
    private readonly ILogger<RegisterCustodianteCommandHandler> _logger;

    public RegisterCustodianteCommandHandler(
        ICustodianteRepository repository,
        ICurrentCompanyService currentCompanyService,
        IAuditService auditService,
        ILogger<RegisterCustodianteCommandHandler> logger)
    {
        _repository = repository;
        _currentCompanyService = currentCompanyService;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<CustodianteDto> HandleAsync(
        RegisterCustodianteCommand command, CancellationToken ct = default)
    {
        var companyId = _currentCompanyService.CompanyId;

        // 1. Check duplicate CNPJ (CAD-08→409, T-47-01)
        if (await _repository.ExistsByCnpjAsync(command.Cnpj, companyId, ct))
            throw new DuplicateEntityException("Custodiante", command.Cnpj);

        // 2. Create via domain factory method
        var custodiante = Custodiante.Register(
            command.RazaoSocial,
            command.Cnpj,
            companyId,
            command.CodigoInterno,
            command.Email,
            command.Telefone);

        // 3. Persist
        await _repository.AddAsync(custodiante, ct);
        _logger.LogInformation("Custodiante {Id} created with CNPJ {Cnpj}", custodiante.Id, command.Cnpj);

        // 4. Audit (ADM-04)
        await _auditService.RecordAsync(
            actorSub: command.ActorSub,
            actorEmail: command.ActorEmail,
            action: ActionType.CustodianteCreated,
            targetUserId: custodiante.Id,
            targetUserName: command.RazaoSocial,
            details: $"Custodiante created with CNPJ {command.Cnpj}",
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