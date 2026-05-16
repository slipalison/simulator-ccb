using Microsoft.Extensions.Logging;
using NSubstitute;
using Onboarding.Application.Common;
using Onboarding.Application.Fundos.Commands;
using Onboarding.Application.Fundos.DTOs;
using Onboarding.Domain.Aggregates.Audit;
using Onboarding.Domain.Aggregates.FundoAggregate;
using Onboarding.Domain.Repositories;
using Shouldly;

namespace Onboarding.Application.Tests.Fundos.Commands;

public class UpdateFundoCommandHandlerTests
{
    private readonly IFundoRepository _repository;
    private readonly ICurrentCompanyService _currentCompanyService;
    private readonly IAuditService _auditService;
    private readonly ILogger<UpdateFundoCommandHandler> _logger;
    private readonly UpdateFundoCommandHandler _sut;

    // Stable company ID used across tests for the "happy path" owner scenario
    private readonly Guid _companyId = Guid.NewGuid();

    public UpdateFundoCommandHandlerTests()
    {
        _repository = Substitute.For<IFundoRepository>();
        _currentCompanyService = Substitute.For<ICurrentCompanyService>();
        _auditService = Substitute.For<IAuditService>();
        _logger = Substitute.For<ILogger<UpdateFundoCommandHandler>>();

        _currentCompanyService.CompanyId.Returns(_companyId);

        _sut = new UpdateFundoCommandHandler(_repository, _currentCompanyService, _auditService, _logger);
    }

    private static UpdateFundoCommand ValidCommand(Guid? id = null) => new(
        Id: id ?? Guid.NewGuid(),
        Nome: "Fundo Atualizado",
        ClasseAnbima: "Classe B",
        Segmento: "Segmento 2",
        DataConstituicao: null,
        ActorSub: "test-sub-123",
        ActorEmail: "actor@teste.com"
    );

    [Fact]
    public async Task HandleAsync_WithValidData_UpdatesMutableFieldsOnly()
    {
        // Arrange — UpdateFundoCommand changes Nome, ClasseAnbima, Segmento, DataConstituicao
        // Status is handled separately via TransitionFundoStatusCommand
        var fundo = Fundo.Register(
            "Fundo Original", "11444777000161", _companyId,
            Guid.NewGuid(), Guid.NewGuid(), TipoFundo.RendaFixa);
        var command = ValidCommand(fundo.Id);
        _repository.GetByIdAsync(fundo.Id, Arg.Any<CancellationToken>()).Returns(fundo);

        // Act
        var result = await _sut.HandleAsync(command);

        // Assert
        result.ShouldNotBeNull();
        result.Nome.ShouldBe(command.Nome);
        result.ClasseAnbima.ShouldBe(command.ClasseAnbima);
        result.Segmento.ShouldBe(command.Segmento);
        // Status is NOT changed by UpdateFundo — only TransitionFundoStatusCommand does that
        result.Status.ShouldBe(FundoStatus.RASCUNHO);

        await _repository.Received(1).SaveAsync(fundo, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenEntityNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var command = ValidCommand();
        _repository.GetByIdAsync(command.Id, Arg.Any<CancellationToken>()).Returns((Fundo?)null);

        // Act & Assert
        await Should.ThrowAsync<KeyNotFoundException>(() => _sut.HandleAsync(command));
    }

    [Fact]
    public async Task HandleAsync_WhenClienteIdMismatch_ThrowsKeyNotFoundException()
    {
        // Arrange — entity belongs to a different company than the current actor
        var differentCompanyId = Guid.NewGuid();
        var fundo = Fundo.Register(
            "Fundo Original", "11444777000161", differentCompanyId,
            Guid.NewGuid(), Guid.NewGuid(), TipoFundo.RendaFixa);
        var command = ValidCommand(fundo.Id);

        _repository.GetByIdAsync(fundo.Id, Arg.Any<CancellationToken>()).Returns(fundo);
        // _companyId is already configured as the current company in ctor (differs from differentCompanyId)

        // Act & Assert — cross-tenant write must be rejected with 404-producing exception
        await Should.ThrowAsync<KeyNotFoundException>(() => _sut.HandleAsync(command));
    }

    [Fact]
    public async Task HandleAsync_RecordsAuditLog()
    {
        // Arrange
        var fundo = Fundo.Register(
            "Fundo Original", "11444777000161", _companyId,
            Guid.NewGuid(), Guid.NewGuid(), TipoFundo.RendaFixa);
        var command = ValidCommand(fundo.Id);
        _repository.GetByIdAsync(fundo.Id, Arg.Any<CancellationToken>()).Returns(fundo);

        // Act
        await _sut.HandleAsync(command);

        // Assert
        await _auditService.Received(1).RecordAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            ActionType.FundoUpdated,
            Arg.Any<Guid?>(),
            command.Nome,
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }
}