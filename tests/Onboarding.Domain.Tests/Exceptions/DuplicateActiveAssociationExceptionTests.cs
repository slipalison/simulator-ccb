using Onboarding.Domain.Exceptions;
using Shouldly;

namespace Onboarding.Domain.Tests.Exceptions;

/// <summary>
/// Tests for DuplicateActiveAssociationException (REL-09, D-18).
/// Covers AssociationType and Key properties to satisfy 80% coverage gate (G11-iter3).
/// </summary>
public class DuplicateActiveAssociationExceptionTests
{
    [Fact]
    public void Constructor_SetsAssociationTypeAndKey()
    {
        const string associationType = "FundoCedente";
        const string key = "FundoId+CedenteId pair already has an active association.";

        var ex = new DuplicateActiveAssociationException(associationType, key);

        ex.AssociationType.ShouldBe(associationType);
        ex.Key.ShouldBe(key);
    }

    [Fact]
    public void Constructor_MessageContainsAssociationTypeAndKey()
    {
        const string associationType = "CedenteTipoAtivo";
        const string key = "CedenteId+TipoAtivoId pair";

        var ex = new DuplicateActiveAssociationException(associationType, key);

        ex.Message.ShouldContain(associationType);
        ex.Message.ShouldContain(key);
    }

    [Fact]
    public void IsA_DomainException()
    {
        var ex = new DuplicateActiveAssociationException("FundoTipoAtivo", "any-key");

        ex.ShouldBeAssignableTo<DomainException>();
    }
}
