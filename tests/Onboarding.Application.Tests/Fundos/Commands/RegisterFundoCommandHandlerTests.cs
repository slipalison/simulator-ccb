using Microsoft.Extensions.Logging;
using NSubstitute;
using Onboarding.Application.Common;
using Onboarding.Application.Fundos.Commands;
using Onboarding.Application.Fundos.DTOs;
using Onboarding.Domain.Aggregates.Audit;
using Onboarding.Domain.Aggregates.ConsultoriaFundoAggregate;
using Onboarding.Domain.Aggregates.CustodianteAggregate;
using Onboarding.Domain.Aggregates.FundoAggregate;
using Onboarding.Domain.Exceptions;
using Onboarding.Domain.Repositories;
using Shouldly;

namespace Onboarding.Application.Tests.Fundos.Commands;

public class RegisterFundoCommandHandlerTests
{
    private readonly IFundoRepository _fundoRepository;
    private readonly IConsultoriaFundoRepository _consultoriaRepository;
    private readonly ICustodianteRepository _custodianteRepository;
    private readonly ICurrentCompanyService _currentCompanyService;
    private readonly IAuditService _auditService;
    private readonly ILogger<RegisterFundoCommandHandler> _logger;
    private readonly RegisterFundoCommandHandler _sut;

    private static readonly Guid CompanyId = Guid.NewGuid();
    private static readonly Guid ConsultoriaId = Guid.NewGuid();
    private static readonly Guid CustodianteId = Guid.NewGuid();

    public RegisterFundoCommandHandlerTests()
    {
        _fundoRepository = Substitute.For<IFundoRepository>();
        _consultoriaRepository = Substitute.For<IConsultoriaFundoRepository>();
        _custodianteRepository = Substitute.For<ICustodianteRepository>();
        _currentCompanyService = Substitute.For<ICurrentCompanyService>();
        _auditService = Substitute.For<IAuditService>();
        _logger = Substitute.For<ILogger<RegisterFundoCommandHandler>>();
        _currentCompanyService.CompanyId.Returns(CompanyId);

        _sut = new RegisterFundoCommandHandler(
            _fundoRepository, _consultoriaRepository, _custodianteRepository,
            _currentCompanyService, _auditService, _logger);
    }

    private static RegisterFundoCommand ValidCommand() => new(
        Nome: "Fundo Teste",
        Cnpj: "11444777000161",
        ConsultoriaFundoId: ConsultoriaId,
        CustodianteId: CustodianteId,
        TipoFundo: TipoFundo.RendaFixa,
        ClasseAnbima: "Classe A",
        Segmento: "Segmento 1",
        DataConstituicao: null,
        ActorSub: "test-sub-123",
        ActorEmail: "actor@teste.com"
    );

    private void SetupValidFkReferences()
    {
        var consultoria = ConsultoriaFundo.Register("Consultoria Teste", "11444777000161", CompanyId);
        _consultoriaRepository.GetByIdAsync(ConsultoriaId, Arg.Any<CancellationToken>()).Returns(consultoria);

        var custodiante = Custodiante.Register("Custodiante Teste", "11444777000161", CompanyId, "CUST-001");
        _custodianteRepository.GetByIdAsync(CustodianteId, Arg.Any<CancellationToken>()).Returns(custodiante);
    }

    [Fact]
    public async Task HandleAsync_WithValidData_CreatesFundoWithRascunhoStatus()
    {
        // Arrange
        var command = ValidCommand();
        SetupValidFkReferences();
        _fundoRepository.ExistsByCnpjAsync(command.Cnpj, CompanyId, Arg.Any<CancellationToken>()).Returns(false);

        // Act
        var result = await _sut.HandleAsync(command);

        // Assert
        result.ShouldNotBeNull();
        result.Nome.ShouldBe(command.Nome);
        result.Status.ShouldBe(FundoStatus.RASCUNHO);

        await _fundoRepository.Received(1).AddAsync(Arg.Is<Fundo>(f =>
            f.Nome == command.Nome &&
            f.Status == FundoStatus.RASCUNHO), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_InvalidConsultoriaFundoId_ThrowsKeyNotFoundException()
    {
        // Arrange
        var command = ValidCommand();
        _consultoriaRepository.GetByIdAsync(ConsultoriaId, Arg.Any<CancellationToken>()).Returns((ConsultoriaFundo?)null);

        // Act & Assert
        var ex = await Should.ThrowAsync<KeyNotFoundException>(() => _sut.HandleAsync(command));
        ex.Message.ShouldContain("ConsultoriaFundo");
    }

    [Fact]
    public async Task HandleAsync_InvalidCustodianteId_ThrowsKeyNotFoundException()
    {
        // Arrange
        var command = ValidCommand();
        var consultoria = ConsultoriaFundo.Register("Test", "11444777000161", CompanyId);
        _consultoriaRepository.GetByIdAsync(ConsultoriaId, Arg.Any<CancellationToken>()).Returns(consultoria);
        _custodianteRepository.GetByIdAsync(CustodianteId, Arg.Any<CancellationToken>()).Returns((Custodiante?)null);

        // Act & Assert
        var ex = await Should.ThrowAsync<KeyNotFoundException>(() => _sut.HandleAsync(command));
        ex.Message.ShouldContain("Custodiante");
    }

    [Fact]
    public async Task HandleAsync_WithDuplicateCnpj_ThrowsDuplicateEntityException()
    {
        // Arrange
        var command = ValidCommand();
        SetupValidFkReferences();
        _fundoRepository.ExistsByCnpjAsync(command.Cnpj, CompanyId, Arg.Any<CancellationToken>()).Returns(true);

        // Act & Assert
        var ex = await Should.ThrowAsync<DuplicateEntityException>(() => _sut.HandleAsync(command));
        ex.EntityType.ShouldBe("Fundo");

        await _fundoRepository.DidNotReceive().AddAsync(Arg.Any<Fundo>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_RecordsAuditLog()
    {
        // Arrange
        var command = ValidCommand();
        SetupValidFkReferences();
        _fundoRepository.ExistsByCnpjAsync(command.Cnpj, CompanyId, Arg.Any<CancellationToken>()).Returns(false);

        // Act
        await _sut.HandleAsync(command);

        // Assert
        await _auditService.Received(1).RecordAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            ActionType.FundoCreated,
            Arg.Any<Guid?>(),
            command.Nome,
            Arg.Is<string>(d => d.Contains("RASCUNHO")),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }
}