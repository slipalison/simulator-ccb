using Onboarding.Application.Admin.DTOs;
using Shouldly;

namespace Onboarding.Application.Tests.Admin.DTOs;

/// <summary>
/// Tests for EmployeeSummaryDto and CompanySummaryDto — admin paginated listing DTOs (ADMIN-01).
/// </summary>
public sealed class AdminDtoTests
{
    // =========================================================================
    // EmployeeSummaryDto
    // =========================================================================

    [Fact]
    public void EmployeeSummaryDto_Constructor_WithAllProperties_SetsAllMembers()
    {
        var id = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var accessGroupId = Guid.NewGuid();

        var dto = new EmployeeSummaryDto(
            Id: id,
            Nome: "Alice Silva",
            Cpf: "52998224725",
            Email: "alice@corp.com",
            Phone: "+5511999990000",
            CompanyId: companyId,
            CompanyRazaoSocial: "Corp Ltda",
            AccessGroupId: accessGroupId,
            AccessGroupName: "Gestor",
            IsDeleted: false,
            KeycloakUserId: "kc-user-abc");

        dto.Id.ShouldBe(id);
        dto.Nome.ShouldBe("Alice Silva");
        dto.Cpf.ShouldBe("52998224725");
        dto.Email.ShouldBe("alice@corp.com");
        dto.Phone.ShouldBe("+5511999990000");
        dto.CompanyId.ShouldBe(companyId);
        dto.CompanyRazaoSocial.ShouldBe("Corp Ltda");
        dto.AccessGroupId.ShouldBe(accessGroupId);
        dto.AccessGroupName.ShouldBe("Gestor");
        dto.IsDeleted.ShouldBeFalse();
        dto.KeycloakUserId.ShouldBe("kc-user-abc");
    }

    [Fact]
    public void EmployeeSummaryDto_Constructor_WithNullOptionals_SetsNullsCorrectly()
    {
        var dto = new EmployeeSummaryDto(
            Id: Guid.NewGuid(),
            Nome: "Bob Souza",
            Cpf: "52998224725",
            Email: "bob@corp.com",
            Phone: "+5521",
            CompanyId: Guid.NewGuid(),
            CompanyRazaoSocial: null,
            AccessGroupId: Guid.NewGuid(),
            AccessGroupName: null,
            IsDeleted: true,
            KeycloakUserId: null);

        dto.CompanyRazaoSocial.ShouldBeNull();
        dto.AccessGroupName.ShouldBeNull();
        dto.KeycloakUserId.ShouldBeNull();
        dto.IsDeleted.ShouldBeTrue();
    }

    [Fact]
    public void EmployeeSummaryDto_Equality_WhenSameValues_ReturnsTrue()
    {
        var id = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var groupId = Guid.NewGuid();

        var dto1 = new EmployeeSummaryDto(id, "Alice", "12345678901", "a@b.com", "+55", companyId, "Corp", groupId, "G", false, "kc-1");
        var dto2 = new EmployeeSummaryDto(id, "Alice", "12345678901", "a@b.com", "+55", companyId, "Corp", groupId, "G", false, "kc-1");

        dto1.ShouldBe(dto2);
    }

    [Fact]
    public void EmployeeSummaryDto_Equality_WhenDifferentKeycloakUserId_ReturnsFalse()
    {
        var id = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var groupId = Guid.NewGuid();

        var dto1 = new EmployeeSummaryDto(id, "Alice", "12345678901", "a@b.com", "+55", companyId, null, groupId, null, false, "kc-1");
        var dto2 = new EmployeeSummaryDto(id, "Alice", "12345678901", "a@b.com", "+55", companyId, null, groupId, null, false, "kc-2");

        dto1.ShouldNotBe(dto2);
    }

    [Fact]
    public void EmployeeSummaryDto_Equality_WhenDifferentCompanyId_ReturnsFalse()
    {
        var id = Guid.NewGuid();
        var groupId = Guid.NewGuid();

        var dto1 = new EmployeeSummaryDto(id, "Alice", "12345678901", "a@b.com", "+55", Guid.NewGuid(), null, groupId, null, false, null);
        var dto2 = new EmployeeSummaryDto(id, "Alice", "12345678901", "a@b.com", "+55", Guid.NewGuid(), null, groupId, null, false, null);

        dto1.ShouldNotBe(dto2);
    }

    [Fact]
    public void EmployeeSummaryDto_WithExpression_OverridingIsDeleted_UpdatesField()
    {
        var original = new EmployeeSummaryDto(
            Guid.NewGuid(), "Carlos", "12345678901", "c@x.com", "+55",
            Guid.NewGuid(), "Corp", Guid.NewGuid(), "G", false, null);

        var deleted = original with { IsDeleted = true };

        deleted.IsDeleted.ShouldBeTrue();
        deleted.Nome.ShouldBe("Carlos");
    }

    [Fact]
    public void EmployeeSummaryDto_WithExpression_OverridingKeycloakUserId_UpdatesId()
    {
        var original = new EmployeeSummaryDto(
            Guid.NewGuid(), "Diana", "12345678901", "d@x.com", "+55",
            Guid.NewGuid(), null, Guid.NewGuid(), null, false, null);

        var updated = original with { KeycloakUserId = "kc-new" };

        updated.KeycloakUserId.ShouldBe("kc-new");
        updated.Nome.ShouldBe("Diana");
    }

    // =========================================================================
    // CompanySummaryDto
    // =========================================================================

    [Fact]
    public void CompanySummaryDto_Constructor_WithAllProperties_SetsAllMembers()
    {
        var id = Guid.NewGuid();

        var dto = new CompanySummaryDto(
            Id: id,
            RazaoSocial: "Empresa LTDA",
            Cnpj: "11222333000181",
            Email: "contato@empresa.com",
            Phone: "+5511999990000",
            IsDeleted: false,
            KeycloakUserId: "kc-company-xyz");

        dto.Id.ShouldBe(id);
        dto.RazaoSocial.ShouldBe("Empresa LTDA");
        dto.Cnpj.ShouldBe("11222333000181");
        dto.Email.ShouldBe("contato@empresa.com");
        dto.Phone.ShouldBe("+5511999990000");
        dto.IsDeleted.ShouldBeFalse();
        dto.KeycloakUserId.ShouldBe("kc-company-xyz");
    }

    [Fact]
    public void CompanySummaryDto_Constructor_WithNullKeycloakUserId_SetsNull()
    {
        var dto = new CompanySummaryDto(
            Id: Guid.NewGuid(),
            RazaoSocial: "Empresa SA",
            Cnpj: "11222333000181",
            Email: "info@empresa.com",
            Phone: "+5521",
            IsDeleted: true,
            KeycloakUserId: null);

        dto.KeycloakUserId.ShouldBeNull();
        dto.IsDeleted.ShouldBeTrue();
    }

    [Fact]
    public void CompanySummaryDto_Equality_WhenSameValues_ReturnsTrue()
    {
        var id = Guid.NewGuid();

        var dto1 = new CompanySummaryDto(id, "Corp", "11222333000181", "a@b.com", "+55", false, "kc-1");
        var dto2 = new CompanySummaryDto(id, "Corp", "11222333000181", "a@b.com", "+55", false, "kc-1");

        dto1.ShouldBe(dto2);
    }

    [Fact]
    public void CompanySummaryDto_Equality_WhenDifferentId_ReturnsFalse()
    {
        var dto1 = new CompanySummaryDto(Guid.NewGuid(), "Corp", "11222333000181", "a@b.com", "+55", false, null);
        var dto2 = new CompanySummaryDto(Guid.NewGuid(), "Corp", "11222333000181", "a@b.com", "+55", false, null);

        dto1.ShouldNotBe(dto2);
    }

    [Fact]
    public void CompanySummaryDto_Equality_WhenDifferentIsDeleted_ReturnsFalse()
    {
        var id = Guid.NewGuid();

        var dto1 = new CompanySummaryDto(id, "Corp", "11222333000181", "a@b.com", "+55", false, null);
        var dto2 = new CompanySummaryDto(id, "Corp", "11222333000181", "a@b.com", "+55", true, null);

        dto1.ShouldNotBe(dto2);
    }

    [Fact]
    public void CompanySummaryDto_Equality_WhenDifferentKeycloakUserId_ReturnsFalse()
    {
        var id = Guid.NewGuid();

        var dto1 = new CompanySummaryDto(id, "Corp", "11222333000181", "a@b.com", "+55", false, "kc-1");
        var dto2 = new CompanySummaryDto(id, "Corp", "11222333000181", "a@b.com", "+55", false, "kc-2");

        dto1.ShouldNotBe(dto2);
    }

    [Fact]
    public void CompanySummaryDto_WithExpression_OverridingRazaoSocial_UpdatesName()
    {
        var original = new CompanySummaryDto(Guid.NewGuid(), "Old Corp", "11222333000181", "a@b.com", "+55", false, null);

        var updated = original with { RazaoSocial = "New Corp" };

        updated.RazaoSocial.ShouldBe("New Corp");
        updated.Cnpj.ShouldBe("11222333000181");
    }

    [Fact]
    public void CompanySummaryDto_WithExpression_OverridingIsDeleted_UpdatesFlag()
    {
        var original = new CompanySummaryDto(Guid.NewGuid(), "Corp", "11222333000181", "a@b.com", "+55", false, "kc-1");

        var deleted = original with { IsDeleted = true };

        deleted.IsDeleted.ShouldBeTrue();
        deleted.RazaoSocial.ShouldBe("Corp");
    }
}
