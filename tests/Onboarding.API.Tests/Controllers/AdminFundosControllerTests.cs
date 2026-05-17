using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Onboarding.API.Controllers;
using Onboarding.API.Security;
using Onboarding.Application.Common;
using Onboarding.Application.Fundos.Queries.Admin;
using Onboarding.Domain.Aggregates.CedenteAggregate;
using Onboarding.Domain.Aggregates.ConsultoriaFundoAggregate;
using Onboarding.Domain.Aggregates.CustodianteAggregate;
using Onboarding.Domain.Aggregates.FundoAggregate;
using Shouldly;

namespace Onboarding.API.Tests.Controllers;

/// <summary>
/// Unit tests for AdminFundosController — cross-company read-only listing (D-8, T-48.6).
///
/// Security invariants verified:
/// - Class-level [Authorize(BearerBackoffice + CrossCompanyAccess)] on every endpoint.
/// - No mutation endpoints exist (D-8).
///
/// Cross-company isolation verified:
/// - Handlers can return rows from multiple different ClientIds in the same response.
/// - No tenant filter applied — results include entities from CompanyA and CompanyB.
///
/// Happy-path tests verify 200 OK + correct PaginatedResult structure forwarded to caller.
/// </summary>
public class AdminFundosControllerTests
{
    // -------------------------------------------------------------------------
    // Mocks
    // -------------------------------------------------------------------------

    private readonly IQueryHandler<ListAdminFundoQuery, PaginatedResult<AdminFundoDto>> _listFundoHandler =
        Substitute.For<IQueryHandler<ListAdminFundoQuery, PaginatedResult<AdminFundoDto>>>();

    private readonly IQueryHandler<ListAdminConsultoriaQuery, PaginatedResult<AdminConsultoriaFundoDto>> _listConsultoriaHandler =
        Substitute.For<IQueryHandler<ListAdminConsultoriaQuery, PaginatedResult<AdminConsultoriaFundoDto>>>();

    private readonly IQueryHandler<ListAdminCustodianteQuery, PaginatedResult<AdminCustodianteDto>> _listCustodianteHandler =
        Substitute.For<IQueryHandler<ListAdminCustodianteQuery, PaginatedResult<AdminCustodianteDto>>>();

    private readonly IQueryHandler<ListAdminCedenteQuery, PaginatedResult<AdminCedenteDto>> _listCedenteHandler =
        Substitute.For<IQueryHandler<ListAdminCedenteQuery, PaginatedResult<AdminCedenteDto>>>();

    // Phase 50 — relationship aggregate admin query handlers
    private readonly IQueryHandler<ListAdminFundoCedenteQuery, PaginatedResult<AdminRelFundoCedenteDto>> _listFundoCedenteHandler =
        Substitute.For<IQueryHandler<ListAdminFundoCedenteQuery, PaginatedResult<AdminRelFundoCedenteDto>>>();
    private readonly IQueryHandler<ListAdminFundoTipoAtivoQuery, PaginatedResult<AdminRelFundoTipoAtivoDto>> _listFundoTipoAtivoHandler =
        Substitute.For<IQueryHandler<ListAdminFundoTipoAtivoQuery, PaginatedResult<AdminRelFundoTipoAtivoDto>>>();
    private readonly IQueryHandler<ListAdminCedenteTipoAtivoQuery, PaginatedResult<AdminRelCedenteTipoAtivoDto>> _listCedenteTipoAtivoHandler =
        Substitute.For<IQueryHandler<ListAdminCedenteTipoAtivoQuery, PaginatedResult<AdminRelCedenteTipoAtivoDto>>>();

    private readonly AdminFundosController _sut;

    // -------------------------------------------------------------------------
    // Test data — two distinct companies to prove cross-company isolation
    // -------------------------------------------------------------------------

    private static readonly Guid CompanyA = Guid.NewGuid();
    private static readonly Guid CompanyB = Guid.NewGuid();
    private static readonly DateTimeOffset FixedTs = new(2026, 5, 12, 0, 0, 0, TimeSpan.Zero);

    // Fundo samples — one per company
    private static readonly AdminFundoDto FundoFromCompanyA =
        new(Guid.NewGuid(), CompanyA, "Empresa Alpha Ltda", "Fundo Alpha",
            "11222333000181", Guid.NewGuid(), Guid.NewGuid(), TipoFundo.RendaFixa,
            null, null, null, FundoStatus.ATIVO, FixedTs);

    private static readonly AdminFundoDto FundoFromCompanyB =
        new(Guid.NewGuid(), CompanyB, "Beta Investimentos S.A.", "Fundo Beta",
            "44555666000144", Guid.NewGuid(), Guid.NewGuid(), TipoFundo.Multimercado,
            null, null, null, FundoStatus.RASCUNHO, FixedTs);

    // ConsultoriaFundo samples — one per company
    private static readonly AdminConsultoriaFundoDto ConsultoriaFromA =
        new(Guid.NewGuid(), CompanyA, "Empresa Alpha Ltda", "Consultoria Alpha",
            null, "11222333000181", null, null, ConsultoriaFundoStatus.ATIVO, FixedTs);

    private static readonly AdminConsultoriaFundoDto ConsultoriaFromB =
        new(Guid.NewGuid(), CompanyB, "Beta Investimentos S.A.", "Consultoria Beta",
            null, "44555666000144", null, null, ConsultoriaFundoStatus.ATIVO, FixedTs);

    // Custodiante samples — one per company
    private static readonly AdminCustodianteDto CustodianteFromA =
        new(Guid.NewGuid(), CompanyA, "Empresa Alpha Ltda", "Custodiante Alpha",
            null, "11222333000181", null, null, CustodianteStatus.ATIVO, FixedTs);

    private static readonly AdminCustodianteDto CustodianteFromB =
        new(Guid.NewGuid(), CompanyB, "Beta Investimentos S.A.", "Custodiante Beta",
            null, "44555666000144", null, null, CustodianteStatus.ATIVO, FixedTs);

    // Cedente samples — one PF from CompanyA, one PJ from CompanyB
    private static readonly AdminCedenteDto CedenteFromA =
        new(Guid.NewGuid(), CompanyA, "Empresa Alpha Ltda", "12345678901",
            "Joao Silva", null, null, null, CedenteTipo.PF, CedenteStatus.ATIVO, FixedTs);

    private static readonly AdminCedenteDto CedenteFromB =
        new(Guid.NewGuid(), CompanyB, "Beta Investimentos S.A.", "99888777000166",
            "Cedente Beta LTDA", null, null, null, CedenteTipo.PJ, CedenteStatus.ATIVO, FixedTs);

    public AdminFundosControllerTests()
    {
        _sut = new AdminFundosController(
            _listFundoHandler,
            _listConsultoriaHandler,
            _listCustodianteHandler,
            _listCedenteHandler,
            _listFundoCedenteHandler,
            _listFundoTipoAtivoHandler,
            _listCedenteTipoAtivoHandler);

        // Admin JWT context (BearerBackoffice sub)
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim("sub", "admin-sub-xyz"),
                    new Claim("email", "admin@backoffice.com")
                }, "BearerBackoffice"))
            }
        };
    }

    // =========================================================================
    // Security: Class-level AuthZ invariant
    // =========================================================================

    [Fact]
    public void Controller_HasBearerBackofficeScheme_AndCrossCompanyAccessPolicy()
    {
        // The class-level [Authorize] must require BearerBackoffice + CrossCompanyAccess.
        // If either is missing, all endpoints are unsecured — a critical multi-tenant leak (D-8).
        var classAttr = typeof(AdminFundosController)
            .GetCustomAttribute<AuthorizeAttribute>();

        classAttr.ShouldNotBeNull("AdminFundosController must have class-level [Authorize]");
        classAttr!.AuthenticationSchemes.ShouldBe("BearerBackoffice");
        classAttr.Policy.ShouldBe(PermissionPolicies.CrossCompanyAccess);
    }

    [Theory]
    [InlineData(nameof(AdminFundosController.ListFundos))]
    [InlineData(nameof(AdminFundosController.ListConsultorias))]
    [InlineData(nameof(AdminFundosController.ListCustodiantes))]
    [InlineData(nameof(AdminFundosController.ListCedentes))]
    [InlineData(nameof(AdminFundosController.ListFundoCedentes))]
    [InlineData(nameof(AdminFundosController.ListFundoTiposAtivos))]
    [InlineData(nameof(AdminFundosController.ListCedenteTiposAtivos))]
    public void Endpoint_DoesNotOverrideClassLevelAuthorize_WithLessRestrictiveAttribute(string methodName)
    {
        // Verify no endpoint-level [AllowAnonymous] or weaker [Authorize] overrides the class attribute.
        var method = typeof(AdminFundosController)
            .GetMethods()
            .Single(m => m.Name == methodName);

        var allowAnon = method.GetCustomAttribute<AllowAnonymousAttribute>();
        allowAnon.ShouldBeNull($"Method {methodName} must NOT have [AllowAnonymous] — it overrides CrossCompanyAccess.");
    }

    // =========================================================================
    // Security: No mutation endpoints exist (D-8)
    // =========================================================================

    [Fact]
    public void Controller_HasNoHttpPost_HttpPut_HttpDelete_Endpoints()
    {
        // D-8 mandates admin fundos controller is read-only. Any mutation endpoint is a violation.
        var methods = typeof(AdminFundosController).GetMethods(BindingFlags.Public | BindingFlags.Instance);

        var mutationMethods = methods.Where(m =>
            m.GetCustomAttribute<HttpPostAttribute>() != null ||
            m.GetCustomAttribute<HttpPutAttribute>() != null ||
            m.GetCustomAttribute<HttpDeleteAttribute>() != null ||
            m.GetCustomAttribute<HttpPatchAttribute>() != null).ToList();

        mutationMethods.ShouldBeEmpty(
            $"AdminFundosController must have no mutation endpoints per D-8. Found: {string.Join(", ", mutationMethods.Select(m => m.Name))}");
    }

    // =========================================================================
    // Cross-company: Handler returns rows from 2 different ClientIds
    // =========================================================================

    [Fact]
    public async Task ListFundos_CrossCompany_ReturnsBothCompanyAAndCompanyBInSameResponse()
    {
        // Arrange — handler returns Fundos from two distinct companies (proves no tenant filter).
        var crossCompanyResult = new PaginatedResult<AdminFundoDto>(
            [FundoFromCompanyA, FundoFromCompanyB], 2, 1, 20);

        _listFundoHandler
            .HandleAsync(Arg.Any<ListAdminFundoQuery>(), Arg.Any<CancellationToken>())
            .Returns(crossCompanyResult);

        // Act
        var actionResult = await _sut.ListFundos(ct: CancellationToken.None);

        // Assert
        var okResult = actionResult.ShouldBeOfType<OkObjectResult>();
        var result = okResult.Value.ShouldBeOfType<PaginatedResult<AdminFundoDto>>();

        result.TotalCount.ShouldBe(2);
        result.Items.Count.ShouldBe(2);

        // The key cross-company assertion: different ClientIds in the same response
        var clientIds = result.Items.Select(f => f.ClienteId).Distinct().ToList();
        clientIds.Count.ShouldBe(2, "Cross-company response must contain rows from at least 2 different ClientIds");
        clientIds.ShouldContain(CompanyA);
        clientIds.ShouldContain(CompanyB);

        // EmpresaNome must be populated for both
        result.Items.ShouldAllBe(f => !string.IsNullOrWhiteSpace(f.EmpresaNome));
    }

    [Fact]
    public async Task ListConsultorias_CrossCompany_ReturnsBothCompanyAAndCompanyBInSameResponse()
    {
        // Arrange
        var crossCompanyResult = new PaginatedResult<AdminConsultoriaFundoDto>(
            [ConsultoriaFromA, ConsultoriaFromB], 2, 1, 20);

        _listConsultoriaHandler
            .HandleAsync(Arg.Any<ListAdminConsultoriaQuery>(), Arg.Any<CancellationToken>())
            .Returns(crossCompanyResult);

        // Act
        var actionResult = await _sut.ListConsultorias(ct: CancellationToken.None);

        // Assert
        var okResult = actionResult.ShouldBeOfType<OkObjectResult>();
        var result = okResult.Value.ShouldBeOfType<PaginatedResult<AdminConsultoriaFundoDto>>();

        result.TotalCount.ShouldBe(2);

        var clientIds = result.Items.Select(c => c.ClienteId).Distinct().ToList();
        clientIds.Count.ShouldBe(2);
        clientIds.ShouldContain(CompanyA);
        clientIds.ShouldContain(CompanyB);
        result.Items.ShouldAllBe(c => !string.IsNullOrWhiteSpace(c.EmpresaNome));
    }

    [Fact]
    public async Task ListCustodiantes_CrossCompany_ReturnsBothCompanyAAndCompanyBInSameResponse()
    {
        // Arrange
        var crossCompanyResult = new PaginatedResult<AdminCustodianteDto>(
            [CustodianteFromA, CustodianteFromB], 2, 1, 20);

        _listCustodianteHandler
            .HandleAsync(Arg.Any<ListAdminCustodianteQuery>(), Arg.Any<CancellationToken>())
            .Returns(crossCompanyResult);

        // Act
        var actionResult = await _sut.ListCustodiantes(ct: CancellationToken.None);

        // Assert
        var okResult = actionResult.ShouldBeOfType<OkObjectResult>();
        var result = okResult.Value.ShouldBeOfType<PaginatedResult<AdminCustodianteDto>>();

        result.TotalCount.ShouldBe(2);

        var clientIds = result.Items.Select(c => c.ClienteId).Distinct().ToList();
        clientIds.Count.ShouldBe(2);
        clientIds.ShouldContain(CompanyA);
        clientIds.ShouldContain(CompanyB);
        result.Items.ShouldAllBe(c => !string.IsNullOrWhiteSpace(c.EmpresaNome));
    }

    [Fact]
    public async Task ListCedentes_CrossCompany_ReturnsBothCompanyAAndCompanyBInSameResponse()
    {
        // Arrange — CedenteFromA is PF (CPF), CedenteFromB is PJ (CNPJ)
        var crossCompanyResult = new PaginatedResult<AdminCedenteDto>(
            [CedenteFromA, CedenteFromB], 2, 1, 20);

        _listCedenteHandler
            .HandleAsync(Arg.Any<ListAdminCedenteQuery>(), Arg.Any<CancellationToken>())
            .Returns(crossCompanyResult);

        // Act
        var actionResult = await _sut.ListCedentes(ct: CancellationToken.None);

        // Assert
        var okResult = actionResult.ShouldBeOfType<OkObjectResult>();
        var result = okResult.Value.ShouldBeOfType<PaginatedResult<AdminCedenteDto>>();

        result.TotalCount.ShouldBe(2);

        var clientIds = result.Items.Select(c => c.ClienteId).Distinct().ToList();
        clientIds.Count.ShouldBe(2);
        clientIds.ShouldContain(CompanyA);
        clientIds.ShouldContain(CompanyB);

        // CedenteTipo should be populated
        result.Items.ShouldContain(c => c.CedenteTipo == CedenteTipo.PF);
        result.Items.ShouldContain(c => c.CedenteTipo == CedenteTipo.PJ);
        result.Items.ShouldAllBe(c => !string.IsNullOrWhiteSpace(c.EmpresaNome));
    }

    // =========================================================================
    // Happy path: 200 OK + correct pagination forwarding
    // =========================================================================

    [Fact]
    public async Task ListFundos_DefaultParameters_Returns200WithPaginatedResult()
    {
        // Arrange
        var emptyResult = new PaginatedResult<AdminFundoDto>([], 0, 1, 20);
        _listFundoHandler
            .HandleAsync(Arg.Any<ListAdminFundoQuery>(), Arg.Any<CancellationToken>())
            .Returns(emptyResult);

        // Act
        var actionResult = await _sut.ListFundos(ct: CancellationToken.None);

        // Assert
        var okResult = actionResult.ShouldBeOfType<OkObjectResult>();
        okResult.StatusCode.ShouldBe(200);
        var result = okResult.Value.ShouldBeOfType<PaginatedResult<AdminFundoDto>>();
        result.Page.ShouldBe(1);
        result.PageSize.ShouldBe(20);
        result.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task ListConsultorias_DefaultParameters_Returns200WithPaginatedResult()
    {
        var emptyResult = new PaginatedResult<AdminConsultoriaFundoDto>([], 0, 1, 20);
        _listConsultoriaHandler
            .HandleAsync(Arg.Any<ListAdminConsultoriaQuery>(), Arg.Any<CancellationToken>())
            .Returns(emptyResult);

        var actionResult = await _sut.ListConsultorias(ct: CancellationToken.None);

        var okResult = actionResult.ShouldBeOfType<OkObjectResult>();
        okResult.StatusCode.ShouldBe(200);
    }

    [Fact]
    public async Task ListCustodiantes_DefaultParameters_Returns200WithPaginatedResult()
    {
        var emptyResult = new PaginatedResult<AdminCustodianteDto>([], 0, 1, 20);
        _listCustodianteHandler
            .HandleAsync(Arg.Any<ListAdminCustodianteQuery>(), Arg.Any<CancellationToken>())
            .Returns(emptyResult);

        var actionResult = await _sut.ListCustodiantes(ct: CancellationToken.None);

        var okResult = actionResult.ShouldBeOfType<OkObjectResult>();
        okResult.StatusCode.ShouldBe(200);
    }

    [Fact]
    public async Task ListCedentes_DefaultParameters_Returns200WithPaginatedResult()
    {
        var emptyResult = new PaginatedResult<AdminCedenteDto>([], 0, 1, 20);
        _listCedenteHandler
            .HandleAsync(Arg.Any<ListAdminCedenteQuery>(), Arg.Any<CancellationToken>())
            .Returns(emptyResult);

        var actionResult = await _sut.ListCedentes(ct: CancellationToken.None);

        var okResult = actionResult.ShouldBeOfType<OkObjectResult>();
        okResult.StatusCode.ShouldBe(200);
    }

    // =========================================================================
    // Query parameter forwarding
    // =========================================================================

    [Fact]
    public async Task ListFundos_QueryParameters_ForwardedToHandler()
    {
        // Arrange
        var targetCompanyId = Guid.NewGuid();
        ListAdminFundoQuery? captured = null;
        _listFundoHandler
            .HandleAsync(Arg.Do<ListAdminFundoQuery>(q => captured = q), Arg.Any<CancellationToken>())
            .Returns(new PaginatedResult<AdminFundoDto>([], 0, 2, 10));

        // Act
        await _sut.ListFundos(page: 2, pageSize: 10, search: "alpha", companyId: targetCompanyId);

        // Assert
        captured.ShouldNotBeNull();
        captured!.Page.ShouldBe(2);
        captured.PageSize.ShouldBe(10);
        captured.Search.ShouldBe("alpha");
        captured.CompanyId.ShouldBe(targetCompanyId);
    }

    [Fact]
    public async Task ListConsultorias_QueryParameters_ForwardedToHandler()
    {
        var targetCompanyId = Guid.NewGuid();
        ListAdminConsultoriaQuery? captured = null;
        _listConsultoriaHandler
            .HandleAsync(Arg.Do<ListAdminConsultoriaQuery>(q => captured = q), Arg.Any<CancellationToken>())
            .Returns(new PaginatedResult<AdminConsultoriaFundoDto>([], 0, 3, 5));

        await _sut.ListConsultorias(page: 3, pageSize: 5, search: "beta", companyId: targetCompanyId);

        captured.ShouldNotBeNull();
        captured!.Page.ShouldBe(3);
        captured.PageSize.ShouldBe(5);
        captured.Search.ShouldBe("beta");
        captured.CompanyId.ShouldBe(targetCompanyId);
    }

    [Fact]
    public async Task ListCustodiantes_QueryParameters_ForwardedToHandler()
    {
        ListAdminCustodianteQuery? captured = null;
        _listCustodianteHandler
            .HandleAsync(Arg.Do<ListAdminCustodianteQuery>(q => captured = q), Arg.Any<CancellationToken>())
            .Returns(new PaginatedResult<AdminCustodianteDto>([], 0, 1, 20));

        await _sut.ListCustodiantes(page: 1, pageSize: 20, search: "custodiante");

        captured.ShouldNotBeNull();
        captured!.Search.ShouldBe("custodiante");
        captured.CompanyId.ShouldBeNull();
    }

    [Fact]
    public async Task ListCedentes_QueryParameters_ForwardedToHandler()
    {
        ListAdminCedenteQuery? captured = null;
        _listCedenteHandler
            .HandleAsync(Arg.Do<ListAdminCedenteQuery>(q => captured = q), Arg.Any<CancellationToken>())
            .Returns(new PaginatedResult<AdminCedenteDto>([], 0, 1, 20));

        await _sut.ListCedentes(page: 1, pageSize: 20, search: "joao");

        captured.ShouldNotBeNull();
        captured!.Search.ShouldBe("joao");
        captured.CompanyId.ShouldBeNull();
    }
}
