using Onboarding.Application.Companies.DTOs;
using Shouldly;

namespace Onboarding.Application.Tests.Companies.DTOs;

/// <summary>
/// Tests for EmployeeListItemDto — employee list item scoped to company (MGMT-02).
/// </summary>
public sealed class EmployeeListItemDtoTests
{
    // -------------------------------------------------------------------------
    // Constructor / property exposure
    // -------------------------------------------------------------------------

    [Fact]
    public void Constructor_WithAllProperties_SetsAllMembers()
    {
        var id = Guid.NewGuid();
        var accessGroupId = Guid.NewGuid();

        var dto = new EmployeeListItemDto(
            Id: id,
            Nome: "Alice Silva",
            Cpf: "52998224725",
            Email: "alice@empresa.com",
            Phone: "+5511999990000",
            AccessGroupId: accessGroupId,
            AccessGroupName: "Gestor",
            IsDeleted: false,
            KeycloakEnabled: true);

        dto.Id.ShouldBe(id);
        dto.Nome.ShouldBe("Alice Silva");
        dto.Cpf.ShouldBe("52998224725");
        dto.Email.ShouldBe("alice@empresa.com");
        dto.Phone.ShouldBe("+5511999990000");
        dto.AccessGroupId.ShouldBe(accessGroupId);
        dto.AccessGroupName.ShouldBe("Gestor");
        dto.IsDeleted.ShouldBeFalse();
        dto.KeycloakEnabled.ShouldBeTrue();
    }

    [Fact]
    public void Constructor_WithNullCpf_SetsNullCpf()
    {
        var dto = new EmployeeListItemDto(
            Id: Guid.NewGuid(),
            Nome: "Bob Souza",
            Cpf: null,
            Email: "bob@empresa.com",
            Phone: "+5521999990000",
            AccessGroupId: Guid.NewGuid(),
            AccessGroupName: "Analista",
            IsDeleted: false,
            KeycloakEnabled: false);

        dto.Cpf.ShouldBeNull();
        dto.KeycloakEnabled.ShouldBeFalse();
    }

    [Fact]
    public void Constructor_IsDeletedTrue_ExposesTrueIsDeleted()
    {
        var dto = new EmployeeListItemDto(
            Id: Guid.NewGuid(),
            Nome: "Carlos Lima",
            Cpf: "52998224725",
            Email: "carlos@empresa.com",
            Phone: "+5531999990000",
            AccessGroupId: Guid.NewGuid(),
            AccessGroupName: "Operador",
            IsDeleted: true,
            KeycloakEnabled: false);

        dto.IsDeleted.ShouldBeTrue();
    }

    // -------------------------------------------------------------------------
    // Record equality (structural)
    // -------------------------------------------------------------------------

    [Fact]
    public void Equality_WhenSameValues_ReturnsTrue()
    {
        var id = Guid.NewGuid();
        var groupId = Guid.NewGuid();

        var dto1 = new EmployeeListItemDto(id, "Alice", "52998224725", "alice@x.com", "+5511", groupId, "Gestor", false, true);
        var dto2 = new EmployeeListItemDto(id, "Alice", "52998224725", "alice@x.com", "+5511", groupId, "Gestor", false, true);

        dto1.ShouldBe(dto2);
    }

    [Fact]
    public void Equality_WhenDifferentId_ReturnsFalse()
    {
        var groupId = Guid.NewGuid();

        var dto1 = new EmployeeListItemDto(Guid.NewGuid(), "Alice", null, "alice@x.com", "+5511", groupId, "Gestor", false, true);
        var dto2 = new EmployeeListItemDto(Guid.NewGuid(), "Alice", null, "alice@x.com", "+5511", groupId, "Gestor", false, true);

        dto1.ShouldNotBe(dto2);
    }

    [Fact]
    public void Equality_WhenDifferentKeycloakEnabled_ReturnsFalse()
    {
        var id = Guid.NewGuid();
        var groupId = Guid.NewGuid();

        var dto1 = new EmployeeListItemDto(id, "Alice", null, "alice@x.com", "+5511", groupId, "Gestor", false, true);
        var dto2 = new EmployeeListItemDto(id, "Alice", null, "alice@x.com", "+5511", groupId, "Gestor", false, false);

        dto1.ShouldNotBe(dto2);
    }

    // -------------------------------------------------------------------------
    // With-expression
    // -------------------------------------------------------------------------

    [Fact]
    public void WithExpression_OverridingIsDeleted_UpdatesField()
    {
        var original = new EmployeeListItemDto(
            Guid.NewGuid(), "Diana", null, "d@x.com", "+55", Guid.NewGuid(), "G", false, true);

        var updated = original with { IsDeleted = true };

        updated.IsDeleted.ShouldBeTrue();
        updated.Nome.ShouldBe("Diana");
    }

    [Fact]
    public void WithExpression_OverridingEmail_UpdatesEmail()
    {
        var original = new EmployeeListItemDto(
            Guid.NewGuid(), "Eva", null, "old@x.com", "+55", Guid.NewGuid(), "G", false, false);

        var updated = original with { Email = "new@x.com" };

        updated.Email.ShouldBe("new@x.com");
        updated.Nome.ShouldBe("Eva");
    }
}
