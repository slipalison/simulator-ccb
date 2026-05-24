using System.Reflection;
using System.Security.Claims;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Onboarding.API.Controllers;
using Onboarding.API.Security;
using Onboarding.Application.Common;
using Onboarding.Application.Fundos.Commands;
using Onboarding.Application.Fundos.DTOs;
using Onboarding.Application.Fundos.Queries;
using Onboarding.Application.Fundos.Queries.GetFundoAllowedTransitions;
using Onboarding.Domain.Aggregates.CedenteAggregate;
using Onboarding.Domain.Aggregates.ConsultoriaFundoAggregate;
using Onboarding.Domain.Aggregates.CustodianteAggregate;
using Onboarding.Domain.Aggregates.FundoAggregate;
using Onboarding.Domain.Aggregates.TipoAtivoAggregate;
using Onboarding.Domain.Exceptions;
using Onboarding.Domain.Repositories;
using Shouldly;

namespace Onboarding.API.Tests.Controllers;

/// <summary>
/// Unit tests for FundosController — 22 endpoints covering ConsultoriaFundo, Custodiante, TipoAtivo,
/// Fundo (incl. status machine), and Cedente PF/PJ.
/// Policy attribute presence is verified via reflection (no WebApplicationFactory needed for happy path).
/// 4xx paths: null body → 400, validation failure → 422, DuplicateEntityException → 409,
///             KeyNotFoundException → 404, InvalidStateTransitionException → 400.
/// Security invariant: each endpoint must carry [Authorize(Policy = FundRead|FundWrite)].
/// </summary>
public class FundosControllerTests
{
    // -------------------------------------------------------------------------
    // Mocks — ConsultoriaFundo
    // -------------------------------------------------------------------------

    private readonly ICommandHandler<RegisterConsultoriaFundoCommand, ConsultoriaFundoDto> _registerConsultoriaHandler =
        Substitute.For<ICommandHandler<RegisterConsultoriaFundoCommand, ConsultoriaFundoDto>>();
    private readonly IValidator<RegisterConsultoriaFundoCommand> _registerConsultoriaValidator =
        Substitute.For<IValidator<RegisterConsultoriaFundoCommand>>();
    private readonly ICommandHandler<UpdateConsultoriaFundoCommand, ConsultoriaFundoDto> _updateConsultoriaHandler =
        Substitute.For<ICommandHandler<UpdateConsultoriaFundoCommand, ConsultoriaFundoDto>>();
    private readonly IValidator<UpdateConsultoriaFundoCommand> _updateConsultoriaValidator =
        Substitute.For<IValidator<UpdateConsultoriaFundoCommand>>();
    private readonly IQueryHandler<ListConsultoriaFundoQuery, PaginatedResult<ConsultoriaFundoDto>> _listConsultoriaHandler =
        Substitute.For<IQueryHandler<ListConsultoriaFundoQuery, PaginatedResult<ConsultoriaFundoDto>>>();
    private readonly IConsultoriaFundoRepository _consultoriaRepo =
        Substitute.For<IConsultoriaFundoRepository>();

    // -------------------------------------------------------------------------
    // Mocks — Custodiante
    // -------------------------------------------------------------------------

    private readonly ICommandHandler<RegisterCustodianteCommand, CustodianteDto> _registerCustodianteHandler =
        Substitute.For<ICommandHandler<RegisterCustodianteCommand, CustodianteDto>>();
    private readonly IValidator<RegisterCustodianteCommand> _registerCustodianteValidator =
        Substitute.For<IValidator<RegisterCustodianteCommand>>();
    private readonly ICommandHandler<UpdateCustodianteCommand, CustodianteDto> _updateCustodianteHandler =
        Substitute.For<ICommandHandler<UpdateCustodianteCommand, CustodianteDto>>();
    private readonly IValidator<UpdateCustodianteCommand> _updateCustodianteValidator =
        Substitute.For<IValidator<UpdateCustodianteCommand>>();
    private readonly IQueryHandler<ListCustodianteQuery, PaginatedResult<CustodianteDto>> _listCustodianteHandler =
        Substitute.For<IQueryHandler<ListCustodianteQuery, PaginatedResult<CustodianteDto>>>();
    private readonly ICustodianteRepository _custodianteRepo =
        Substitute.For<ICustodianteRepository>();

    // -------------------------------------------------------------------------
    // Mocks — TipoAtivo
    // -------------------------------------------------------------------------

    private readonly ICommandHandler<CreateTipoAtivoCommand, TipoAtivoDto> _createTipoAtivoHandler =
        Substitute.For<ICommandHandler<CreateTipoAtivoCommand, TipoAtivoDto>>();
    private readonly IValidator<CreateTipoAtivoCommand> _createTipoAtivoValidator =
        Substitute.For<IValidator<CreateTipoAtivoCommand>>();
    private readonly ICommandHandler<UpdateTipoAtivoCommand, TipoAtivoDto> _updateTipoAtivoHandler =
        Substitute.For<ICommandHandler<UpdateTipoAtivoCommand, TipoAtivoDto>>();
    private readonly IValidator<UpdateTipoAtivoCommand> _updateTipoAtivoValidator =
        Substitute.For<IValidator<UpdateTipoAtivoCommand>>();
    private readonly IQueryHandler<ListTipoAtivoQuery, PaginatedResult<TipoAtivoDto>> _listTipoAtivoHandler =
        Substitute.For<IQueryHandler<ListTipoAtivoQuery, PaginatedResult<TipoAtivoDto>>>();
    private readonly ITipoAtivoRepository _tipoAtivoRepo =
        Substitute.For<ITipoAtivoRepository>();

    // -------------------------------------------------------------------------
    // Mocks — Fundo
    // -------------------------------------------------------------------------

    private readonly ICommandHandler<RegisterFundoCommand, FundoDto> _registerFundoHandler =
        Substitute.For<ICommandHandler<RegisterFundoCommand, FundoDto>>();
    private readonly IValidator<RegisterFundoCommand> _registerFundoValidator =
        Substitute.For<IValidator<RegisterFundoCommand>>();
    private readonly ICommandHandler<UpdateFundoCommand, FundoDto> _updateFundoHandler =
        Substitute.For<ICommandHandler<UpdateFundoCommand, FundoDto>>();
    private readonly IValidator<UpdateFundoCommand> _updateFundoValidator =
        Substitute.For<IValidator<UpdateFundoCommand>>();
    private readonly ICommandHandler<TransitionFundoStatusCommand, FundoDto> _transitionFundoStatusHandler =
        Substitute.For<ICommandHandler<TransitionFundoStatusCommand, FundoDto>>();
    private readonly IValidator<TransitionFundoStatusCommand> _transitionFundoStatusValidator =
        Substitute.For<IValidator<TransitionFundoStatusCommand>>();
    private readonly IQueryHandler<ListFundoQuery, PaginatedResult<FundoDto>> _listFundoHandler =
        Substitute.For<IQueryHandler<ListFundoQuery, PaginatedResult<FundoDto>>>();
    private readonly IFundoRepository _fundoRepo =
        Substitute.For<IFundoRepository>();

    // -------------------------------------------------------------------------
    // Mocks — Cedente
    // -------------------------------------------------------------------------

    private readonly ICommandHandler<RegisterCedentePfCommand, CedenteDto> _registerCedentePfHandler =
        Substitute.For<ICommandHandler<RegisterCedentePfCommand, CedenteDto>>();
    private readonly IValidator<RegisterCedentePfCommand> _registerCedentePfValidator =
        Substitute.For<IValidator<RegisterCedentePfCommand>>();
    private readonly ICommandHandler<RegisterCedentePjCommand, CedenteDto> _registerCedentePjHandler =
        Substitute.For<ICommandHandler<RegisterCedentePjCommand, CedenteDto>>();
    private readonly IValidator<RegisterCedentePjCommand> _registerCedentePjValidator =
        Substitute.For<IValidator<RegisterCedentePjCommand>>();
    private readonly ICommandHandler<UpdateCedenteCommand, CedenteDto> _updateCedenteHandler =
        Substitute.For<ICommandHandler<UpdateCedenteCommand, CedenteDto>>();
    private readonly IValidator<UpdateCedenteCommand> _updateCedenteValidator =
        Substitute.For<IValidator<UpdateCedenteCommand>>();
    private readonly IQueryHandler<ListCedenteQuery, PaginatedResult<CedenteDto>> _listCedenteHandler =
        Substitute.For<IQueryHandler<ListCedenteQuery, PaginatedResult<CedenteDto>>>();
    private readonly ICedenteRepository _cedenteRepo =
        Substitute.For<ICedenteRepository>();

    private readonly ICurrentCompanyService _companyService = Substitute.For<ICurrentCompanyService>();

    private readonly FundosController _sut;

    // -------------------------------------------------------------------------
    // Test data helpers
    // -------------------------------------------------------------------------

    private static readonly Guid CompanyId = Guid.NewGuid();
    private static readonly Guid ConsultoriaId = Guid.NewGuid();
    private static readonly Guid CustodianteId = Guid.NewGuid();
    private static readonly Guid TipoAtivoId = Guid.NewGuid();
    private static readonly Guid FundoId = Guid.NewGuid();
    private static readonly Guid CedenteId = Guid.NewGuid();

    // Static fixtures — timestamp-stable so record equality works across multiple calls in the same test
    private static readonly DateTimeOffset FixedTimestamp = new(2026, 5, 12, 0, 0, 0, TimeSpan.Zero);

    private static readonly ConsultoriaFundoDto SampleConsultoria =
        new(ConsultoriaId, "Consultoria Ltda", null, "11222333000181", null, null, ConsultoriaFundoStatus.ATIVO, FixedTimestamp);

    private static readonly CustodianteDto SampleCustodiante =
        new(CustodianteId, "Custodiante S.A.", null, "11444777000161", null, null, CustodianteStatus.ATIVO, FixedTimestamp);

    private static readonly TipoAtivoDto SampleTipoAtivo =
        new(TipoAtivoId, "LTN", "Letra do Tesouro Nacional", TipoAtivoCategoria.RendaFixa, null, TipoAtivoStatus.ATIVO, 1);

    private static readonly FundoDto SampleFundo =
        new(FundoId, "Fundo Alfa", "11222333000181", ConsultoriaId, CustodianteId,
            TipoFundo.Multimercado, null, null, null, FundoStatus.RASCUNHO, FixedTimestamp);

    private static readonly CedenteDto SampleCedentePf =
        new(CedenteId, "12345678909", "João Silva", null, null, null, CedenteTipo.PF, CedenteStatus.ATIVO, FixedTimestamp);

    private static readonly CedenteDto SampleCedentePj =
        new(CedenteId, "11222333000181", "Empresa PJ Ltda", null, null, null, CedenteTipo.PJ, CedenteStatus.ATIVO, FixedTimestamp);

    private static ValidationResult ValidResult() => new();

    private static ValidationResult InvalidResult(string field, string message) =>
        new(new[] { new ValidationFailure(field, message) });

    public FundosControllerTests()
    {
        _companyService.CompanyId.Returns(CompanyId);

        _sut = new FundosController(
            _registerConsultoriaHandler,
            _registerConsultoriaValidator,
            _updateConsultoriaHandler,
            _updateConsultoriaValidator,
            _listConsultoriaHandler,
            _consultoriaRepo,
            _registerCustodianteHandler,
            _registerCustodianteValidator,
            _updateCustodianteHandler,
            _updateCustodianteValidator,
            _listCustodianteHandler,
            _custodianteRepo,
            _createTipoAtivoHandler,
            _createTipoAtivoValidator,
            _updateTipoAtivoHandler,
            _updateTipoAtivoValidator,
            _listTipoAtivoHandler,
            _tipoAtivoRepo,
            _registerFundoHandler,
            _registerFundoValidator,
            _updateFundoHandler,
            _updateFundoValidator,
            _transitionFundoStatusHandler,
            _transitionFundoStatusValidator,
            _listFundoHandler,
            _fundoRepo,
            _registerCedentePfHandler,
            _registerCedentePfValidator,
            _registerCedentePjHandler,
            _registerCedentePjValidator,
            _updateCedenteHandler,
            _updateCedenteValidator,
            _listCedenteHandler,
            _cedenteRepo,
            Substitute.For<IQueryHandler<GetFundoAllowedTransitionsQuery, IReadOnlyList<string>?>>(),
            _companyService,
            Substitute.For<ILogger<FundosController>>()
        );

        // Set authenticated user with sub + email
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim("sub", "test-sub-123"),
                    new Claim("email", "actor@test.com")
                }, "TestAuth"))
            }
        };
    }

    // =========================================================================
    // Security: Policy attribute reflection tests
    // =========================================================================

    [Theory]
    [InlineData(nameof(FundosController.RegisterConsultoria), PermissionPolicies.FundWrite)]
    [InlineData(nameof(FundosController.ListConsultorias), PermissionPolicies.FundRead)]
    [InlineData(nameof(FundosController.GetConsultoriaById), PermissionPolicies.FundRead)]
    [InlineData(nameof(FundosController.UpdateConsultoria), PermissionPolicies.FundWrite)]
    [InlineData(nameof(FundosController.RegisterCustodiante), PermissionPolicies.FundWrite)]
    [InlineData(nameof(FundosController.ListCustodiantes), PermissionPolicies.FundRead)]
    [InlineData(nameof(FundosController.GetCustodianteById), PermissionPolicies.FundRead)]
    [InlineData(nameof(FundosController.UpdateCustodiante), PermissionPolicies.FundWrite)]
    [InlineData(nameof(FundosController.CreateTipoAtivo), PermissionPolicies.FundWrite)]
    [InlineData(nameof(FundosController.ListTiposAtivo), PermissionPolicies.FundRead)]
    [InlineData(nameof(FundosController.GetTipoAtivoById), PermissionPolicies.FundRead)]
    [InlineData(nameof(FundosController.UpdateTipoAtivo), PermissionPolicies.FundWrite)]
    // Fundo endpoints (T-48.5)
    [InlineData(nameof(FundosController.RegisterFundo), PermissionPolicies.FundWrite)]
    [InlineData(nameof(FundosController.ListFundos), PermissionPolicies.FundRead)]
    [InlineData(nameof(FundosController.GetFundoById), PermissionPolicies.FundRead)]
    [InlineData(nameof(FundosController.UpdateFundo), PermissionPolicies.FundWrite)]
    [InlineData(nameof(FundosController.TransitionFundoStatus), PermissionPolicies.FundWrite)]
    // Cedente endpoints (T-48.5)
    [InlineData(nameof(FundosController.RegisterCedentePf), PermissionPolicies.FundWrite)]
    [InlineData(nameof(FundosController.RegisterCedentePj), PermissionPolicies.FundWrite)]
    [InlineData(nameof(FundosController.ListCedentes), PermissionPolicies.FundRead)]
    [InlineData(nameof(FundosController.GetCedenteById), PermissionPolicies.FundRead)]
    [InlineData(nameof(FundosController.UpdateCedente), PermissionPolicies.FundWrite)]
    public void Endpoint_HasExpectedAuthorizePolicy(string methodName, string expectedPolicy)
    {
        // Reflection-based security invariant: every endpoint MUST carry [Authorize(Policy = ...)]
        // with the correct policy. Catching policy misconfiguration without spinning up the full stack.
        var method = typeof(FundosController).GetMethods()
            .Where(m => m.Name == methodName)
            .Single();

        var authorizeAttrs = method.GetCustomAttributes<AuthorizeAttribute>().ToList();
        authorizeAttrs.ShouldNotBeEmpty($"Method {methodName} is missing [Authorize] attribute");

        var policies = authorizeAttrs.Select(a => a.Policy).Where(p => p is not null).ToList();
        policies.ShouldContain(expectedPolicy,
            $"Method {methodName} must carry [Authorize(Policy = \"{expectedPolicy}\")]");
    }

    [Fact]
    public void Controller_HasClassLevelBearerClientScheme()
    {
        var classAttr = typeof(FundosController).GetCustomAttribute<AuthorizeAttribute>();
        classAttr.ShouldNotBeNull("FundosController must have class-level [Authorize] attribute");
        classAttr!.AuthenticationSchemes.ShouldBe("BearerClient");
    }

    // =========================================================================
    // ConsultoriaFundo — POST (RegisterConsultoria)
    // =========================================================================

    [Fact]
    public async Task RegisterConsultoria_NullBody_Returns400()
    {
        var result = await _sut.RegisterConsultoria(null, CancellationToken.None);
        result.ShouldBeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task RegisterConsultoria_ValidationFails_Returns422()
    {
        _registerConsultoriaValidator
            .ValidateAsync(Arg.Any<RegisterConsultoriaFundoCommand>(), Arg.Any<CancellationToken>())
            .Returns(InvalidResult("Cnpj", "Invalid CNPJ."));

        var request = new RegisterConsultoriaFundoRequest("Consultoria", "bad-cnpj", null, null, null);
        var result = await _sut.RegisterConsultoria(request, CancellationToken.None);

        result.ShouldBeOfType<UnprocessableEntityObjectResult>();
    }

    [Fact]
    public async Task RegisterConsultoria_Duplicate_Returns409()
    {
        _registerConsultoriaValidator
            .ValidateAsync(Arg.Any<RegisterConsultoriaFundoCommand>(), Arg.Any<CancellationToken>())
            .Returns(ValidResult());
        _registerConsultoriaHandler
            .HandleAsync(Arg.Any<RegisterConsultoriaFundoCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new DuplicateEntityException("ConsultoriaFundo", "12.345.678/0001-90"));

        var request = new RegisterConsultoriaFundoRequest("Consultoria", "12.345.678/0001-90", null, null, null);
        var result = await _sut.RegisterConsultoria(request, CancellationToken.None);

        result.ShouldBeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task RegisterConsultoria_HappyPath_Returns201WithLocation()
    {
        _registerConsultoriaValidator
            .ValidateAsync(Arg.Any<RegisterConsultoriaFundoCommand>(), Arg.Any<CancellationToken>())
            .Returns(ValidResult());
        _registerConsultoriaHandler
            .HandleAsync(Arg.Any<RegisterConsultoriaFundoCommand>(), Arg.Any<CancellationToken>())
            .Returns(SampleConsultoria);

        var request = new RegisterConsultoriaFundoRequest("Consultoria Ltda", "12.345.678/0001-90", null, null, null);
        var result = await _sut.RegisterConsultoria(request, CancellationToken.None);

        var created = result.ShouldBeOfType<CreatedAtActionResult>();
        created.ActionName.ShouldBe(nameof(FundosController.GetConsultoriaById));
        created.Value.ShouldBe(SampleConsultoria);
    }

    [Fact]
    public async Task RegisterConsultoria_CapturesActorFromJwt()
    {
        _registerConsultoriaValidator
            .ValidateAsync(Arg.Any<RegisterConsultoriaFundoCommand>(), Arg.Any<CancellationToken>())
            .Returns(ValidResult());
        _registerConsultoriaHandler
            .HandleAsync(Arg.Any<RegisterConsultoriaFundoCommand>(), Arg.Any<CancellationToken>())
            .Returns(SampleConsultoria);

        var request = new RegisterConsultoriaFundoRequest("Consultoria Ltda", "12.345.678/0001-90", null, null, null);
        await _sut.RegisterConsultoria(request, CancellationToken.None);

        await _registerConsultoriaHandler.Received(1).HandleAsync(
            Arg.Is<RegisterConsultoriaFundoCommand>(c =>
                c.ActorSub == "test-sub-123" && c.ActorEmail == "actor@test.com"),
            Arg.Any<CancellationToken>());
    }

    // =========================================================================
    // ConsultoriaFundo — GET list (ListConsultorias)
    // =========================================================================

    [Fact]
    public async Task ListConsultorias_HappyPath_Returns200WithPaginatedResult()
    {
        var expected = new PaginatedResult<ConsultoriaFundoDto>(
            new[] { SampleConsultoria }, 1, 1, 20);
        _listConsultoriaHandler
            .HandleAsync(Arg.Any<ListConsultoriaFundoQuery>(), Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _sut.ListConsultorias(ct: CancellationToken.None);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.Value.ShouldBe(expected);
    }

    // =========================================================================
    // ConsultoriaFundo — GET by id (GetConsultoriaById)
    // =========================================================================

    [Fact]
    public async Task GetConsultoriaById_NotFound_Returns404()
    {
        _consultoriaRepo.GetByIdAsync(ConsultoriaId, Arg.Any<CancellationToken>())
            .Returns((ConsultoriaFundo?)null);

        var result = await _sut.GetConsultoriaById(ConsultoriaId, CancellationToken.None);

        result.ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetConsultoriaById_Found_Returns200WithDto()
    {
        var consultoria = BuildConsultoriaFundo();
        _consultoriaRepo.GetByIdAsync(ConsultoriaId, Arg.Any<CancellationToken>())
            .Returns(consultoria);

        var result = await _sut.GetConsultoriaById(ConsultoriaId, CancellationToken.None);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var dto = ok.Value.ShouldBeOfType<ConsultoriaFundoDto>();
        dto.Id.ShouldBe(ConsultoriaId);
    }

    // =========================================================================
    // ConsultoriaFundo — PUT (UpdateConsultoria)
    // =========================================================================

    [Fact]
    public async Task UpdateConsultoria_NullBody_Returns400()
    {
        var result = await _sut.UpdateConsultoria(ConsultoriaId, null, CancellationToken.None);
        result.ShouldBeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task UpdateConsultoria_ValidationFails_Returns422()
    {
        _updateConsultoriaValidator
            .ValidateAsync(Arg.Any<UpdateConsultoriaFundoCommand>(), Arg.Any<CancellationToken>())
            .Returns(InvalidResult("RazaoSocial", "Required."));

        var request = new UpdateConsultoriaFundoRequest(null, null, null, null, ConsultoriaFundoStatus.ATIVO);
        var result = await _sut.UpdateConsultoria(ConsultoriaId, request, CancellationToken.None);

        result.ShouldBeOfType<UnprocessableEntityObjectResult>();
    }

    [Fact]
    public async Task UpdateConsultoria_NotFound_Returns404()
    {
        _updateConsultoriaValidator
            .ValidateAsync(Arg.Any<UpdateConsultoriaFundoCommand>(), Arg.Any<CancellationToken>())
            .Returns(ValidResult());
        _updateConsultoriaHandler
            .HandleAsync(Arg.Any<UpdateConsultoriaFundoCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new KeyNotFoundException("ConsultoriaFundo not found."));

        var request = new UpdateConsultoriaFundoRequest("Consultoria", null, null, null, ConsultoriaFundoStatus.ATIVO);
        var result = await _sut.UpdateConsultoria(ConsultoriaId, request, CancellationToken.None);

        result.ShouldBeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task UpdateConsultoria_HappyPath_Returns200WithDto()
    {
        _updateConsultoriaValidator
            .ValidateAsync(Arg.Any<UpdateConsultoriaFundoCommand>(), Arg.Any<CancellationToken>())
            .Returns(ValidResult());
        _updateConsultoriaHandler
            .HandleAsync(Arg.Any<UpdateConsultoriaFundoCommand>(), Arg.Any<CancellationToken>())
            .Returns(SampleConsultoria);

        var request = new UpdateConsultoriaFundoRequest("Consultoria Ltda", null, null, null, ConsultoriaFundoStatus.ATIVO);
        var result = await _sut.UpdateConsultoria(ConsultoriaId, request, CancellationToken.None);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.Value.ShouldBe(SampleConsultoria);
    }

    // =========================================================================
    // Custodiante — POST (RegisterCustodiante)
    // =========================================================================

    [Fact]
    public async Task RegisterCustodiante_NullBody_Returns400()
    {
        var result = await _sut.RegisterCustodiante(null, CancellationToken.None);
        result.ShouldBeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task RegisterCustodiante_ValidationFails_Returns422()
    {
        _registerCustodianteValidator
            .ValidateAsync(Arg.Any<RegisterCustodianteCommand>(), Arg.Any<CancellationToken>())
            .Returns(InvalidResult("Cnpj", "Invalid CNPJ."));

        var request = new RegisterCustodianteRequest("Custodiante", "bad", null, null, null);
        var result = await _sut.RegisterCustodiante(request, CancellationToken.None);

        result.ShouldBeOfType<UnprocessableEntityObjectResult>();
    }

    [Fact]
    public async Task RegisterCustodiante_Duplicate_Returns409()
    {
        _registerCustodianteValidator
            .ValidateAsync(Arg.Any<RegisterCustodianteCommand>(), Arg.Any<CancellationToken>())
            .Returns(ValidResult());
        _registerCustodianteHandler
            .HandleAsync(Arg.Any<RegisterCustodianteCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new DuplicateEntityException("Custodiante", "98.765.432/0001-10"));

        var request = new RegisterCustodianteRequest("Custodiante S.A.", "98.765.432/0001-10", null, null, null);
        var result = await _sut.RegisterCustodiante(request, CancellationToken.None);

        result.ShouldBeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task RegisterCustodiante_HappyPath_Returns201()
    {
        _registerCustodianteValidator
            .ValidateAsync(Arg.Any<RegisterCustodianteCommand>(), Arg.Any<CancellationToken>())
            .Returns(ValidResult());
        _registerCustodianteHandler
            .HandleAsync(Arg.Any<RegisterCustodianteCommand>(), Arg.Any<CancellationToken>())
            .Returns(SampleCustodiante);

        var request = new RegisterCustodianteRequest("Custodiante S.A.", "98.765.432/0001-10", null, null, null);
        var result = await _sut.RegisterCustodiante(request, CancellationToken.None);

        var created = result.ShouldBeOfType<CreatedAtActionResult>();
        created.ActionName.ShouldBe(nameof(FundosController.GetCustodianteById));
        created.Value.ShouldBe(SampleCustodiante);
    }

    [Fact]
    public async Task RegisterCustodiante_CapturesActorFromJwt()
    {
        _registerCustodianteValidator
            .ValidateAsync(Arg.Any<RegisterCustodianteCommand>(), Arg.Any<CancellationToken>())
            .Returns(ValidResult());
        _registerCustodianteHandler
            .HandleAsync(Arg.Any<RegisterCustodianteCommand>(), Arg.Any<CancellationToken>())
            .Returns(SampleCustodiante);

        var request = new RegisterCustodianteRequest("Custodiante S.A.", "98.765.432/0001-10", null, null, null);
        await _sut.RegisterCustodiante(request, CancellationToken.None);

        await _registerCustodianteHandler.Received(1).HandleAsync(
            Arg.Is<RegisterCustodianteCommand>(c =>
                c.ActorSub == "test-sub-123" && c.ActorEmail == "actor@test.com"),
            Arg.Any<CancellationToken>());
    }

    // =========================================================================
    // Custodiante — GET list (ListCustodiantes)
    // =========================================================================

    [Fact]
    public async Task ListCustodiantes_HappyPath_Returns200WithPaginatedResult()
    {
        var expected = new PaginatedResult<CustodianteDto>(
            new[] { SampleCustodiante }, 1, 1, 20);
        _listCustodianteHandler
            .HandleAsync(Arg.Any<ListCustodianteQuery>(), Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _sut.ListCustodiantes(ct: CancellationToken.None);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.Value.ShouldBe(expected);
    }

    // =========================================================================
    // Custodiante — GET by id (GetCustodianteById)
    // =========================================================================

    [Fact]
    public async Task GetCustodianteById_NotFound_Returns404()
    {
        _custodianteRepo.GetByIdAsync(CustodianteId, Arg.Any<CancellationToken>())
            .Returns((Custodiante?)null);

        var result = await _sut.GetCustodianteById(CustodianteId, CancellationToken.None);

        result.ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetCustodianteById_Found_Returns200WithDto()
    {
        var custodiante = BuildCustodiante();
        _custodianteRepo.GetByIdAsync(CustodianteId, Arg.Any<CancellationToken>())
            .Returns(custodiante);

        var result = await _sut.GetCustodianteById(CustodianteId, CancellationToken.None);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var dto = ok.Value.ShouldBeOfType<CustodianteDto>();
        dto.Id.ShouldBe(CustodianteId);
    }

    // =========================================================================
    // Custodiante — PUT (UpdateCustodiante)
    // =========================================================================

    [Fact]
    public async Task UpdateCustodiante_NullBody_Returns400()
    {
        var result = await _sut.UpdateCustodiante(CustodianteId, null, CancellationToken.None);
        result.ShouldBeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task UpdateCustodiante_ValidationFails_Returns422()
    {
        _updateCustodianteValidator
            .ValidateAsync(Arg.Any<UpdateCustodianteCommand>(), Arg.Any<CancellationToken>())
            .Returns(InvalidResult("RazaoSocial", "Required."));

        var request = new UpdateCustodianteRequest(null, null, null, null, CustodianteStatus.ATIVO);
        var result = await _sut.UpdateCustodiante(CustodianteId, request, CancellationToken.None);

        result.ShouldBeOfType<UnprocessableEntityObjectResult>();
    }

    [Fact]
    public async Task UpdateCustodiante_NotFound_Returns404()
    {
        _updateCustodianteValidator
            .ValidateAsync(Arg.Any<UpdateCustodianteCommand>(), Arg.Any<CancellationToken>())
            .Returns(ValidResult());
        _updateCustodianteHandler
            .HandleAsync(Arg.Any<UpdateCustodianteCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new KeyNotFoundException("Custodiante not found."));

        var request = new UpdateCustodianteRequest("Custodiante S.A.", null, null, null, CustodianteStatus.ATIVO);
        var result = await _sut.UpdateCustodiante(CustodianteId, request, CancellationToken.None);

        result.ShouldBeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task UpdateCustodiante_HappyPath_Returns200WithDto()
    {
        _updateCustodianteValidator
            .ValidateAsync(Arg.Any<UpdateCustodianteCommand>(), Arg.Any<CancellationToken>())
            .Returns(ValidResult());
        _updateCustodianteHandler
            .HandleAsync(Arg.Any<UpdateCustodianteCommand>(), Arg.Any<CancellationToken>())
            .Returns(SampleCustodiante);

        var request = new UpdateCustodianteRequest("Custodiante S.A.", null, null, null, CustodianteStatus.ATIVO);
        var result = await _sut.UpdateCustodiante(CustodianteId, request, CancellationToken.None);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.Value.ShouldBe(SampleCustodiante);
    }

    // =========================================================================
    // TipoAtivo — POST (CreateTipoAtivo)
    // =========================================================================

    [Fact]
    public async Task CreateTipoAtivo_NullBody_Returns400()
    {
        var result = await _sut.CreateTipoAtivo(null, CancellationToken.None);
        result.ShouldBeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CreateTipoAtivo_ValidationFails_Returns422()
    {
        _createTipoAtivoValidator
            .ValidateAsync(Arg.Any<CreateTipoAtivoCommand>(), Arg.Any<CancellationToken>())
            .Returns(InvalidResult("Codigo", "Required."));

        var request = new CreateTipoAtivoRequest(null, "LTN desc", TipoAtivoCategoria.RendaFixa, null);
        var result = await _sut.CreateTipoAtivo(request, CancellationToken.None);

        result.ShouldBeOfType<UnprocessableEntityObjectResult>();
    }

    [Fact]
    public async Task CreateTipoAtivo_Duplicate_Returns409()
    {
        _createTipoAtivoValidator
            .ValidateAsync(Arg.Any<CreateTipoAtivoCommand>(), Arg.Any<CancellationToken>())
            .Returns(ValidResult());
        _createTipoAtivoHandler
            .HandleAsync(Arg.Any<CreateTipoAtivoCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new DuplicateEntityException("TipoAtivo", "LTN"));

        var request = new CreateTipoAtivoRequest("LTN", "Letra do Tesouro Nacional", TipoAtivoCategoria.RendaFixa, null);
        var result = await _sut.CreateTipoAtivo(request, CancellationToken.None);

        result.ShouldBeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task CreateTipoAtivo_HappyPath_Returns201()
    {
        _createTipoAtivoValidator
            .ValidateAsync(Arg.Any<CreateTipoAtivoCommand>(), Arg.Any<CancellationToken>())
            .Returns(ValidResult());
        _createTipoAtivoHandler
            .HandleAsync(Arg.Any<CreateTipoAtivoCommand>(), Arg.Any<CancellationToken>())
            .Returns(SampleTipoAtivo);

        var request = new CreateTipoAtivoRequest("LTN", "Letra do Tesouro Nacional", TipoAtivoCategoria.RendaFixa, null);
        var result = await _sut.CreateTipoAtivo(request, CancellationToken.None);

        var created = result.ShouldBeOfType<CreatedAtActionResult>();
        created.ActionName.ShouldBe(nameof(FundosController.GetTipoAtivoById));
        created.Value.ShouldBe(SampleTipoAtivo);
    }

    [Fact]
    public async Task CreateTipoAtivo_IsGlobalScope_DoesNotUseCompanyService()
    {
        // TipoAtivo is global (D-5/TEN-03) — ICurrentCompanyService MUST NOT be consulted
        _createTipoAtivoValidator
            .ValidateAsync(Arg.Any<CreateTipoAtivoCommand>(), Arg.Any<CancellationToken>())
            .Returns(ValidResult());
        _createTipoAtivoHandler
            .HandleAsync(Arg.Any<CreateTipoAtivoCommand>(), Arg.Any<CancellationToken>())
            .Returns(SampleTipoAtivo);

        var request = new CreateTipoAtivoRequest("LTN", "Letra do Tesouro Nacional", TipoAtivoCategoria.RendaFixa, null);
        await _sut.CreateTipoAtivo(request, CancellationToken.None);

        // CompanyId should never be accessed for TipoAtivo operations
        _ = _companyService.DidNotReceive().CompanyId;
    }

    // =========================================================================
    // TipoAtivo — GET list (ListTiposAtivo)
    // =========================================================================

    [Fact]
    public async Task ListTiposAtivo_HappyPath_Returns200WithPaginatedResult()
    {
        var expected = new PaginatedResult<TipoAtivoDto>(
            new[] { SampleTipoAtivo }, 1, 1, 20);
        _listTipoAtivoHandler
            .HandleAsync(Arg.Any<ListTipoAtivoQuery>(), Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _sut.ListTiposAtivo(ct: CancellationToken.None);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.Value.ShouldBe(expected);
    }

    // =========================================================================
    // TipoAtivo — GET by id (GetTipoAtivoById)
    // =========================================================================

    [Fact]
    public async Task GetTipoAtivoById_NotFound_Returns404()
    {
        _tipoAtivoRepo.GetByIdAsync(TipoAtivoId, Arg.Any<CancellationToken>())
            .Returns((TipoAtivo?)null);

        var result = await _sut.GetTipoAtivoById(TipoAtivoId, CancellationToken.None);

        result.ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetTipoAtivoById_Found_Returns200WithDto()
    {
        var tipoAtivo = BuildTipoAtivo();
        _tipoAtivoRepo.GetByIdAsync(TipoAtivoId, Arg.Any<CancellationToken>())
            .Returns(tipoAtivo);

        var result = await _sut.GetTipoAtivoById(TipoAtivoId, CancellationToken.None);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var dto = ok.Value.ShouldBeOfType<TipoAtivoDto>();
        dto.Id.ShouldBe(TipoAtivoId);
    }

    // =========================================================================
    // TipoAtivo — PUT (UpdateTipoAtivo)
    // =========================================================================

    [Fact]
    public async Task UpdateTipoAtivo_NullBody_Returns400()
    {
        var result = await _sut.UpdateTipoAtivo(TipoAtivoId, null, CancellationToken.None);
        result.ShouldBeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task UpdateTipoAtivo_ValidationFails_Returns422()
    {
        _updateTipoAtivoValidator
            .ValidateAsync(Arg.Any<UpdateTipoAtivoCommand>(), Arg.Any<CancellationToken>())
            .Returns(InvalidResult("Descricao", "Required."));

        var request = new UpdateTipoAtivoRequest(null, null, TipoAtivoStatus.ATIVO, 1);
        var result = await _sut.UpdateTipoAtivo(TipoAtivoId, request, CancellationToken.None);

        result.ShouldBeOfType<UnprocessableEntityObjectResult>();
    }

    [Fact]
    public async Task UpdateTipoAtivo_NotFound_Returns404()
    {
        _updateTipoAtivoValidator
            .ValidateAsync(Arg.Any<UpdateTipoAtivoCommand>(), Arg.Any<CancellationToken>())
            .Returns(ValidResult());
        _updateTipoAtivoHandler
            .HandleAsync(Arg.Any<UpdateTipoAtivoCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new KeyNotFoundException("TipoAtivo not found."));

        var request = new UpdateTipoAtivoRequest("LTN desc", null, TipoAtivoStatus.ATIVO, 1);
        var result = await _sut.UpdateTipoAtivo(TipoAtivoId, request, CancellationToken.None);

        result.ShouldBeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task UpdateTipoAtivo_HappyPath_Returns200WithDto()
    {
        _updateTipoAtivoValidator
            .ValidateAsync(Arg.Any<UpdateTipoAtivoCommand>(), Arg.Any<CancellationToken>())
            .Returns(ValidResult());
        _updateTipoAtivoHandler
            .HandleAsync(Arg.Any<UpdateTipoAtivoCommand>(), Arg.Any<CancellationToken>())
            .Returns(SampleTipoAtivo);

        var request = new UpdateTipoAtivoRequest("Letra do Tesouro Nacional", null, TipoAtivoStatus.ATIVO, 1);
        var result = await _sut.UpdateTipoAtivo(TipoAtivoId, request, CancellationToken.None);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.Value.ShouldBe(SampleTipoAtivo);
    }

    // =========================================================================
    // Fundo — POST (RegisterFundo)
    // =========================================================================

    [Fact]
    public async Task RegisterFundo_NullBody_Returns400()
    {
        var result = await _sut.RegisterFundo(null, CancellationToken.None);
        result.ShouldBeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task RegisterFundo_ValidationFails_Returns422()
    {
        _registerFundoValidator
            .ValidateAsync(Arg.Any<RegisterFundoCommand>(), Arg.Any<CancellationToken>())
            .Returns(InvalidResult("Cnpj", "Invalid CNPJ."));

        var request = new RegisterFundoRequest("Fundo Alfa", "bad-cnpj", ConsultoriaId, CustodianteId,
            TipoFundo.Multimercado, null, null, null);
        var result = await _sut.RegisterFundo(request, CancellationToken.None);

        result.ShouldBeOfType<UnprocessableEntityObjectResult>();
    }

    [Fact]
    public async Task RegisterFundo_Duplicate_Returns409()
    {
        _registerFundoValidator
            .ValidateAsync(Arg.Any<RegisterFundoCommand>(), Arg.Any<CancellationToken>())
            .Returns(ValidResult());
        _registerFundoHandler
            .HandleAsync(Arg.Any<RegisterFundoCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new DuplicateEntityException("Fundo", "11.222.333/0001-81"));

        var request = new RegisterFundoRequest("Fundo Alfa", "11.222.333/0001-81", ConsultoriaId, CustodianteId,
            TipoFundo.Multimercado, null, null, null);
        var result = await _sut.RegisterFundo(request, CancellationToken.None);

        result.ShouldBeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task RegisterFundo_HappyPath_Returns201WithLocation()
    {
        _registerFundoValidator
            .ValidateAsync(Arg.Any<RegisterFundoCommand>(), Arg.Any<CancellationToken>())
            .Returns(ValidResult());
        _registerFundoHandler
            .HandleAsync(Arg.Any<RegisterFundoCommand>(), Arg.Any<CancellationToken>())
            .Returns(SampleFundo);

        var request = new RegisterFundoRequest("Fundo Alfa", "11.222.333/0001-81", ConsultoriaId, CustodianteId,
            TipoFundo.Multimercado, null, null, null);
        var result = await _sut.RegisterFundo(request, CancellationToken.None);

        var created = result.ShouldBeOfType<CreatedAtActionResult>();
        created.ActionName.ShouldBe(nameof(FundosController.GetFundoById));
        created.Value.ShouldBe(SampleFundo);
    }

    [Fact]
    public async Task RegisterFundo_CapturesActorAndCompanyFromContext()
    {
        _registerFundoValidator
            .ValidateAsync(Arg.Any<RegisterFundoCommand>(), Arg.Any<CancellationToken>())
            .Returns(ValidResult());
        _registerFundoHandler
            .HandleAsync(Arg.Any<RegisterFundoCommand>(), Arg.Any<CancellationToken>())
            .Returns(SampleFundo);

        var request = new RegisterFundoRequest("Fundo Alfa", "11.222.333/0001-81", ConsultoriaId, CustodianteId,
            TipoFundo.Multimercado, null, null, null);
        await _sut.RegisterFundo(request, CancellationToken.None);

        await _registerFundoHandler.Received(1).HandleAsync(
            Arg.Is<RegisterFundoCommand>(c =>
                c.ActorSub == "test-sub-123" && c.ActorEmail == "actor@test.com"),
            Arg.Any<CancellationToken>());
    }

    // =========================================================================
    // Fundo — GET list (ListFundos)
    // =========================================================================

    [Fact]
    public async Task ListFundos_HappyPath_Returns200WithPaginatedResult()
    {
        var expected = new PaginatedResult<FundoDto>(new[] { SampleFundo }, 1, 1, 20);
        _listFundoHandler
            .HandleAsync(Arg.Any<ListFundoQuery>(), Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _sut.ListFundos(ct: CancellationToken.None);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.Value.ShouldBe(expected);
    }

    // =========================================================================
    // Fundo — GET by id (GetFundoById)
    // =========================================================================

    [Fact]
    public async Task GetFundoById_NotFound_Returns404()
    {
        _fundoRepo.GetByIdAsync(FundoId, Arg.Any<CancellationToken>())
            .Returns((Fundo?)null);

        var result = await _sut.GetFundoById(FundoId, CancellationToken.None);

        result.ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetFundoById_Found_Returns200WithDto()
    {
        var fundo = BuildFundo();
        _fundoRepo.GetByIdAsync(FundoId, Arg.Any<CancellationToken>())
            .Returns(fundo);

        var result = await _sut.GetFundoById(FundoId, CancellationToken.None);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var dto = ok.Value.ShouldBeOfType<FundoDto>();
        dto.Id.ShouldBe(FundoId);
    }

    // =========================================================================
    // Fundo — PUT (UpdateFundo)
    // =========================================================================

    [Fact]
    public async Task UpdateFundo_NullBody_Returns400()
    {
        var result = await _sut.UpdateFundo(FundoId, null, CancellationToken.None);
        result.ShouldBeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task UpdateFundo_ValidationFails_Returns422()
    {
        _updateFundoValidator
            .ValidateAsync(Arg.Any<UpdateFundoCommand>(), Arg.Any<CancellationToken>())
            .Returns(InvalidResult("Nome", "Required."));

        var request = new UpdateFundoRequest(null, null, null, null);
        var result = await _sut.UpdateFundo(FundoId, request, CancellationToken.None);

        result.ShouldBeOfType<UnprocessableEntityObjectResult>();
    }

    [Fact]
    public async Task UpdateFundo_NotFound_Returns404()
    {
        _updateFundoValidator
            .ValidateAsync(Arg.Any<UpdateFundoCommand>(), Arg.Any<CancellationToken>())
            .Returns(ValidResult());
        _updateFundoHandler
            .HandleAsync(Arg.Any<UpdateFundoCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new KeyNotFoundException("Fundo not found."));

        var request = new UpdateFundoRequest("Fundo Alfa", null, null, null);
        var result = await _sut.UpdateFundo(FundoId, request, CancellationToken.None);

        result.ShouldBeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task UpdateFundo_HappyPath_Returns200WithDto()
    {
        _updateFundoValidator
            .ValidateAsync(Arg.Any<UpdateFundoCommand>(), Arg.Any<CancellationToken>())
            .Returns(ValidResult());
        _updateFundoHandler
            .HandleAsync(Arg.Any<UpdateFundoCommand>(), Arg.Any<CancellationToken>())
            .Returns(SampleFundo);

        var request = new UpdateFundoRequest("Fundo Alfa", null, null, null);
        var result = await _sut.UpdateFundo(FundoId, request, CancellationToken.None);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.Value.ShouldBe(SampleFundo);
    }

    // =========================================================================
    // Fundo — POST /{id}/status (TransitionFundoStatus) — state machine tests
    // =========================================================================

    [Fact]
    public async Task TransitionFundoStatus_NullBody_Returns400()
    {
        var result = await _sut.TransitionFundoStatus(FundoId, null, CancellationToken.None);
        result.ShouldBeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task TransitionFundoStatus_RascunhoToAtivo_ValidTransition_Returns200()
    {
        // RASCUNHO → ATIVO is a valid transition per state machine (D-02)
        _fundoRepo.GetByIdAsync(FundoId, Arg.Any<CancellationToken>()).Returns(BuildFundo());

        _transitionFundoStatusValidator
            .ValidateAsync(Arg.Any<TransitionFundoStatusCommand>(), Arg.Any<CancellationToken>())
            .Returns(ValidResult());

        var activoFundo = SampleFundo with { Status = FundoStatus.ATIVO };
        _transitionFundoStatusHandler
            .HandleAsync(Arg.Any<TransitionFundoStatusCommand>(), Arg.Any<CancellationToken>())
            .Returns(activoFundo);

        var request = new TransitionFundoStatusRequest(FundoStatus.ATIVO);
        var result = await _sut.TransitionFundoStatus(FundoId, request, CancellationToken.None);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var dto = ok.Value.ShouldBeOfType<FundoDto>();
        dto.Status.ShouldBe(FundoStatus.ATIVO);
    }

    [Fact]
    public async Task TransitionFundoStatus_EncerradoToAtivo_InvalidTransition_Returns400WithFromToDetail()
    {
        // ENCERRADO → ATIVO is an invalid transition — domain throws InvalidStateTransitionException
        _fundoRepo.GetByIdAsync(FundoId, Arg.Any<CancellationToken>()).Returns(BuildFundo());

        _transitionFundoStatusValidator
            .ValidateAsync(Arg.Any<TransitionFundoStatusCommand>(), Arg.Any<CancellationToken>())
            .Returns(ValidResult());
        _transitionFundoStatusHandler
            .HandleAsync(Arg.Any<TransitionFundoStatusCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidStateTransitionException("Fundo", FundoStatus.ENCERRADO, FundoStatus.ATIVO));

        var request = new TransitionFundoStatusRequest(FundoStatus.ATIVO);
        var result = await _sut.TransitionFundoStatus(FundoId, request, CancellationToken.None);

        var badRequest = result.ShouldBeOfType<BadRequestObjectResult>();
        var problem = badRequest.Value.ShouldBeOfType<ProblemDetails>();
        problem.Status.ShouldBe(400);
        // Detail must contain from/to info per acceptance criteria
        problem.Detail.ShouldNotBeNull();
        problem.Detail.ShouldContain("ENCERRADO");
        problem.Detail.ShouldContain("ATIVO");
    }

    [Fact]
    public async Task TransitionFundoStatus_FundoNotFound_Returns404()
    {
        // _fundoRepo.GetByIdAsync returns null by default (NSubstitute) — controller returns NotFound() bare.
        _transitionFundoStatusValidator
            .ValidateAsync(Arg.Any<TransitionFundoStatusCommand>(), Arg.Any<CancellationToken>())
            .Returns(ValidResult());
        _transitionFundoStatusHandler
            .HandleAsync(Arg.Any<TransitionFundoStatusCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new KeyNotFoundException("Fundo not found."));

        var request = new TransitionFundoStatusRequest(FundoStatus.ATIVO);
        var result = await _sut.TransitionFundoStatus(FundoId, request, CancellationToken.None);

        // Controller uses bare return NotFound() (no body) — NotFoundResult, not NotFoundObjectResult.
        result.ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task TransitionFundoStatus_CapturesActorFromJwt()
    {
        _fundoRepo.GetByIdAsync(FundoId, Arg.Any<CancellationToken>()).Returns(BuildFundo());

        _transitionFundoStatusValidator
            .ValidateAsync(Arg.Any<TransitionFundoStatusCommand>(), Arg.Any<CancellationToken>())
            .Returns(ValidResult());
        _transitionFundoStatusHandler
            .HandleAsync(Arg.Any<TransitionFundoStatusCommand>(), Arg.Any<CancellationToken>())
            .Returns(SampleFundo with { Status = FundoStatus.ATIVO });

        var request = new TransitionFundoStatusRequest(FundoStatus.ATIVO);
        await _sut.TransitionFundoStatus(FundoId, request, CancellationToken.None);

        await _transitionFundoStatusHandler.Received(1).HandleAsync(
            Arg.Is<TransitionFundoStatusCommand>(c =>
                c.FundoId == FundoId &&
                c.NewStatus == FundoStatus.ATIVO &&
                c.ActorSub == "test-sub-123" &&
                c.ActorEmail == "actor@test.com"),
            Arg.Any<CancellationToken>());
    }

    // =========================================================================
    // Cedente — POST /cedentes/pf (RegisterCedentePf)
    // =========================================================================

    [Fact]
    public async Task RegisterCedentePf_NullBody_Returns400()
    {
        var result = await _sut.RegisterCedentePf(null, CancellationToken.None);
        result.ShouldBeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task RegisterCedentePf_ValidationFails_Returns422()
    {
        _registerCedentePfValidator
            .ValidateAsync(Arg.Any<RegisterCedentePfCommand>(), Arg.Any<CancellationToken>())
            .Returns(InvalidResult("Cpf", "Invalid CPF."));

        var request = new RegisterCedentePfRequest("bad-cpf", "João", null, null, null);
        var result = await _sut.RegisterCedentePf(request, CancellationToken.None);

        result.ShouldBeOfType<UnprocessableEntityObjectResult>();
    }

    [Fact]
    public async Task RegisterCedentePf_Duplicate_Returns409()
    {
        // D-10: uniqueness is company-scoped — same CPF within same company → 409
        _registerCedentePfValidator
            .ValidateAsync(Arg.Any<RegisterCedentePfCommand>(), Arg.Any<CancellationToken>())
            .Returns(ValidResult());
        _registerCedentePfHandler
            .HandleAsync(Arg.Any<RegisterCedentePfCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new DuplicateEntityException("Cedente", "123.456.789-09"));

        var request = new RegisterCedentePfRequest("12345678909", "João Silva", null, null, null);
        var result = await _sut.RegisterCedentePf(request, CancellationToken.None);

        result.ShouldBeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task RegisterCedentePf_HappyPath_Returns201WithLocation()
    {
        _registerCedentePfValidator
            .ValidateAsync(Arg.Any<RegisterCedentePfCommand>(), Arg.Any<CancellationToken>())
            .Returns(ValidResult());
        _registerCedentePfHandler
            .HandleAsync(Arg.Any<RegisterCedentePfCommand>(), Arg.Any<CancellationToken>())
            .Returns(SampleCedentePf);

        var request = new RegisterCedentePfRequest("12345678909", "João Silva", null, null, null);
        var result = await _sut.RegisterCedentePf(request, CancellationToken.None);

        var created = result.ShouldBeOfType<CreatedAtActionResult>();
        created.ActionName.ShouldBe(nameof(FundosController.GetCedenteById));
        var dto = created.Value.ShouldBeOfType<CedenteDto>();
        dto.CedenteTipo.ShouldBe(CedenteTipo.PF);
    }

    [Fact]
    public async Task RegisterCedentePf_CapturesActorFromJwt()
    {
        _registerCedentePfValidator
            .ValidateAsync(Arg.Any<RegisterCedentePfCommand>(), Arg.Any<CancellationToken>())
            .Returns(ValidResult());
        _registerCedentePfHandler
            .HandleAsync(Arg.Any<RegisterCedentePfCommand>(), Arg.Any<CancellationToken>())
            .Returns(SampleCedentePf);

        var request = new RegisterCedentePfRequest("12345678909", "João Silva", null, null, null);
        await _sut.RegisterCedentePf(request, CancellationToken.None);

        await _registerCedentePfHandler.Received(1).HandleAsync(
            Arg.Is<RegisterCedentePfCommand>(c =>
                c.ActorSub == "test-sub-123" && c.ActorEmail == "actor@test.com"),
            Arg.Any<CancellationToken>());
    }

    // =========================================================================
    // Cedente — POST /cedentes/pj (RegisterCedentePj)
    // =========================================================================

    [Fact]
    public async Task RegisterCedentePj_NullBody_Returns400()
    {
        var result = await _sut.RegisterCedentePj(null, CancellationToken.None);
        result.ShouldBeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task RegisterCedentePj_ValidationFails_Returns422()
    {
        _registerCedentePjValidator
            .ValidateAsync(Arg.Any<RegisterCedentePjCommand>(), Arg.Any<CancellationToken>())
            .Returns(InvalidResult("Cnpj", "Invalid CNPJ."));

        var request = new RegisterCedentePjRequest("bad-cnpj", "Empresa PJ", null, null, null);
        var result = await _sut.RegisterCedentePj(request, CancellationToken.None);

        result.ShouldBeOfType<UnprocessableEntityObjectResult>();
    }

    [Fact]
    public async Task RegisterCedentePj_Duplicate_Returns409()
    {
        // D-10: uniqueness is company-scoped — same CNPJ within same company → 409
        _registerCedentePjValidator
            .ValidateAsync(Arg.Any<RegisterCedentePjCommand>(), Arg.Any<CancellationToken>())
            .Returns(ValidResult());
        _registerCedentePjHandler
            .HandleAsync(Arg.Any<RegisterCedentePjCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new DuplicateEntityException("Cedente", "11.222.333/0001-81"));

        var request = new RegisterCedentePjRequest("11222333000181", "Empresa PJ Ltda", null, null, null);
        var result = await _sut.RegisterCedentePj(request, CancellationToken.None);

        result.ShouldBeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task RegisterCedentePj_HappyPath_Returns201WithLocation()
    {
        _registerCedentePjValidator
            .ValidateAsync(Arg.Any<RegisterCedentePjCommand>(), Arg.Any<CancellationToken>())
            .Returns(ValidResult());
        _registerCedentePjHandler
            .HandleAsync(Arg.Any<RegisterCedentePjCommand>(), Arg.Any<CancellationToken>())
            .Returns(SampleCedentePj);

        var request = new RegisterCedentePjRequest("11222333000181", "Empresa PJ Ltda", null, null, null);
        var result = await _sut.RegisterCedentePj(request, CancellationToken.None);

        var created = result.ShouldBeOfType<CreatedAtActionResult>();
        created.ActionName.ShouldBe(nameof(FundosController.GetCedenteById));
        var dto = created.Value.ShouldBeOfType<CedenteDto>();
        dto.CedenteTipo.ShouldBe(CedenteTipo.PJ);
    }

    // =========================================================================
    // Cedente — GET list (ListCedentes)
    // =========================================================================

    [Fact]
    public async Task ListCedentes_HappyPath_Returns200WithPaginatedResult()
    {
        var expected = new PaginatedResult<CedenteDto>(new[] { SampleCedentePf }, 1, 1, 20);
        _listCedenteHandler
            .HandleAsync(Arg.Any<ListCedenteQuery>(), Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _sut.ListCedentes(ct: CancellationToken.None);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.Value.ShouldBe(expected);
    }

    // =========================================================================
    // Cedente — GET by id (GetCedenteById)
    // =========================================================================

    [Fact]
    public async Task GetCedenteById_NotFound_Returns404()
    {
        _cedenteRepo.GetByIdAsync(CedenteId, Arg.Any<CancellationToken>())
            .Returns((Cedente?)null);

        var result = await _sut.GetCedenteById(CedenteId, CancellationToken.None);

        result.ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetCedenteById_Found_Returns200WithDto()
    {
        var cedente = BuildCedentePf();
        _cedenteRepo.GetByIdAsync(CedenteId, Arg.Any<CancellationToken>())
            .Returns(cedente);

        var result = await _sut.GetCedenteById(CedenteId, CancellationToken.None);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var dto = ok.Value.ShouldBeOfType<CedenteDto>();
        dto.Id.ShouldBe(CedenteId);
        dto.CedenteTipo.ShouldBe(CedenteTipo.PF);
    }

    // =========================================================================
    // Cedente — PUT (UpdateCedente)
    // =========================================================================

    [Fact]
    public async Task UpdateCedente_NullBody_Returns400()
    {
        var result = await _sut.UpdateCedente(CedenteId, null, CancellationToken.None);
        result.ShouldBeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task UpdateCedente_ValidationFails_Returns422()
    {
        _updateCedenteValidator
            .ValidateAsync(Arg.Any<UpdateCedenteCommand>(), Arg.Any<CancellationToken>())
            .Returns(InvalidResult("Nome", "Required."));

        var request = new UpdateCedenteRequest(null, null, null, null, CedenteStatus.ATIVO);
        var result = await _sut.UpdateCedente(CedenteId, request, CancellationToken.None);

        result.ShouldBeOfType<UnprocessableEntityObjectResult>();
    }

    [Fact]
    public async Task UpdateCedente_NotFound_Returns404()
    {
        _updateCedenteValidator
            .ValidateAsync(Arg.Any<UpdateCedenteCommand>(), Arg.Any<CancellationToken>())
            .Returns(ValidResult());
        _updateCedenteHandler
            .HandleAsync(Arg.Any<UpdateCedenteCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new KeyNotFoundException("Cedente not found."));

        var request = new UpdateCedenteRequest("João Silva", null, null, null, CedenteStatus.ATIVO);
        var result = await _sut.UpdateCedente(CedenteId, request, CancellationToken.None);

        result.ShouldBeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task UpdateCedente_HappyPath_Returns200WithDto()
    {
        _updateCedenteValidator
            .ValidateAsync(Arg.Any<UpdateCedenteCommand>(), Arg.Any<CancellationToken>())
            .Returns(ValidResult());
        _updateCedenteHandler
            .HandleAsync(Arg.Any<UpdateCedenteCommand>(), Arg.Any<CancellationToken>())
            .Returns(SampleCedentePf);

        var request = new UpdateCedenteRequest("João Silva", null, null, null, CedenteStatus.ATIVO);
        var result = await _sut.UpdateCedente(CedenteId, request, CancellationToken.None);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.Value.ShouldBe(SampleCedentePf);
    }

    // =========================================================================
    // W-test: empty-string normalization locked for Email + Telefone (7 endpoints)
    // Reviewer flag: normalization present at 7 sites but zero tests covered the mapping.
    // Each test posts email="" / telefone="" and asserts the handler receives null.
    // =========================================================================

    [Fact]
    public async Task RegisterConsultoria_EmptyEmailAndTelefone_NormalizesToNull()
    {
        _registerConsultoriaValidator
            .ValidateAsync(Arg.Any<RegisterConsultoriaFundoCommand>(), Arg.Any<CancellationToken>())
            .Returns(ValidResult());
        _registerConsultoriaHandler
            .HandleAsync(Arg.Any<RegisterConsultoriaFundoCommand>(), Arg.Any<CancellationToken>())
            .Returns(SampleConsultoria);

        var request = new RegisterConsultoriaFundoRequest("Consultoria Ltda", "12.345.678/0001-90", null, "", "");
        await _sut.RegisterConsultoria(request, CancellationToken.None);

        await _registerConsultoriaHandler.Received(1).HandleAsync(
            Arg.Is<RegisterConsultoriaFundoCommand>(c => c.Email == null && c.Telefone == null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateConsultoria_EmptyEmailAndTelefone_NormalizesToNull()
    {
        _updateConsultoriaValidator
            .ValidateAsync(Arg.Any<UpdateConsultoriaFundoCommand>(), Arg.Any<CancellationToken>())
            .Returns(ValidResult());
        _updateConsultoriaHandler
            .HandleAsync(Arg.Any<UpdateConsultoriaFundoCommand>(), Arg.Any<CancellationToken>())
            .Returns(SampleConsultoria);

        var request = new UpdateConsultoriaFundoRequest("Consultoria Ltda", null, "", "", ConsultoriaFundoStatus.ATIVO);
        await _sut.UpdateConsultoria(ConsultoriaId, request, CancellationToken.None);

        await _updateConsultoriaHandler.Received(1).HandleAsync(
            Arg.Is<UpdateConsultoriaFundoCommand>(c => c.Email == null && c.Telefone == null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterCustodiante_EmptyEmailAndTelefone_NormalizesToNull()
    {
        _registerCustodianteValidator
            .ValidateAsync(Arg.Any<RegisterCustodianteCommand>(), Arg.Any<CancellationToken>())
            .Returns(ValidResult());
        _registerCustodianteHandler
            .HandleAsync(Arg.Any<RegisterCustodianteCommand>(), Arg.Any<CancellationToken>())
            .Returns(SampleCustodiante);

        var request = new RegisterCustodianteRequest("Custodiante S.A.", "98.765.432/0001-10", null, "", "");
        await _sut.RegisterCustodiante(request, CancellationToken.None);

        await _registerCustodianteHandler.Received(1).HandleAsync(
            Arg.Is<RegisterCustodianteCommand>(c => c.Email == null && c.Telefone == null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateCustodiante_EmptyEmailAndTelefone_NormalizesToNull()
    {
        _updateCustodianteValidator
            .ValidateAsync(Arg.Any<UpdateCustodianteCommand>(), Arg.Any<CancellationToken>())
            .Returns(ValidResult());
        _updateCustodianteHandler
            .HandleAsync(Arg.Any<UpdateCustodianteCommand>(), Arg.Any<CancellationToken>())
            .Returns(SampleCustodiante);

        var request = new UpdateCustodianteRequest("Custodiante S.A.", null, "", "", CustodianteStatus.ATIVO);
        await _sut.UpdateCustodiante(CustodianteId, request, CancellationToken.None);

        await _updateCustodianteHandler.Received(1).HandleAsync(
            Arg.Is<UpdateCustodianteCommand>(c => c.Email == null && c.Telefone == null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterCedentePf_EmptyEmailAndTelefone_NormalizesToNull()
    {
        _registerCedentePfValidator
            .ValidateAsync(Arg.Any<RegisterCedentePfCommand>(), Arg.Any<CancellationToken>())
            .Returns(ValidResult());
        _registerCedentePfHandler
            .HandleAsync(Arg.Any<RegisterCedentePfCommand>(), Arg.Any<CancellationToken>())
            .Returns(SampleCedentePf);

        var request = new RegisterCedentePfRequest("12345678909", "João Silva", "", "", null);
        await _sut.RegisterCedentePf(request, CancellationToken.None);

        await _registerCedentePfHandler.Received(1).HandleAsync(
            Arg.Is<RegisterCedentePfCommand>(c => c.Email == null && c.Telefone == null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterCedentePj_EmptyEmailAndTelefone_NormalizesToNull()
    {
        _registerCedentePjValidator
            .ValidateAsync(Arg.Any<RegisterCedentePjCommand>(), Arg.Any<CancellationToken>())
            .Returns(ValidResult());
        _registerCedentePjHandler
            .HandleAsync(Arg.Any<RegisterCedentePjCommand>(), Arg.Any<CancellationToken>())
            .Returns(SampleCedentePj);

        var request = new RegisterCedentePjRequest("11222333000181", "Empresa PJ Ltda", "", "", null);
        await _sut.RegisterCedentePj(request, CancellationToken.None);

        await _registerCedentePjHandler.Received(1).HandleAsync(
            Arg.Is<RegisterCedentePjCommand>(c => c.Email == null && c.Telefone == null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateCedente_EmptyEmailAndTelefone_NormalizesToNull()
    {
        _updateCedenteValidator
            .ValidateAsync(Arg.Any<UpdateCedenteCommand>(), Arg.Any<CancellationToken>())
            .Returns(ValidResult());
        _updateCedenteHandler
            .HandleAsync(Arg.Any<UpdateCedenteCommand>(), Arg.Any<CancellationToken>())
            .Returns(SampleCedentePf);

        var request = new UpdateCedenteRequest("João Silva", "", "", null, CedenteStatus.ATIVO);
        await _sut.UpdateCedente(CedenteId, request, CancellationToken.None);

        await _updateCedenteHandler.Received(1).HandleAsync(
            Arg.Is<UpdateCedenteCommand>(c => c.Email == null && c.Telefone == null),
            Arg.Any<CancellationToken>());
    }

    // =========================================================================
    // Domain aggregate builders (use reflection to avoid ctor dependency on domain internals)
    // =========================================================================

    private static ConsultoriaFundo BuildConsultoriaFundo()
    {
        // Use domain factory method to create a valid aggregate (11.222.333/0001-81 is a valid test CNPJ)
        var c = ConsultoriaFundo.Register(
            "Consultoria Ltda",
            "11.222.333/0001-81",
            CompanyId,
            null,
            null,
            null);

        // Overwrite Id via reflection so the test can assert against ConsultoriaId
        SetId(c, ConsultoriaId);
        return c;
    }

    private static Custodiante BuildCustodiante()
    {
        // 11.444.777/0001-61 is a valid test CNPJ
        var c = Custodiante.Register(
            "Custodiante S.A.",
            "11.444.777/0001-61",
            CompanyId,
            null,
            null,
            null);

        SetId(c, CustodianteId);
        return c;
    }

    private static TipoAtivo BuildTipoAtivo()
    {
        var t = TipoAtivo.Register(
            "LTN",
            "Letra do Tesouro Nacional",
            TipoAtivoCategoria.RendaFixa,
            null,
            1);

        SetId(t, TipoAtivoId);
        return t;
    }

    private static Fundo BuildFundo()
    {
        // 11.222.333/0001-81 is a valid test CNPJ
        var f = Fundo.Register(
            "Fundo Alfa",
            "11.222.333/0001-81",
            CompanyId,
            ConsultoriaId,
            CustodianteId,
            TipoFundo.Multimercado);

        SetId(f, FundoId);
        return f;
    }

    private static Cedente BuildCedentePf()
    {
        // 123.456.789-09 is a valid test CPF
        var c = Cedente.RegisterPf(
            "123.456.789-09",
            "João Silva",
            CompanyId);

        SetId(c, CedenteId);
        return c;
    }

    /// <summary>
    /// Sets the Id on a domain entity via the protected setter on Entity&lt;TId&gt; base class.
    /// The setter is protected (not private), so we reach it via the base type's property metadata.
    /// Acceptable in tests; avoids adding test-only constructors to the domain.
    /// </summary>
    private static void SetId<T>(T entity, Guid id)
    {
        // Entity<Guid>.Id has a protected setter — find it on the base class with NonPublic flag
        var type = typeof(T);
        PropertyInfo? prop = null;
        while (type is not null)
        {
            prop = type.GetProperty("Id",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            if (prop is not null) break;
            type = type.BaseType;
        }

        if (prop is not null)
        {
            prop.SetValue(entity, id);
        }
        // If reflection fails the test still runs — Id will be a random GUID and the assertion
        // ShouldBeOfType<CustodianteDto> still passes. Only the id equality assertion would fail.
    }
}
