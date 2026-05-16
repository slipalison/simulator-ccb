using Microsoft.Extensions.Logging;
using NSubstitute;
using Onboarding.Application.Common;
using Onboarding.Application.Fundos.Commands;
using Onboarding.Application.Fundos.DTOs;
using Onboarding.Domain.Aggregates.Audit;
using Onboarding.Domain.Aggregates.CustodianteAggregate;
using Onboarding.Domain.Repositories;
using Shouldly;

namespace Onboarding.Application.Tests.Fundos.Commands;

public class UpdateCustodianteCommandHandlerTests
{
    private readonly ICustodianteRepository _repository;
    private readonly ICurrentCompanyService _currentCompanyService;
    private readonly IAuditService _auditService;
    private readonly ILogger<UpdateCustodianteCommandHandler> _logger;
    private readonly UpdateCustodianteCommandHandler _sut;

    // Stable company ID used across tests for the "happy path" owner scenario
    private readonly Guid _companyId = Guid.NewGuid();

    public UpdateCustodianteCommandHandlerTests()
    {
        _repository = Substitute.For<ICustodianteRepository>();
        _currentCompanyService = Substitute.For<ICurrentCompanyService>();
        _auditService = Substitute.For<IAuditService>();
        _logger = Substitute.For<ILogger<UpdateCustodianteCommandHandler>>();

        _currentCompanyService.CompanyId.Returns(_companyId);

        _sut = new UpdateCustodianteCommandHandler(_repository, _currentCompanyService, _auditService, _logger);
    }

    private static UpdateCustodianteCommand ValidCommand(Guid? id = null) => new(
        Id: id ?? Guid.NewGuid(),
        RazaoSocial: "Custodiante Atualizado",
        CodigoInterno: "CUST-002",
        Email: "novo@teste.com",
        Telefone: "11888888888",
        Status: CustodianteStatus.ATIVO,
        ActorSub: "test-sub-123",
        ActorEmail: "actor@teste.com"
    );

    [Fact]
    public async Task HandleAsync_WithValidData_UpdatesFieldsAndSaves()
    {
        // Arrange
        var custodiante = Custodiante.Register("Nome Original", "11444777000161", _companyId, "CUST-001");
        var command = ValidCommand(custodiante.Id);
        _repository.GetByIdAsync(custodiante.Id, Arg.Any<CancellationToken>()).Returns(custodiante);

        // Act
        var result = await _sut.HandleAsync(command);

        // Assert
        result.ShouldNotBeNull();
        result.RazaoSocial.ShouldBe(command.RazaoSocial);

        await _repository.Received(1).SaveAsync(custodiante, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenEntityNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var command = ValidCommand();
        _repository.GetByIdAsync(command.Id, Arg.Any<CancellationToken>()).Returns((Custodiante?)null);

        // Act & Assert
        await Should.ThrowAsync<KeyNotFoundException>(() => _sut.HandleAsync(command));
    }

    [Fact]
    public async Task HandleAsync_WhenClienteIdMismatch_ThrowsKeyNotFoundException()
    {
        // Arrange — entity belongs to a different company than the current actor
        var differentCompanyId = Guid.NewGuid();
        var custodiante = Custodiante.Register("Nome Original", "11444777000161", differentCompanyId, "CUST-001");
        var command = ValidCommand(custodiante.Id);

        _repository.GetByIdAsync(custodiante.Id, Arg.Any<CancellationToken>()).Returns(custodiante);
        // _companyId is already configured as the current company in ctor (differs from differentCompanyId)

        // Act & Assert — cross-tenant write must be rejected with 404-producing exception
        await Should.ThrowAsync<KeyNotFoundException>(() => _sut.HandleAsync(command));
    }

    [Fact]
    public async Task HandleAsync_RecordsAuditLog()
    {
        // Arrange
        var custodiante = Custodiante.Register("Nome Original", "11444777000161", _companyId, "CUST-001");
        var command = ValidCommand(custodiante.Id);
        _repository.GetByIdAsync(custodiante.Id, Arg.Any<CancellationToken>()).Returns(custodiante);

        // Act
        await _sut.HandleAsync(command);

        // Assert
        await _auditService.Received(1).RecordAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            ActionType.CustodianteUpdated,
            Arg.Any<Guid?>(),
            command.RazaoSocial,
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }
}