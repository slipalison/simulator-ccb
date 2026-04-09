using Onboarding.Domain.Aggregates.ClientAggregate;
using Shouldly;

namespace Onboarding.Domain.Tests.Aggregates;

public class ClientAnonymizeTests
{
    private const string ValidCpf = "52998224725";
    private const string ValidCnpj = "11222333000181";

    [Fact]
    public void Anonymize_ShouldSetDeletedAtAndScrubPii()
    {
        // Arrange
        var client = Client.RegisterPessoaFisica("John Doe", ValidCpf, "john@example.com", "11999999999");

        // Act
        client.Anonymize();

        // Assert
        client.DeletedAt.ShouldNotBeNull();
        client.DeletedAt.Value.Kind.ShouldBe(DateTimeKind.Utc);
        client.IsDeleted.ShouldBeTrue();
        client.Name.ShouldBe("Usuário Excluído");
        client.Email.Value.ShouldStartWith("deleted-");
        client.Email.Value.ShouldEndWith("@internal.local");
        client.Phone.Value.ShouldBe("0000000000"); // PhoneNumber.Create strips non-digits
        client.Cpf.ShouldBeNull();
        client.Cnpj.ShouldBeNull();
        client.RazaoSocial.ShouldBeNull();
    }

    [Fact]
    public void Anonymize_WhenAlreadyDeleted_ShouldBeIdempotent()
    {
        // Arrange
        var client = Client.RegisterPessoaFisica("John Doe", ValidCpf, "john@example.com", "11999999999");
        client.Anonymize();
        var firstDeletedAt = client.DeletedAt;

        // Act — call again
        client.Anonymize();

        // Assert — DeletedAt should not change
        client.DeletedAt.ShouldBe(firstDeletedAt);
        client.Name.ShouldBe("Usuário Excluído");
    }

    [Fact]
    public void Anonymize_PessoaJuridica_ShouldScrubAllPii()
    {
        // Arrange
        var client = Client.RegisterPessoaJuridica("Acme Corp", ValidCnpj, "acme@example.com", "11999999999");

        // Act
        client.Anonymize();

        // Assert
        client.DeletedAt.ShouldNotBeNull();
        client.Name.ShouldBe("Usuário Excluído");
        client.Cnpj.ShouldBeNull();
        client.RazaoSocial.ShouldBeNull();
    }
}

public class ClientUpdateTests
{
    private const string ValidCpf = "52998224725";

    [Fact]
    public void Update_WithValidData_ShouldUpdateFields()
    {
        // Arrange
        var client = Client.RegisterPessoaFisica("Old Name", ValidCpf, "old@example.com", "11999999999");

        // Act
        client.Update("New Name", null, "new@example.com", "11888888888");

        // Assert
        client.Name.ShouldBe("New Name");
        client.Email.Value.ShouldBe("new@example.com");
        client.Phone.Value.ShouldBe("11888888888");
    }

    [Fact]
    public void Update_WithEmptyName_ShouldThrowArgumentException()
    {
        // Arrange
        var client = Client.RegisterPessoaFisica("Old Name", ValidCpf, "old@example.com", "11999999999");

        // Act + Assert
        Should.Throw<ArgumentException>(() =>
            client.Update("", null, "new@example.com", "11888888888"));
    }

    [Fact]
    public void Update_WithNullName_ShouldThrowArgumentException()
    {
        // Arrange
        var client = Client.RegisterPessoaFisica("Old Name", ValidCpf, "old@example.com", "11999999999");

        // Act + Assert
        Should.Throw<ArgumentException>(() =>
            client.Update(null!, null, "new@example.com", "11888888888"));
    }

    [Fact]
    public void Update_WithWhitespaceName_ShouldThrowArgumentException()
    {
        // Arrange
        var client = Client.RegisterPessoaFisica("Old Name", ValidCpf, "old@example.com", "11999999999");

        // Act + Assert
        Should.Throw<ArgumentException>(() =>
            client.Update("   ", null, "new@example.com", "11888888888"));
    }
}
