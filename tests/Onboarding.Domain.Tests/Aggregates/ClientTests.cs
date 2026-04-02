using Onboarding.Domain.Aggregates.ClientAggregate;
using Shouldly;

namespace Onboarding.Domain.Tests.Aggregates;

public class ClientTests
{
    [Fact]
    public void RegisterPessoaFisica_ValidInputs_CreatesClient()
    {
        var client = Client.RegisterPessoaFisica(
            "João Silva",
            "529.982.247-25",
            "joao@example.com",
            "11999998888");

        client.ShouldNotBeNull();
        client.Id.ShouldNotBe(Guid.Empty);
        client.Type.ShouldBe(ClientType.PessoaFisica);
        client.Cpf.ShouldNotBeNull();
        client.Cnpj.ShouldBeNull();
    }

    [Fact]
    public void RegisterPessoaJuridica_ValidInputs_CreatesClient()
    {
        var client = Client.RegisterPessoaJuridica(
            "Empresa SA",
            "11222333000181",
            "empresa@example.com",
            "11999998888");

        client.ShouldNotBeNull();
        client.Type.ShouldBe(ClientType.PessoaJuridica);
        client.Cnpj.ShouldNotBeNull();
        client.Cpf.ShouldBeNull();
    }

    [Fact]
    public void RegisterPessoaFisica_InvalidCpf_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() =>
            Client.RegisterPessoaFisica(
                "João Silva",
                "000.000.000-00",
                "joao@example.com",
                "11999998888"));
    }

    [Fact]
    public void RegisterPessoaFisica_NullName_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            Client.RegisterPessoaFisica(
                null!,
                "529.982.247-25",
                "joao@example.com",
                "11999998888"));
    }

    [Fact]
    public void TwoClients_SameId_AreEqual()
    {
        var client1 = Client.RegisterPessoaFisica(
            "João Silva",
            "529.982.247-25",
            "joao@example.com",
            "11999998888");

        // Use reflection to create a second client with the same Id for equality test
        var client2 = Client.RegisterPessoaFisica(
            "Outro Nome",
            "529.982.247-25",
            "outro@example.com",
            "11999998888");

        // Set same Id to test entity equality
        var idProperty = typeof(Client).BaseType!.GetProperty("Id")!;
        idProperty.SetValue(client2, client1.Id);

        client1.ShouldBe(client2);
        (client1 == client2).ShouldBeTrue();
    }
}
