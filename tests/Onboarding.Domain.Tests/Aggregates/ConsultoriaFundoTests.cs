using Onboarding.Domain.Aggregates.ConsultoriaFundoAggregate;
using Onboarding.Domain.Exceptions;
using Shouldly;

namespace Onboarding.Domain.Tests.Aggregates;

public class ConsultoriaFundoTests
{
    private static ConsultoriaFundo CreateValid() =>
        ConsultoriaFundo.Register(
            razaoSocial: "Consultoria Teste LTDA",
            cnpj: "11222333000181",
            clientId: Guid.NewGuid());

    [Fact]
    public void Register_ValidData_CreatesWithAtivoStatus()
    {
        var cf = CreateValid();
        cf.ShouldNotBeNull();
        cf.Id.ShouldNotBe(Guid.Empty);
        cf.RazaoSocial.ShouldBe("Consultoria Teste LTDA");
        cf.Status.ShouldBe(ConsultoriaFundoStatus.ATIVO);
        cf.ClienteId.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void Register_WithOptionalFields_SetsFields()
    {
        var cf = ConsultoriaFundo.Register(
            razaoSocial: "Consultoria Teste",
            cnpj: "11222333000181",
            clientId: Guid.NewGuid(),
            nomeFantasia: "CT",
            email: "test@example.com",
            telefone: "11999999999");

        cf.NomeFantasia.ShouldBe("CT");
        cf.Email.ShouldNotBeNull();
        cf.Telefone.ShouldNotBeNull();
    }

    [Fact]
    public void Register_EmptyRazaoSocial_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() =>
            ConsultoriaFundo.Register("", "11222333000181", Guid.NewGuid()));
    }

    [Fact]
    public void Register_EmptyClientId_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() =>
            ConsultoriaFundo.Register("Consultoria Teste", "11222333000181", Guid.Empty));
    }

    [Fact]
    public void Register_InvalidCnpj_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() =>
            ConsultoriaFundo.Register("Teste", "00000000000000", Guid.NewGuid()));
    }

    [Fact]
    public void Update_ValidData_UpdatesAllFields()
    {
        var cf = CreateValid();
        cf.Update("Nova Razao", "Novo Fantasia", "novo@example.com", "11988888888", ConsultoriaFundoStatus.INATIVO);

        cf.RazaoSocial.ShouldBe("Nova Razao");
        cf.NomeFantasia.ShouldBe("Novo Fantasia");
        cf.Email!.Value.ShouldBe("novo@example.com");
        cf.Telefone!.Value.ShouldBe("11988888888");
        cf.Status.ShouldBe(ConsultoriaFundoStatus.INATIVO);
    }

    [Fact]
    public void Update_EmptyRazaoSocial_ThrowsArgumentException()
    {
        var cf = CreateValid();
        Should.Throw<ArgumentException>(() =>
            cf.Update("", null, null, null, ConsultoriaFundoStatus.ATIVO));
    }

    [Fact]
    public void ConsultoriaFundoStatus_HasAtivoAndInativo()
    {
        var values = Enum.GetValues<ConsultoriaFundoStatus>();
        values.Length.ShouldBe(2);
        values.ShouldContain(ConsultoriaFundoStatus.ATIVO);
        values.ShouldContain(ConsultoriaFundoStatus.INATIVO);
    }
}