using FluentValidation;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Onboarding.Application.Common;
using Onboarding.Application.Fundos.Commands.CreateFundoCedente;
using Onboarding.Application.Fundos.Commands.TransitionFundoCedenteStatus;
using Onboarding.Application.Fundos.Commands.UpdateFundoCedenteLimite;
using Onboarding.Application.Fundos.Queries.GetFundoCedentes;
using Onboarding.Domain.Aggregates.CedenteAggregate;
using Onboarding.Domain.Aggregates.FundoCedenteAggregate;
using Onboarding.Domain.Aggregates.FundoAggregate;
using Onboarding.Domain.Aggregates.Audit;
using Onboarding.Domain.Exceptions;
using Onboarding.Domain.Repositories;
using Onboarding.Domain.ValueObjects;
using Shouldly;

namespace Onboarding.Application.Tests.Fundos;

public class FundoCedenteHandlerTests
{
    // === Infrastructure ===

    private readonly IFundoCedenteAggregateRepository _relRepo;
    private readonly IFundoRepository _fundoRepo;
    private readonly ICedenteRepository _cedenteRepo;
    private readonly IAuditService _auditService;
    private readonly ILogger<CreateFundoCedenteHandler> _createLogger;
    private readonly ILogger<UpdateFundoCedenteLimiteHandler> _updateLogger;
    private readonly ILogger<TransitionFundoCedenteStatusHandler> _transitionLogger;

    public FundoCedenteHandlerTests()
    {
        _relRepo = Substitute.For<IFundoCedenteAggregateRepository>();
        _fundoRepo = Substitute.For<IFundoRepository>();
        _cedenteRepo = Substitute.For<ICedenteRepository>();
        _auditService = Substitute.For<IAuditService>();
        _createLogger = Substitute.For<ILogger<CreateFundoCedenteHandler>>();
        _updateLogger = Substitute.For<ILogger<UpdateFundoCedenteLimiteHandler>>();
        _transitionLogger = Substitute.For<ILogger<TransitionFundoCedenteStatusHandler>>();
    }

    private CreateFundoCedenteHandler CreateHandler()
        => new(
            _relRepo,
            _fundoRepo,
            _cedenteRepo,
            _auditService,
            new CreateFundoCedenteValidator(),
            _createLogger);

    private UpdateFundoCedenteLimiteHandler UpdateHandler()
        => new(
            _relRepo,
            _auditService,
            new UpdateFundoCedenteLimiteValidator(),
            _updateLogger);

    private TransitionFundoCedenteStatusHandler TransitionHandler()
        => new(
            _relRepo,
            _auditService,
            new TransitionFundoCedenteStatusValidator(),
            _transitionLogger);

    private static Fundo MakeFundo()
        => Fundo.Register("TestFundo", "11222333000181", Guid.NewGuid(),
            Guid.NewGuid(), Guid.NewGuid(), TipoFundo.RendaFixa);

    private static FundoCedenteAggregate MakeAssociation(Guid? fundoId = null, Guid? cedenteId = null)
    {
        var fId = fundoId ?? Guid.NewGuid();
        var cId = cedenteId ?? Guid.NewGuid();
        var limite = LimiteExposicao.Create(50m, null);
        var janela = JanelaVigencia.Create(DateTimeOffset.UtcNow);
        return FundoCedenteAggregate.Create(fId, cId, limite, janela);
    }

    // =========================================================================
    // CreateFundoCedenteHandler
    // =========================================================================

    [Fact]
    public async Task CreateFundoCedente_HappyPath_ReturnsDtoWithAtivoStatus()
    {
        var fundo = MakeFundo();
        var cedenteId = Guid.NewGuid();
        var cmd = new CreateFundoCedenteCommand(
            fundo.Id, cedenteId, 50m, null,
            DateTimeOffset.UtcNow, null,
            "sub-123", "actor@test.com");

        _fundoRepo.GetByIdAsync(fundo.Id, Arg.Any<CancellationToken>()).Returns(fundo);
        _cedenteRepo.GetByIdAsync(cedenteId, Arg.Any<CancellationToken>())
            .Returns(Cedente.RegisterPf("52998224725", "Test Cedente", Guid.NewGuid()));
        _relRepo.ExistsActiveAsync(fundo.Id, cedenteId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await CreateHandler().HandleAsync(cmd);

        result.Status.ShouldBe(RelationshipStatus.ATIVO);
        result.FundoId.ShouldBe(fundo.Id);
        result.CedenteId.ShouldBe(cedenteId);
        result.LimitePercentual.ShouldBe(50m);
        await _relRepo.Received(1).AddAsync(Arg.Any<FundoCedenteAggregate>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateFundoCedente_ExistingActive_ThrowsDuplicateActiveAssociation()
    {
        var fundo = MakeFundo();
        var cedenteId = Guid.NewGuid();
        var cmd = new CreateFundoCedenteCommand(
            fundo.Id, cedenteId, 50m, null,
            DateTimeOffset.UtcNow, null,
            "sub-123", "actor@test.com");

        _fundoRepo.GetByIdAsync(fundo.Id, Arg.Any<CancellationToken>()).Returns(fundo);
        _cedenteRepo.GetByIdAsync(cedenteId, Arg.Any<CancellationToken>())
            .Returns(Cedente.RegisterPf("52998224725", "Test Cedente", Guid.NewGuid()));
        _relRepo.ExistsActiveAsync(fundo.Id, cedenteId, Arg.Any<CancellationToken>()).Returns(true);

        await Should.ThrowAsync<DuplicateActiveAssociationException>(() =>
            CreateHandler().HandleAsync(cmd));
    }

    [Fact]
    public async Task CreateFundoCedente_FundoNotFound_ThrowsKeyNotFoundException()
    {
        var fundoId = Guid.NewGuid();
        var cmd = new CreateFundoCedenteCommand(
            fundoId, Guid.NewGuid(), 50m, null,
            DateTimeOffset.UtcNow, null,
            "sub-123", "actor@test.com");

        _fundoRepo.GetByIdAsync(fundoId, Arg.Any<CancellationToken>()).Returns((Fundo?)null);

        await Should.ThrowAsync<KeyNotFoundException>(() =>
            CreateHandler().HandleAsync(cmd));
    }

    [Fact]
    public async Task CreateFundoCedente_BothLimitsNull_ThrowsValidationException()
    {
        var cmd = new CreateFundoCedenteCommand(
            Guid.NewGuid(), Guid.NewGuid(), null, null,
            DateTimeOffset.UtcNow, null,
            "sub-123", "actor@test.com");

        await Should.ThrowAsync<ValidationException>(() =>
            CreateHandler().HandleAsync(cmd));
    }

    [Fact]
    public async Task CreateFundoCedente_RecordsAuditLog()
    {
        var fundo = MakeFundo();
        var cedenteId = Guid.NewGuid();
        var cmd = new CreateFundoCedenteCommand(
            fundo.Id, cedenteId, 50m, null,
            DateTimeOffset.UtcNow, null,
            "sub-123", "actor@test.com");

        _fundoRepo.GetByIdAsync(fundo.Id, Arg.Any<CancellationToken>()).Returns(fundo);
        _cedenteRepo.GetByIdAsync(cedenteId, Arg.Any<CancellationToken>())
            .Returns(Cedente.RegisterPf("52998224725", "Test Cedente", Guid.NewGuid()));
        _relRepo.ExistsActiveAsync(fundo.Id, cedenteId, Arg.Any<CancellationToken>()).Returns(false);

        await CreateHandler().HandleAsync(cmd);

        await _auditService.Received(1).RecordAsync(
            "sub-123", "actor@test.com",
            ActionType.RelFundoCedenteCreated,
            Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
    }

    // =========================================================================
    // UpdateFundoCedenteLimiteHandler
    // =========================================================================

    [Fact]
    public async Task UpdateFundoCedenteLimite_HappyPath_UpdatesLimite()
    {
        var association = MakeAssociation();
        var cmd = new UpdateFundoCedenteLimiteCommand(
            association.Id, 75m, 100000m, "sub-123", "actor@test.com");

        _relRepo.GetByIdAsync(association.Id, Arg.Any<CancellationToken>()).Returns(association);

        var result = await UpdateHandler().HandleAsync(cmd);

        result.LimitePercentual.ShouldBe(75m);
        result.LimiteValor.ShouldBe(100000m);
        await _relRepo.Received(1).SaveAsync(Arg.Any<FundoCedenteAggregate>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateFundoCedenteLimite_NotFound_ThrowsKeyNotFoundException()
    {
        var id = Guid.NewGuid();
        var cmd = new UpdateFundoCedenteLimiteCommand(id, 50m, null, "sub-123", "actor@test.com");

        _relRepo.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns((FundoCedenteAggregate?)null);

        await Should.ThrowAsync<KeyNotFoundException>(() =>
            UpdateHandler().HandleAsync(cmd));
    }

    [Fact]
    public async Task UpdateFundoCedenteLimite_BothNull_ThrowsValidationException()
    {
        var cmd = new UpdateFundoCedenteLimiteCommand(
            Guid.NewGuid(), null, null, "sub-123", "actor@test.com");

        await Should.ThrowAsync<ValidationException>(() =>
            UpdateHandler().HandleAsync(cmd));
    }

    [Fact]
    public async Task UpdateFundoCedenteLimite_HistoricoStatus_ThrowsInvalidStateTransition()
    {
        var association = MakeAssociation();
        association.TransitionTo(RelationshipStatus.HISTORICO);
        var cmd = new UpdateFundoCedenteLimiteCommand(
            association.Id, 50m, null, "sub-123", "actor@test.com");

        _relRepo.GetByIdAsync(association.Id, Arg.Any<CancellationToken>()).Returns(association);

        await Should.ThrowAsync<InvalidStateTransitionException>(() =>
            UpdateHandler().HandleAsync(cmd));
    }

    // =========================================================================
    // TransitionFundoCedenteStatusHandler
    // =========================================================================

    [Fact]
    public async Task TransitionFundoCedenteStatus_AtivoToInativo_Succeeds()
    {
        var association = MakeAssociation();
        var cmd = new TransitionFundoCedenteStatusCommand(
            association.Id, RelationshipStatus.INATIVO, "sub-123", "actor@test.com");

        _relRepo.GetByIdAsync(association.Id, Arg.Any<CancellationToken>()).Returns(association);

        var result = await TransitionHandler().HandleAsync(cmd);

        result.Status.ShouldBe(RelationshipStatus.INATIVO);
        await _relRepo.Received(1).SaveAsync(Arg.Any<FundoCedenteAggregate>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TransitionFundoCedenteStatus_HistoricoTerminal_ThrowsInvalidStateTransition()
    {
        var association = MakeAssociation();
        association.TransitionTo(RelationshipStatus.HISTORICO);
        var cmd = new TransitionFundoCedenteStatusCommand(
            association.Id, RelationshipStatus.ATIVO, "sub-123", "actor@test.com");

        _relRepo.GetByIdAsync(association.Id, Arg.Any<CancellationToken>()).Returns(association);

        await Should.ThrowAsync<InvalidStateTransitionException>(() =>
            TransitionHandler().HandleAsync(cmd));
    }

    [Fact]
    public async Task TransitionFundoCedenteStatus_RecordsAuditLog()
    {
        var association = MakeAssociation();
        var cmd = new TransitionFundoCedenteStatusCommand(
            association.Id, RelationshipStatus.INATIVO, "sub-123", "actor@test.com");

        _relRepo.GetByIdAsync(association.Id, Arg.Any<CancellationToken>()).Returns(association);

        await TransitionHandler().HandleAsync(cmd);

        await _auditService.Received(1).RecordAsync(
            "sub-123", "actor@test.com",
            ActionType.RelFundoCedenteStatusChanged,
            Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TransitionFundoCedenteStatus_NotFound_ThrowsKeyNotFoundException()
    {
        var id = Guid.NewGuid();
        var cmd = new TransitionFundoCedenteStatusCommand(
            id, RelationshipStatus.INATIVO, "sub-123", "actor@test.com");

        _relRepo.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns((FundoCedenteAggregate?)null);

        await Should.ThrowAsync<KeyNotFoundException>(() =>
            TransitionHandler().HandleAsync(cmd));
    }

    // =========================================================================
    // GetFundoCedentesQueryHandler
    // =========================================================================

    [Fact]
    public async Task GetFundoCedentes_ReturnsPagedResults()
    {
        var fundoId = Guid.NewGuid();
        var assoc = MakeAssociation(fundoId);
        var query = new GetFundoCedentesQuery(fundoId, 1, 20);

        _relRepo.GetPagedByFundoAsync(fundoId, 1, 20, Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<FundoCedenteAggregate>)[assoc], 1));

        var handler = new GetFundoCedentesQueryHandler(_relRepo);
        var result = await handler.HandleAsync(query);

        result.Items.Count.ShouldBe(1);
        result.TotalCount.ShouldBe(1);
        result.Items[0].FundoId.ShouldBe(fundoId);
    }
}
