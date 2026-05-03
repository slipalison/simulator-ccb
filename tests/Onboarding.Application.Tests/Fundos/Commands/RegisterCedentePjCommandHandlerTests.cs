using Microsoft.Extensions.Logging;
using NSubstitute;
using Onboarding.Application.Common;
using Onboarding.Application.Fundos.Commands;
using Onboarding.Application.Fundos.DTOs;
using Onboarding.Domain.Aggregates.Audit;
using Onboarding.Domain.Aggregates.CedenteAggregate;
using Onboarding.Domain.Exceptions;
using Onboarding.Domain.Repositories;
using Onboarding.Domain.ValueObjects;
using Shouldly;

namespace Onboarding.Application.Tests.Fundos.Commands;

public class RegisterCedentePjCommandHandlerTests
{
    private readonly ICedenteRepository _repository;
    private readonly ICurrentCompanyService _currentCompanyService;
    private readonly IAuditService _auditService;
    private readonly ILogger<RegisterCedentePjCommandHandler> _logger;
    private readonly RegisterCedentePjCommandHandler _sut;

    private static readonly Guid CompanyId = Guid.NewGuid();

    public RegisterCedentePjCommandHandlerTests()
    {
        _repository = Substitute.For<ICedenteRepository>();
        _currentCompanyService = Substitute.For<ICurrentCompanyService>();
        _auditService = Substitute.For<IAuditService>();
        _logger = Substitute.For<ILogger<RegisterCedentePjCommandHandler>>();
        _currentCompanyService.CompanyId.Returns(CompanyId);

        _sut = new RegisterCedentePjCommandHandler(
            _repository, _currentCompanyService, _auditService, _logger);
    }

    private static RegisterCedentePjCommand ValidCommand() => new(
        Cnpj: "11444777000161",
        RazaoSocial: "Empresa Teste LTDA",
        Email: "empresa@teste.com",
        Telefone: "11999999999",
        Endereco: "Av. Teste, 1000",
        ActorSub: "test-sub-123",
        ActorEmail: "actor@teste.com"
    );

    [Fact]
    public async Task HandleAsync_WithValidCnpj_CreatesCedenteWithPjVariant()
    {
        // Arrange
        var command = ValidCommand();
        _repository.ExistsByDocumentoAsync(Arg.Any<CedenteDocumento>(), CompanyId, Arg.Any<CancellationToken>()).Returns(false);

        // Act
        var result = await _sut.HandleAsync(command);

        // Assert
        result.ShouldNotBeNull();
        result.Nome.ShouldBe(command.RazaoSocial);
        result.CedenteTipo.ShouldBe(CedenteTipo.PJ);
        result.Documento.ShouldBe(command.Cnpj);

        await _repository.Received(1).AddAsync(Arg.Is<Cedente>(c =>
            c.Nome == command.RazaoSocial &&
            c.Documento.IsPj), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithDuplicateCnpj_ThrowsDuplicateEntityException()
    {
        // Arrange
        var command = ValidCommand();
        _repository.ExistsByDocumentoAsync(Arg.Any<CedenteDocumento>(), CompanyId, Arg.Any<CancellationToken>()).Returns(true);

        // Act & Assert
        var ex = await Should.ThrowAsync<DuplicateEntityException>(() => _sut.HandleAsync(command));
        ex.EntityType.ShouldBe("Cedente");

        await _repository.DidNotReceive().AddAsync(Arg.Any<Cedente>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_RecordsAuditLog()
    {
        // Arrange
        var command = ValidCommand();
        _repository.ExistsByDocumentoAsync(Arg.Any<CedenteDocumento>(), CompanyId, Arg.Any<CancellationToken>()).Returns(false);

        // Act
        await _sut.HandleAsync(command);

        // Assert
        await _auditService.Received(1).RecordAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            ActionType.CedenteCreated,
            Arg.Any<Guid?>(),
            command.RazaoSocial,
            Arg.Is<string>(d => d.Contains("CNPJ")),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }
}