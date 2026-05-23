using FluentValidation;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Onboarding.Application.Common;
using Onboarding.Application.Fundos.Commands.CreateCedenteTipoAtivo;
using Onboarding.Application.Fundos.Commands.TransitionCedenteTipoAtivoStatus;
using Onboarding.Application.Fundos.Commands.UpdateCedenteTipoAtivoLimite;
using Onboarding.Application.Fundos.Queries.GetCedenteTiposAtivos;
using Onboarding.Domain.Aggregates.CedenteAggregate;
using Onboarding.Domain.Aggregates.CedenteTipoAtivoAggregate;
using Onboarding.Domain.Aggregates.FundoCedenteAggregate;
using Onboarding.Domain.Aggregates.Audit;
using Onboarding.Domain.Aggregates.TipoAtivoAggregate;
using Onboarding.Domain.Exceptions;
using Onboarding.Domain.Repositories;
using Onboarding.Domain.ValueObjects;
using Shouldly;

namespace Onboarding.Application.Tests.Fundos;

public class CedenteTipoAtivoHandlerTests
{
    private readonly ICedenteTipoAtivoAggregateRepository _relRepo;
    private readonly ICedenteRepository _cedenteRepo;
    private readonly ITipoAtivoRepository _tipoAtivoRepo;
    private readonly IAuditService _auditService;

    public CedenteTipoAtivoHandlerTests()
    {
        _relRepo = Substitute.For<ICedenteTipoAtivoAggregateRepository>();
        _cedenteRepo = Substitute.For<ICedenteRepository>();
        _tipoAtivoRepo = Substitute.For<ITipoAtivoRepository>();
        _auditService = Substitute.For<IAuditService>();
    }

    private static CedenteTipoAtivoAggregate MakeAssoc(Guid? cedenteId = null, Guid? tipoAtivoId = null)
    {
        var cId = cedenteId ?? Guid.NewGuid();
        var tId = tipoAtivoId ?? Guid.NewGuid();
        return CedenteTipoAtivoAggregate.Create(cId, tId,
            LimiteExposicao.Create(30m, null),
            JanelaVigencia.Create(DateTimeOffset.UtcNow));
    }

    private CreateCedenteTipoAtivoHandler CreateHandler()
        => new(_relRepo, _cedenteRepo, _tipoAtivoRepo, _auditService,
            new CreateCedenteTipoAtivoValidator(),
            Substitute.For<ILogger<CreateCedenteTipoAtivoHandler>>());

    // =========================================================================
    // Create
    // =========================================================================

    [Fact]
    public async Task CreateCedenteTipoAtivo_HappyPath_ReturnsAtivoDto()
    {
        var cedenteId = Guid.NewGuid();
        var tipoAtivoId = Guid.NewGuid();
        var cmd = new CreateCedenteTipoAtivoCommand(
            cedenteId, tipoAtivoId, 30m, null,
            DateTimeOffset.UtcNow, null, "sub", "actor@test.com");

        _cedenteRepo.GetByIdAsync(cedenteId, Arg.Any<CancellationToken>())
            .Returns(Cedente.RegisterPf("52998224725", "Cedente Teste", Guid.NewGuid()));
        _tipoAtivoRepo.GetByIdAsync(tipoAtivoId, Arg.Any<CancellationToken>())
            .Returns(TipoAtivo.Register("CDB", "CDB Desc", TipoAtivoCategoria.RendaFixa));
        _relRepo.ExistsActiveAsync(cedenteId, tipoAtivoId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await CreateHandler().HandleAsync(cmd);

        result.Status.ShouldBe(RelationshipStatus.ATIVO);
        result.CedenteId.ShouldBe(cedenteId);
        await _relRepo.Received(1).AddAsync(Arg.Any<CedenteTipoAtivoAggregate>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateCedenteTipoAtivo_DuplicateActive_ThrowsDuplicateActiveAssociation()
    {
        var cedenteId = Guid.NewGuid();
        var tipoAtivoId = Guid.NewGuid();
        var cmd = new CreateCedenteTipoAtivoCommand(
            cedenteId, tipoAtivoId, 30m, null,
            DateTimeOffset.UtcNow, null, "sub", "actor@test.com");

        _cedenteRepo.GetByIdAsync(cedenteId, Arg.Any<CancellationToken>())
            .Returns(Cedente.RegisterPf("52998224725", "Cedente Teste", Guid.NewGuid()));
        _tipoAtivoRepo.GetByIdAsync(tipoAtivoId, Arg.Any<CancellationToken>())
            .Returns(TipoAtivo.Register("CDB", "CDB Desc", TipoAtivoCategoria.RendaFixa));
        _relRepo.ExistsActiveAsync(cedenteId, tipoAtivoId, Arg.Any<CancellationToken>()).Returns(true);

        await Should.ThrowAsync<DuplicateActiveAssociationException>(() =>
            CreateHandler().HandleAsync(cmd));
    }

    [Fact]
    public async Task CreateCedenteTipoAtivo_CedenteNotFound_ThrowsKeyNotFound()
    {
        var cedenteId = Guid.NewGuid();
        var cmd = new CreateCedenteTipoAtivoCommand(
            cedenteId, Guid.NewGuid(), 30m, null,
            DateTimeOffset.UtcNow, null, "sub", "actor@test.com");

        _cedenteRepo.GetByIdAsync(cedenteId, Arg.Any<CancellationToken>()).Returns((Cedente?)null);

        await Should.ThrowAsync<KeyNotFoundException>(() => CreateHandler().HandleAsync(cmd));
    }

    [Fact]
    public async Task CreateCedenteTipoAtivo_BothLimitsNull_ThrowsValidation()
    {
        var cmd = new CreateCedenteTipoAtivoCommand(
            Guid.NewGuid(), Guid.NewGuid(), null, null,
            DateTimeOffset.UtcNow, null, "sub", "actor@test.com");

        await Should.ThrowAsync<ValidationException>(() => CreateHandler().HandleAsync(cmd));
    }

    // =========================================================================
    // Update
    // =========================================================================

    [Fact]
    public async Task UpdateCedenteTipoAtivoLimite_HappyPath_Updates()
    {
        var assoc = MakeAssoc();
        var cmd = new UpdateCedenteTipoAtivoLimiteCommand(assoc.Id, 60m, null, "sub", "actor@test.com");

        _relRepo.GetByIdAsync(assoc.Id, Arg.Any<CancellationToken>()).Returns(assoc);

        var handler = new UpdateCedenteTipoAtivoLimiteHandler(
            _relRepo, _auditService,
            new UpdateCedenteTipoAtivoLimiteValidator(),
            Substitute.For<ILogger<UpdateCedenteTipoAtivoLimiteHandler>>());

        var result = await handler.HandleAsync(cmd);

        result.LimitePercentual.ShouldBe(60m);
    }

    [Fact]
    public async Task UpdateCedenteTipoAtivoLimite_Historico_ThrowsInvalidStateTransition()
    {
        var assoc = MakeAssoc();
        assoc.TransitionTo(RelationshipStatus.HISTORICO);
        var cmd = new UpdateCedenteTipoAtivoLimiteCommand(assoc.Id, 50m, null, "sub", "actor@test.com");

        _relRepo.GetByIdAsync(assoc.Id, Arg.Any<CancellationToken>()).Returns(assoc);

        var handler = new UpdateCedenteTipoAtivoLimiteHandler(
            _relRepo, _auditService,
            new UpdateCedenteTipoAtivoLimiteValidator(),
            Substitute.For<ILogger<UpdateCedenteTipoAtivoLimiteHandler>>());

        await Should.ThrowAsync<InvalidStateTransitionException>(() => handler.HandleAsync(cmd));
    }

    // =========================================================================
    // Transition
    // =========================================================================

    [Fact]
    public async Task TransitionCedenteTipoAtivoStatus_AtivoToInativo_Succeeds()
    {
        var assoc = MakeAssoc();
        var cmd = new TransitionCedenteTipoAtivoStatusCommand(
            assoc.Id, RelationshipStatus.INATIVO, "sub", "actor@test.com");

        _relRepo.GetByIdAsync(assoc.Id, Arg.Any<CancellationToken>()).Returns(assoc);

        var handler = new TransitionCedenteTipoAtivoStatusHandler(
            _relRepo, _auditService,
            new TransitionCedenteTipoAtivoStatusValidator(),
            Substitute.For<ILogger<TransitionCedenteTipoAtivoStatusHandler>>());

        var result = await handler.HandleAsync(cmd);

        result.Status.ShouldBe(RelationshipStatus.INATIVO);
        await _auditService.Received(1).RecordAsync(
            Arg.Any<string>(), Arg.Any<string>(),
            ActionType.RelCedenteTipoAtivoStatusChanged,
            Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TransitionCedenteTipoAtivoStatus_HistoricoTerminal_ThrowsInvalidStateTransition()
    {
        var assoc = MakeAssoc();
        assoc.TransitionTo(RelationshipStatus.HISTORICO);
        var cmd = new TransitionCedenteTipoAtivoStatusCommand(
            assoc.Id, RelationshipStatus.ATIVO, "sub", "actor@test.com");

        _relRepo.GetByIdAsync(assoc.Id, Arg.Any<CancellationToken>()).Returns(assoc);

        var handler = new TransitionCedenteTipoAtivoStatusHandler(
            _relRepo, _auditService,
            new TransitionCedenteTipoAtivoStatusValidator(),
            Substitute.For<ILogger<TransitionCedenteTipoAtivoStatusHandler>>());

        await Should.ThrowAsync<InvalidStateTransitionException>(() => handler.HandleAsync(cmd));
    }

    // =========================================================================
    // Query
    // =========================================================================

    [Fact]
    public async Task GetCedenteTiposAtivos_ReturnsPagedResults()
    {
        var cedenteId = Guid.NewGuid();
        var assoc = MakeAssoc(cedenteId);
        var query = new GetCedenteTiposAtivosQuery(cedenteId, 1, 20);

        _relRepo.GetPagedByCedenteAsync(cedenteId, 1, 20, Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<CedenteTipoAtivoAggregate>)[assoc], 1));

        var handler = new GetCedenteTiposAtivosQueryHandler(_relRepo);
        var result = await handler.HandleAsync(query);

        result.Items.Count.ShouldBe(1);
        result.Items[0].CedenteId.ShouldBe(cedenteId);
    }
}
