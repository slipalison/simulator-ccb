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

public class RegisterCedentePfCommandHandlerTests
{
    private readonly ICedenteRepository _repository;
    private readonly ICurrentCompanyService _currentCompanyService;
    private readonly IAuditService _auditService;
    private readonly ILogger<RegisterCedentePfCommandHandler> _logger;
    private readonly RegisterCedentePfCommandHandler _sut;

    private static readonly Guid CompanyId = Guid.NewGuid();

    public RegisterCedentePfCommandHandlerTests()
    {
        _repository = Substitute.For<ICedenteRepository>();
        _currentCompanyService = Substitute.For<ICurrentCompanyService>();
        _auditService = Substitute.For<IAuditService>();
        _logger = Substitute.For<ILogger<RegisterCedentePfCommandHandler>>();
        _currentCompanyService.CompanyId.Returns(CompanyId);

        _sut = new RegisterCedentePfCommandHandler(
            _repository, _currentCompanyService, _auditService, _logger);
    }

    private static RegisterCedentePfCommand ValidCommand() => new(
        Cpf: "52998224725",
        Nome: "João da Silva",
        Email: "joao@teste.com",
        Telefone: "11999999999",
        Endereco: "Rua Teste, 123",
        ActorSub: "test-sub-123",
        ActorEmail: "actor@teste.com"
    );

    [Fact]
    public async Task HandleAsync_WithValidCpf_CreatesCedenteWithPfVariant()
    {
        // Arrange
        var command = ValidCommand();
        _repository.ExistsByDocumentoAsync(Arg.Any<CedenteDocumento>(), CompanyId, Arg.Any<CancellationToken>()).Returns(false);

        // Act
        var result = await _sut.HandleAsync(command);

        // Assert
        result.ShouldNotBeNull();
        result.Nome.ShouldBe(command.Nome);
        result.CedenteTipo.ShouldBe(CedenteTipo.PF);
        result.Documento.ShouldBe(command.Cpf);

        await _repository.Received(1).AddAsync(Arg.Is<Cedente>(c =>
            c.Nome == command.Nome &&
            c.Documento.IsPf), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithDuplicateCpf_ThrowsDuplicateEntityException()
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
            command.Nome,
            Arg.Is<string>(d => d.Contains("CPF")),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }
}