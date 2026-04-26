using Onboarding.Domain.Aggregates.CompanyAggregate;
using Onboarding.Domain.Aggregates.EmployeeAggregate;
using Shouldly;

namespace Onboarding.Domain.Tests.Aggregates.EmployeeAggregate;

public class EmployeeTests
{
    private const string ValidCpf = "52998224725";

    [Fact]
    public void Register_ValidInputs_CreatesEmployee()
    {
        var companyId = Guid.NewGuid();
        var accessGroupId = Guid.NewGuid();

        var employee = Employee.Register(
            "João Silva",
            ValidCpf,
            "joao@empresa.com",
            "11999998888",
            companyId,
            accessGroupId);

        employee.ShouldNotBeNull();
        employee.Id.ShouldNotBe(Guid.Empty);
        employee.Nome.ShouldBe("João Silva");
        employee.Cpf.Value.ShouldBe(ValidCpf);
        employee.Email.Value.ShouldBe("joao@empresa.com");
        employee.Phone.Value.ShouldBe("11999998888");
        employee.CompanyId.ShouldBe(companyId);
        employee.AccessGroupId.ShouldBe(accessGroupId);
        employee.KeycloakUserId.ShouldBeNull();
        employee.DeletedAt.ShouldBeNull();
        employee.IsDeleted.ShouldBeFalse();
    }

    [Fact]
    public void Register_NullNome_ThrowsArgumentException()
    {
        var companyId = Guid.NewGuid();
        var accessGroupId = Guid.NewGuid();

        Should.Throw<ArgumentException>(() =>
            Employee.Register(null!, ValidCpf, "joao@empresa.com", "11999998888", companyId, accessGroupId));
    }

    [Fact]
    public void Register_EmptyNome_ThrowsArgumentException()
    {
        var companyId = Guid.NewGuid();
        var accessGroupId = Guid.NewGuid();

        Should.Throw<ArgumentException>(() =>
            Employee.Register("", ValidCpf, "joao@empresa.com", "11999998888", companyId, accessGroupId));
    }

    [Fact]
    public void Register_InvalidCpf_ThrowsArgumentException()
    {
        var companyId = Guid.NewGuid();
        var accessGroupId = Guid.NewGuid();

        Should.Throw<ArgumentException>(() =>
            Employee.Register("João", "00000000000", "joao@empresa.com", "11999998888", companyId, accessGroupId));
    }

    [Fact]
    public void SetKeycloakUserId_SetsValue()
    {
        var employee = Employee.Register("João", ValidCpf, "joao@empresa.com", "11999998888", Guid.NewGuid(), Guid.NewGuid());
        employee.SetKeycloakUserId("kc-sub-123");
        employee.KeycloakUserId.ShouldBe("kc-sub-123");
    }

    [Fact]
    public void SetKeycloakUserId_NullValue_ThrowsArgumentNullException()
    {
        var employee = Employee.Register("João", ValidCpf, "joao@empresa.com", "11999998888", Guid.NewGuid(), Guid.NewGuid());
        Should.Throw<ArgumentNullException>(() => employee.SetKeycloakUserId(null!));
    }

    [Fact]
    public void Anonymize_SetsDeletedAtAndScrubsPii()
    {
        var employee = Employee.Register("João", ValidCpf, "joao@empresa.com", "11999998888", Guid.NewGuid(), Guid.NewGuid());

        employee.Anonymize();

        employee.DeletedAt.ShouldNotBeNull();
        employee.IsDeleted.ShouldBeTrue();
        employee.Nome.ShouldBe("Usuário Excluído");
        employee.Cpf.ShouldBeNull();
        employee.Email.Value.ShouldStartWith("deleted-");
        employee.Email.Value.ShouldEndWith("@internal.local");
    }

    [Fact]
    public void Anonymize_WhenAlreadyDeleted_IsIdempotent()
    {
        var employee = Employee.Register("João", ValidCpf, "joao@empresa.com", "11999998888", Guid.NewGuid(), Guid.NewGuid());
        employee.Anonymize();
        var firstDeletedAt = employee.DeletedAt;
        employee.Anonymize();
        employee.DeletedAt.ShouldBe(firstDeletedAt);
    }

    [Fact]
    public void Update_ValidData_UpdatesFields()
    {
        var employee = Employee.Register("Old Name", ValidCpf, "old@empresa.com", "11999999999", Guid.NewGuid(), Guid.NewGuid());
        employee.Update("New Name", "new@empresa.com", "11888888888");
        employee.Nome.ShouldBe("New Name");
        employee.Email.Value.ShouldBe("new@empresa.com");
        employee.Phone.Value.ShouldBe("11888888888");
    }

    [Fact]
    public void Update_EmptyNome_ThrowsArgumentException()
    {
        var employee = Employee.Register("Old Name", ValidCpf, "old@empresa.com", "11999999999", Guid.NewGuid(), Guid.NewGuid());
        Should.Throw<ArgumentException>(() => employee.Update("", "new@empresa.com", "11888888888"));
    }

    [Fact]
    public void SetAccessGroup_ChangesAccessGroupId()
    {
        var originalGroupId = Guid.NewGuid();
        var newGroupId = Guid.NewGuid();
        var employee = Employee.Register("João", ValidCpf, "joao@empresa.com", "11999998888", Guid.NewGuid(), originalGroupId);

        employee.SetAccessGroup(newGroupId);

        employee.AccessGroupId.ShouldBe(newGroupId);
    }
}

public class AccessGroupTests
{
    [Fact]
    public void Create_ValidInputs_CreatesAccessGroup()
    {
        var companyId = Guid.NewGuid();
        var group = AccessGroup.Create(companyId, "admin-empresa", new[] { Permissions.EmployeesRead, Permissions.EmployeesWrite });

        group.ShouldNotBeNull();
        group.Id.ShouldNotBe(Guid.Empty);
        group.CompanyId.ShouldBe(companyId);
        group.Name.ShouldBe("admin-empresa");
        group.Permissions.ShouldContain(Permissions.EmployeesRead);
        group.Permissions.ShouldContain(Permissions.EmployeesWrite);
    }

    [Fact]
    public void Create_NullName_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() =>
            AccessGroup.Create(Guid.NewGuid(), null!, new[] { Permissions.EmployeesRead }));
    }

    [Fact]
    public void Create_EmptyName_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() =>
            AccessGroup.Create(Guid.NewGuid(), "", new[] { Permissions.EmployeesRead }));
    }

    [Fact]
    public void CreateDefaultGroups_ReturnsThreeGroups()
    {
        var companyId = Guid.NewGuid();
        var groups = AccessGroup.CreateDefaultGroups(companyId);

        groups.Count.ShouldBe(3);
        groups[0].Name.ShouldBe("admin-empresa");
        groups[0].Permissions.Count.ShouldBe(6); // All permissions
        groups[1].Name.ShouldBe("viewer");
        groups[1].Permissions.ShouldContain(Permissions.EmployeesRead);
        groups[1].Permissions.ShouldContain(Permissions.AuditRead);
        groups[2].Name.ShouldBe("dashboard");
        groups[2].Permissions.ShouldContain(Permissions.DashboardAccess);
    }

    [Fact]
    public void UpdatePermissions_ValidPermissions_UpdatesList()
    {
        var group = AccessGroup.Create(Guid.NewGuid(), "custom", new[] { Permissions.EmployeesRead });
        group.UpdatePermissions(new[] { Permissions.EmployeesWrite, Permissions.AuditRead });
        group.Permissions.ShouldContain(Permissions.EmployeesWrite);
        group.Permissions.ShouldContain(Permissions.AuditRead);
        group.Permissions.ShouldNotContain(Permissions.EmployeesRead);
    }

    [Fact]
    public void UpdatePermissions_InvalidPermission_ThrowsArgumentException()
    {
        var group = AccessGroup.Create(Guid.NewGuid(), "custom", new[] { Permissions.EmployeesRead });
        Should.Throw<ArgumentException>(() =>
            group.UpdatePermissions(new[] { "invalid:permission" }));
    }
}

public class PermissionsTests
{
    [Fact]
    public void All_ContainsSixConstants()
    {
        Permissions.All.Length.ShouldBe(6);
        Permissions.All.ShouldContain(Permissions.EmployeesRead);
        Permissions.All.ShouldContain(Permissions.EmployeesWrite);
        Permissions.All.ShouldContain(Permissions.EmployeesDelete);
        Permissions.All.ShouldContain(Permissions.AuditRead);
        Permissions.All.ShouldContain(Permissions.DashboardAccess);
        Permissions.All.ShouldContain(Permissions.AccessGroupsManage);
    }
}