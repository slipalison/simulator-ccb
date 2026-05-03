using Onboarding.Domain.Aggregates.EmployeeAggregate;
using Shouldly;

namespace Onboarding.Domain.Tests.Aggregates;

public class AccessGroupTests
{
    [Fact]
    public void Create_validData_setsCompanyIdNamePermissions()
    {
        var companyId = Guid.NewGuid();
        var group = AccessGroup.Create(companyId, "admin-empresa", new[] { Permissions.EmployeesRead, Permissions.EmployeesWrite });

        group.ShouldNotBeNull();
        group.Id.ShouldNotBe(Guid.Empty);
        group.CompanyId.ShouldBe(companyId);
        group.Name.ShouldBe("admin-empresa");
        group.Permissions.ShouldContain(Permissions.EmployeesRead);
        group.Permissions.ShouldContain(Permissions.EmployeesWrite);
        group.CreatedAt.ShouldNotBe(default);
    }

    [Fact]
    public void Create_emptyName_throwsArgumentException()
    {
        Should.Throw<ArgumentException>(() =>
            AccessGroup.Create(Guid.NewGuid(), "", new[] { Permissions.EmployeesRead }));
    }

    [Fact]
    public void Create_nullName_throwsArgumentException()
    {
        Should.Throw<ArgumentException>(() =>
            AccessGroup.Create(Guid.NewGuid(), null!, new[] { Permissions.EmployeesRead }));
    }

    [Fact]
    public void CreateDefaultGroups_returns3Groups()
    {
        var companyId = Guid.NewGuid();
        var groups = AccessGroup.CreateDefaultGroups(companyId);

        groups.Count.ShouldBe(3);
    }

    [Fact]
    public void CreateDefaultGroups_adminEmpresa_hasAllPermissions()
    {
        var companyId = Guid.NewGuid();
        var groups = AccessGroup.CreateDefaultGroups(companyId);

        var admin = groups[0];
        admin.Name.ShouldBe("admin-empresa");
        admin.Permissions.Count.ShouldBe(Permissions.All.Length);
        foreach (var perm in Permissions.All)
        {
            admin.Permissions.ShouldContain(perm);
        }
    }

    [Fact]
    public void CreateDefaultGroups_viewer_hasReadAndAuditPermissions()
    {
        var companyId = Guid.NewGuid();
        var groups = AccessGroup.CreateDefaultGroups(companyId);

        var viewer = groups[1];
        viewer.Name.ShouldBe("viewer");
        viewer.Permissions.Count.ShouldBe(2);
        viewer.Permissions.ShouldContain(Permissions.EmployeesRead);
        viewer.Permissions.ShouldContain(Permissions.AuditRead);
    }

    [Fact]
    public void CreateDefaultGroups_dashboard_hasDashboardAccessOnly()
    {
        var companyId = Guid.NewGuid();
        var groups = AccessGroup.CreateDefaultGroups(companyId);

        var dashboard = groups[2];
        dashboard.Name.ShouldBe("dashboard");
        dashboard.Permissions.Count.ShouldBe(1);
        dashboard.Permissions.ShouldContain(Permissions.DashboardAccess);
    }

    [Fact]
    public void UpdatePermissions_validPermissions_replacesList()
    {
        var group = AccessGroup.Create(Guid.NewGuid(), "custom", new[] { Permissions.EmployeesRead });

        group.UpdatePermissions(new[] { Permissions.EmployeesWrite, Permissions.AuditRead });

        group.Permissions.ShouldContain(Permissions.EmployeesWrite);
        group.Permissions.ShouldContain(Permissions.AuditRead);
        group.Permissions.ShouldNotContain(Permissions.EmployeesRead);
    }

    [Fact]
    public void UpdatePermissions_invalidPermission_throwsArgumentException()
    {
        var group = AccessGroup.Create(Guid.NewGuid(), "custom", new[] { Permissions.EmployeesRead });

        Should.Throw<ArgumentException>(() =>
            group.UpdatePermissions(new[] { "invalid:permission" }));
    }
}