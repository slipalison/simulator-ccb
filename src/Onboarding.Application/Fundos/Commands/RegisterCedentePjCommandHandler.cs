using Microsoft.Extensions.Logging;
using Onboarding.Application.Common;
using Onboarding.Application.Fundos.DTOs;
using Onboarding.Domain.Aggregates.Audit;
using Onboarding.Domain.Aggregates.CedenteAggregate;
using Onboarding.Domain.Exceptions;
using Onboarding.Domain.Repositories;
using Onboarding.Domain.ValueObjects;

namespace Onboarding.Application.Fundos.Commands;

/// <summary>
/// Handler: register a new Cedente PJ with CNPJ validation via Cnpj.Create(),
/// uniqueness check via CedenteDocumento.Pj() (CAD-18→409), and audit logging (ADM-04).
/// </summary>
public sealed class RegisterCedentePjCommandHandler
    : ICommandHandler<RegisterCedentePjCommand, CedenteDto>
{
    private readonly ICedenteRepository _repository;
    private readonly ICurrentCompanyService _currentCompanyService;
    private readonly IAuditService _auditService;
    private readonly ILogger<RegisterCedentePjCommandHandler> _logger;

    public RegisterCedentePjCommandHandler(
        ICedenteRepository repository,
        ICurrentCompanyService currentCompanyService,
        IAuditService auditService,
        ILogger<RegisterCedentePjCommandHandler> logger)
    {
        _repository = repository;
        _currentCompanyService = currentCompanyService;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<CedenteDto> HandleAsync(
        RegisterCedentePjCommand command, CancellationToken ct = default)
    {
        var companyId = _currentCompanyService.CompanyId;

        // 1. Validate CNPJ via domain value object (check digits)
        var cnpj = Cnpj.Create(command.Cnpj);

        // 2. Build CedenteDocumento.Pj() and check uniqueness (CAD-18→409)
        var documento = CedenteDocumento.Pj(cnpj);
        if (await _repository.ExistsByDocumentoAsync(documento, companyId, ct))
            throw new DuplicateEntityException("Cedente", command.Cnpj);

        // 3. Create via domain factory method
        var cedente = Cedente.RegisterPj(
            command.Cnpj,
            command.RazaoSocial,
            companyId,
            command.Email,
            command.Telefone,
            command.Endereco);

        // 4. Persist
        await _repository.AddAsync(cedente, ct);
        _logger.LogInformation("Cedente PJ {Id} created with CNPJ {Cnpj}", cedente.Id, command.Cnpj);

        // 5. Audit (ADM-04)
        await _auditService.RecordAsync(
            actorSub: command.ActorSub,
            actorEmail: command.ActorEmail,
            action: ActionType.CedenteCreated,
            targetUserId: cedente.Id,
            targetUserName: command.RazaoSocial,
            details: $"Cedente PJ created with CNPJ {command.Cnpj}",
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