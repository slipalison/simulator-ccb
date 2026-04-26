using Onboarding.Domain.Aggregates.CompanyAggregate;
using Onboarding.Domain.ValueObjects;
using Shouldly;

namespace Onboarding.Domain.Tests.Aggregates.CompanyAggregate;

public class CompanyTests
{
    private const string ValidCnpj = "11222333000181";

    [Fact]
    public void Register_ValidInputs_CreatesCompany()
    {
        var terms = TermsAcceptance.Create("1.0", "192.168.1.1");

        var company = Company.Register(
            "Empresa SA",
            ValidCnpj,
            "empresa@example.com",
            "11999998888",
            terms);

        company.ShouldNotBeNull();
        company.Id.ShouldNotBe(Guid.Empty);
        company.RazaoSocial.ShouldBe("Empresa SA");
        company.Cnpj.Value.ShouldBe(ValidCnpj);
        company.Email.Value.ShouldBe("empresa@example.com");
        company.Phone.Value.ShouldBe("11999998888");
        company.TermsAcceptance.ShouldNotBeNull();
        company.TermsAcceptance.TermsVersion.ShouldBe("1.0");
        company.KeycloakUserId.ShouldBeNull();
        company.DeletedAt.ShouldBeNull();
        company.IsDeleted.ShouldBeFalse();
    }

    [Fact]
    public void Register_NullTermsAcceptance_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() =>
            Company.Register("Empresa SA", ValidCnpj, "empresa@example.com", "11999998888", null!));
    }

    [Fact]
    public void Register_NullRazaoSocial_ThrowsArgumentException()
    {
        var terms = TermsAcceptance.Create("1.0", "192.168.1.1");

        Should.Throw<ArgumentException>(() =>
            Company.Register(null!, ValidCnpj, "empresa@example.com", "11999998888", terms));
    }

    [Fact]
    public void Register_EmptyRazaoSocial_ThrowsArgumentException()
    {
        var terms = TermsAcceptance.Create("1.0", "192.168.1.1");

        Should.Throw<ArgumentException>(() =>
            Company.Register("", ValidCnpj, "empresa@example.com", "11999998888", terms));
    }

    [Fact]
    public void Register_InvalidCnpj_ThrowsArgumentException()
    {
        var terms = TermsAcceptance.Create("1.0", "192.168.1.1");

        Should.Throw<ArgumentException>(() =>
            Company.Register("Empresa SA", "00000000000000", "empresa@example.com", "11999998888", terms));
    }

    [Fact]
    public void SetKeycloakUserId_SetsValue()
    {
        var terms = TermsAcceptance.Create("1.0", "192.168.1.1");
        var company = Company.Register("Empresa SA", ValidCnpj, "empresa@example.com", "11999998888", terms);

        company.SetKeycloakUserId("keycloak-sub-123");

        company.KeycloakUserId.ShouldBe("keycloak-sub-123");
    }

    [Fact]
    public void SetKeycloakUserId_NullValue_ThrowsArgumentNullException()
    {
        var terms = TermsAcceptance.Create("1.0", "192.168.1.1");
        var company = Company.Register("Empresa SA", ValidCnpj, "empresa@example.com", "11999998888", terms);

        Should.Throw<ArgumentNullException>(() => company.SetKeycloakUserId(null!));
    }

    [Fact]
    public void Anonymize_SetsDeletedAtAndScrubsPii()
    {
        var terms = TermsAcceptance.Create("1.0", "192.168.1.1");
        var company = Company.Register("Empresa SA", ValidCnpj, "empresa@example.com", "11999998888", terms);

        company.Anonymize();

        company.DeletedAt.ShouldNotBeNull();
        company.IsDeleted.ShouldBeTrue();
        company.RazaoSocial.ShouldBe("Empresa Excluída");
        company.Cnpj.ShouldBeNull();
        company.Email.Value.ShouldStartWith("deleted-");
        company.Email.Value.ShouldEndWith("@internal.local");
        company.Phone.Value.ShouldBe("0000000000");
    }

    [Fact]
    public void Anonymize_WhenAlreadyDeleted_IsIdempotent()
    {
        var terms = TermsAcceptance.Create("1.0", "192.168.1.1");
        var company = Company.Register("Empresa SA", ValidCnpj, "empresa@example.com", "11999998888", terms);

        company.Anonymize();
        var firstDeletedAt = company.DeletedAt;

        company.Anonymize();

        company.DeletedAt.ShouldBe(firstDeletedAt);
    }

    [Fact]
    public void Update_ValidData_UpdatesFields()
    {
        var terms = TermsAcceptance.Create("1.0", "192.168.1.1");
        var company = Company.Register("Old Name", ValidCnpj, "old@example.com", "11999999999", terms);

        company.Update("New Name", "new@example.com", "11888888888");

        company.RazaoSocial.ShouldBe("New Name");
        company.Email.Value.ShouldBe("new@example.com");
        company.Phone.Value.ShouldBe("11888888888");
    }

    [Fact]
    public void Update_EmptyRazaoSocial_ThrowsArgumentException()
    {
        var terms = TermsAcceptance.Create("1.0", "192.168.1.1");
        var company = Company.Register("Old Name", ValidCnpj, "old@example.com", "11999999999", terms);

        Should.Throw<ArgumentException>(() =>
            company.Update("", "new@example.com", "11888888888"));
    }
}

public class TermsAcceptanceTests
{
    [Fact]
    public void Create_ValidInputs_CreatesTermsAcceptance()
    {
        var before = DateTimeOffset.UtcNow;
        var terms = TermsAcceptance.Create("1.0", "192.168.1.1");

        terms.AcceptedAt.ShouldBeGreaterThanOrEqualTo(before);
        terms.TermsVersion.ShouldBe("1.0");
        terms.IpAddress.ShouldBe("192.168.1.1");
    }

    [Fact]
    public void Create_NullTermsVersion_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() =>
            TermsAcceptance.Create(null!, "192.168.1.1"));
    }

    [Fact]
    public void Create_EmptyTermsVersion_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() =>
            TermsAcceptance.Create("", "192.168.1.1"));
    }

    [Fact]
    public void Create_NullIpAddress_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() =>
            TermsAcceptance.Create("1.0", null!));
    }

    [Fact]
    public void Create_EmptyIpAddress_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() =>
            TermsAcceptance.Create("1.0", ""));
    }

    [Fact]
    public void CurrentVersion_IsSet()
    {
        TermsAcceptance.CurrentVersion.ShouldBe("1.0");
    }
}