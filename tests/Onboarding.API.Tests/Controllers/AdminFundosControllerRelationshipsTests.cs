using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Onboarding.API.Controllers;
using Onboarding.Application.Common;
using Onboarding.Application.Fundos.Queries.Admin;
using Onboarding.Domain.Aggregates.FundoCedenteAggregate;
using Shouldly;

namespace Onboarding.API.Tests.Controllers;

/// <summary>
/// Tests for AdminFundosController Phase 50 relationship aggregate endpoints.
/// Phase 55 refactor: uses IQueryDispatcher (D-62, was 11 ctor deps).
/// </summary>
public class AdminFundosControllerRelationshipsTests
{
    private static readonly Guid CompanyA = Guid.NewGuid();
    private static readonly Guid CompanyB = Guid.NewGuid();
    private static readonly DateTimeOffset FixedTs = new(2026, 5, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly IQueryDispatcher _queries = Substitute.For<IQueryDispatcher>();
    private readonly AdminFundosController _sut;

    private static readonly AdminRelFundoCedenteDto FundoCedenteFromA =
        new(Guid.NewGuid(), CompanyA, "Empresa Alpha", Guid.NewGuid(), "Fundo Alpha",
            Guid.NewGuid(), 50m, null, FixedTs, null, RelationshipStatus.ATIVO, FixedTs);

    private static readonly AdminRelFundoCedenteDto FundoCedenteFromB =
        new(Guid.NewGuid(), CompanyB, "Empresa Beta", Guid.NewGuid(), "Fundo Beta",
            Guid.NewGuid(), null, 100_000m, FixedTs, null, RelationshipStatus.ATIVO, FixedTs);

    private static readonly AdminRelFundoTipoAtivoDto FundoTipoAtivoFromA =
        new(Guid.NewGuid(), CompanyA, "Empresa Alpha", Guid.NewGuid(), "Fundo Alpha",
            Guid.NewGuid(), 30m, null, FixedTs, null, RelationshipStatus.ATIVO, FixedTs);

    private static readonly AdminRelCedenteTipoAtivoDto CedenteTipoAtivoFromB =
        new(Guid.NewGuid(), CompanyB, "Empresa Beta", Guid.NewGuid(), Guid.NewGuid(),
            null, 500_000m, FixedTs, null, RelationshipStatus.INATIVO, FixedTs);

    public AdminFundosControllerRelationshipsTests()
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
    // ListFundoCedentes
    // =========================================================================

    [Fact]
    public async Task ListFundoCedentes_CrossCompany_ReturnsBothCompaniesInSameResponse()
    {
        var crossCompanyResult = new PaginatedResult<AdminRelFundoCedenteDto>(
            [FundoCedenteFromA, FundoCedenteFromB], 2, 1, 20);
        _queries.Query<PaginatedResult<AdminRelFundoCedenteDto>>(Arg.Any<ListAdminFundoCedenteQuery>(), Arg.Any<CancellationToken>())
            .Returns(crossCompanyResult);

        var result = await _sut.ListFundoCedentes(ct: CancellationToken.None);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var paged = ok.Value.ShouldBeOfType<PaginatedResult<AdminRelFundoCedenteDto>>();
        paged.TotalCount.ShouldBe(2);
        paged.Items.Select(x => x.ClienteId).ShouldContain(CompanyA);
        paged.Items.Select(x => x.ClienteId).ShouldContain(CompanyB);
    }

    [Fact]
    public async Task ListFundoCedentes_WithCompanyFilter_PassesFilterToDispatcher()
    {
        ListAdminFundoCedenteQuery? captured = null;
        _queries.Query<PaginatedResult<AdminRelFundoCedenteDto>>(
            Arg.Do<object>(q => captured = q as ListAdminFundoCedenteQuery),
            Arg.Any<CancellationToken>())
            .Returns(new PaginatedResult<AdminRelFundoCedenteDto>([], 0, 1, 20));

        await _sut.ListFundoCedentes(companyId: CompanyA, ct: CancellationToken.None);

        captured.ShouldNotBeNull();
        captured!.CompanyId.ShouldBe(CompanyA);
    }

    [Fact]
    public async Task ListFundoCedentes_EmptyResult_Returns200WithEmptyList()
    {
        _queries.Query<PaginatedResult<AdminRelFundoCedenteDto>>(Arg.Any<ListAdminFundoCedenteQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PaginatedResult<AdminRelFundoCedenteDto>([], 0, 1, 20));

        var result = await _sut.ListFundoCedentes(ct: CancellationToken.None);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var paged = ok.Value.ShouldBeOfType<PaginatedResult<AdminRelFundoCedenteDto>>();
        paged.Items.ShouldBeEmpty();
    }

    // =========================================================================
    // ListFundoTiposAtivos
    // =========================================================================

    [Fact]
    public async Task ListFundoTiposAtivos_CrossCompany_Returns200()
    {
        var crossCompanyResult = new PaginatedResult<AdminRelFundoTipoAtivoDto>(
            [FundoTipoAtivoFromA], 1, 1, 20);
        _queries.Query<PaginatedResult<AdminRelFundoTipoAtivoDto>>(Arg.Any<ListAdminFundoTipoAtivoQuery>(), Arg.Any<CancellationToken>())
            .Returns(crossCompanyResult);

        var result = await _sut.ListFundoTiposAtivos(ct: CancellationToken.None);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var paged = ok.Value.ShouldBeOfType<PaginatedResult<AdminRelFundoTipoAtivoDto>>();
        paged.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task ListFundoTiposAtivos_WithCompanyFilter_PassesFilterToDispatcher()
    {
        ListAdminFundoTipoAtivoQuery? captured = null;
        _queries.Query<PaginatedResult<AdminRelFundoTipoAtivoDto>>(
            Arg.Do<object>(q => captured = q as ListAdminFundoTipoAtivoQuery),
            Arg.Any<CancellationToken>())
            .Returns(new PaginatedResult<AdminRelFundoTipoAtivoDto>([], 0, 1, 20));

        await _sut.ListFundoTiposAtivos(companyId: CompanyB, ct: CancellationToken.None);

        captured.ShouldNotBeNull();
        captured!.CompanyId.ShouldBe(CompanyB);
    }

    // =========================================================================
    // ListCedenteTiposAtivos
    // =========================================================================

    [Fact]
    public async Task ListCedenteTiposAtivos_CrossCompany_Returns200()
    {
        var crossCompanyResult = new PaginatedResult<AdminRelCedenteTipoAtivoDto>(
            [CedenteTipoAtivoFromB], 1, 1, 20);
        _queries.Query<PaginatedResult<AdminRelCedenteTipoAtivoDto>>(Arg.Any<ListAdminCedenteTipoAtivoQuery>(), Arg.Any<CancellationToken>())
            .Returns(crossCompanyResult);

        var result = await _sut.ListCedenteTiposAtivos(ct: CancellationToken.None);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var paged = ok.Value.ShouldBeOfType<PaginatedResult<AdminRelCedenteTipoAtivoDto>>();
        paged.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task ListCedenteTiposAtivos_WithCompanyFilter_PassesFilterToDispatcher()
    {
        ListAdminCedenteTipoAtivoQuery? captured = null;
        _queries.Query<PaginatedResult<AdminRelCedenteTipoAtivoDto>>(
            Arg.Do<object>(q => captured = q as ListAdminCedenteTipoAtivoQuery),
            Arg.Any<CancellationToken>())
            .Returns(new PaginatedResult<AdminRelCedenteTipoAtivoDto>([], 0, 1, 20));

        await _sut.ListCedenteTiposAtivos(companyId: CompanyA, ct: CancellationToken.None);

        captured.ShouldNotBeNull();
        captured!.CompanyId.ShouldBe(CompanyA);
    }
}
