using Onboarding.Domain.Aggregates.EmployeeAggregate;
using Onboarding.Domain.ValueObjects;
using Shouldly;

namespace Onboarding.Domain.Tests.Aggregates;

public class EmployeeTests
{
    private const string ValidCpf = "52998224725";

    [Fact]
    public void Register_validData_createsEmployeeWithAllProperties()
    {
        var companyId = Guid.NewGuid();
        var accessGroupId = Guid.NewGuid();

        var employee = Employee.Register(
            "João",
            ValidCpf,
            "joao@test.com",
            "11999999999",
            companyId,
            accessGroupId);

        employee.ShouldNotBeNull();
        employee.Id.ShouldNotBe(Guid.Empty);
        employee.Nome.ShouldBe("João");
        employee.Cpf.Value.ShouldBe(ValidCpf);
        employee.Email.Value.ShouldBe("joao@test.com");
        employee.Phone.Value.ShouldBe("11999999999");
        employee.KeycloakUserId.ShouldBeNull();
        employee.DeletedAt.ShouldBeNull();
        employee.IsDeleted.ShouldBeFalse();
    }

    [Fact]
    public void Register_nullNome_throwsArgumentException()
    {
        var companyId = Guid.NewGuid();
        var accessGroupId = Guid.NewGuid();

        Should.Throw<ArgumentException>(() =>
            Employee.Register(null!, ValidCpf, "joao@test.com", "11999999999", companyId, accessGroupId));
    }

    [Fact]
    public void Register_emptyNome_throwsArgumentException()
    {
        var companyId = Guid.NewGuid();
        var accessGroupId = Guid.NewGuid();

        Should.Throw<ArgumentException>(() =>
            Employee.Register("", ValidCpf, "joao@test.com", "11999999999", companyId, accessGroupId));
    }

    [Fact]
    public void Register_invalidCpf_throwsArgumentException()
    {
        var companyId = Guid.NewGuid();
        var accessGroupId = Guid.NewGuid();

        Should.Throw<ArgumentException>(() =>
            Employee.Register("João", "00000000000", "joao@test.com", "11999999999", companyId, accessGroupId));
    }

    [Fact]
    public void Register_setsCompanyId()
    {
        var companyId = Guid.NewGuid();
        var accessGroupId = Guid.NewGuid();

        var employee = Employee.Register("João", ValidCpf, "joao@test.com", "11999999999", companyId, accessGroupId);

        employee.CompanyId.ShouldBe(companyId);
    }

    [Fact]
    public void Register_setsAccessGroupId()
    {
        var companyId = Guid.NewGuid();
        var accessGroupId = Guid.NewGuid();

        var employee = Employee.Register("João", ValidCpf, "joao@test.com", "11999999999", companyId, accessGroupId);

        employee.AccessGroupId.ShouldBe(accessGroupId);
    }

    [Fact]
    public void SetKeycloakUserId_setsValue()
    {
        var employee = Employee.Register("João", ValidCpf, "joao@test.com", "11999999999", Guid.NewGuid(), Guid.NewGuid());

        employee.SetKeycloakUserId("kc-sub-123");

        employee.KeycloakUserId.ShouldBe("kc-sub-123");
    }

    [Fact]
    public void Anonymize_clearsPiiAndSetsDeletedAt()
    {
        var employee = Employee.Register("João", ValidCpf, "joao@test.com", "11999999999", Guid.NewGuid(), Guid.NewGuid());

        employee.Anonymize();

        employee.DeletedAt.ShouldNotBeNull();
        employee.IsDeleted.ShouldBeTrue();
        employee.Nome.ShouldBe("Usuário Excluído");
        employee.Cpf.ShouldBeNull();
        employee.Email.Value.ShouldStartWith("deleted-");
        employee.Email.Value.ShouldEndWith("@internal.local");
    }

    [Fact]
    public void Anonymize_calledTwice_isIdempotent()
    {
        var employee = Employee.Register("João", ValidCpf, "joao@test.com", "11999999999", Guid.NewGuid(), Guid.NewGuid());
        employee.Anonymize();
        var firstDeletedAt = employee.DeletedAt;

        employee.Anonymize();

        employee.DeletedAt.ShouldBe(firstDeletedAt);
    }

    [Fact]
    public void Update_validData_updatesNomeEmailPhone()
    {
        var employee = Employee.Register("Old Name", ValidCpf, "old@test.com", "11999999999", Guid.NewGuid(), Guid.NewGuid());

        employee.Update("New Name", "new@test.com", "11888888888");

        employee.Nome.ShouldBe("New Name");
        employee.Email.Value.ShouldBe("new@test.com");
        employee.Phone.Value.ShouldBe("11888888888");
    }

    [Fact]
    public void Update_emptyNome_throwsArgumentException()
    {
        var employee = Employee.Register("Old Name", ValidCpf, "old@test.com", "11999999999", Guid.NewGuid(), Guid.NewGuid());

        Should.Throw<ArgumentException>(() =>
            employee.Update("", "new@test.com", "11888888888"));
    }

    [Fact]
    public void SetAccessGroup_updatesAccessGroupId()
    {
        var originalGroupId = Guid.NewGuid();
        var newGroupId = Guid.NewGuid();
        var employee = Employee.Register("João", ValidCpf, "joao@test.com", "11999999999", Guid.NewGuid(), originalGroupId);

        employee.SetAccessGroup(newGroupId);

        employee.AccessGroupId.ShouldBe(newGroupId);
    }
}