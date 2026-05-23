using Onboarding.Application.Fundos.Queries.Admin;
using Shouldly;

namespace Onboarding.Application.Tests.Fundos.Admin;

/// <summary>
/// Unit tests for the 4 admin GET-by-id query record types (D-8, Phase 51).
/// Handlers live in Infrastructure (marked [ExcludeFromCodeCoverage]);
/// query records are Application-layer contracts that require coverage per D-2.
/// </summary>
public sealed class GetAdminByIdQueryTests
{
    // =========================================================================
    // GetAdminFundoByIdQuery
    // =========================================================================

    [Fact]
    public void GetAdminFundoByIdQuery_Constructor_SetsId()
    {
        var id = Guid.NewGuid();
        var query = new GetAdminFundoByIdQuery(id);
        query.Id.ShouldBe(id);
    }

    [Fact]
    public void GetAdminFundoByIdQuery_EqualityByValue_WhenSameId()
    {
        var id = Guid.NewGuid();
        var q1 = new GetAdminFundoByIdQuery(id);
        var q2 = new GetAdminFundoByIdQuery(id);
        q1.ShouldBe(q2);
    }

    [Fact]
    public void GetAdminFundoByIdQuery_NotEqual_WhenDifferentId()
    {
        var q1 = new GetAdminFundoByIdQuery(Guid.NewGuid());
        var q2 = new GetAdminFundoByIdQuery(Guid.NewGuid());
        q1.ShouldNotBe(q2);
    }

    // =========================================================================
    // GetAdminCedenteByIdQuery
    // =========================================================================

    [Fact]
    public void GetAdminCedenteByIdQuery_Constructor_SetsId()
    {
        var id = Guid.NewGuid();
        var query = new GetAdminCedenteByIdQuery(id);
        query.Id.ShouldBe(id);
    }

    [Fact]
    public void GetAdminCedenteByIdQuery_EqualityByValue_WhenSameId()
    {
        var id = Guid.NewGuid();
        var q1 = new GetAdminCedenteByIdQuery(id);
        var q2 = new GetAdminCedenteByIdQuery(id);
        q1.ShouldBe(q2);
    }

    [Fact]
    public void GetAdminCedenteByIdQuery_NotEqual_WhenDifferentId()
    {
        var q1 = new GetAdminCedenteByIdQuery(Guid.NewGuid());
        var q2 = new GetAdminCedenteByIdQuery(Guid.NewGuid());
        q1.ShouldNotBe(q2);
    }

    // =========================================================================
    // GetAdminConsultoriaFundoByIdQuery
    // =========================================================================

    [Fact]
    public void GetAdminConsultoriaFundoByIdQuery_Constructor_SetsId()
    {
        var id = Guid.NewGuid();
        var query = new GetAdminConsultoriaFundoByIdQuery(id);
        query.Id.ShouldBe(id);
    }

    [Fact]
    public void GetAdminConsultoriaFundoByIdQuery_EqualityByValue_WhenSameId()
    {
        var id = Guid.NewGuid();
        var q1 = new GetAdminConsultoriaFundoByIdQuery(id);
        var q2 = new GetAdminConsultoriaFundoByIdQuery(id);
        q1.ShouldBe(q2);
    }

    [Fact]
    public void GetAdminConsultoriaFundoByIdQuery_NotEqual_WhenDifferentId()
    {
        var q1 = new GetAdminConsultoriaFundoByIdQuery(Guid.NewGuid());
        var q2 = new GetAdminConsultoriaFundoByIdQuery(Guid.NewGuid());
        q1.ShouldNotBe(q2);
    }

    // =========================================================================
    // GetAdminCustodianteByIdQuery
    // =========================================================================

    [Fact]
    public void GetAdminCustodianteByIdQuery_Constructor_SetsId()
    {
        var id = Guid.NewGuid();
        var query = new GetAdminCustodianteByIdQuery(id);
        query.Id.ShouldBe(id);
    }

    [Fact]
    public void GetAdminCustodianteByIdQuery_EqualityByValue_WhenSameId()
    {
        var id = Guid.NewGuid();
        var q1 = new GetAdminCustodianteByIdQuery(id);
        var q2 = new GetAdminCustodianteByIdQuery(id);
        q1.ShouldBe(q2);
    }

    [Fact]
    public void GetAdminCustodianteByIdQuery_NotEqual_WhenDifferentId()
    {
        var q1 = new GetAdminCustodianteByIdQuery(Guid.NewGuid());
        var q2 = new GetAdminCustodianteByIdQuery(Guid.NewGuid());
        q1.ShouldNotBe(q2);
    }
}
