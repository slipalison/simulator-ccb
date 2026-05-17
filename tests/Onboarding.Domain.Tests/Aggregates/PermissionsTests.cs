using Onboarding.Domain.Aggregates.EmployeeAggregate;
using Shouldly;

namespace Onboarding.Domain.Tests.Aggregates;

public class PermissionsTests
{
    [Fact]
    public void All_containsExactly10Permissions()
    {
        Permissions.All.Length.ShouldBe(10);
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

    [Fact]
    public void All_containsFundsRead()
    {
        Permissions.All.ShouldContain(Permissions.FundsRead);
        Permissions.FundsRead.ShouldBe("funds:read");
    }

    [Fact]
    public void All_containsFundsWrite()
    {
        Permissions.All.ShouldContain(Permissions.FundsWrite);
        Permissions.FundsWrite.ShouldBe("funds:write");
    }

    [Fact]
    public void All_containsFundsDelete()
    {
        Permissions.All.ShouldContain(Permissions.FundsDelete);
        Permissions.FundsDelete.ShouldBe("funds:delete");
    }

    [Fact]
    public void All_containsFundsManage()
    {
        Permissions.All.ShouldContain(Permissions.FundsManage);
        Permissions.FundsManage.ShouldBe("funds:manage");
    }
}