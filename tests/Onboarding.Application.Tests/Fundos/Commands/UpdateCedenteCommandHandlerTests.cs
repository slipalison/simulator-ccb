using Microsoft.Extensions.Logging;
using NSubstitute;
using Onboarding.Application.Common;
using Onboarding.Application.Fundos.Commands;
using Onboarding.Application.Fundos.DTOs;
using Onboarding.Domain.Aggregates.Audit;
using Onboarding.Domain.Aggregates.CedenteAggregate;
using Onboarding.Domain.Repositories;
using Shouldly;

namespace Onboarding.Application.Tests.Fundos.Commands;

public class UpdateCedenteCommandHandlerTests
{
    private readonly ICedenteRepository _repository;
    private readonly IAuditService _auditService;
    private readonly ILogger<UpdateCedenteCommandHandler> _logger;
    private readonly UpdateCedenteCommandHandler _sut;

    public UpdateCedenteCommandHandlerTests()
    {
        _repository = Substitute.For<ICedenteRepository>();
        _auditService = Substitute.For<IAuditService>();
        _logger = Substitute.For<ILogger<UpdateCedenteCommandHandler>>();

        _sut = new UpdateCedenteCommandHandler(_repository, _auditService, _logger);
    }

    private static UpdateCedenteCommand ValidCommand(Guid? id = null) => new(
        Id: id ?? Guid.NewGuid(),
        Nome: "Nome Atualizado",
        Email: "atualizado@teste.com",
        Telefone: "11888888888",
        Endereco: "Rua Nova, 456",
        Status: CedenteStatus.ATIVO,
        ActorSub: "test-sub-123",
        ActorEmail: "actor@teste.com"
    );

    [Fact]
    public async Task HandleAsync_WithValidData_UpdatesCedenteFields()
    {
        // Arrange
        var cedente = Cedente.RegisterPf("52998224725", "Nome Original", Guid.NewGuid(), "old@teste.com");
        var command = ValidCommand(cedente.Id);
        _repository.GetByIdAsync(cedente.Id, Arg.Any<CancellationToken>()).Returns(cedente);

        // Act
        var result = await _sut.HandleAsync(command);

        // Assert
        result.ShouldNotBeNull();
        result.Nome.ShouldBe(command.Nome);

        await _repository.Received(1).SaveAsync(cedente, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenEntityNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var command = ValidCommand();
        _repository.GetByIdAsync(command.Id, Arg.Any<CancellationToken>()).Returns((Cedente?)null);

        // Act & Assert
        await Should.ThrowAsync<KeyNotFoundException>(() => _sut.HandleAsync(command));
    }

    [Fact]
    public async Task HandleAsync_RecordsAuditLog()
    {
        // Arrange
        var cedente = Cedente.RegisterPf("52998224725", "Nome Original", Guid.NewGuid());
        var command = ValidCommand(cedente.Id);
        _repository.GetByIdAsync(cedente.Id, Arg.Any<CancellationToken>()).Returns(cedente);

        // Act
        await _sut.HandleAsync(command);

        // Assert
        await _auditService.Received(1).RecordAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            ActionType.CedenteUpdated,
            Arg.Any<Guid?>(),
            command.Nome,
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }
}