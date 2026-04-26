using Onboarding.Domain.Aggregates.CompanyAggregate;
using Onboarding.Domain.ValueObjects;
using Shouldly;

namespace Onboarding.Domain.Tests.Aggregates;

public class CompanyTests
{
    private const string ValidCnpj = "11222333000181";

    [Fact]
    public void Register_validData_createsCompanyWithAllProperties()
    {
        var terms = TermsAcceptance.Create("1.0", "192.168.1.1");

        var company = Company.Register(
            "Empresa Teste",
            ValidCnpj,
            "test@company.com",
            "11999999999",
            terms);

        company.ShouldNotBeNull();
        company.Id.ShouldNotBe(Guid.Empty);
        company.RazaoSocial.ShouldBe("Empresa Teste");
        company.Cnpj.Value.ShouldBe(ValidCnpj);
        company.Email.Value.ShouldBe("test@company.com");
        company.Phone.Value.ShouldBe("11999999999");
        company.TermsAcceptance.ShouldNotBeNull();
        company.TermsAcceptance.TermsVersion.ShouldBe("1.0");
        company.KeycloakUserId.ShouldBeNull();
        company.DeletedAt.ShouldBeNull();
        company.IsDeleted.ShouldBeFalse();
    }

    [Fact]
    public void Register_nullRazaoSocial_throwsArgumentException()
    {
        var terms = TermsAcceptance.Create("1.0", "192.168.1.1");

        Should.Throw<ArgumentException>(() =>
            Company.Register(null!, ValidCnpj, "test@company.com", "11999999999", terms));
    }

    [Fact]
    public void Register_emptyRazaoSocial_throwsArgumentException()
    {
        var terms = TermsAcceptance.Create("1.0", "192.168.1.1");

        Should.Throw<ArgumentException>(() =>
            Company.Register("", ValidCnpj, "test@company.com", "11999999999", terms));
    }

    [Fact]
    public void Register_invalidCnpj_throwsArgumentException()
    {
        var terms = TermsAcceptance.Create("1.0", "192.168.1.1");

        Should.Throw<ArgumentException>(() =>
            Company.Register("Empresa Teste", "00000000000000", "test@company.com", "11999999999", terms));
    }

    [Fact]
    public void Register_nullTermsAcceptance_throwsArgumentException()
    {
        Should.Throw<ArgumentException>(() =>
            Company.Register("Empresa Teste", ValidCnpj, "test@company.com", "11999999999", null!));
    }

    [Fact]
    public void SetKeycloakUserId_setsValue()
    {
        var terms = TermsAcceptance.Create("1.0", "192.168.1.1");
        var company = Company.Register("Empresa Teste", ValidCnpj, "test@company.com", "11999999999", terms);

        company.SetKeycloakUserId("keycloak-sub-123");

        company.KeycloakUserId.ShouldBe("keycloak-sub-123");
    }

    [Fact]
    public void Anonymize_clearsPiiAndSetsDeletedAt()
    {
        var terms = TermsAcceptance.Create("1.0", "192.168.1.1");
        var company = Company.Register("Empresa Teste", ValidCnpj, "test@company.com", "11999999999", terms);

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
    public void Anonymize_calledTwice_isIdempotent()
    {
        var terms = TermsAcceptance.Create("1.0", "192.168.1.1");
        var company = Company.Register("Empresa Teste", ValidCnpj, "test@company.com", "11999999999", terms);

        company.Anonymize();
        var firstDeletedAt = company.DeletedAt;

        company.Anonymize();

        company.DeletedAt.ShouldBe(firstDeletedAt);
    }

    [Fact]
    public void Update_validData_updatesProperties()
    {
        var terms = TermsAcceptance.Create("1.0", "192.168.1.1");
        var company = Company.Register("Old Name", ValidCnpj, "old@company.com", "11999999999", terms);

        company.Update("New Name", "new@company.com", "11888888888");

        company.RazaoSocial.ShouldBe("New Name");
        company.Email.Value.ShouldBe("new@company.com");
        company.Phone.Value.ShouldBe("11888888888");
    }

    [Fact]
    public void Update_emptyRazaoSocial_throwsArgumentException()
    {
        var terms = TermsAcceptance.Create("1.0", "192.168.1.1");
        var company = Company.Register("Old Name", ValidCnpj, "old@company.com", "11999999999", terms);

        Should.Throw<ArgumentException>(() =>
            company.Update("", "new@company.com", "11888888888"));
    }
}