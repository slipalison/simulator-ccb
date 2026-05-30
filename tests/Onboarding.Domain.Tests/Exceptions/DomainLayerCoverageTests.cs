using Onboarding.Domain.Aggregates.CompanyAggregate;
using Onboarding.Domain.Aggregates.FundoAggregate;
using Onboarding.Domain.Common;
using Onboarding.Domain.Exceptions;
using Onboarding.Domain.ValueObjects;
using Shouldly;

namespace Onboarding.Domain.Tests.Exceptions;

/// <summary>
/// Coverage tests for Domain layer files that were below the 80% gate (D-49 / W4-1).
/// Targets:
///   - RegistrationFailedException (was 0%)
///   - DuplicateEntityException     (was 41.7%)
///   - Entity&lt;TId&gt;                  (was 50%)
///   - DomainException              (was 50%)
///   - DuplicateCompanyException    (was 50%)
///   - DuplicateKeycloakUserException (was 50%)
///   - InvalidStateTransitionException (was exactly 80% — nudged above)
/// Concrete subclass of Entity&lt;Guid&gt; used: Company (existing aggregate).
/// </summary>

// ── RegistrationFailedException ─────────────────────────────────────────────

public class RegistrationFailedExceptionTests
{
    [Fact]
    public void Constructor_MessageOnly_SetsMessage()
    {
        const string msg = "Keycloak registration failed.";

        var ex = new RegistrationFailedException(msg);

        ex.Message.ShouldBe(msg);
        ex.InnerException.ShouldBeNull();
    }

    [Fact]
    public void Constructor_MessageAndInner_SetsMessageAndInnerException()
    {
        const string msg = "Keycloak step failed after DB persist.";
        var inner = new InvalidOperationException("keycloak error detail");

        var ex = new RegistrationFailedException(msg, inner);

        ex.Message.ShouldBe(msg);
        ex.InnerException.ShouldBeSameAs(inner);
    }

    [Fact]
    public void IsA_Exception()
    {
        var ex = new RegistrationFailedException("any");

        ex.ShouldBeAssignableTo<Exception>();
    }
}

// ── DuplicateEntityException ─────────────────────────────────────────────────

public class DuplicateEntityExceptionTests
{
    [Fact]
    public void Constructor_EntityTypeAndKey_SetsProperties()
    {
        const string entityType = "Fundo";
        const string keyValue = "11222333000181";

        var ex = new DuplicateEntityException(entityType, keyValue);

        ex.EntityType.ShouldBe(entityType);
        ex.KeyValue.ShouldBe(keyValue);
    }

    [Fact]
    public void Constructor_EntityTypeAndKey_MessageContainsBoth()
    {
        const string entityType = "Cedente";
        const string keyValue = "99888777000166";

        var ex = new DuplicateEntityException(entityType, keyValue);

        ex.Message.ShouldContain(entityType);
        ex.Message.ShouldContain(keyValue);
    }

    [Fact]
    public void Constructor_MessageOnly_SetsEmptyEntityTypeAndKey()
    {
        const string msg = "Already exists.";

        var ex = new DuplicateEntityException(msg);

        ex.Message.ShouldBe(msg);
        ex.EntityType.ShouldBe(string.Empty);
        ex.KeyValue.ShouldBe(string.Empty);
    }

    [Fact]
    public void IsA_DomainException()
    {
        var ex = new DuplicateEntityException("Fundo", "key");

        ex.ShouldBeAssignableTo<DomainException>();
    }
}

// ── DomainException (via concrete subclass DuplicateEntityException) ─────────

public class DomainExceptionTests
{
    [Fact]
    public void Constructor_MessageOnly_SetsMessage()
    {
        // DomainException is abstract; test via DuplicateEntityException (message-only ctor)
        const string msg = "Business rule violated.";

        var ex = new DuplicateEntityException(msg);

        ex.Message.ShouldBe(msg);
        ex.InnerException.ShouldBeNull();
    }

    [Fact]
    public void Constructor_MessageAndInner_SetsInnerException()
    {
        // InvalidStateTransitionException exposes the DomainException(msg, inner) ctor
        // indirectly; use a message-only ctor and verify base chain for the inner path
        // via an ad-hoc local subclass produced through DuplicateEntityException which
        // inherits DomainException (message-only). The inner-exception path is exercised
        // by the InvalidStateTransitionException(string) ctor (delegates to DomainException(string)).
        // For completeness, exercise via RegistrationFailedException which is a sibling
        // that has the same structural pattern but is not DomainException.
        // The real exercise is: DomainException(string, Exception) ctor hit by
        // InvalidStateTransitionException or any subclass that calls base(msg, inner).
        //
        // Because none of the shipped subclasses expose an (string, Exception) ctor
        // we exercise it via a locally-defined anonymous concrete subclass.
        var inner = new Exception("original");
        const string msg = "Domain error with cause.";

        var ex = new ConcreteDomainException(msg, inner);

        ex.Message.ShouldBe(msg);
        ex.InnerException.ShouldBeSameAs(inner);
    }

    [Fact]
    public void IsAbstractBase_AllShippedSubclassesInheritFromIt()
    {
        new DuplicateEntityException("T", "K").ShouldBeAssignableTo<DomainException>();
        new InvalidStateTransitionException("Fundo", FundoStatus.RASCUNHO, FundoStatus.SUSPENSO)
            .ShouldBeAssignableTo<DomainException>();
    }

    // Minimal concrete subclass used only in this test file.
    private sealed class ConcreteDomainException : DomainException
    {
        public ConcreteDomainException(string message, Exception inner) : base(message, inner) { }
    }
}

// ── DuplicateCompanyException ─────────────────────────────────────────────────

public class DuplicateCompanyExceptionTests
{
    [Fact]
    public void Constructor_MessageOnly_SetsMessage()
    {
        const string msg = "Company with this CNPJ already exists.";

        var ex = new DuplicateCompanyException(msg);

        ex.Message.ShouldBe(msg);
        ex.InnerException.ShouldBeNull();
    }

    [Fact]
    public void Constructor_MessageAndInner_SetsMessageAndInnerException()
    {
        const string msg = "Company conflict.";
        var inner = new InvalidOperationException("db unique constraint");

        var ex = new DuplicateCompanyException(msg, inner);

        ex.Message.ShouldBe(msg);
        ex.InnerException.ShouldBeSameAs(inner);
    }

    [Fact]
    public void IsA_Exception()
    {
        var ex = new DuplicateCompanyException("msg");

        ex.ShouldBeAssignableTo<Exception>();
    }
}

// ── DuplicateKeycloakUserException ────────────────────────────────────────────

public class DuplicateKeycloakUserExceptionTests
{
    [Fact]
    public void Constructor_MessageOnly_SetsMessage()
    {
        const string msg = "Keycloak user already exists for this email.";

        var ex = new DuplicateKeycloakUserException(msg);

        ex.Message.ShouldBe(msg);
        ex.InnerException.ShouldBeNull();
    }

    [Fact]
    public void Constructor_MessageAndInner_SetsMessageAndInnerException()
    {
        const string msg = "Keycloak duplicate conflict.";
        var inner = new HttpRequestException("409 from Keycloak");

        var ex = new DuplicateKeycloakUserException(msg, inner);

        ex.Message.ShouldBe(msg);
        ex.InnerException.ShouldBeSameAs(inner);
    }

    [Fact]
    public void IsA_Exception()
    {
        var ex = new DuplicateKeycloakUserException("msg");

        ex.ShouldBeAssignableTo<Exception>();
    }
}

// ── InvalidStateTransitionException ──────────────────────────────────────────

public class InvalidStateTransitionExceptionTests
{
    [Fact]
    public void Constructor_FullArgs_SetsEntityNameFromTo()
    {
        const string entityName = "Fundo";

        var ex = new InvalidStateTransitionException(entityName, FundoStatus.RASCUNHO, FundoStatus.SUSPENSO);

        ex.EntityName.ShouldBe(entityName);
        ex.From.ShouldBe(FundoStatus.RASCUNHO);
        ex.To.ShouldBe(FundoStatus.SUSPENSO);
    }

    [Fact]
    public void Constructor_FullArgs_MessageContainsFromAndTo()
    {
        var ex = new InvalidStateTransitionException("Fundo", FundoStatus.ATIVO, FundoStatus.RASCUNHO);

        ex.Message.ShouldContain(nameof(FundoStatus.ATIVO));
        ex.Message.ShouldContain(nameof(FundoStatus.RASCUNHO));
    }

    [Fact]
    public void Constructor_MessageOnly_SetsEmptyEntityNameAndDefaultStatuses()
    {
        const string msg = "Transition not allowed.";

        var ex = new InvalidStateTransitionException(msg);

        ex.Message.ShouldBe(msg);
        ex.EntityName.ShouldBe(string.Empty);
        ex.From.ShouldBe(default(FundoStatus));
        ex.To.ShouldBe(default(FundoStatus));
    }

    [Fact]
    public void IsA_DomainException()
    {
        var ex = new InvalidStateTransitionException("Fundo", FundoStatus.ATIVO, FundoStatus.ENCERRADO);

        ex.ShouldBeAssignableTo<DomainException>();
    }
}

// ── Entity<TId> ───────────────────────────────────────────────────────────────
// Tests via Company (sealed : Entity<Guid>) — avoids creating a dummy aggregate.

public class EntityBaseTests
{
    private static Company CreateCompany(string razaoSocial = "Empresa A") =>
        Company.Register(
            razaoSocial,
            "11222333000181",
            "contact@empresa.com",
            "11999999999",
            TermsAcceptance.Create("1.0", "127.0.0.1"));

    // ── Equals / == / != ──────────────────────────────────────────────────

    [Fact]
    public void Equals_SameReference_ReturnsTrue()
    {
        var company = CreateCompany();

        company.Equals(company).ShouldBeTrue();
    }

    [Fact]
    public void Equals_SameInstance_OperatorEqualsReturnsTrue()
    {
        var c = CreateCompany();
        Company? boxed = c;

        // Cast through object to avoid CS1718 same-variable warning while still
        // exercising the operator==(Entity<TId>?, Entity<TId>?) overload.
        (boxed == c).ShouldBeTrue();
    }

    [Fact]
    public void Equals_NullObject_ReturnsFalse()
    {
        var c = CreateCompany();

        c.Equals(null).ShouldBeFalse();
    }

    [Fact]
    public void Equals_NonEntityObject_ReturnsFalse()
    {
        var c = CreateCompany();

        c.Equals("not an entity").ShouldBeFalse();
    }

    [Fact]
    public void Equals_DifferentId_ReturnsFalse()
    {
        var a = CreateCompany("Empresa A");
        var b = CreateCompany("Empresa B"); // different Id (Guid.NewGuid each call)

        a.Equals(b).ShouldBeFalse();
    }

    [Fact]
    public void OperatorEquals_BothNull_ReturnsTrue()
    {
        Company? left = null;
        Company? right = null;

        (left == right).ShouldBeTrue();
    }

    [Fact]
    public void OperatorEquals_LeftNull_ReturnsFalse()
    {
        Company? left = null;
        var right = CreateCompany();

        (left == right).ShouldBeFalse();
    }

    [Fact]
    public void OperatorNotEquals_DifferentInstances_ReturnsTrue()
    {
        var a = CreateCompany("A");
        var b = CreateCompany("B");

        (a != b).ShouldBeTrue();
    }

    [Fact]
    public void OperatorNotEquals_SameReference_ReturnsFalse()
    {
        var c = CreateCompany();
        Company? boxed = c;

        // Cast through nullable alias to avoid CS1718 while exercising !=.
        (boxed != c).ShouldBeFalse();
    }

    [Fact]
    public void GetHashCode_ReturnsSameValueForSameInstance()
    {
        var c = CreateCompany();

        c.GetHashCode().ShouldBe(c.GetHashCode());
    }

    [Fact]
    public void GetHashCode_EqualsIdHashCode()
    {
        var c = CreateCompany();

        c.GetHashCode().ShouldBe(c.Id.GetHashCode());
    }

    // ── Id property ───────────────────────────────────────────────────────

    [Fact]
    public void Id_AfterRegister_IsNotEmpty()
    {
        var c = CreateCompany();

        c.Id.ShouldNotBe(Guid.Empty);
    }
}
