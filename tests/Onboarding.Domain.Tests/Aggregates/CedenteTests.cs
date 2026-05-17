using Onboarding.Domain.Aggregates.CedenteAggregate;
using Onboarding.Domain.Exceptions;
using Shouldly;

namespace Onboarding.Domain.Tests.Aggregates;

public class CedenteTests
{
    // === PF path ===

    [Fact]
    public void RegisterPf_ValidData_CreatesCedenteWithPfDocumento()
    {
        var cedente = Cedente.RegisterPf(
            cpf: "52998224725",
            nome: "João Silva",
            clientId: Guid.NewGuid());

        cedente.ShouldNotBeNull();
        cedente.Id.ShouldNotBe(Guid.Empty);
        cedente.Nome.ShouldBe("João Silva");
        cedente.Documento.IsPf.ShouldBeTrue();
        cedente.Documento.IsPj.ShouldBeFalse();
        cedente.Status.ShouldBe(CedenteStatus.ATIVO);
        cedente.ClienteId.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void RegisterPf_WithOptionalFields_SetsFields()
    {
        var cedente = Cedente.RegisterPf(
            cpf: "52998224725",
            nome: "João Silva",
            clientId: Guid.NewGuid(),
            email: "joao@example.com",
            telefone: "11999999999",
            endereco: "Rua Teste, 123");

        cedente.Email.ShouldNotBeNull();
        cedente.Telefone.ShouldNotBeNull();
        cedente.Endereco.ShouldBe("Rua Teste, 123");
    }

    [Fact]
    public void RegisterPf_EmptyNome_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() =>
            Cedente.RegisterPf("52998224725", "", Guid.NewGuid()));
    }

    [Fact]
    public void RegisterPf_EmptyClientId_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() =>
            Cedente.RegisterPf("52998224725", "João Silva", Guid.Empty));
    }

    [Fact]
    public void RegisterPf_InvalidCpf_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() =>
            Cedente.RegisterPf("00000000000", "Nome", Guid.NewGuid()));
    }

    // === PJ path ===

    [Fact]
    public void RegisterPj_ValidData_CreatesCedenteWithPjDocumento()
    {
        var cedente = Cedente.RegisterPj(
            cnpj: "11222333000181",
            razaoSocial: "Empresa LTDA",
            clientId: Guid.NewGuid());

        cedente.ShouldNotBeNull();
        cedente.Nome.ShouldBe("Empresa LTDA");
        cedente.Documento.IsPj.ShouldBeTrue();
        cedente.Documento.IsPf.ShouldBeFalse();
        cedente.Status.ShouldBe(CedenteStatus.ATIVO);
    }

    [Fact]
    public void RegisterPj_WithOptionalFields_SetsFields()
    {
        var cedente = Cedente.RegisterPj(
            cnpj: "11222333000181",
            razaoSocial: "Empresa LTDA",
            clientId: Guid.NewGuid(),
            email: "empresa@example.com",
            telefone: "21988888888",
            endereco: "Av. Teste, 456");

        cedente.Email.ShouldNotBeNull();
        cedente.Telefone.ShouldNotBeNull();
        cedente.Endereco.ShouldBe("Av. Teste, 456");
    }

    [Fact]
    public void RegisterPj_EmptyRazaoSocial_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() =>
            Cedente.RegisterPj("11222333000181", "", Guid.NewGuid()));
    }

    [Fact]
    public void RegisterPj_EmptyClientId_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() =>
            Cedente.RegisterPj("11222333000181", "Empresa LTDA", Guid.Empty));
    }

    [Fact]
    public void RegisterPj_InvalidCnpj_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() =>
            Cedente.RegisterPj("00000000000000", "Empresa", Guid.NewGuid()));
    }

    // === Update ===

    [Fact]
    public void Update_ValidData_UpdatesAllFields()
    {
        var cedente = Cedente.RegisterPf("52998224725", "João", Guid.NewGuid());
        cedente.Update("João Atualizado", "novo@example.com", "11999999999", "Novo Endereço", CedenteStatus.INATIVO);

        cedente.Nome.ShouldBe("João Atualizado");
        cedente.Email!.Value.ShouldBe("novo@example.com");
        cedente.Telefone!.Value.ShouldBe("11999999999");
        cedente.Endereco.ShouldBe("Novo Endereço");
        cedente.Status.ShouldBe(CedenteStatus.INATIVO);
    }

    [Fact]
    public void Update_EmptyNome_ThrowsArgumentException()
    {
        var cedente = Cedente.RegisterPf("52998224725", "João", Guid.NewGuid());
        Should.Throw<ArgumentException>(() =>
            cedente.Update("", null, null, null, CedenteStatus.ATIVO));
    }

    // === CedenteTipoAtivo management ===

    [Fact]
    public void AddTipoAtivo_ValidId_AddsCedenteTipoAtivo()
    {
        var cedente = Cedente.RegisterPf("52998224725", "João", Guid.NewGuid());
        var tipoId = Guid.NewGuid();

        cedente.AddTipoAtivo(tipoId);

        cedente.TiposAtivo.Count.ShouldBe(1);
        cedente.TiposAtivo[0].TipoAtivoId.ShouldBe(tipoId);
    }

    [Fact]
    public void AddTipoAtivo_DuplicateId_ThrowsDuplicateEntityException()
    {
        var cedente = Cedente.RegisterPf("52998224725", "João", Guid.NewGuid());
        var tipoId = Guid.NewGuid();

        cedente.AddTipoAtivo(tipoId);
        Should.Throw<DuplicateEntityException>(() =>
            cedente.AddTipoAtivo(tipoId));
    }

    [Fact]
    public void RemoveTipoAtivo_ExistingId_RemovesFromCollection()
    {
        var cedente = Cedente.RegisterPf("52998224725", "João", Guid.NewGuid());
        var tipoId = Guid.NewGuid();

        cedente.AddTipoAtivo(tipoId);
        cedente.RemoveTipoAtivo(tipoId);

        cedente.TiposAtivo.ShouldBeEmpty();
    }

    [Fact]
    public void RemoveTipoAtivo_NonExistentId_DoesNothing()
    {
        var cedente = Cedente.RegisterPf("52998224725", "João", Guid.NewGuid());
        cedente.RemoveTipoAtivo(Guid.NewGuid());
        cedente.TiposAtivo.ShouldBeEmpty();
    }

    [Fact]
    public void CedenteStatus_HasAtivoAndInativo()
    {
        var values = Enum.GetValues<CedenteStatus>();
        values.Length.ShouldBe(2);
        values.ShouldContain(CedenteStatus.ATIVO);
        values.ShouldContain(CedenteStatus.INATIVO);
    }
}