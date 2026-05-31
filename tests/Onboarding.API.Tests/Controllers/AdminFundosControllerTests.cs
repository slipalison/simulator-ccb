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
/// Unit tests for AdminFundosController — Phase 55 refactor (D-60..D-63).
/// D-62: 1 ctor dep (IQueryDispatcher) — was 11. All query-only.
/// </summary>
public class AdminFundosControllerTests
{
    private readonly IQueryDispatcher _queries = Substitute.For<IQueryDispatcher>();
    private readonly AdminFundosController _sut;

    private static readonly Guid CompanyA = Guid.NewGuid();
    private static readonly Guid CompanyB = Guid.NewGuid();
    private static readonly DateTimeOffset FixedTs = new(2026, 5, 12, 0, 0, 0, TimeSpan.Zero);

    private static readonly AdminFundoDto FundoFromCompanyA =
        new(Guid.NewGuid(), CompanyA, "Empresa Alpha Ltda", "Fundo Alpha",
            "11222333000181", Guid.NewGuid(), Guid.NewGuid(), TipoFundo.RendaFixa,
            null, null, null, FundoStatus.ATIVO, FixedTs);

    private static readonly AdminFundoDto FundoFromCompanyB =
        new(Guid.NewGuid(), CompanyB, "Beta Investimentos S.A.", "Fundo Beta",
            "44555666000144", Guid.NewGuid(), Guid.NewGuid(), TipoFundo.Multimercado,
            null, null, null, FundoStatus.RASCUNHO, FixedTs);

    private static readonly AdminConsultoriaFundoDto ConsultoriaFromA =
        new(Guid.NewGuid(), CompanyA, "Empresa Alpha Ltda", "Consultoria Alpha",
            null, "11222333000181", null, null, ConsultoriaFundoStatus.ATIVO, FixedTs);

    private static readonly AdminConsultoriaFundoDto ConsultoriaFromB =
        new(Guid.NewGuid(), CompanyB, "Beta Investimentos S.A.", "Consultoria Beta",
            null, "44555666000144", null, null, ConsultoriaFundoStatus.ATIVO, FixedTs);

    private static readonly AdminCustodianteDto CustodianteFromA =
        new(Guid.NewGuid(), CompanyA, "Empresa Alpha Ltda", "Custodiante Alpha",
            null, "11222333000181", null, null, CustodianteStatus.ATIVO, FixedTs);

    private static readonly AdminCustodianteDto CustodianteFromB =
        new(Guid.NewGuid(), CompanyB, "Beta Investimentos S.A.", "Custodiante Beta",
            null, "44555666000144", null, null, CustodianteStatus.ATIVO, FixedTs);

    private static readonly AdminCedenteDto CedenteFromA =
        new(Guid.NewGuid(), CompanyA, "Empresa Alpha Ltda", "12345678901",
            "Joao Silva", null, null, null, CedenteTipo.PF, CedenteStatus.ATIVO, FixedTs);

    private static readonly AdminCedenteDto CedenteFromB =
        new(Guid.NewGuid(), CompanyB, "Beta Investimentos S.A.", "99888777000166",
            "Cedente Beta LTDA", null, null, null, CedenteTipo.PJ, CedenteStatus.ATIVO, FixedTs);

    public AdminFundosControllerTests()
    {
        _sut = new AdminFundosController(_queries);

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
        var method = typeof(AdminFundosController)
            .GetMethods()
            .Single(m => m.Name == methodName);

        var allowAnon = method.GetCustomAttribute<AllowAnonymousAttribute>();
        allowAnon.ShouldBeNull($"Method {methodName} must NOT have [AllowAnonymous]");
    }

    // =========================================================================
    // Security: No mutation endpoints exist (D-8)
    // =========================================================================

    [Fact]
    public void Controller_HasNoHttpPost_HttpPut_HttpDelete_Endpoints()
    {
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
        var crossCompanyResult = new PaginatedResult<AdminFundoDto>(
            [FundoFromCompanyA, FundoFromCompanyB], 2, 1, 20);
        _queries.Query<PaginatedResult<AdminFundoDto>>(Arg.Any<ListAdminFundoQuery>(), Arg.Any<CancellationToken>())
            .Returns(crossCompanyResult);

        var actionResult = await _sut.ListFundos(ct: CancellationToken.None);

        var okResult = actionResult.ShouldBeOfType<OkObjectResult>();
        var result = okResult.Value.ShouldBeOfType<PaginatedResult<AdminFundoDto>>();

        result.TotalCount.ShouldBe(2);
        result.Items.Count.ShouldBe(2);

        var clientIds = result.Items.Select(f => f.ClienteId).Distinct().ToList();
        clientIds.Count.ShouldBe(2);
        clientIds.ShouldContain(CompanyA);
        clientIds.ShouldContain(CompanyB);
        result.Items.ShouldAllBe(f => !string.IsNullOrWhiteSpace(f.EmpresaNome));
    }

    [Fact]
    public async Task ListConsultorias_CrossCompany_ReturnsBothCompanyAAndCompanyBInSameResponse()
    {
        var crossCompanyResult = new PaginatedResult<AdminConsultoriaFundoDto>(
            [ConsultoriaFromA, ConsultoriaFromB], 2, 1, 20);
        _queries.Query<PaginatedResult<AdminConsultoriaFundoDto>>(Arg.Any<ListAdminConsultoriaQuery>(), Arg.Any<CancellationToken>())
            .Returns(crossCompanyResult);

        var actionResult = await _sut.ListConsultorias(ct: CancellationToken.None);

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
        var crossCompanyResult = new PaginatedResult<AdminCustodianteDto>(
            [CustodianteFromA, CustodianteFromB], 2, 1, 20);
        _queries.Query<PaginatedResult<AdminCustodianteDto>>(Arg.Any<ListAdminCustodianteQuery>(), Arg.Any<CancellationToken>())
            .Returns(crossCompanyResult);

        var actionResult = await _sut.ListCustodiantes(ct: CancellationToken.None);

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
        var crossCompanyResult = new PaginatedResult<AdminCedenteDto>(
            [CedenteFromA, CedenteFromB], 2, 1, 20);
        _queries.Query<PaginatedResult<AdminCedenteDto>>(Arg.Any<ListAdminCedenteQuery>(), Arg.Any<CancellationToken>())
            .Returns(crossCompanyResult);

        var actionResult = await _sut.ListCedentes(ct: CancellationToken.None);

        var okResult = actionResult.ShouldBeOfType<OkObjectResult>();
        var result = okResult.Value.ShouldBeOfType<PaginatedResult<AdminCedenteDto>>();
        result.TotalCount.ShouldBe(2);
        var clientIds = result.Items.Select(c => c.ClienteId).Distinct().ToList();
        clientIds.Count.ShouldBe(2);
        clientIds.ShouldContain(CompanyA);
        clientIds.ShouldContain(CompanyB);
        result.Items.ShouldContain(c => c.CedenteTipo == CedenteTipo.PF);
        result.Items.ShouldContain(c => c.CedenteTipo == CedenteTipo.PJ);
        result.Items.ShouldAllBe(c => !string.IsNullOrWhiteSpace(c.EmpresaNome));
    }

    // =========================================================================
    // Happy path: 200 OK + correct pagination
    // =========================================================================

    [Fact]
    public async Task ListFundos_DefaultParameters_Returns200WithPaginatedResult()
    {
        var emptyResult = new PaginatedResult<AdminFundoDto>([], 0, 1, 20);
        _queries.Query<PaginatedResult<AdminFundoDto>>(Arg.Any<ListAdminFundoQuery>(), Arg.Any<CancellationToken>())
            .Returns(emptyResult);

        var actionResult = await _sut.ListFundos(ct: CancellationToken.None);

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
        _queries.Query<PaginatedResult<AdminConsultoriaFundoDto>>(Arg.Any<ListAdminConsultoriaQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PaginatedResult<AdminConsultoriaFundoDto>([], 0, 1, 20));

        var actionResult = await _sut.ListConsultorias(ct: CancellationToken.None);
        actionResult.ShouldBeOfType<OkObjectResult>().StatusCode.ShouldBe(200);
    }

    [Fact]
    public async Task ListCustodiantes_DefaultParameters_Returns200WithPaginatedResult()
    {
        _queries.Query<PaginatedResult<AdminCustodianteDto>>(Arg.Any<ListAdminCustodianteQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PaginatedResult<AdminCustodianteDto>([], 0, 1, 20));

        var actionResult = await _sut.ListCustodiantes(ct: CancellationToken.None);
        actionResult.ShouldBeOfType<OkObjectResult>().StatusCode.ShouldBe(200);
    }

    [Fact]
    public async Task ListCedentes_DefaultParameters_Returns200WithPaginatedResult()
    {
        _queries.Query<PaginatedResult<AdminCedenteDto>>(Arg.Any<ListAdminCedenteQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PaginatedResult<AdminCedenteDto>([], 0, 1, 20));

        var actionResult = await _sut.ListCedentes(ct: CancellationToken.None);
        actionResult.ShouldBeOfType<OkObjectResult>().StatusCode.ShouldBe(200);
    }

    // =========================================================================
    // Query parameter forwarding
    // =========================================================================

    [Fact]
    public async Task ListFundos_QueryParameters_ForwardedToDispatcher()
    {
        var targetCompanyId = Guid.NewGuid();
        ListAdminFundoQuery? captured = null;
        _queries.Query<PaginatedResult<AdminFundoDto>>(
            Arg.Do<object>(q => captured = q as ListAdminFundoQuery),
            Arg.Any<CancellationToken>())
            .Returns(new PaginatedResult<AdminFundoDto>([], 0, 2, 10));

        await _sut.ListFundos(page: 2, pageSize: 10, search: "alpha", companyId: targetCompanyId);

        captured.ShouldNotBeNull();
        captured!.Page.ShouldBe(2);
        captured.PageSize.ShouldBe(10);
        captured.Search.ShouldBe("alpha");
        captured.CompanyId.ShouldBe(targetCompanyId);
    }

    [Fact]
    public async Task ListConsultorias_QueryParameters_ForwardedToDispatcher()
    {
        var targetCompanyId = Guid.NewGuid();
        ListAdminConsultoriaQuery? captured = null;
        _queries.Query<PaginatedResult<AdminConsultoriaFundoDto>>(
            Arg.Do<object>(q => captured = q as ListAdminConsultoriaQuery),
            Arg.Any<CancellationToken>())
            .Returns(new PaginatedResult<AdminConsultoriaFundoDto>([], 0, 3, 5));

        await _sut.ListConsultorias(page: 3, pageSize: 5, search: "beta", companyId: targetCompanyId);

        captured.ShouldNotBeNull();
        captured!.Page.ShouldBe(3);
        captured.PageSize.ShouldBe(5);
        captured.Search.ShouldBe("beta");
        captured.CompanyId.ShouldBe(targetCompanyId);
    }

    [Fact]
    public async Task ListCustodiantes_QueryParameters_ForwardedToDispatcher()
    {
        ListAdminCustodianteQuery? captured = null;
        _queries.Query<PaginatedResult<AdminCustodianteDto>>(
            Arg.Do<object>(q => captured = q as ListAdminCustodianteQuery),
            Arg.Any<CancellationToken>())
            .Returns(new PaginatedResult<AdminCustodianteDto>([], 0, 1, 20));

        await _sut.ListCustodiantes(page: 1, pageSize: 20, search: "custodiante");

        captured.ShouldNotBeNull();
        captured!.Search.ShouldBe("custodiante");
        captured.CompanyId.ShouldBeNull();
    }

    [Fact]
    public async Task ListCedentes_QueryParameters_ForwardedToDispatcher()
    {
        ListAdminCedenteQuery? captured = null;
        _queries.Query<PaginatedResult<AdminCedenteDto>>(
            Arg.Do<object>(q => captured = q as ListAdminCedenteQuery),
            Arg.Any<CancellationToken>())
            .Returns(new PaginatedResult<AdminCedenteDto>([], 0, 1, 20));

        await _sut.ListCedentes(page: 1, pageSize: 20, search: "joao");

        captured.ShouldNotBeNull();
        captured!.Search.ShouldBe("joao");
        captured.CompanyId.ShouldBeNull();
    }
}
