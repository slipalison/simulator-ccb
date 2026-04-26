using Onboarding.Domain.Aggregates.EmployeeAggregate;
using Shouldly;

namespace Onboarding.Domain.Tests.Aggregates;

public class PermissionsTests
{
    [Fact]
    public void All_containsExactly6Permissions()
    {
        Permissions.All.Length.ShouldBe(6);
    }

    [Fact]
    public void All_containsEmployeesRead()
    {
        Permissions.All.ShouldContain(Permissions.EmployeesRead);
        Permissions.EmployeesRead.ShouldBe("employees:read");
    }

    [Fact]
    public void All_containsEmployeesWrite()
    {
        Permissions.All.ShouldContain(Permissions.EmployeesWrite);
        Permissions.EmployeesWrite.ShouldBe("employees:write");
    }

    [Fact]
    public void All_containsEmployeesDelete()
    {
        Permissions.All.ShouldContain(Permissions.EmployeesDelete);
        Permissions.EmployeesDelete.ShouldBe("employees:delete");
    }

    [Fact]
    public void All_containsAuditRead()
    {
        Permissions.All.ShouldContain(Permissions.AuditRead);
        Permissions.AuditRead.ShouldBe("audit:read");
    }

    [Fact]
    public void All_containsDashboardAccess()
    {
        Permissions.All.ShouldContain(Permissions.DashboardAccess);
        Permissions.DashboardAccess.ShouldBe("dashboard:access");
    }

    [Fact]
    public void All_containsAccessGroupsManage()
    {
        Permissions.All.ShouldContain(Permissions.AccessGroupsManage);
        Permissions.AccessGroupsManage.ShouldBe("access-groups:manage");
    }
}