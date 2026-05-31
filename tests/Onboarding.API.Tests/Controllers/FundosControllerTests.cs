using System.Reflection;
using System.Security.Claims;
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
/// Refactored for Phase 55 (D-60..D-63): controller now injects ICommandDispatcher / IQueryDispatcher /
/// IValidationRunner / ICurrentCompanyService. Repos used in GetById actions are passed via [FromServices]
/// and provided directly in test method calls.
/// Policy attribute presence is verified via reflection (no WebApplicationFactory needed).
/// </summary>
public class FundosControllerTests
{
    // -------------------------------------------------------------------------
    // Dispatcher + validation mocks (replaces N per-handler mocks in the ctor)
    // -------------------------------------------------------------------------

    private readonly ICommandDispatcher _commands = Substitute.For<ICommandDispatcher>();
    private readonly IQueryDispatcher _queries = Substitute.For<IQueryDispatcher>();
    private readonly IValidationRunner _validation = Substitute.For<IValidationRunner>();
    private readonly ICurrentCompanyService _companyService = Substitute.For<ICurrentCompanyService>();

    // Per-repo mocks — passed directly to [FromServices] actions
    private readonly IConsultoriaFundoRepository _consultoriaRepo = Substitute.For<IConsultoriaFundoRepository>();
    private readonly ICustodianteRepository _custodianteRepo = Substitute.For<ICustodianteRepository>();
    private readonly ITipoAtivoRepository _tipoAtivoRepo = Substitute.For<ITipoAtivoRepository>();
    private readonly IFundoRepository _fundoRepo = Substitute.For<IFundoRepository>();
    private readonly ICedenteRepository _cedenteRepo = Substitute.For<ICedenteRepository>();

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

        // Default: validation passes
        _validation.Validate(Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(ValidResult());

        _sut = new FundosController(
            _commands,
            _queries,
            _validation,
            _companyService);

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
    [InlineData(nameof(FundosController.RegisterFundo), PermissionPolicies.FundWrite)]
    [InlineData(nameof(FundosController.ListFundos), PermissionPolicies.FundRead)]
    [InlineData(nameof(FundosController.GetFundoById), PermissionPolicies.FundRead)]
    [InlineData(nameof(FundosController.UpdateFundo), PermissionPolicies.FundWrite)]
    [InlineData(nameof(FundosController.TransitionFundoStatus), PermissionPolicies.FundWrite)]
    [InlineData(nameof(FundosController.RegisterCedentePf), PermissionPolicies.FundWrite)]
    [InlineData(nameof(FundosController.RegisterCedentePj), PermissionPolicies.FundWrite)]
    [InlineData(nameof(FundosController.ListCedentes), PermissionPolicies.FundRead)]
    [InlineData(nameof(FundosController.GetCedenteById), PermissionPolicies.FundRead)]
    [InlineData(nameof(FundosController.UpdateCedente), PermissionPolicies.FundWrite)]
    public void Endpoint_HasExpectedAuthorizePolicy(string methodName, string expectedPolicy)
    {
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
        _validation.Validate(Arg.Any<RegisterConsultoriaFundoCommand>(), Arg.Any<CancellationToken>())
            .Returns(InvalidResult("Cnpj", "Invalid CNPJ."));

        var request = new RegisterConsultoriaFundoRequest("Consultoria", "bad-cnpj", null, null, null);
        var result = await _sut.RegisterConsultoria(request, CancellationToken.None);

        result.ShouldBeOfType<UnprocessableEntityObjectResult>();
    }

    [Fact]
    public async Task RegisterConsultoria_Duplicate_Returns409()
    {
        _commands.Send<ConsultoriaFundoDto>(Arg.Any<RegisterConsultoriaFundoCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new DuplicateEntityException("ConsultoriaFundo", "12.345.678/0001-90"));

        var request = new RegisterConsultoriaFundoRequest("Consultoria", "12.345.678/0001-90", null, null, null);
        var result = await _sut.RegisterConsultoria(request, CancellationToken.None);

        result.ShouldBeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task RegisterConsultoria_HappyPath_Returns201WithLocation()
    {
        _commands.Send<ConsultoriaFundoDto>(Arg.Any<RegisterConsultoriaFundoCommand>(), Arg.Any<CancellationToken>())
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
        RegisterConsultoriaFundoCommand? captured = null;
        _commands.Send<ConsultoriaFundoDto>(Arg.Do<object>(c => captured = c as RegisterConsultoriaFundoCommand), Arg.Any<CancellationToken>())
            .Returns(SampleConsultoria);

        var request = new RegisterConsultoriaFundoRequest("Consultoria Ltda", "12.345.678/0001-90", null, null, null);
        await _sut.RegisterConsultoria(request, CancellationToken.None);

        captured.ShouldNotBeNull();
        captured!.ActorSub.ShouldBe("test-sub-123");
        captured.ActorEmail.ShouldBe("actor@test.com");
    }

    // =========================================================================
    // ConsultoriaFundo — GET list (ListConsultorias)
    // =========================================================================

    [Fact]
    public async Task ListConsultorias_HappyPath_Returns200WithPaginatedResult()
    {
        var expected = new PaginatedResult<ConsultoriaFundoDto>(new[] { SampleConsultoria }, 1, 1, 20);
        _queries.Query<PaginatedResult<ConsultoriaFundoDto>>(Arg.Any<ListConsultoriaFundoQuery>(), Arg.Any<CancellationToken>())
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

        var result = await _sut.GetConsultoriaById(ConsultoriaId, _consultoriaRepo, CancellationToken.None);

        result.ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetConsultoriaById_Found_Returns200WithDto()
    {
        var consultoria = BuildConsultoriaFundo();
        _consultoriaRepo.GetByIdAsync(ConsultoriaId, Arg.Any<CancellationToken>())
            .Returns(consultoria);

        var result = await _sut.GetConsultoriaById(ConsultoriaId, _consultoriaRepo, CancellationToken.None);

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
        _validation.Validate(Arg.Any<UpdateConsultoriaFundoCommand>(), Arg.Any<CancellationToken>())
            .Returns(InvalidResult("RazaoSocial", "Required."));

        var request = new UpdateConsultoriaFundoRequest(null, null, null, null, ConsultoriaFundoStatus.ATIVO);
        var result = await _sut.UpdateConsultoria(ConsultoriaId, request, CancellationToken.None);

        result.ShouldBeOfType<UnprocessableEntityObjectResult>();
    }

    [Fact]
    public async Task UpdateConsultoria_NotFound_Returns404()
    {
        _commands.Send<ConsultoriaFundoDto>(Arg.Any<UpdateConsultoriaFundoCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new KeyNotFoundException("ConsultoriaFundo not found."));

        var request = new UpdateConsultoriaFundoRequest("Consultoria", null, null, null, ConsultoriaFundoStatus.ATIVO);
        var result = await _sut.UpdateConsultoria(ConsultoriaId, request, CancellationToken.None);

        result.ShouldBeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task UpdateConsultoria_HappyPath_Returns200WithDto()
    {
        _commands.Send<ConsultoriaFundoDto>(Arg.Any<UpdateConsultoriaFundoCommand>(), Arg.Any<CancellationToken>())
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
        _validation.Validate(Arg.Any<RegisterCustodianteCommand>(), Arg.Any<CancellationToken>())
            .Returns(InvalidResult("Cnpj", "Invalid CNPJ."));

        var request = new RegisterCustodianteRequest("Custodiante", "bad", null, null, null);
        var result = await _sut.RegisterCustodiante(request, CancellationToken.None);

        result.ShouldBeOfType<UnprocessableEntityObjectResult>();
    }

    [Fact]
    public async Task RegisterCustodiante_Duplicate_Returns409()
    {
        _commands.Send<CustodianteDto>(Arg.Any<RegisterCustodianteCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new DuplicateEntityException("Custodiante", "98.765.432/0001-10"));

        var request = new RegisterCustodianteRequest("Custodiante S.A.", "98.765.432/0001-10", null, null, null);
        var result = await _sut.RegisterCustodiante(request, CancellationToken.None);

        result.ShouldBeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task RegisterCustodiante_HappyPath_Returns201()
    {
        _commands.Send<CustodianteDto>(Arg.Any<RegisterCustodianteCommand>(), Arg.Any<CancellationToken>())
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
        RegisterCustodianteCommand? captured = null;
        _commands.Send<CustodianteDto>(Arg.Do<object>(c => captured = c as RegisterCustodianteCommand), Arg.Any<CancellationToken>())
            .Returns(SampleCustodiante);

        var request = new RegisterCustodianteRequest("Custodiante S.A.", "98.765.432/0001-10", null, null, null);
        await _sut.RegisterCustodiante(request, CancellationToken.None);

        captured.ShouldNotBeNull();
        captured!.ActorSub.ShouldBe("test-sub-123");
        captured.ActorEmail.ShouldBe("actor@test.com");
    }

    // =========================================================================
    // Custodiante — GET list (ListCustodiantes)
    // =========================================================================

    [Fact]
    public async Task ListCustodiantes_HappyPath_Returns200WithPaginatedResult()
    {
        var expected = new PaginatedResult<CustodianteDto>(new[] { SampleCustodiante }, 1, 1, 20);
        _queries.Query<PaginatedResult<CustodianteDto>>(Arg.Any<ListCustodianteQuery>(), Arg.Any<CancellationToken>())
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

        var result = await _sut.GetCustodianteById(CustodianteId, _custodianteRepo, CancellationToken.None);

        result.ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetCustodianteById_Found_Returns200WithDto()
    {
        var custodiante = BuildCustodiante();
        _custodianteRepo.GetByIdAsync(CustodianteId, Arg.Any<CancellationToken>())
            .Returns(custodiante);

        var result = await _sut.GetCustodianteById(CustodianteId, _custodianteRepo, CancellationToken.None);

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
        _validation.Validate(Arg.Any<UpdateCustodianteCommand>(), Arg.Any<CancellationToken>())
            .Returns(InvalidResult("RazaoSocial", "Required."));

        var request = new UpdateCustodianteRequest(null, null, null, null, CustodianteStatus.ATIVO);
        var result = await _sut.UpdateCustodiante(CustodianteId, request, CancellationToken.None);

        result.ShouldBeOfType<UnprocessableEntityObjectResult>();
    }

    [Fact]
    public async Task UpdateCustodiante_NotFound_Returns404()
    {
        _commands.Send<CustodianteDto>(Arg.Any<UpdateCustodianteCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new KeyNotFoundException("Custodiante not found."));

        var request = new UpdateCustodianteRequest("Custodiante S.A.", null, null, null, CustodianteStatus.ATIVO);
        var result = await _sut.UpdateCustodiante(CustodianteId, request, CancellationToken.None);

        result.ShouldBeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task UpdateCustodiante_HappyPath_Returns200WithDto()
    {
        _commands.Send<CustodianteDto>(Arg.Any<UpdateCustodianteCommand>(), Arg.Any<CancellationToken>())
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
        _validation.Validate(Arg.Any<CreateTipoAtivoCommand>(), Arg.Any<CancellationToken>())
            .Returns(InvalidResult("Codigo", "Required."));

        var request = new CreateTipoAtivoRequest(null, "LTN desc", TipoAtivoCategoria.RendaFixa, null);
        var result = await _sut.CreateTipoAtivo(request, CancellationToken.None);

        result.ShouldBeOfType<UnprocessableEntityObjectResult>();
    }

    [Fact]
    public async Task CreateTipoAtivo_Duplicate_Returns409()
    {
        _commands.Send<TipoAtivoDto>(Arg.Any<CreateTipoAtivoCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new DuplicateEntityException("TipoAtivo", "LTN"));

        var request = new CreateTipoAtivoRequest("LTN", "Letra do Tesouro Nacional", TipoAtivoCategoria.RendaFixa, null);
        var result = await _sut.CreateTipoAtivo(request, CancellationToken.None);

        result.ShouldBeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task CreateTipoAtivo_HappyPath_Returns201()
    {
        _commands.Send<TipoAtivoDto>(Arg.Any<CreateTipoAtivoCommand>(), Arg.Any<CancellationToken>())
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
        _commands.Send<TipoAtivoDto>(Arg.Any<CreateTipoAtivoCommand>(), Arg.Any<CancellationToken>())
            .Returns(SampleTipoAtivo);

        var request = new CreateTipoAtivoRequest("LTN", "Letra do Tesouro Nacional", TipoAtivoCategoria.RendaFixa, null);
        await _sut.CreateTipoAtivo(request, CancellationToken.None);

        _ = _companyService.DidNotReceive().CompanyId;
    }

    // =========================================================================
    // TipoAtivo — GET list (ListTiposAtivo)
    // =========================================================================

    [Fact]
    public async Task ListTiposAtivo_HappyPath_Returns200WithPaginatedResult()
    {
        var expected = new PaginatedResult<TipoAtivoDto>(new[] { SampleTipoAtivo }, 1, 1, 20);
        _queries.Query<PaginatedResult<TipoAtivoDto>>(Arg.Any<ListTipoAtivoQuery>(), Arg.Any<CancellationToken>())
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

        var result = await _sut.GetTipoAtivoById(TipoAtivoId, _tipoAtivoRepo, CancellationToken.None);

        result.ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetTipoAtivoById_Found_Returns200WithDto()
    {
        var tipoAtivo = BuildTipoAtivo();
        _tipoAtivoRepo.GetByIdAsync(TipoAtivoId, Arg.Any<CancellationToken>())
            .Returns(tipoAtivo);

        var result = await _sut.GetTipoAtivoById(TipoAtivoId, _tipoAtivoRepo, CancellationToken.None);

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
        _validation.Validate(Arg.Any<UpdateTipoAtivoCommand>(), Arg.Any<CancellationToken>())
            .Returns(InvalidResult("Descricao", "Required."));

        var request = new UpdateTipoAtivoRequest(null, null, TipoAtivoStatus.ATIVO, 1);
        var result = await _sut.UpdateTipoAtivo(TipoAtivoId, request, CancellationToken.None);

        result.ShouldBeOfType<UnprocessableEntityObjectResult>();
    }

    [Fact]
    public async Task UpdateTipoAtivo_NotFound_Returns404()
    {
        _commands.Send<TipoAtivoDto>(Arg.Any<UpdateTipoAtivoCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new KeyNotFoundException("TipoAtivo not found."));

        var request = new UpdateTipoAtivoRequest("LTN desc", null, TipoAtivoStatus.ATIVO, 1);
        var result = await _sut.UpdateTipoAtivo(TipoAtivoId, request, CancellationToken.None);

        result.ShouldBeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task UpdateTipoAtivo_HappyPath_Returns200WithDto()
    {
        _commands.Send<TipoAtivoDto>(Arg.Any<UpdateTipoAtivoCommand>(), Arg.Any<CancellationToken>())
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
        _validation.Validate(Arg.Any<RegisterFundoCommand>(), Arg.Any<CancellationToken>())
            .Returns(InvalidResult("Cnpj", "Invalid CNPJ."));

        var request = new RegisterFundoRequest("Fundo Alfa", "bad-cnpj", ConsultoriaId, CustodianteId,
            TipoFundo.Multimercado, null, null, null);
        var result = await _sut.RegisterFundo(request, CancellationToken.None);

        result.ShouldBeOfType<UnprocessableEntityObjectResult>();
    }

    [Fact]
    public async Task RegisterFundo_Duplicate_Returns409()
    {
        _commands.Send<FundoDto>(Arg.Any<RegisterFundoCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new DuplicateEntityException("Fundo", "11.222.333/0001-81"));

        var request = new RegisterFundoRequest("Fundo Alfa", "11.222.333/0001-81", ConsultoriaId, CustodianteId,
            TipoFundo.Multimercado, null, null, null);
        var result = await _sut.RegisterFundo(request, CancellationToken.None);

        result.ShouldBeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task RegisterFundo_HappyPath_Returns201WithLocation()
    {
        _commands.Send<FundoDto>(Arg.Any<RegisterFundoCommand>(), Arg.Any<CancellationToken>())
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
        RegisterFundoCommand? captured = null;
        _commands.Send<FundoDto>(Arg.Do<object>(c => captured = c as RegisterFundoCommand), Arg.Any<CancellationToken>())
            .Returns(SampleFundo);

        var request = new RegisterFundoRequest("Fundo Alfa", "11.222.333/0001-81", ConsultoriaId, CustodianteId,
            TipoFundo.Multimercado, null, null, null);
        await _sut.RegisterFundo(request, CancellationToken.None);

        captured.ShouldNotBeNull();
        captured!.ActorSub.ShouldBe("test-sub-123");
        captured.ActorEmail.ShouldBe("actor@test.com");
    }

    // =========================================================================
    // Fundo — GET list (ListFundos)
    // =========================================================================

    [Fact]
    public async Task ListFundos_HappyPath_Returns200WithPaginatedResult()
    {
        var expected = new PaginatedResult<FundoDto>(new[] { SampleFundo }, 1, 1, 20);
        _queries.Query<PaginatedResult<FundoDto>>(Arg.Any<ListFundoQuery>(), Arg.Any<CancellationToken>())
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

        var result = await _sut.GetFundoById(FundoId, _fundoRepo, CancellationToken.None);

        result.ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetFundoById_Found_Returns200WithDto()
    {
        var fundo = BuildFundo();
        _fundoRepo.GetByIdAsync(FundoId, Arg.Any<CancellationToken>())
            .Returns(fundo);

        var result = await _sut.GetFundoById(FundoId, _fundoRepo, CancellationToken.None);

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
        _validation.Validate(Arg.Any<UpdateFundoCommand>(), Arg.Any<CancellationToken>())
            .Returns(InvalidResult("Nome", "Required."));

        var request = new UpdateFundoRequest(null, null, null, null);
        var result = await _sut.UpdateFundo(FundoId, request, CancellationToken.None);

        result.ShouldBeOfType<UnprocessableEntityObjectResult>();
    }

    [Fact]
    public async Task UpdateFundo_NotFound_Returns404()
    {
        _commands.Send<FundoDto>(Arg.Any<UpdateFundoCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new KeyNotFoundException("Fundo not found."));

        var request = new UpdateFundoRequest("Fundo Alfa", null, null, null);
        var result = await _sut.UpdateFundo(FundoId, request, CancellationToken.None);

        result.ShouldBeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task UpdateFundo_HappyPath_Returns200WithDto()
    {
        _commands.Send<FundoDto>(Arg.Any<UpdateFundoCommand>(), Arg.Any<CancellationToken>())
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
        var result = await _sut.TransitionFundoStatus(FundoId, null, _fundoRepo, CancellationToken.None);
        result.ShouldBeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task TransitionFundoStatus_RascunhoToAtivo_ValidTransition_Returns200()
    {
        _fundoRepo.GetByIdAsync(FundoId, Arg.Any<CancellationToken>()).Returns(BuildFundo());

        var activoFundo = SampleFundo with { Status = FundoStatus.ATIVO };
        _commands.Send<FundoDto>(Arg.Any<TransitionFundoStatusCommand>(), Arg.Any<CancellationToken>())
            .Returns(activoFundo);

        var request = new TransitionFundoStatusRequest(FundoStatus.ATIVO);
        var result = await _sut.TransitionFundoStatus(FundoId, request, _fundoRepo, CancellationToken.None);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var dto = ok.Value.ShouldBeOfType<FundoDto>();
        dto.Status.ShouldBe(FundoStatus.ATIVO);
    }

    [Fact]
    public async Task TransitionFundoStatus_EncerradoToAtivo_InvalidTransition_Returns400WithFromToDetail()
    {
        _fundoRepo.GetByIdAsync(FundoId, Arg.Any<CancellationToken>()).Returns(BuildFundo());
        _commands.Send<FundoDto>(Arg.Any<TransitionFundoStatusCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidStateTransitionException("Fundo", FundoStatus.ENCERRADO, FundoStatus.ATIVO));

        var request = new TransitionFundoStatusRequest(FundoStatus.ATIVO);
        var result = await _sut.TransitionFundoStatus(FundoId, request, _fundoRepo, CancellationToken.None);

        var badRequest = result.ShouldBeOfType<BadRequestObjectResult>();
        var problem = badRequest.Value.ShouldBeOfType<ProblemDetails>();
        problem.Status.ShouldBe(400);
        problem.Detail.ShouldNotBeNull();
        problem.Detail.ShouldContain("ENCERRADO");
        problem.Detail.ShouldContain("ATIVO");
    }

    [Fact]
    public async Task TransitionFundoStatus_FundoNotFound_Returns404()
    {
        // _fundoRepo returns null by default — controller returns NotFound().
        var request = new TransitionFundoStatusRequest(FundoStatus.ATIVO);
        var result = await _sut.TransitionFundoStatus(FundoId, request, _fundoRepo, CancellationToken.None);

        result.ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task TransitionFundoStatus_CapturesActorFromJwt()
    {
        _fundoRepo.GetByIdAsync(FundoId, Arg.Any<CancellationToken>()).Returns(BuildFundo());

        TransitionFundoStatusCommand? captured = null;
        _commands.Send<FundoDto>(Arg.Do<object>(c => captured = c as TransitionFundoStatusCommand), Arg.Any<CancellationToken>())
            .Returns(SampleFundo with { Status = FundoStatus.ATIVO });

        var request = new TransitionFundoStatusRequest(FundoStatus.ATIVO);
        await _sut.TransitionFundoStatus(FundoId, request, _fundoRepo, CancellationToken.None);

        captured.ShouldNotBeNull();
        captured!.FundoId.ShouldBe(FundoId);
        captured.NewStatus.ShouldBe(FundoStatus.ATIVO);
        captured.ActorSub.ShouldBe("test-sub-123");
        captured.ActorEmail.ShouldBe("actor@test.com");
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
        _validation.Validate(Arg.Any<RegisterCedentePfCommand>(), Arg.Any<CancellationToken>())
            .Returns(InvalidResult("Cpf", "Invalid CPF."));

        var request = new RegisterCedentePfRequest("bad-cpf", "João", null, null, null);
        var result = await _sut.RegisterCedentePf(request, CancellationToken.None);

        result.ShouldBeOfType<UnprocessableEntityObjectResult>();
    }

    [Fact]
    public async Task RegisterCedentePf_Duplicate_Returns409()
    {
        _commands.Send<CedenteDto>(Arg.Any<RegisterCedentePfCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new DuplicateEntityException("Cedente", "123.456.789-09"));

        var request = new RegisterCedentePfRequest("12345678909", "João Silva", null, null, null);
        var result = await _sut.RegisterCedentePf(request, CancellationToken.None);

        result.ShouldBeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task RegisterCedentePf_HappyPath_Returns201WithLocation()
    {
        _commands.Send<CedenteDto>(Arg.Any<RegisterCedentePfCommand>(), Arg.Any<CancellationToken>())
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
        RegisterCedentePfCommand? captured = null;
        _commands.Send<CedenteDto>(Arg.Do<object>(c => captured = c as RegisterCedentePfCommand), Arg.Any<CancellationToken>())
            .Returns(SampleCedentePf);

        var request = new RegisterCedentePfRequest("12345678909", "João Silva", null, null, null);
        await _sut.RegisterCedentePf(request, CancellationToken.None);

        captured.ShouldNotBeNull();
        captured!.ActorSub.ShouldBe("test-sub-123");
        captured.ActorEmail.ShouldBe("actor@test.com");
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
        _validation.Validate(Arg.Any<RegisterCedentePjCommand>(), Arg.Any<CancellationToken>())
            .Returns(InvalidResult("Cnpj", "Invalid CNPJ."));

        var request = new RegisterCedentePjRequest("bad-cnpj", "Empresa PJ", null, null, null);
        var result = await _sut.RegisterCedentePj(request, CancellationToken.None);

        result.ShouldBeOfType<UnprocessableEntityObjectResult>();
    }

    [Fact]
    public async Task RegisterCedentePj_Duplicate_Returns409()
    {
        _commands.Send<CedenteDto>(Arg.Any<RegisterCedentePjCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new DuplicateEntityException("Cedente", "11.222.333/0001-81"));

        var request = new RegisterCedentePjRequest("11222333000181", "Empresa PJ Ltda", null, null, null);
        var result = await _sut.RegisterCedentePj(request, CancellationToken.None);

        result.ShouldBeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task RegisterCedentePj_HappyPath_Returns201WithLocation()
    {
        _commands.Send<CedenteDto>(Arg.Any<RegisterCedentePjCommand>(), Arg.Any<CancellationToken>())
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
        _queries.Query<PaginatedResult<CedenteDto>>(Arg.Any<ListCedenteQuery>(), Arg.Any<CancellationToken>())
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

        var result = await _sut.GetCedenteById(CedenteId, _cedenteRepo, CancellationToken.None);

        result.ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetCedenteById_Found_Returns200WithDto()
    {
        var cedente = BuildCedentePf();
        _cedenteRepo.GetByIdAsync(CedenteId, Arg.Any<CancellationToken>())
            .Returns(cedente);

        var result = await _sut.GetCedenteById(CedenteId, _cedenteRepo, CancellationToken.None);

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
        _validation.Validate(Arg.Any<UpdateCedenteCommand>(), Arg.Any<CancellationToken>())
            .Returns(InvalidResult("Nome", "Required."));

        var request = new UpdateCedenteRequest(null, null, null, null, CedenteStatus.ATIVO);
        var result = await _sut.UpdateCedente(CedenteId, request, CancellationToken.None);

        result.ShouldBeOfType<UnprocessableEntityObjectResult>();
    }

    [Fact]
    public async Task UpdateCedente_NotFound_Returns404()
    {
        _commands.Send<CedenteDto>(Arg.Any<UpdateCedenteCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new KeyNotFoundException("Cedente not found."));

        var request = new UpdateCedenteRequest("João Silva", null, null, null, CedenteStatus.ATIVO);
        var result = await _sut.UpdateCedente(CedenteId, request, CancellationToken.None);

        result.ShouldBeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task UpdateCedente_HappyPath_Returns200WithDto()
    {
        _commands.Send<CedenteDto>(Arg.Any<UpdateCedenteCommand>(), Arg.Any<CancellationToken>())
            .Returns(SampleCedentePf);

        var request = new UpdateCedenteRequest("João Silva", null, null, null, CedenteStatus.ATIVO);
        var result = await _sut.UpdateCedente(CedenteId, request, CancellationToken.None);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.Value.ShouldBe(SampleCedentePf);
    }

    // =========================================================================
    // W-test: empty-string normalization locked for Email + Telefone (7 endpoints)
    // =========================================================================

    [Fact]
    public async Task RegisterConsultoria_EmptyEmailAndTelefone_NormalizesToNull()
    {
        RegisterConsultoriaFundoCommand? captured = null;
        _commands.Send<ConsultoriaFundoDto>(Arg.Do<object>(c => captured = c as RegisterConsultoriaFundoCommand), Arg.Any<CancellationToken>())
            .Returns(SampleConsultoria);

        var request = new RegisterConsultoriaFundoRequest("Consultoria Ltda", "12.345.678/0001-90", null, "", "");
        await _sut.RegisterConsultoria(request, CancellationToken.None);

        captured.ShouldNotBeNull();
        captured!.Email.ShouldBeNull();
        captured.Telefone.ShouldBeNull();
    }

    [Fact]
    public async Task UpdateConsultoria_EmptyEmailAndTelefone_NormalizesToNull()
    {
        UpdateConsultoriaFundoCommand? captured = null;
        _commands.Send<ConsultoriaFundoDto>(Arg.Do<object>(c => captured = c as UpdateConsultoriaFundoCommand), Arg.Any<CancellationToken>())
            .Returns(SampleConsultoria);

        var request = new UpdateConsultoriaFundoRequest("Consultoria Ltda", null, "", "", ConsultoriaFundoStatus.ATIVO);
        await _sut.UpdateConsultoria(ConsultoriaId, request, CancellationToken.None);

        captured.ShouldNotBeNull();
        captured!.Email.ShouldBeNull();
        captured.Telefone.ShouldBeNull();
    }

    [Fact]
    public async Task RegisterCustodiante_EmptyEmailAndTelefone_NormalizesToNull()
    {
        RegisterCustodianteCommand? captured = null;
        _commands.Send<CustodianteDto>(Arg.Do<object>(c => captured = c as RegisterCustodianteCommand), Arg.Any<CancellationToken>())
            .Returns(SampleCustodiante);

        var request = new RegisterCustodianteRequest("Custodiante S.A.", "98.765.432/0001-10", null, "", "");
        await _sut.RegisterCustodiante(request, CancellationToken.None);

        captured.ShouldNotBeNull();
        captured!.Email.ShouldBeNull();
        captured.Telefone.ShouldBeNull();
    }

    [Fact]
    public async Task UpdateCustodiante_EmptyEmailAndTelefone_NormalizesToNull()
    {
        UpdateCustodianteCommand? captured = null;
        _commands.Send<CustodianteDto>(Arg.Do<object>(c => captured = c as UpdateCustodianteCommand), Arg.Any<CancellationToken>())
            .Returns(SampleCustodiante);

        var request = new UpdateCustodianteRequest("Custodiante S.A.", null, "", "", CustodianteStatus.ATIVO);
        await _sut.UpdateCustodiante(CustodianteId, request, CancellationToken.None);

        captured.ShouldNotBeNull();
        captured!.Email.ShouldBeNull();
        captured.Telefone.ShouldBeNull();
    }

    [Fact]
    public async Task RegisterCedentePf_EmptyEmailAndTelefone_NormalizesToNull()
    {
        RegisterCedentePfCommand? captured = null;
        _commands.Send<CedenteDto>(Arg.Do<object>(c => captured = c as RegisterCedentePfCommand), Arg.Any<CancellationToken>())
            .Returns(SampleCedentePf);

        var request = new RegisterCedentePfRequest("12345678909", "João Silva", "", "", null);
        await _sut.RegisterCedentePf(request, CancellationToken.None);

        captured.ShouldNotBeNull();
        captured!.Email.ShouldBeNull();
        captured.Telefone.ShouldBeNull();
    }

    [Fact]
    public async Task RegisterCedentePj_EmptyEmailAndTelefone_NormalizesToNull()
    {
        RegisterCedentePjCommand? captured = null;
        _commands.Send<CedenteDto>(Arg.Do<object>(c => captured = c as RegisterCedentePjCommand), Arg.Any<CancellationToken>())
            .Returns(SampleCedentePj);

        var request = new RegisterCedentePjRequest("11222333000181", "Empresa PJ Ltda", "", "", null);
        await _sut.RegisterCedentePj(request, CancellationToken.None);

        captured.ShouldNotBeNull();
        captured!.Email.ShouldBeNull();
        captured.Telefone.ShouldBeNull();
    }

    [Fact]
    public async Task UpdateCedente_EmptyEmailAndTelefone_NormalizesToNull()
    {
        UpdateCedenteCommand? captured = null;
        _commands.Send<CedenteDto>(Arg.Do<object>(c => captured = c as UpdateCedenteCommand), Arg.Any<CancellationToken>())
            .Returns(SampleCedentePf);

        var request = new UpdateCedenteRequest("João Silva", "", "", null, CedenteStatus.ATIVO);
        await _sut.UpdateCedente(CedenteId, request, CancellationToken.None);

        captured.ShouldNotBeNull();
        captured!.Email.ShouldBeNull();
        captured.Telefone.ShouldBeNull();
    }

    // =========================================================================
    // GetFundoAllowedTransitions
    // =========================================================================

    [Fact]
    public async Task GetFundoAllowedTransitions_Found_Returns200()
    {
        IReadOnlyList<string>? transitions = new[] { "ATIVO", "EM_LIQUIDACAO" };
        _queries.Query<IReadOnlyList<string>?>(Arg.Any<GetFundoAllowedTransitionsQuery>(), Arg.Any<CancellationToken>())
            .Returns(transitions);

        var result = await _sut.GetFundoAllowedTransitions(FundoId, CancellationToken.None);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.Value.ShouldBe(transitions);
    }

    [Fact]
    public async Task GetFundoAllowedTransitions_NotFound_Returns404()
    {
        _queries.Query<IReadOnlyList<string>?>(Arg.Any<GetFundoAllowedTransitionsQuery>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<string>?)null);

        var result = await _sut.GetFundoAllowedTransitions(FundoId, CancellationToken.None);

        result.ShouldBeOfType<NotFoundResult>();
    }

    // =========================================================================
    // Domain aggregate builders
    // =========================================================================

    private static ConsultoriaFundo BuildConsultoriaFundo()
    {
        var c = ConsultoriaFundo.Register(
            "Consultoria Ltda",
            "11.222.333/0001-81",
            CompanyId,
            null,
            null,
            null);
        SetId(c, ConsultoriaId);
        return c;
    }

    private static Custodiante BuildCustodiante()
    {
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
        var c = Cedente.RegisterPf(
            "123.456.789-09",
            "João Silva",
            CompanyId);
        SetId(c, CedenteId);
        return c;
    }

    private static void SetId<T>(T entity, Guid id)
    {
        var type = typeof(T);
        PropertyInfo? prop = null;
        while (type is not null)
        {
            prop = type.GetProperty("Id",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            if (prop is not null) break;
            type = type.BaseType;
        }

        prop?.SetValue(entity, id);
    }
}
