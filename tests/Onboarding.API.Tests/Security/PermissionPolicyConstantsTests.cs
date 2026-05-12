using Onboarding.API.Security;
using Shouldly;

namespace Onboarding.API.Tests.Security;

/// <summary>
/// Tests for the fund permission policy constants added in Phase 48 (T-48.1).
/// Verifies policy name keys are stable strings used by [Authorize(Policy = ...)] attributes.
/// </summary>
[Trait("Category", "Security")]
public class PermissionPolicyConstantsTests
{
    // -------------------------------------------------------------------------
    // Happy path — constants exist and values match their own names
    // -------------------------------------------------------------------------

    [Fact]
    public void FundRead_ValueMatchesConstantName()
    {
        PermissionPolicies.FundRead.ShouldBe("FundRead");
    }

    [Fact]
    public void FundWrite_ValueMatchesConstantName()
    {
        PermissionPolicies.FundWrite.ShouldBe("FundWrite");
    }

    [Fact]
    public void FundDelete_ValueMatchesConstantName()
    {
        PermissionPolicies.FundDelete.ShouldBe("FundDelete");
    }

    [Fact]
    public void FundManage_ValueMatchesConstantName()
    {
        PermissionPolicies.FundManage.ShouldBe("FundManage");
    }

    // -------------------------------------------------------------------------
    // Uniqueness — fund policy names must not collide with each other
    // or with pre-existing policy constants (bypass attempt: no shadowing)
    // -------------------------------------------------------------------------

    [Fact]
    public void FundPolicyNames_AreDistinctFromEachOther()
    {
        var fundPolicies = new[]
        {
            PermissionPolicies.FundRead,
            PermissionPolicies.FundWrite,
            PermissionPolicies.FundDelete,
            PermissionPolicies.FundManage,
        };

        fundPolicies.Distinct().Count().ShouldBe(fundPolicies.Length);
    }

    [Fact]
    public void FundPolicyNames_DoNotCollideWithExistingPolicies()
    {
        var existingPolicies = new[]
        {
            PermissionPolicies.EmployeeRead,
            PermissionPolicies.EmployeeWrite,
            PermissionPolicies.EmployeeDelete,
            PermissionPolicies.AuditRead,
            PermissionPolicies.DashboardAccess,
            PermissionPolicies.AccessGroupsManage,
            PermissionPolicies.CrossCompanyAccess,
        };

        var fundPolicies = new[]
        {
            PermissionPolicies.FundRead,
            PermissionPolicies.FundWrite,
            PermissionPolicies.FundDelete,
            PermissionPolicies.FundManage,
        };

        foreach (var fundPolicy in fundPolicies)
        {
            existingPolicies.ShouldNotContain(fundPolicy,
                $"Fund policy '{fundPolicy}' collides with a pre-existing policy constant — " +
                "this would silently redirect authorization checks to the wrong policy.");
        }
    }

    // -------------------------------------------------------------------------
    // Security invariant — fund policy names must NOT equal Keycloak claim values
    // (policy keys and claim values are intentionally different namespaces)
    // -------------------------------------------------------------------------

    [Fact]
    public void FundPolicyNames_AreNotKeycloakClaimValues()
    {
        // Claim values use resource:action format (e.g. "funds:read").
        // Policy constants use PascalCase (e.g. "FundRead").
        // Mixing them up would break authorization wiring silently.
        var keycloakClaimValues = new[]
        {
            Domain.Aggregates.EmployeeAggregate.Permissions.FundsRead,
            Domain.Aggregates.EmployeeAggregate.Permissions.FundsWrite,
            Domain.Aggregates.EmployeeAggregate.Permissions.FundsDelete,
            Domain.Aggregates.EmployeeAggregate.Permissions.FundsManage,
        };

        var fundPolicies = new[]
        {
            PermissionPolicies.FundRead,
            PermissionPolicies.FundWrite,
            PermissionPolicies.FundDelete,
            PermissionPolicies.FundManage,
        };

        foreach (var fundPolicy in fundPolicies)
        {
            keycloakClaimValues.ShouldNotContain(fundPolicy,
                $"Policy constant '{fundPolicy}' must not equal a Keycloak claim value. " +
                "Policy names are opaque keys; claim values use resource:action format.");
        }
    }
}
