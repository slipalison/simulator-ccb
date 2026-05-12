using Onboarding.API.Security;
using Shouldly;

namespace Onboarding.API.Tests.Security;

/// <summary>
/// Tests for PermissionPolicyConstants — verifies fund policy constants exist with correct values.
/// These are compile-time constants so this test guards against accidental renaming or value changes.
/// </summary>
public class PermissionPolicyConstantsTests
{
    [Fact]
    public void FundRead_HasCorrectValue()
    {
        PermissionPolicies.FundRead.ShouldBe("FundRead");
    }

    [Fact]
    public void FundWrite_HasCorrectValue()
    {
        PermissionPolicies.FundWrite.ShouldBe("FundWrite");
    }

    [Fact]
    public void FundDelete_HasCorrectValue()
    {
        PermissionPolicies.FundDelete.ShouldBe("FundDelete");
    }

    [Fact]
    public void FundManage_HasCorrectValue()
    {
        PermissionPolicies.FundManage.ShouldBe("FundManage");
    }

    [Fact]
    public void ExistingPolicies_AreUnchanged()
    {
        PermissionPolicies.EmployeeRead.ShouldBe("EmployeeRead");
        PermissionPolicies.EmployeeWrite.ShouldBe("EmployeeWrite");
        PermissionPolicies.EmployeeDelete.ShouldBe("EmployeeDelete");
        PermissionPolicies.AuditRead.ShouldBe("AuditRead");
        PermissionPolicies.DashboardAccess.ShouldBe("DashboardAccess");
        PermissionPolicies.AccessGroupsManage.ShouldBe("AccessGroupsManage");
        PermissionPolicies.CrossCompanyAccess.ShouldBe("CrossCompanyAccess");
    }
}