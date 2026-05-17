using Microsoft.Extensions.Logging;
using Onboarding.Application.Common;
using Onboarding.Application.Fundos.DTOs;
using Onboarding.Domain.Aggregates.Audit;
using Onboarding.Domain.Aggregates.FundoAggregate;
using Onboarding.Domain.Exceptions;
using Onboarding.Domain.Repositories;

namespace Onboarding.Application.Fundos.Commands;

/// <summary>
/// Handler: register a new Fundo with CNPJ validation, FK reference checks, RASCUNHO status (CAD-09, CAD-12→409),
/// and audit logging (ADM-04).
/// </summary>
public sealed class RegisterFundoCommandHandler
    : ICommandHandler<RegisterFundoCommand, FundoDto>
{
    private readonly IFundoRepository _fundoRepository;
    private readonly IConsultoriaFundoRepository _consultoriaRepository;
    private readonly ICustodianteRepository _custodianteRepository;
    private readonly ICurrentCompanyService _currentCompanyService;
    private readonly IAuditService _auditService;
    private readonly ILogger<RegisterFundoCommandHandler> _logger;

    public RegisterFundoCommandHandler(
        IFundoRepository fundoRepository,
        IConsultoriaFundoRepository consultoriaRepository,
        ICustodianteRepository custodianteRepository,
        ICurrentCompanyService currentCompanyService,
        IAuditService auditService,
        ILogger<RegisterFundoCommandHandler> logger)
    {
        _fundoRepository = fundoRepository;
        _consultoriaRepository = consultoriaRepository;
        _custodianteRepository = custodianteRepository;
        _currentCompanyService = currentCompanyService;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<FundoDto> HandleAsync(
        RegisterFundoCommand command, CancellationToken ct = default)
    {
        var companyId = _currentCompanyService.CompanyId;

        // 1. Validate ConsultoriaFundoId FK reference exists
        var consultoria = await _consultoriaRepository.GetByIdAsync(command.ConsultoriaFundoId, ct)
            ?? throw new KeyNotFoundException(
                $"ConsultoriaFundo with ID '{command.ConsultoriaFundoId}' not found.");

        // 2. Validate CustodianteId FK reference exists
        var custodiante = await _custodianteRepository.GetByIdAsync(command.CustodianteId, ct)
            ?? throw new KeyNotFoundException(
                $"Custodiante with ID '{command.CustodianteId}' not found.");

        // 3. Check duplicate CNPJ (CAD-12→409)
        if (await _fundoRepository.ExistsByCnpjAsync(command.Cnpj, companyId, ct))
            throw new DuplicateEntityException("Fundo", command.Cnpj);

        // 4. Create via domain factory method — ALWAYS starts as RASCUNHO (D-02)
        var fundo = Fundo.Register(
            command.Nome,
            command.Cnpj,
            companyId,
            command.ConsultoriaFundoId,
            command.CustodianteId,
            command.TipoFundo,
            command.ClasseAnbima,
            command.Segmento,
            command.DataConstituicao);

        // 5. Persist
        await _fundoRepository.AddAsync(fundo, ct);
        _logger.LogInformation("Fundo {Id} created with CNPJ {Cnpj}", fundo.Id, command.Cnpj);

        // 6. Audit (ADM-04)
        await _auditService.RecordAsync(
            actorSub: command.ActorSub,
            actorEmail: command.ActorEmail,
            action: ActionType.FundoCreated,
            targetUserId: fundo.Id,
            targetUserName: command.Nome,
            details: $"Fundo created with CNPJ {command.Cnpj}, status RASCUNHO",
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