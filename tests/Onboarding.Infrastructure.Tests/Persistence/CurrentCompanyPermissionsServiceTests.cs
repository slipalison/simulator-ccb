using Onboarding.Application.Common;
using Onboarding.Infrastructure.Persistence;
using Shouldly;

namespace Onboarding.Infrastructure.Tests.Persistence;

/// <summary>
/// Tests for CurrentCompanyPermissionsService covering the interface-explicit members.
/// D-10: populated by ClientClaimsMiddleware from JWT claims per request.
/// </summary>
public sealed class CurrentCompanyPermissionsServiceTests
{
    [Fact]
    public void CompanyId_DefaultsToGuidEmpty()
    {
        var sut = new CurrentCompanyPermissionsService();
        sut.CompanyId.ShouldBe(Guid.Empty);
    }

    [Fact]
    public void PermissionList_DefaultsToEmpty()
    {
        var sut = new CurrentCompanyPermissionsService();
        sut.PermissionList.ShouldBeEmpty();
    }

    [Fact]
    public void IsCompanyOwnerFlag_DefaultsToFalse()
    {
        var sut = new CurrentCompanyPermissionsService();
        sut.IsCompanyOwnerFlag.ShouldBeFalse();
    }

    [Fact]
    public void CompanyId_SetThroughPublicProperty_ExposedViaInterface()
    {
        // Arrange
        var sut = new CurrentCompanyPermissionsService();
        var expected = Guid.NewGuid();

        // Act
        sut.CompanyId = expected;

        // Assert — interface member delegates to the property
        ((ICurrentCompanyPermissionsService)sut).CompanyId.ShouldBe(expected);
    }

    [Fact]
    public void Permissions_SetThroughPermissionList_ExposedViaInterface()
    {
        // Arrange
        var sut = new CurrentCompanyPermissionsService();
        sut.PermissionList = ["employees:read", "audit:read"];

        // Act
        IReadOnlyList<string> perms = ((ICurrentCompanyPermissionsService)sut).Permissions;

        // Assert
        perms.Count.ShouldBe(2);
        perms.ShouldContain("employees:read");
        perms.ShouldContain("audit:read");
    }

    [Fact]
    public void IsCompanyOwner_SetThroughFlag_ExposedViaInterface()
    {
        // Arrange
        var sut = new CurrentCompanyPermissionsService();
        sut.IsCompanyOwnerFlag = true;

        // Assert
        ((ICurrentCompanyPermissionsService)sut).IsCompanyOwner.ShouldBeTrue();
    }

    [Fact]
    public void PermissionList_CanBeReplacedWithNewList()
    {
        // Arrange
        var sut = new CurrentCompanyPermissionsService();
        sut.PermissionList = ["old:perm"];

        // Act
        sut.PermissionList = ["new:perm1", "new:perm2"];

        // Assert
        sut.PermissionList.Count.ShouldBe(2);
        sut.PermissionList[0].ShouldBe("new:perm1");
    }
}
