using System.Reflection;
using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Onboarding.API.Controllers;
using Onboarding.API.Security;
using Onboarding.Application.Common;
using Onboarding.Application.Fundos.Commands;
using Onboarding.Application.Fundos.DTOs;
using Onboarding.Application.Fundos.Queries;
using Onboarding.Application.Fundos.Queries.GetCedenteTipoAtivoAllowedTransitions;
using Onboarding.Application.Fundos.Queries.GetFundoCedenteAllowedTransitions;
using Onboarding.Application.Fundos.Queries.GetFundoAllowedTransitions;
using Onboarding.Application.Fundos.Queries.GetFundoTipoAtivoAllowedTransitions;
using Onboarding.Domain.Aggregates.CedenteAggregate;
using Onboarding.Domain.Aggregates.ConsultoriaFundoAggregate;
using Onboarding.Domain.Aggregates.CustodianteAggregate;
using Onboarding.Domain.Aggregates.FundoCedenteAggregate;
using Onboarding.Domain.Aggregates.FundoAggregate;
using Onboarding.Domain.Aggregates.TipoAtivoAggregate;
using Onboarding.Domain.Repositories;
using Shouldly;

namespace Onboarding.API.Tests.Controllers;

/// <summary>
/// Tests for the 4 GET /allowed-transitions endpoints added in Phase 51 T-1 (D-25).
/// Covers: 200 OK with correct payload, 404 on not-found/cross-tenant.
/// Authorization: policy attribute presence verified by reflection.
/// </summary>
public class FundosControllerAllowedTransitionsTests
{
    private static readonly Guid FundoId = Guid.NewGuid();
    private static readonly Guid AssocId = Guid.NewGuid();
    private static readonly Guid CedenteId = Guid.NewGuid();

    private static readonly IReadOnlyList<string> AllowedStates = new[] { "ATIVO", "SUSPENSO" };

    // =========================================================================
    // GET /api/fundos/{id}/allowed-transitions
    // =========================================================================

    [Fact]
    public async Task GetFundoAllowedTransitions_found_returns200WithList()
    {
        var handler = Substitute.For<IQueryHandler<GetFundoAllowedTransitionsQuery, IReadOnlyList<string>?>>();
        handler.HandleAsync(Arg.Any<GetFundoAllowedTransitionsQuery>(), Arg.Any<CancellationToken>())
            .Returns(AllowedStates);

        var sut = BuildFundosController(allowedTransitionsHandler: handler);
        var result = await sut.GetFundoAllowedTransitions(FundoId, CancellationToken.None);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.Value.ShouldBe(AllowedStates);
    }

    [Fact]
    public async Task GetFundoAllowedTransitions_notFound_returns404()
    {
        var handler = Substitute.For<IQueryHandler<GetFundoAllowedTransitionsQuery, IReadOnlyList<string>?>>();
        handler.HandleAsync(Arg.Any<GetFundoAllowedTransitionsQuery>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<string>?)null);

        var sut = BuildFundosController(allowedTransitionsHandler: handler);
        var result = await sut.GetFundoAllowedTransitions(FundoId, CancellationToken.None);

        result.ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public void GetFundoAllowedTransitions_endpoint_hasFundReadPolicy()
    {
        var method = typeof(FundosController).GetMethod(nameof(FundosController.GetFundoAllowedTransitions));
        var attr = method!.GetCustomAttribute<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>();
        attr.ShouldNotBeNull();
        attr.Policy.ShouldBe(PermissionPolicies.FundRead);
    }

    // =========================================================================
    // GET /api/fundos/{fundoId}/cedentes/{id}/allowed-transitions
    // =========================================================================

    [Fact]
    public async Task GetFundoCedenteAllowedTransitions_found_returns200WithList()
    {
        var handler = Substitute.For<IQueryHandler<GetFundoCedenteAllowedTransitionsQuery, IReadOnlyList<string>?>>();
        handler.HandleAsync(Arg.Any<GetFundoCedenteAllowedTransitionsQuery>(), Arg.Any<CancellationToken>())
            .Returns(AllowedStates);

        var sut = BuildFundoCedentesController(handler);
        var result = await sut.GetFundoCedenteAllowedTransitions(FundoId, AssocId, CancellationToken.None);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.Value.ShouldBe(AllowedStates);
    }

    [Fact]
    public async Task GetFundoCedenteAllowedTransitions_notFound_returns404()
    {
        var handler = Substitute.For<IQueryHandler<GetFundoCedenteAllowedTransitionsQuery, IReadOnlyList<string>?>>();
        handler.HandleAsync(Arg.Any<GetFundoCedenteAllowedTransitionsQuery>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<string>?)null);

        var sut = BuildFundoCedentesController(handler);
        var result = await sut.GetFundoCedenteAllowedTransitions(FundoId, AssocId, CancellationToken.None);

        result.ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public void GetFundoCedenteAllowedTransitions_endpoint_hasFundReadPolicy()
    {
        var method = typeof(FundoCedentesController)
            .GetMethod(nameof(FundoCedentesController.GetFundoCedenteAllowedTransitions));
        var attr = method!.GetCustomAttribute<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>();
        attr.ShouldNotBeNull();
        attr.Policy.ShouldBe(PermissionPolicies.FundRead);
    }

    // =========================================================================
    // GET /api/fundos/{fundoId}/tipos-ativos/{id}/allowed-transitions
    // =========================================================================

    [Fact]
    public async Task GetFundoTipoAtivoAllowedTransitions_found_returns200WithList()
    {
        var handler = Substitute.For<IQueryHandler<GetFundoTipoAtivoAllowedTransitionsQuery, IReadOnlyList<string>?>>();
        handler.HandleAsync(Arg.Any<GetFundoTipoAtivoAllowedTransitionsQuery>(), Arg.Any<CancellationToken>())
            .Returns(AllowedStates);

        var sut = BuildFundoTiposAtivosController(handler);
        var result = await sut.GetFundoTipoAtivoAllowedTransitions(FundoId, AssocId, CancellationToken.None);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.Value.ShouldBe(AllowedStates);
    }

    [Fact]
    public async Task GetFundoTipoAtivoAllowedTransitions_notFound_returns404()
    {
        var handler = Substitute.For<IQueryHandler<GetFundoTipoAtivoAllowedTransitionsQuery, IReadOnlyList<string>?>>();
        handler.HandleAsync(Arg.Any<GetFundoTipoAtivoAllowedTransitionsQuery>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<string>?)null);

        var sut = BuildFundoTiposAtivosController(handler);
        var result = await sut.GetFundoTipoAtivoAllowedTransitions(FundoId, AssocId, CancellationToken.None);

        result.ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public void GetFundoTipoAtivoAllowedTransitions_endpoint_hasFundReadPolicy()
    {
        var method = typeof(FundoTiposAtivosController)
            .GetMethod(nameof(FundoTiposAtivosController.GetFundoTipoAtivoAllowedTransitions));
        var attr = method!.GetCustomAttribute<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>();
        attr.ShouldNotBeNull();
        attr.Policy.ShouldBe(PermissionPolicies.FundRead);
    }

    // =========================================================================
    // GET /api/cedentes/{cedenteId}/tipos-ativos/{id}/allowed-transitions
    // =========================================================================

    [Fact]
    public async Task GetCedenteTipoAtivoAllowedTransitions_found_returns200WithList()
    {
        var handler = Substitute.For<IQueryHandler<GetCedenteTipoAtivoAllowedTransitionsQuery, IReadOnlyList<string>?>>();
        handler.HandleAsync(Arg.Any<GetCedenteTipoAtivoAllowedTransitionsQuery>(), Arg.Any<CancellationToken>())
            .Returns(AllowedStates);

        var sut = BuildCedenteTiposAtivosController(handler);
        var result = await sut.GetCedenteTipoAtivoAllowedTransitions(CedenteId, AssocId, CancellationToken.None);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.Value.ShouldBe(AllowedStates);
    }

    [Fact]
    public async Task GetCedenteTipoAtivoAllowedTransitions_notFound_returns404()
    {
        var handler = Substitute.For<IQueryHandler<GetCedenteTipoAtivoAllowedTransitionsQuery, IReadOnlyList<string>?>>();
        handler.HandleAsync(Arg.Any<GetCedenteTipoAtivoAllowedTransitionsQuery>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<string>?)null);

        var sut = BuildCedenteTiposAtivosController(handler);
        var result = await sut.GetCedenteTipoAtivoAllowedTransitions(CedenteId, AssocId, CancellationToken.None);

        result.ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public void GetCedenteTipoAtivoAllowedTransitions_endpoint_hasFundReadPolicy()
    {
        var method = typeof(CedenteTiposAtivosController)
            .GetMethod(nameof(CedenteTiposAtivosController.GetCedenteTipoAtivoAllowedTransitions));
        var attr = method!.GetCustomAttribute<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>();
        attr.ShouldNotBeNull();
        attr.Policy.ShouldBe(PermissionPolicies.FundRead);
    }

    // =========================================================================
    // Helpers — build controllers with NSubstitute stubs for unrelated deps
    // =========================================================================

    private static FundosController BuildFundosController(
        IQueryHandler<GetFundoAllowedTransitionsQuery, IReadOnlyList<string>?>? allowedTransitionsHandler = null)
    {
        var company = Substitute.For<ICurrentCompanyService>();
        company.CompanyId.Returns(Guid.NewGuid());
        var ctrl = new FundosController(
            Substitute.For<ICommandHandler<RegisterConsultoriaFundoCommand, ConsultoriaFundoDto>>(),
            Substitute.For<IValidator<RegisterConsultoriaFundoCommand>>(),
            Substitute.For<ICommandHandler<UpdateConsultoriaFundoCommand, ConsultoriaFundoDto>>(),
            Substitute.For<IValidator<UpdateConsultoriaFundoCommand>>(),
            Substitute.For<IQueryHandler<ListConsultoriaFundoQuery, PaginatedResult<ConsultoriaFundoDto>>>(),
            Substitute.For<IConsultoriaFundoRepository>(),
            Substitute.For<ICommandHandler<RegisterCustodianteCommand, CustodianteDto>>(),
            Substitute.For<IValidator<RegisterCustodianteCommand>>(),
            Substitute.For<ICommandHandler<UpdateCustodianteCommand, CustodianteDto>>(),
            Substitute.For<IValidator<UpdateCustodianteCommand>>(),
            Substitute.For<IQueryHandler<ListCustodianteQuery, PaginatedResult<CustodianteDto>>>(),
            Substitute.For<ICustodianteRepository>(),
            Substitute.For<ICommandHandler<CreateTipoAtivoCommand, TipoAtivoDto>>(),
            Substitute.For<IValidator<CreateTipoAtivoCommand>>(),
            Substitute.For<ICommandHandler<UpdateTipoAtivoCommand, TipoAtivoDto>>(),
            Substitute.For<IValidator<UpdateTipoAtivoCommand>>(),
            Substitute.For<IQueryHandler<ListTipoAtivoQuery, PaginatedResult<TipoAtivoDto>>>(),
            Substitute.For<ITipoAtivoRepository>(),
            Substitute.For<ICommandHandler<RegisterFundoCommand, FundoDto>>(),
            Substitute.For<IValidator<RegisterFundoCommand>>(),
            Substitute.For<ICommandHandler<UpdateFundoCommand, FundoDto>>(),
            Substitute.For<IValidator<UpdateFundoCommand>>(),
            Substitute.For<ICommandHandler<TransitionFundoStatusCommand, FundoDto>>(),
            Substitute.For<IValidator<TransitionFundoStatusCommand>>(),
            Substitute.For<IQueryHandler<ListFundoQuery, PaginatedResult<FundoDto>>>(),
            Substitute.For<IFundoRepository>(),
            Substitute.For<ICommandHandler<RegisterCedentePfCommand, CedenteDto>>(),
            Substitute.For<IValidator<RegisterCedentePfCommand>>(),
            Substitute.For<ICommandHandler<RegisterCedentePjCommand, CedenteDto>>(),
            Substitute.For<IValidator<RegisterCedentePjCommand>>(),
            Substitute.For<ICommandHandler<UpdateCedenteCommand, CedenteDto>>(),
            Substitute.For<IValidator<UpdateCedenteCommand>>(),
            Substitute.For<IQueryHandler<ListCedenteQuery, PaginatedResult<CedenteDto>>>(),
            Substitute.For<ICedenteRepository>(),
            allowedTransitionsHandler ??
                Substitute.For<IQueryHandler<GetFundoAllowedTransitionsQuery, IReadOnlyList<string>?>>(),
            company,
            Substitute.For<ILogger<FundosController>>());

        ctrl.ControllerContext = MakeControllerContext();
        return ctrl;
    }

    private static FundoCedentesController BuildFundoCedentesController(
        IQueryHandler<GetFundoCedenteAllowedTransitionsQuery, IReadOnlyList<string>?>? handler = null)
    {
        var company = Substitute.For<ICurrentCompanyService>();
        company.CompanyId.Returns(Guid.NewGuid());
        var ctrl = new FundoCedentesController(
            Substitute.For<ICommandHandler<Application.Fundos.Commands.CreateFundoCedente.CreateFundoCedenteCommand, RelFundoCedenteDto>>(),
            Substitute.For<ICommandHandler<Application.Fundos.Commands.UpdateFundoCedenteLimite.UpdateFundoCedenteLimiteCommand, RelFundoCedenteDto>>(),
            Substitute.For<ICommandHandler<Application.Fundos.Commands.TransitionFundoCedenteStatus.TransitionFundoCedenteStatusCommand, RelFundoCedenteDto>>(),
            Substitute.For<IQueryHandler<Application.Fundos.Queries.GetFundoCedentes.GetFundoCedentesQuery, PaginatedResult<RelFundoCedenteDto>>>(),
            handler ?? Substitute.For<IQueryHandler<GetFundoCedenteAllowedTransitionsQuery, IReadOnlyList<string>?>>(),
            Substitute.For<IFundoCedenteAggregateRepository>(),
            Substitute.For<IFundoRepository>(),
            company);
        ctrl.ControllerContext = MakeControllerContext();
        return ctrl;
    }

    private static FundoTiposAtivosController BuildFundoTiposAtivosController(
        IQueryHandler<GetFundoTipoAtivoAllowedTransitionsQuery, IReadOnlyList<string>?>? handler = null)
    {
        var company = Substitute.For<ICurrentCompanyService>();
        company.CompanyId.Returns(Guid.NewGuid());
        var ctrl = new FundoTiposAtivosController(
            Substitute.For<ICommandHandler<Application.Fundos.Commands.CreateFundoTipoAtivo.CreateFundoTipoAtivoCommand, RelFundoTipoAtivoDto>>(),
            Substitute.For<ICommandHandler<Application.Fundos.Commands.UpdateFundoTipoAtivoLimite.UpdateFundoTipoAtivoLimiteCommand, RelFundoTipoAtivoDto>>(),
            Substitute.For<ICommandHandler<Application.Fundos.Commands.TransitionFundoTipoAtivoStatus.TransitionFundoTipoAtivoStatusCommand, RelFundoTipoAtivoDto>>(),
            Substitute.For<IQueryHandler<Application.Fundos.Queries.GetFundoTiposAtivos.GetFundoTiposAtivosQuery, PaginatedResult<RelFundoTipoAtivoDto>>>(),
            handler ?? Substitute.For<IQueryHandler<GetFundoTipoAtivoAllowedTransitionsQuery, IReadOnlyList<string>?>>(),
            Substitute.For<IFundoTipoAtivoAggregateRepository>(),
            Substitute.For<IFundoRepository>(),
            company);
        ctrl.ControllerContext = MakeControllerContext();
        return ctrl;
    }

    private static CedenteTiposAtivosController BuildCedenteTiposAtivosController(
        IQueryHandler<GetCedenteTipoAtivoAllowedTransitionsQuery, IReadOnlyList<string>?>? handler = null)
    {
        var company = Substitute.For<ICurrentCompanyService>();
        company.CompanyId.Returns(Guid.NewGuid());
        var ctrl = new CedenteTiposAtivosController(
            Substitute.For<ICommandHandler<Application.Fundos.Commands.CreateCedenteTipoAtivo.CreateCedenteTipoAtivoCommand, RelCedenteTipoAtivoDto>>(),
            Substitute.For<ICommandHandler<Application.Fundos.Commands.UpdateCedenteTipoAtivoLimite.UpdateCedenteTipoAtivoLimiteCommand, RelCedenteTipoAtivoDto>>(),
            Substitute.For<ICommandHandler<Application.Fundos.Commands.TransitionCedenteTipoAtivoStatus.TransitionCedenteTipoAtivoStatusCommand, RelCedenteTipoAtivoDto>>(),
            Substitute.For<IQueryHandler<Application.Fundos.Queries.GetCedenteTiposAtivos.GetCedenteTiposAtivosQuery, PaginatedResult<RelCedenteTipoAtivoDto>>>(),
            handler ?? Substitute.For<IQueryHandler<GetCedenteTipoAtivoAllowedTransitionsQuery, IReadOnlyList<string>?>>(),
            Substitute.For<ICedenteTipoAtivoAggregateRepository>(),
            Substitute.For<ICedenteRepository>(),
            company);
        ctrl.ControllerContext = MakeControllerContext();
        return ctrl;
    }

    private static ControllerContext MakeControllerContext() => new()
    {
        HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("sub", "test-sub"),
                new Claim("email", "actor@test.com")
            }, "TestAuth"))
        }
    };
}
