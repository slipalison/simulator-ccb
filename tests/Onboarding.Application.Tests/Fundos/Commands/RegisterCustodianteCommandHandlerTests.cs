using Microsoft.Extensions.Logging;
using NSubstitute;
using Onboarding.Application.Common;
using Onboarding.Application.Fundos.Commands;
using Onboarding.Application.Fundos.DTOs;
using Onboarding.Domain.Aggregates.Audit;
using Onboarding.Domain.Aggregates.CustodianteAggregate;
using Onboarding.Domain.Exceptions;
using Onboarding.Domain.Repositories;
using Shouldly;

namespace Onboarding.Application.Tests.Fundos.Commands;

public class RegisterCustodianteCommandHandlerTests
{
    private readonly ICustodianteRepository _repository;
    private readonly ICurrentCompanyService _currentCompanyService;
    private readonly IAuditService _auditService;
    private readonly ILogger<RegisterCustodianteCommandHandler> _logger;
    private readonly RegisterCustodianteCommandHandler _sut;

    private static readonly Guid CompanyId = Guid.NewGuid();

    public RegisterCustodianteCommandHandlerTests()
    {
        _repository = Substitute.For<ICustodianteRepository>();
        _currentCompanyService = Substitute.For<ICurrentCompanyService>();
        _auditService = Substitute.For<IAuditService>();
        _logger = Substitute.For<ILogger<RegisterCustodianteCommandHandler>>();
        _currentCompanyService.CompanyId.Returns(CompanyId);

        _sut = new RegisterCustodianteCommandHandler(
            _repository, _currentCompanyService, _auditService, _logger);
    }

    private static RegisterCustodianteCommand ValidCommand() => new(
        RazaoSocial: "Custodiante Teste LTDA",
        Cnpj: "11444777000161",
        CodigoInterno: "CUST-001",
        Email: "custodiante@teste.com",
        Telefone: "11999999999",
        ActorSub: "test-sub-123",
        ActorEmail: "actor@teste.com"
    );

    [Fact]
    public async Task HandleAsync_WithValidData_CreatesAndPersistsCustodiante()
    {
        // Arrange
        var command = ValidCommand();
        _repository.ExistsByCnpjAsync(command.Cnpj, CompanyId, Arg.Any<CancellationToken>()).Returns(false);

        // Act
        var result = await _sut.HandleAsync(command);

        // Assert
        result.ShouldNotBeNull();
        result.RazaoSocial.ShouldBe(command.RazaoSocial);
        result.Cnpj.ShouldBe(command.Cnpj);
        result.Status.ShouldBe(CustodianteStatus.ATIVO);

        await _repository.Received(1).AddAsync(Arg.Is<Custodiante>(c =>
            c.RazaoSocial == command.RazaoSocial), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithDuplicateCnpj_ThrowsDuplicateEntityException()
    {
        // Arrange
        var command = ValidCommand();
        _repository.ExistsByCnpjAsync(command.Cnpj, CompanyId, Arg.Any<CancellationToken>()).Returns(true);

        // Act & Assert
        var ex = await Should.ThrowAsync<DuplicateEntityException>(() => _sut.HandleAsync(command));
        ex.EntityType.ShouldBe("Custodiante");
        ex.KeyValue.ShouldBe(command.Cnpj);

        await _repository.DidNotReceive().AddAsync(Arg.Any<Custodiante>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_RecordsAuditLog()
    {
        // Arrange
        var command = ValidCommand();
        _repository.ExistsByCnpjAsync(command.Cnpj, CompanyId, Arg.Any<CancellationToken>()).Returns(false);

        // Act
        await _sut.HandleAsync(command);

        // Assert
        await _auditService.Received(1).RecordAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            ActionType.CustodianteCreated,
            Arg.Any<Guid?>(),
            command.RazaoSocial,
            Arg.Is<string>(d => d.Contains(command.Cnpj)),
            Arg.Any<string?>(),
            Arg.Any<string?>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
    }
}