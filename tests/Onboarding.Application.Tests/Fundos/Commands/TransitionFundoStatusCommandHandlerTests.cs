using Microsoft.Extensions.Logging;
using NSubstitute;
using Onboarding.Application.Common;
using Onboarding.Application.Fundos.Commands;
using Onboarding.Application.Fundos.DTOs;
using Onboarding.Domain.Aggregates.Audit;
using Onboarding.Domain.Aggregates.FundoAggregate;
using Onboarding.Domain.Exceptions;
using Onboarding.Domain.Repositories;
using Shouldly;

namespace Onboarding.Application.Tests.Fundos.Commands;

public class TransitionFundoStatusCommandHandlerTests
{
    private readonly IFundoRepository _repository;
    private readonly IAuditService _auditService;
    private readonly ILogger<TransitionFundoStatusCommandHandler> _logger;
    private readonly TransitionFundoStatusCommandHandler _sut;

    public TransitionFundoStatusCommandHandlerTests()
    {
        _repository = Substitute.For<IFundoRepository>();
        _auditService = Substitute.For<IAuditService>();
        _logger = Substitute.For<ILogger<TransitionFundoStatusCommandHandler>>();

        _sut = new TransitionFundoStatusCommandHandler(_repository, _auditService, _logger);
    }

    private static Fundo CreateFundoWithStatus(FundoStatus status)
    {
        // Use the domain Register method then manually set status for testing
        var fundo = Fundo.Register(
            "Fundo Teste", "11444777000161", Guid.NewGuid(),
            Guid.NewGuid(), Guid.NewGuid(), TipoFundo.RendaFixa);

        // Transition through valid states to reach the desired starting status
        if (status != FundoStatus.RASCUNHO)
        {
            fundo.TransitionTo(FundoStatus.ATIVO); // RASCUNHO → ATIVO
            if (status == FundoStatus.SUSPENSO)
                fundo.TransitionTo(FundoStatus.SUSPENSO); // ATIVO → SUSPENSO
            else if (status == FundoStatus.EM_LIQUIDACAO)
                fundo.TransitionTo(FundoStatus.EM_LIQUIDACAO); // ATIVO → EM_LIQUIDACAO
        }

        return fundo;
    }

    private static Fundo CreateFundoAtEmLiquidacao()
    {
        var fundo = CreateFundoWithStatus(FundoStatus.ATIVO);
        fundo.TransitionTo(FundoStatus.EM_LIQUIDACAO);
        return fundo;
    }

    // === Valid Transitions (D-02) ===

    [Fact]
    public async Task HandleAsync_RascunhoToAtivo_Succeeds()
    {
        // Arrange
        var fundo = CreateFundoWithStatus(FundoStatus.RASCUNHO);
        var command = new TransitionFundoStatusCommand(fundo.Id, FundoStatus.ATIVO, "test-sub-123", "actor@teste.com");
        _repository.GetByIdAsync(fundo.Id, Arg.Any<CancellationToken>()).Returns(fundo);

        // Act
        var result = await _sut.HandleAsync(command);

        // Assert
        result.Status.ShouldBe(FundoStatus.ATIVO);
        await _repository.Received(1).SaveAsync(fundo, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_AtivoToSuspenso_Succeeds()
    {
        // Arrange
        var fundo = CreateFundoWithStatus(FundoStatus.ATIVO);
        var command = new TransitionFundoStatusCommand(fundo.Id, FundoStatus.SUSPENSO, "test-sub-123", "actor@teste.com");
        _repository.GetByIdAsync(fundo.Id, Arg.Any<CancellationToken>()).Returns(fundo);

        // Act
        var result = await _sut.HandleAsync(command);

        // Assert
        result.Status.ShouldBe(FundoStatus.SUSPENSO);
    }

    [Fact]
    public async Task HandleAsync_SuspensoToAtivo_Succeeds()
    {
        // Arrange
        var fundo = CreateFundoWithStatus(FundoStatus.SUSPENSO);
        var command = new TransitionFundoStatusCommand(fundo.Id, FundoStatus.ATIVO, "test-sub-123", "actor@teste.com");
        _repository.GetByIdAsync(fundo.Id, Arg.Any<CancellationToken>()).Returns(fundo);

        // Act
        var result = await _sut.HandleAsync(command);

        // Assert
        result.Status.ShouldBe(FundoStatus.ATIVO);
    }

    [Fact]
    public async Task HandleAsync_AtivoToEmLiquidacao_Succeeds()
    {
        // Arrange
        var fundo = CreateFundoWithStatus(FundoStatus.ATIVO);
        var command = new TransitionFundoStatusCommand(fundo.Id, FundoStatus.EM_LIQUIDACAO, "test-sub-123", "actor@teste.com");
        _repository.GetByIdAsync(fundo.Id, Arg.Any<CancellationToken>()).Returns(fundo);

        // Act
        var result = await _sut.HandleAsync(command);

        // Assert
        result.Status.ShouldBe(FundoStatus.EM_LIQUIDACAO);
    }

    [Fact]
    public async Task HandleAsync_EmLiquidacaoToEncerrado_Succeeds()
    {
        // Arrange
        var fundo = CreateFundoAtEmLiquidacao();
        var command = new TransitionFundoStatusCommand(fundo.Id, FundoStatus.ENCERRADO, "test-sub-123", "actor@teste.com");
        _repository.GetByIdAsync(fundo.Id, Arg.Any<CancellationToken>()).Returns(fundo);

        // Act
        var result = await _sut.HandleAsync(command);

        // Assert
        result.Status.ShouldBe(FundoStatus.ENCERRADO);
    }

    [Fact]
    public async Task HandleAsync_AtivoBackToSuspenso_BidirectionalSucceeds()
    {
        // Arrange — ATIVO→SUSPENSO and back SUSPENSO→ATIVO
        var fundo = CreateFundoWithStatus(FundoStatus.ATIVO);
        var commandSuspend = new TransitionFundoStatusCommand(fundo.Id, FundoStatus.SUSPENSO, "test-sub-123", "actor@teste.com");
        _repository.GetByIdAsync(fundo.Id, Arg.Any<CancellationToken>()).Returns(fundo);

        // Act — suspend
        await _sut.HandleAsync(commandSuspend);
        fundo.Status.ShouldBe(FundoStatus.SUSPENSO);

        // Act — reactivate
        var commandReactivate = new TransitionFundoStatusCommand(fundo.Id, FundoStatus.ATIVO, "test-sub-123", "actor@teste.com");
        var result = await _sut.HandleAsync(commandReactivate);
        result.Status.ShouldBe(FundoStatus.ATIVO);
    }

    // === Invalid Transitions (D-02) ===

    [Fact]
    public async Task HandleAsync_EncerradoToAtivo_ThrowsInvalidStateTransitionException()
    {
        // Arrange — reach ENCERRADO state first
        var fundo = CreateFundoAtEmLiquidacao();
        fundo.TransitionTo(FundoStatus.ENCERRADO);
        var command = new TransitionFundoStatusCommand(fundo.Id, FundoStatus.ATIVO, "test-sub-123", "actor@teste.com");
        _repository.GetByIdAsync(fundo.Id, Arg.Any<CancellationToken>()).Returns(fundo);

        // Act & Assert
        var ex = await Should.ThrowAsync<InvalidStateTransitionException>(() => _sut.HandleAsync(command));
        ex.From.ShouldBe(FundoStatus.ENCERRADO);
        ex.To.ShouldBe(FundoStatus.ATIVO);
    }

    [Fact]
    public async Task HandleAsync_RascunhoToSuspenso_ThrowsInvalidStateTransitionException()
    {
        // Arrange
        var fundo = CreateFundoWithStatus(FundoStatus.RASCUNHO);
        var command = new TransitionFundoStatusCommand(fundo.Id, FundoStatus.SUSPENSO, "test-sub-123", "actor@teste.com");
        _repository.GetByIdAsync(fundo.Id, Arg.Any<CancellationToken>()).Returns(fundo);

        // Act & Assert
        var ex = await Should.ThrowAsync<InvalidStateTransitionException>(() => _sut.HandleAsync(command));
        ex.From.ShouldBe(FundoStatus.RASCUNHO);
        ex.To.ShouldBe(FundoStatus.SUSPENSO);
    }

    [Fact]
    public async Task HandleAsync_SuspensoToEncerrado_ThrowsInvalidStateTransitionException()
    {
        // Arrange
        var fundo = CreateFundoWithStatus(FundoStatus.SUSPENSO);
        var command = new TransitionFundoStatusCommand(fundo.Id, FundoStatus.ENCERRADO, "test-sub-123", "actor@teste.com");
        _repository.GetByIdAsync(fundo.Id, Arg.Any<CancellationToken>()).Returns(fundo);

        // Act & Assert
        var ex = await Should.ThrowAsync<InvalidStateTransitionException>(() => _sut.HandleAsync(command));
        ex.From.ShouldBe(FundoStatus.SUSPENSO);
        ex.To.ShouldBe(FundoStatus.ENCERRADO);
    }

    [Fact]
    public async Task HandleAsync_FundoNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var fundoId = Guid.NewGuid();
        var command = new TransitionFundoStatusCommand(fundoId, FundoStatus.ATIVO, "test-sub-123", "actor@teste.com");
        _repository.GetByIdAsync(fundoId, Arg.Any<CancellationToken>()).Returns((Fundo?)null);

        // Act & Assert
        await Should.ThrowAsync<KeyNotFoundException>(() => _sut.HandleAsync(command));
    }

    // === Audit ===

    [Fact]
    public async Task HandleAsync_RecordsAuditLogWithStatusTransition()
    {
        // Arrange
        var fundo = CreateFundoWithStatus(FundoStatus.RASCUNHO);
        var command = new TransitionFundoStatusCommand(fundo.Id, FundoStatus.ATIVO, "test-sub-123", "actor@teste.com");
        _repository.GetByIdAsync(fundo.Id, Arg.Any<CancellationToken>()).Returns(fundo);

        // Act
        await _sut.HandleAsync(command);

        // Assert
        await _auditService.Received(1).RecordAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            ActionType.FundoStatusChanged,
            Arg.Any<Guid?>(),
            fundo.Nome,
            Arg.Is<string>(d => d.Contains("RASCUNHO") && d.Contains("ATIVO")),
            Arg.Any<string?>(),
            Arg.Any<string?>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
    }
}