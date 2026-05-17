using FluentValidation;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Onboarding.Application.Common;
using Onboarding.Application.Fundos.Commands.CreateFundoTipoAtivo;
using Onboarding.Application.Fundos.Commands.TransitionFundoTipoAtivoStatus;
using Onboarding.Application.Fundos.Commands.UpdateFundoTipoAtivoLimite;
using Onboarding.Application.Fundos.Queries.GetFundoTiposAtivos;
using Onboarding.Domain.Aggregates.FundoAggregate;
using Onboarding.Domain.Aggregates.FundoCedenteAggregate;
using Onboarding.Domain.Aggregates.FundoTipoAtivoAggregate;
using Onboarding.Domain.Aggregates.Audit;
using Onboarding.Domain.Aggregates.TipoAtivoAggregate;
using Onboarding.Domain.Exceptions;
using Onboarding.Domain.Repositories;
using Onboarding.Domain.ValueObjects;
using Shouldly;

namespace Onboarding.Application.Tests.Fundos;

public class FundoTipoAtivoHandlerTests
{
    private readonly IFundoTipoAtivoAggregateRepository _relRepo;
    private readonly IFundoRepository _fundoRepo;
    private readonly ITipoAtivoRepository _tipoAtivoRepo;
    private readonly IAuditService _auditService;

    public FundoTipoAtivoHandlerTests()
    {
        _relRepo = Substitute.For<IFundoTipoAtivoAggregateRepository>();
        _fundoRepo = Substitute.For<IFundoRepository>();
        _tipoAtivoRepo = Substitute.For<ITipoAtivoRepository>();
        _auditService = Substitute.For<IAuditService>();
    }

    private static FundoTipoAtivoAggregate MakeAssoc(Guid? fundoId = null, Guid? tipoAtivoId = null)
    {
        var fId = fundoId ?? Guid.NewGuid();
        var tId = tipoAtivoId ?? Guid.NewGuid();
        return FundoTipoAtivoAggregate.Create(fId, tId,
            LimiteExposicao.Create(30m, null),
            JanelaVigencia.Create(DateTimeOffset.UtcNow));
    }

    private static Fundo MakeFundo()
        => Fundo.Register("TestFundo", "11222333000181", Guid.NewGuid(),
            Guid.NewGuid(), Guid.NewGuid(), TipoFundo.RendaFixa);

    private CreateFundoTipoAtivoHandler CreateHandler()
        => new(_relRepo, _fundoRepo, _tipoAtivoRepo, _auditService,
            new CreateFundoTipoAtivoValidator(),
            Substitute.For<ILogger<CreateFundoTipoAtivoHandler>>());

    // =========================================================================
    // Create
    // =========================================================================

    [Fact]
    public async Task CreateFundoTipoAtivo_HappyPath_ReturnsTipoAtivoDto()
    {
        var fundoId = Guid.NewGuid();
        var tipoAtivoId = Guid.NewGuid();
        var cmd = new CreateFundoTipoAtivoCommand(
            fundoId, tipoAtivoId, 30m, null,
            DateTimeOffset.UtcNow, null, "sub", "actor@test.com");

        _fundoRepo.GetByIdAsync(fundoId, Arg.Any<CancellationToken>())
            .Returns(MakeFundo());
        _tipoAtivoRepo.GetByIdAsync(tipoAtivoId, Arg.Any<CancellationToken>())
            .Returns(TipoAtivo.Register("CDB", "CDB Desc", TipoAtivoCategoria.RendaFixa));
        _relRepo.ExistsActiveAsync(fundoId, tipoAtivoId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await CreateHandler().HandleAsync(cmd);

        result.Status.ShouldBe(RelationshipStatus.ATIVO);
        result.FundoId.ShouldBe(fundoId);
        result.TipoAtivoId.ShouldBe(tipoAtivoId);
        await _relRepo.Received(1).AddAsync(Arg.Any<FundoTipoAtivoAggregate>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateFundoTipoAtivo_DuplicateActive_ThrowsDuplicateActiveAssociation()
    {
        var fundoId = Guid.NewGuid();
        var tipoAtivoId = Guid.NewGuid();
        var cmd = new CreateFundoTipoAtivoCommand(
            fundoId, tipoAtivoId, 30m, null,
            DateTimeOffset.UtcNow, null, "sub", "actor@test.com");

        _fundoRepo.GetByIdAsync(fundoId, Arg.Any<CancellationToken>())
            .Returns(MakeFundo());
        _tipoAtivoRepo.GetByIdAsync(tipoAtivoId, Arg.Any<CancellationToken>())
            .Returns(TipoAtivo.Register("CDB", "CDB Desc", TipoAtivoCategoria.RendaFixa));
        _relRepo.ExistsActiveAsync(fundoId, tipoAtivoId, Arg.Any<CancellationToken>()).Returns(true);

        await Should.ThrowAsync<DuplicateActiveAssociationException>(() =>
            CreateHandler().HandleAsync(cmd));
    }

    [Fact]
    public async Task CreateFundoTipoAtivo_FundoNotFound_ThrowsKeyNotFound()
    {
        var fundoId = Guid.NewGuid();
        var cmd = new CreateFundoTipoAtivoCommand(
            fundoId, Guid.NewGuid(), 30m, null,
            DateTimeOffset.UtcNow, null, "sub", "actor@test.com");

        _fundoRepo.GetByIdAsync(fundoId, Arg.Any<CancellationToken>()).Returns((Fundo?)null);

        await Should.ThrowAsync<KeyNotFoundException>(() => CreateHandler().HandleAsync(cmd));
    }

    [Fact]
    public async Task CreateFundoTipoAtivo_TipoAtivoNotFound_ThrowsKeyNotFound()
    {
        var fundoId = Guid.NewGuid();
        var tipoAtivoId = Guid.NewGuid();
        var cmd = new CreateFundoTipoAtivoCommand(
            fundoId, tipoAtivoId, 30m, null,
            DateTimeOffset.UtcNow, null, "sub", "actor@test.com");

        _fundoRepo.GetByIdAsync(fundoId, Arg.Any<CancellationToken>())
            .Returns(MakeFundo());
        _tipoAtivoRepo.GetByIdAsync(tipoAtivoId, Arg.Any<CancellationToken>()).Returns((TipoAtivo?)null);

        await Should.ThrowAsync<KeyNotFoundException>(() => CreateHandler().HandleAsync(cmd));
    }

    [Fact]
    public async Task CreateFundoTipoAtivo_BothLimitsNull_ThrowsValidation()
    {
        var cmd = new CreateFundoTipoAtivoCommand(
            Guid.NewGuid(), Guid.NewGuid(), null, null,
            DateTimeOffset.UtcNow, null, "sub", "actor@test.com");

        await Should.ThrowAsync<ValidationException>(() => CreateHandler().HandleAsync(cmd));
    }

    // =========================================================================
    // Update
    // =========================================================================

    [Fact]
    public async Task UpdateFundoTipoAtivoLimite_HappyPath_Updates()
    {
        var assoc = MakeAssoc();
        var cmd = new UpdateFundoTipoAtivoLimiteCommand(assoc.Id, 60m, null, "sub", "actor@test.com");

        _relRepo.GetByIdAsync(assoc.Id, Arg.Any<CancellationToken>()).Returns(assoc);

        var handler = new UpdateFundoTipoAtivoLimiteHandler(
            _relRepo, _auditService,
            new UpdateFundoTipoAtivoLimiteValidator(),
            Substitute.For<ILogger<UpdateFundoTipoAtivoLimiteHandler>>());

        var result = await handler.HandleAsync(cmd);

        result.LimitePercentual.ShouldBe(60m);
    }

    [Fact]
    public async Task UpdateFundoTipoAtivoLimite_Historico_ThrowsInvalidStateTransition()
    {
        var assoc = MakeAssoc();
        assoc.TransitionTo(RelationshipStatus.HISTORICO);
        var cmd = new UpdateFundoTipoAtivoLimiteCommand(assoc.Id, 50m, null, "sub", "actor@test.com");

        _relRepo.GetByIdAsync(assoc.Id, Arg.Any<CancellationToken>()).Returns(assoc);

        var handler = new UpdateFundoTipoAtivoLimiteHandler(
            _relRepo, _auditService,
            new UpdateFundoTipoAtivoLimiteValidator(),
            Substitute.For<ILogger<UpdateFundoTipoAtivoLimiteHandler>>());

        await Should.ThrowAsync<InvalidStateTransitionException>(() => handler.HandleAsync(cmd));
    }

    [Fact]
    public async Task UpdateFundoTipoAtivoLimite_NotFound_ThrowsKeyNotFound()
    {
        var id = Guid.NewGuid();
        var cmd = new UpdateFundoTipoAtivoLimiteCommand(id, 60m, null, "sub", "actor@test.com");

        _relRepo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((FundoTipoAtivoAggregate?)null);

        var handler = new UpdateFundoTipoAtivoLimiteHandler(
            _relRepo, _auditService,
            new UpdateFundoTipoAtivoLimiteValidator(),
            Substitute.For<ILogger<UpdateFundoTipoAtivoLimiteHandler>>());

        await Should.ThrowAsync<KeyNotFoundException>(() => handler.HandleAsync(cmd));
    }

    // =========================================================================
    // Transition
    // =========================================================================

    [Fact]
    public async Task TransitionFundoTipoAtivoStatus_AtivoToInativo_Succeeds()
    {
        var assoc = MakeAssoc();
        var cmd = new TransitionFundoTipoAtivoStatusCommand(
            assoc.Id, RelationshipStatus.INATIVO, "sub", "actor@test.com");

        _relRepo.GetByIdAsync(assoc.Id, Arg.Any<CancellationToken>()).Returns(assoc);

        var handler = new TransitionFundoTipoAtivoStatusHandler(
            _relRepo, _auditService,
            new TransitionFundoTipoAtivoStatusValidator(),
            Substitute.For<ILogger<TransitionFundoTipoAtivoStatusHandler>>());

        var result = await handler.HandleAsync(cmd);

        result.Status.ShouldBe(RelationshipStatus.INATIVO);
        await _auditService.Received(1).RecordAsync(
            Arg.Any<string>(), Arg.Any<string>(),
            ActionType.RelFundoTipoAtivoStatusChanged,
            Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TransitionFundoTipoAtivoStatus_InativoToAtivo_Succeeds()
    {
        var assoc = MakeAssoc();
        assoc.TransitionTo(RelationshipStatus.INATIVO);
        var cmd = new TransitionFundoTipoAtivoStatusCommand(
            assoc.Id, RelationshipStatus.ATIVO, "sub", "actor@test.com");

        _relRepo.GetByIdAsync(assoc.Id, Arg.Any<CancellationToken>()).Returns(assoc);

        var handler = new TransitionFundoTipoAtivoStatusHandler(
            _relRepo, _auditService,
            new TransitionFundoTipoAtivoStatusValidator(),
            Substitute.For<ILogger<TransitionFundoTipoAtivoStatusHandler>>());

        var result = await handler.HandleAsync(cmd);

        result.Status.ShouldBe(RelationshipStatus.ATIVO);
    }

    [Fact]
    public async Task TransitionFundoTipoAtivoStatus_HistoricoTerminal_ThrowsInvalidStateTransition()
    {
        var assoc = MakeAssoc();
        assoc.TransitionTo(RelationshipStatus.HISTORICO);
        var cmd = new TransitionFundoTipoAtivoStatusCommand(
            assoc.Id, RelationshipStatus.ATIVO, "sub", "actor@test.com");

        _relRepo.GetByIdAsync(assoc.Id, Arg.Any<CancellationToken>()).Returns(assoc);

        var handler = new TransitionFundoTipoAtivoStatusHandler(
            _relRepo, _auditService,
            new TransitionFundoTipoAtivoStatusValidator(),
            Substitute.For<ILogger<TransitionFundoTipoAtivoStatusHandler>>());

        await Should.ThrowAsync<InvalidStateTransitionException>(() => handler.HandleAsync(cmd));
    }

    [Fact]
    public async Task TransitionFundoTipoAtivoStatus_NotFound_ThrowsKeyNotFound()
    {
        var id = Guid.NewGuid();
        var cmd = new TransitionFundoTipoAtivoStatusCommand(
            id, RelationshipStatus.INATIVO, "sub", "actor@test.com");

        _relRepo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((FundoTipoAtivoAggregate?)null);

        var handler = new TransitionFundoTipoAtivoStatusHandler(
            _relRepo, _auditService,
            new TransitionFundoTipoAtivoStatusValidator(),
            Substitute.For<ILogger<TransitionFundoTipoAtivoStatusHandler>>());

        await Should.ThrowAsync<KeyNotFoundException>(() => handler.HandleAsync(cmd));
    }

    // =========================================================================
    // Query
    // =========================================================================

    [Fact]
    public async Task GetFundoTiposAtivos_ReturnsPagedResults()
    {
        var fundoId = Guid.NewGuid();
        var assoc = MakeAssoc(fundoId);
        var query = new GetFundoTiposAtivosQuery(fundoId, 1, 20);

        _relRepo.GetPagedByFundoAsync(fundoId, 1, 20, Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<FundoTipoAtivoAggregate>)[assoc], 1));

        var handler = new GetFundoTiposAtivosQueryHandler(_relRepo);
        var result = await handler.HandleAsync(query);

        result.Items.Count.ShouldBe(1);
        result.Items[0].FundoId.ShouldBe(fundoId);
    }

    [Fact]
    public async Task GetFundoTiposAtivos_EmptyPage_ReturnsEmpty()
    {
        var fundoId = Guid.NewGuid();
        var query = new GetFundoTiposAtivosQuery(fundoId, 2, 20);

        _relRepo.GetPagedByFundoAsync(fundoId, 2, 20, Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<FundoTipoAtivoAggregate>)[], 0));

        var handler = new GetFundoTiposAtivosQueryHandler(_relRepo);
        var result = await handler.HandleAsync(query);

        result.Items.ShouldBeEmpty();
        result.TotalCount.ShouldBe(0);
    }
}
