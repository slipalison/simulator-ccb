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
/// Handler: register a new Cedente PF with CPF validation via Cpf.Create(),
/// uniqueness check via CedenteDocumento.Pf() (CAD-18→409), and audit logging (ADM-04).
/// </summary>
public sealed class RegisterCedentePfCommandHandler
    : ICommandHandler<RegisterCedentePfCommand, CedenteDto>
{
    private readonly ICedenteRepository _repository;
    private readonly ICurrentCompanyService _currentCompanyService;
    private readonly IAuditService _auditService;
    private readonly ILogger<RegisterCedentePfCommandHandler> _logger;

    public RegisterCedentePfCommandHandler(
        ICedenteRepository repository,
        ICurrentCompanyService currentCompanyService,
        IAuditService auditService,
        ILogger<RegisterCedentePfCommandHandler> logger)
    {
        _repository = repository;
        _currentCompanyService = currentCompanyService;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<CedenteDto> HandleAsync(
        RegisterCedentePfCommand command, CancellationToken ct = default)
    {
        var companyId = _currentCompanyService.CompanyId;

        // 1. Validate CPF via domain value object (check digits)
        // Cpf.Create() in the validator already caught format errors,
        // but we create it here again to build CedenteDocumento.
        var cpf = Cpf.Create(command.Cpf);

        // 2. Build CedenteDocumento.Pf() and check uniqueness (CAD-18→409)
        var documento = CedenteDocumento.Pf(cpf);
        if (await _repository.ExistsByDocumentoAsync(documento, companyId, ct))
            throw new DuplicateEntityException("Cedente", command.Cpf);

        // 3. Create via domain factory method
        var cedente = Cedente.RegisterPf(
            command.Cpf,
            command.Nome,
            companyId,
            command.Email,
            command.Telefone,
            command.Endereco);

        // 4. Persist
        await _repository.AddAsync(cedente, ct);
        _logger.LogInformation("Cedente PF {Id} created with CPF", cedente.Id);

        // 5. Audit (ADM-04)
        await _auditService.RecordAsync(
            actorSub: command.ActorSub,
            actorEmail: command.ActorEmail,
            action: ActionType.CedenteCreated,
            targetUserId: cedente.Id,
            targetUserName: command.Nome,
            details: $"Cedente PF created with CPF",
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