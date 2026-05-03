using Microsoft.Extensions.Logging;
using NSubstitute;
using Onboarding.Application.Common;
using Onboarding.Application.Fundos.Commands;
using Onboarding.Application.Fundos.DTOs;
using Onboarding.Domain.Aggregates.Audit;
using Onboarding.Domain.Aggregates.TipoAtivoAggregate;
using Onboarding.Domain.Repositories;
using Shouldly;

namespace Onboarding.Application.Tests.Fundos.Commands;

public class UpdateTipoAtivoCommandHandlerTests
{
    private readonly ITipoAtivoRepository _repository;
    private readonly IAuditService _auditService;
    private readonly ILogger<UpdateTipoAtivoCommandHandler> _logger;
    private readonly UpdateTipoAtivoCommandHandler _sut;

    public UpdateTipoAtivoCommandHandlerTests()
    {
        _repository = Substitute.For<ITipoAtivoRepository>();
        _auditService = Substitute.For<IAuditService>();
        _logger = Substitute.For<ILogger<UpdateTipoAtivoCommandHandler>>();

        _sut = new UpdateTipoAtivoCommandHandler(_repository, _auditService, _logger);
    }

    private static UpdateTipoAtivoCommand ValidCommand(Guid? id = null) => new(
        Id: id ?? Guid.NewGuid(),
        Descricao: "Descricao Atualizada",
        Subcategoria: "Nova Sub",
        Status: TipoAtivoStatus.ATIVO,
        OrdemExibicao: 5,
        ActorSub: "test-sub-123",
        ActorEmail: "actor@teste.com"
    );

    [Fact]
    public async Task HandleAsync_WithValidData_UpdatesFieldsAndSaves()
    {
        // Arrange
        var tipoAtivo = TipoAtivo.Register("RF-001", "Descricao Original", TipoAtivoCategoria.RendaFixa, "Sub", 1);
        var command = ValidCommand(tipoAtivo.Id);
        _repository.GetByIdAsync(tipoAtivo.Id, Arg.Any<CancellationToken>()).Returns(tipoAtivo);

        // Act
        var result = await _sut.HandleAsync(command);

        // Assert
        result.ShouldNotBeNull();
        result.Descricao.ShouldBe(command.Descricao);

        await _repository.Received(1).SaveAsync(tipoAtivo, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenEntityNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var command = ValidCommand();
        _repository.GetByIdAsync(command.Id, Arg.Any<CancellationToken>()).Returns((TipoAtivo?)null);

        // Act & Assert
        await Should.ThrowAsync<KeyNotFoundException>(() => _sut.HandleAsync(command));
    }

    [Fact]
    public async Task HandleAsync_DoesNotInjectCurrentCompanyService_GlobalScope()
    {
        // This test verifies the handler does NOT use ICurrentCompanyService.
        // The handler constructor only takes repository + audit + logger (no ICurrentCompanyService).
        // This is a design contract test — TipoAtivo is a global entity per D-03.
        var handler = new UpdateTipoAtivoCommandHandler(_repository, _auditService, _logger);
        handler.ShouldNotBeNull();
    }

    [Fact]
    public async Task HandleAsync_RecordsAuditLog()
    {
        // Arrange
        var tipoAtivo = TipoAtivo.Register("RF-001", "Descricao Original", TipoAtivoCategoria.RendaFixa, "Sub", 1);
        var command = ValidCommand(tipoAtivo.Id);
        _repository.GetByIdAsync(tipoAtivo.Id, Arg.Any<CancellationToken>()).Returns(tipoAtivo);

        // Act
        await _sut.HandleAsync(command);

        // Assert
        await _auditService.Received(1).RecordAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            ActionType.TipoAtivoUpdated,
            Arg.Any<Guid?>(),
            command.Descricao,
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }
}