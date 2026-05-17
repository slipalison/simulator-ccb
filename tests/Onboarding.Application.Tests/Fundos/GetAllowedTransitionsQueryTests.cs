using NSubstitute;
using Onboarding.Application.Common;
using Onboarding.Application.Fundos.Queries.GetCedenteTipoAtivoAllowedTransitions;
using Onboarding.Application.Fundos.Queries.GetFundoAllowedTransitions;
using Onboarding.Application.Fundos.Queries.GetFundoCedenteAllowedTransitions;
using Onboarding.Application.Fundos.Queries.GetFundoTipoAtivoAllowedTransitions;
using Onboarding.Domain.Aggregates.CedenteTipoAtivoAggregate;
using Onboarding.Domain.Aggregates.FundoCedenteAggregate;
using Onboarding.Domain.Aggregates.FundoAggregate;
using Onboarding.Domain.Aggregates.FundoTipoAtivoAggregate;
using Onboarding.Domain.Repositories;
using Onboarding.Domain.ValueObjects;
using Shouldly;

namespace Onboarding.Application.Tests.Fundos;

/// <summary>
/// Unit tests for the 4 GetAllowedTransitions query handlers (D-25).
/// Covers: happy path, not-found, cross-tenant guard (returns null).
/// </summary>
public class GetAllowedTransitionsQueryTests
{
    private static readonly Guid CompanyId = Guid.NewGuid();
    private static readonly Guid OtherCompanyId = Guid.NewGuid();

    // Shared helpers
    private static LimiteExposicao Limite => LimiteExposicao.FromPercentual(50m);
    private static JanelaVigencia Janela => JanelaVigencia.Create(DateTimeOffset.UtcNow);

    // =========================================================================
    // GetFundoAllowedTransitionsQueryHandler
    // =========================================================================

    [Fact]
    public async Task HandleAsync_fundoFound_returnsFundoAllowedNextStates()
    {
        var fundoRepo = Substitute.For<IFundoRepository>();
        var company = Substitute.For<ICurrentCompanyService>();
        company.CompanyId.Returns(CompanyId);

        var fundo = MakeFundo(CompanyId, FundoStatus.RASCUNHO);
        fundoRepo.GetByIdAsync(fundo.Id, Arg.Any<CancellationToken>()).Returns(fundo);

        var sut = new GetFundoAllowedTransitionsQueryHandler(fundoRepo, company);
        var result = await sut.HandleAsync(new GetFundoAllowedTransitionsQuery(fundo.Id));

        result.ShouldNotBeNull();
        result.ShouldContain("ATIVO");
        result.Count.ShouldBe(1);
    }

    [Fact]
    public async Task HandleAsync_fundoNotFound_returnsNull()
    {
        var fundoRepo = Substitute.For<IFundoRepository>();
        var company = Substitute.For<ICurrentCompanyService>();
        company.CompanyId.Returns(CompanyId);
        fundoRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Domain.Aggregates.FundoAggregate.Fundo?)null);

        var sut = new GetFundoAllowedTransitionsQueryHandler(fundoRepo, company);
        var result = await sut.HandleAsync(new GetFundoAllowedTransitionsQuery(Guid.NewGuid()));

        result.ShouldBeNull();
    }

    [Fact]
    public async Task HandleAsync_fundoCrossTenant_returnsNull()
    {
        var fundoRepo = Substitute.For<IFundoRepository>();
        var company = Substitute.For<ICurrentCompanyService>();
        company.CompanyId.Returns(CompanyId);

        var fundo = MakeFundo(OtherCompanyId, FundoStatus.RASCUNHO);
        fundoRepo.GetByIdAsync(fundo.Id, Arg.Any<CancellationToken>()).Returns(fundo);

        var sut = new GetFundoAllowedTransitionsQueryHandler(fundoRepo, company);
        var result = await sut.HandleAsync(new GetFundoAllowedTransitionsQuery(fundo.Id));

        result.ShouldBeNull();
    }

    // =========================================================================
    // GetFundoCedenteAllowedTransitionsQueryHandler
    // =========================================================================

    [Fact]
    public async Task HandleAsync_fundoCedenteFound_returnsAllowedNextStates()
    {
        var fundoRepo = Substitute.For<IFundoRepository>();
        var repo = Substitute.For<IFundoCedenteAggregateRepository>();
        var company = Substitute.For<ICurrentCompanyService>();
        company.CompanyId.Returns(CompanyId);

        var fundo = MakeFundo(CompanyId, FundoStatus.RASCUNHO);
        var assoc = FundoCedenteAggregate.Create(fundo.Id, Guid.NewGuid(), Limite, Janela);
        fundoRepo.GetByIdAsync(fundo.Id, Arg.Any<CancellationToken>()).Returns(fundo);
        repo.GetByIdAsync(assoc.Id, Arg.Any<CancellationToken>()).Returns(assoc);

        var sut = new GetFundoCedenteAllowedTransitionsQueryHandler(repo, fundoRepo, company);
        var result = await sut.HandleAsync(
            new GetFundoCedenteAllowedTransitionsQuery(fundo.Id, assoc.Id));

        result.ShouldNotBeNull();
        result.ShouldContain("INATIVO");
        result.ShouldContain("HISTORICO");
    }

    [Fact]
    public async Task HandleAsync_fundoCedenteFundoCrossTenant_returnsNull()
    {
        var fundoRepo = Substitute.For<IFundoRepository>();
        var repo = Substitute.For<IFundoCedenteAggregateRepository>();
        var company = Substitute.For<ICurrentCompanyService>();
        company.CompanyId.Returns(CompanyId);

        var fundo = MakeFundo(OtherCompanyId, FundoStatus.RASCUNHO);
        fundoRepo.GetByIdAsync(fundo.Id, Arg.Any<CancellationToken>()).Returns(fundo);

        var sut = new GetFundoCedenteAllowedTransitionsQueryHandler(repo, fundoRepo, company);
        var result = await sut.HandleAsync(
            new GetFundoCedenteAllowedTransitionsQuery(fundo.Id, Guid.NewGuid()));

        result.ShouldBeNull();
    }

    [Fact]
    public async Task HandleAsync_fundoCedenteAssocNotFound_returnsNull()
    {
        var fundoRepo = Substitute.For<IFundoRepository>();
        var repo = Substitute.For<IFundoCedenteAggregateRepository>();
        var company = Substitute.For<ICurrentCompanyService>();
        company.CompanyId.Returns(CompanyId);

        var fundo = MakeFundo(CompanyId, FundoStatus.RASCUNHO);
        fundoRepo.GetByIdAsync(fundo.Id, Arg.Any<CancellationToken>()).Returns(fundo);
        repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((FundoCedenteAggregate?)null);

        var sut = new GetFundoCedenteAllowedTransitionsQueryHandler(repo, fundoRepo, company);
        var result = await sut.HandleAsync(
            new GetFundoCedenteAllowedTransitionsQuery(fundo.Id, Guid.NewGuid()));

        result.ShouldBeNull();
    }

    [Fact]
    public async Task HandleAsync_fundoCedenteAssocBelongsToDifferentFundo_returnsNull()
    {
        var fundoRepo = Substitute.For<IFundoRepository>();
        var repo = Substitute.For<IFundoCedenteAggregateRepository>();
        var company = Substitute.For<ICurrentCompanyService>();
        company.CompanyId.Returns(CompanyId);

        var fundo = MakeFundo(CompanyId, FundoStatus.RASCUNHO);
        // Association belongs to a DIFFERENT fundo
        var assoc = FundoCedenteAggregate.Create(Guid.NewGuid(), Guid.NewGuid(), Limite, Janela);
        fundoRepo.GetByIdAsync(fundo.Id, Arg.Any<CancellationToken>()).Returns(fundo);
        repo.GetByIdAsync(assoc.Id, Arg.Any<CancellationToken>()).Returns(assoc);

        var sut = new GetFundoCedenteAllowedTransitionsQueryHandler(repo, fundoRepo, company);
        var result = await sut.HandleAsync(
            new GetFundoCedenteAllowedTransitionsQuery(fundo.Id, assoc.Id));

        result.ShouldBeNull();
    }

    // =========================================================================
    // GetFundoTipoAtivoAllowedTransitionsQueryHandler
    // =========================================================================

    [Fact]
    public async Task HandleAsync_fundoTipoAtivoFound_returnsAllowedNextStates()
    {
        var fundoRepo = Substitute.For<IFundoRepository>();
        var repo = Substitute.For<IFundoTipoAtivoAggregateRepository>();
        var company = Substitute.For<ICurrentCompanyService>();
        company.CompanyId.Returns(CompanyId);

        var fundo = MakeFundo(CompanyId, FundoStatus.RASCUNHO);
        var assoc = FundoTipoAtivoAggregate.Create(fundo.Id, Guid.NewGuid(), Limite, Janela);
        fundoRepo.GetByIdAsync(fundo.Id, Arg.Any<CancellationToken>()).Returns(fundo);
        repo.GetByIdAsync(assoc.Id, Arg.Any<CancellationToken>()).Returns(assoc);

        var sut = new GetFundoTipoAtivoAllowedTransitionsQueryHandler(repo, fundoRepo, company);
        var result = await sut.HandleAsync(
            new GetFundoTipoAtivoAllowedTransitionsQuery(fundo.Id, assoc.Id));

        result.ShouldNotBeNull();
        result.ShouldContain("INATIVO");
        result.ShouldContain("HISTORICO");
    }

    [Fact]
    public async Task HandleAsync_fundoTipoAtivoCrossTenant_returnsNull()
    {
        var fundoRepo = Substitute.For<IFundoRepository>();
        var repo = Substitute.For<IFundoTipoAtivoAggregateRepository>();
        var company = Substitute.For<ICurrentCompanyService>();
        company.CompanyId.Returns(CompanyId);

        var fundo = MakeFundo(OtherCompanyId, FundoStatus.RASCUNHO);
        fundoRepo.GetByIdAsync(fundo.Id, Arg.Any<CancellationToken>()).Returns(fundo);

        var sut = new GetFundoTipoAtivoAllowedTransitionsQueryHandler(repo, fundoRepo, company);
        var result = await sut.HandleAsync(
            new GetFundoTipoAtivoAllowedTransitionsQuery(fundo.Id, Guid.NewGuid()));

        result.ShouldBeNull();
    }

    // =========================================================================
    // GetCedenteTipoAtivoAllowedTransitionsQueryHandler
    // =========================================================================

    [Fact]
    public async Task HandleAsync_cedenteTipoAtivoFound_returnsAllowedNextStates()
    {
        var cedenteRepo = Substitute.For<ICedenteRepository>();
        var repo = Substitute.For<ICedenteTipoAtivoAggregateRepository>();
        var company = Substitute.For<ICurrentCompanyService>();
        company.CompanyId.Returns(CompanyId);

        var cedente = MakeCedente(CompanyId);
        var assoc = CedenteTipoAtivoAggregate.Create(cedente.Id, Guid.NewGuid(), Limite, Janela);
        cedenteRepo.GetByIdAsync(cedente.Id, Arg.Any<CancellationToken>()).Returns(cedente);
        repo.GetByIdAsync(assoc.Id, Arg.Any<CancellationToken>()).Returns(assoc);

        var sut = new GetCedenteTipoAtivoAllowedTransitionsQueryHandler(repo, cedenteRepo, company);
        var result = await sut.HandleAsync(
            new GetCedenteTipoAtivoAllowedTransitionsQuery(cedente.Id, assoc.Id));

        result.ShouldNotBeNull();
        result.ShouldContain("INATIVO");
        result.ShouldContain("HISTORICO");
    }

    [Fact]
    public async Task HandleAsync_cedenteTipoAtivoCrossTenant_returnsNull()
    {
        var cedenteRepo = Substitute.For<ICedenteRepository>();
        var repo = Substitute.For<ICedenteTipoAtivoAggregateRepository>();
        var company = Substitute.For<ICurrentCompanyService>();
        company.CompanyId.Returns(CompanyId);

        var cedente = MakeCedente(OtherCompanyId);
        cedenteRepo.GetByIdAsync(cedente.Id, Arg.Any<CancellationToken>()).Returns(cedente);

        var sut = new GetCedenteTipoAtivoAllowedTransitionsQueryHandler(repo, cedenteRepo, company);
        var result = await sut.HandleAsync(
            new GetCedenteTipoAtivoAllowedTransitionsQuery(cedente.Id, Guid.NewGuid()));

        result.ShouldBeNull();
    }

    [Fact]
    public async Task HandleAsync_cedenteTipoAtivoNotFound_returnsNull()
    {
        var cedenteRepo = Substitute.For<ICedenteRepository>();
        var repo = Substitute.For<ICedenteTipoAtivoAggregateRepository>();
        var company = Substitute.For<ICurrentCompanyService>();
        company.CompanyId.Returns(CompanyId);
        cedenteRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Domain.Aggregates.CedenteAggregate.Cedente?)null);

        var sut = new GetCedenteTipoAtivoAllowedTransitionsQueryHandler(repo, cedenteRepo, company);
        var result = await sut.HandleAsync(
            new GetCedenteTipoAtivoAllowedTransitionsQuery(Guid.NewGuid(), Guid.NewGuid()));

        result.ShouldBeNull();
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    private static Domain.Aggregates.FundoAggregate.Fundo MakeFundo(
        Guid clienteId, FundoStatus status)
    {
        var fundo = Domain.Aggregates.FundoAggregate.Fundo.Register(
            "Fundo Teste", "11222333000181", clienteId,
            Guid.NewGuid(), Guid.NewGuid(), TipoFundo.Multimercado);
        if (status != FundoStatus.RASCUNHO)
            fundo.TransitionTo(FundoStatus.ATIVO);
        return fundo;
    }

    private static Domain.Aggregates.CedenteAggregate.Cedente MakeCedente(Guid clienteId) =>
        Domain.Aggregates.CedenteAggregate.Cedente.RegisterPf(
            "12345678909", "João Teste", clienteId);
}
