using Onboarding.Domain.Aggregates.CustodianteAggregate;
using Onboarding.Domain.Exceptions;
using Shouldly;

namespace Onboarding.Domain.Tests.Aggregates;

public class CustodianteTests
{
    private static Custodiante CreateValid() =>
        Custodiante.Register(
            razaoSocial: "Custodiante Teste SA",
            cnpj: "11222333000181",
            clientId: Guid.NewGuid());

    [Fact]
    public void Register_ValidData_CreatesWithAtivoStatus()
    {
        var c = CreateValid();
        c.ShouldNotBeNull();
        c.Id.ShouldNotBe(Guid.Empty);
        c.RazaoSocial.ShouldBe("Custodiante Teste SA");
        c.Status.ShouldBe(CustodianteStatus.ATIVO);
        c.ClienteId.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void Register_WithCodigoInterno_SetsCodigoInterno()
    {
        var c = Custodiante.Register(
            razaoSocial: "Custodiante Teste",
            cnpj: "11222333000181",
            clientId: Guid.NewGuid(),
            codigoInterno: "CUST-001");

        c.CodigoInterno.ShouldBe("CUST-001");
    }

    [Fact]
    public void Register_EmptyRazaoSocial_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() =>
            Custodiante.Register("", "11222333000181", Guid.NewGuid()));
    }

    [Fact]
    public void Register_EmptyClientId_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() =>
            Custodiante.Register("Custodiante Teste", "11222333000181", Guid.Empty));
    }

    [Fact]
    public void Register_InvalidCnpj_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() =>
            Custodiante.Register("Teste", "00000000000000", Guid.NewGuid()));
    }

    [Fact]
    public void Update_ValidData_UpdatesAllFields()
    {
        var c = CreateValid();
        c.Update("Nova Razao", "COD-002", "novo@example.com", "11988888888", CustodianteStatus.INATIVO);

        c.RazaoSocial.ShouldBe("Nova Razao");
        c.CodigoInterno.ShouldBe("COD-002");
        c.Email!.Value.ShouldBe("novo@example.com");
        c.Telefone!.Value.ShouldBe("11988888888");
        c.Status.ShouldBe(CustodianteStatus.INATIVO);
    }

    [Fact]
    public void Update_EmptyRazaoSocial_ThrowsArgumentException()
    {
        var c = CreateValid();
        Should.Throw<ArgumentException>(() =>
            c.Update("", null, null, null, CustodianteStatus.ATIVO));
    }

    [Fact]
    public void CustodianteStatus_HasAtivoAndInativo()
    {
        var values = Enum.GetValues<CustodianteStatus>();
        values.Length.ShouldBe(2);
        values.ShouldContain(CustodianteStatus.ATIVO);
        values.ShouldContain(CustodianteStatus.INATIVO);
    }
}