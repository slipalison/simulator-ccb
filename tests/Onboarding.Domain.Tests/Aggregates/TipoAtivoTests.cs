using Onboarding.Domain.Aggregates.TipoAtivoAggregate;
using Shouldly;

namespace Onboarding.Domain.Tests.Aggregates;

public class TipoAtivoTests
{
    private static TipoAtivo CreateValid() =>
        TipoAtivo.Register(
            codigo: "RF-001",
            descricao: "Título Pública Federal",
            categoria: TipoAtivoCategoria.RendaFixa);

    [Fact]
    public void Register_ValidData_CreatesWithAtivoStatus()
    {
        var ta = CreateValid();
        ta.ShouldNotBeNull();
        ta.Id.ShouldNotBe(Guid.Empty);
        ta.Codigo.ShouldBe("RF-001");
        ta.Descricao.ShouldBe("Título Pública Federal");
        ta.Categoria.ShouldBe(TipoAtivoCategoria.RendaFixa);
        ta.Status.ShouldBe(TipoAtivoStatus.ATIVO);
        ta.OrdemExibicao.ShouldBe(0);
    }

    [Fact]
    public void Register_WithOptionalFields_SetsFields()
    {
        var ta = TipoAtivo.Register(
            codigo: "RV-001",
            descricao: "Ação",
            categoria: TipoAtivoCategoria.RendaVariavel,
            subcategoria: "Ações Brasil",
            ordemExibicao: 5);

        ta.Subcategoria.ShouldBe("Ações Brasil");
        ta.OrdemExibicao.ShouldBe(5);
    }

    [Fact]
    public void Register_EmptyCodigo_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() =>
            TipoAtivo.Register("", "Descrição", TipoAtivoCategoria.RendaFixa));
    }

    [Fact]
    public void Register_EmptyDescricao_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() =>
            TipoAtivo.Register("COD-1", "", TipoAtivoCategoria.RendaFixa));
    }

    [Fact]
    public void Update_ValidData_UpdatesFields()
    {
        var ta = CreateValid();
        ta.Update("Nova Descrição", "Nova Subcategoria", TipoAtivoStatus.INATIVO, 10);

        ta.Descricao.ShouldBe("Nova Descrição");
        ta.Subcategoria.ShouldBe("Nova Subcategoria");
        ta.Status.ShouldBe(TipoAtivoStatus.INATIVO);
        ta.OrdemExibicao.ShouldBe(10);
    }

    [Fact]
    public void Update_EmptyDescricao_ThrowsArgumentException()
    {
        var ta = CreateValid();
        Should.Throw<ArgumentException>(() =>
            ta.Update("", null, TipoAtivoStatus.ATIVO, 0));
    }

    [Fact]
    public void TipoAtivoStatus_HasAtivoAndInativo()
    {
        var values = Enum.GetValues<TipoAtivoStatus>();
        values.Length.ShouldBe(2);
        values.ShouldContain(TipoAtivoStatus.ATIVO);
        values.ShouldContain(TipoAtivoStatus.INATIVO);
    }

    [Fact]
    public void TipoAtivo_HasNoClientIdProperty()
    {
        // D-03/TEN-03: TipoAtivo is a global entity — no ClientId
        var ta = CreateValid();
        var hasClientId = ta.GetType().GetProperty("ClienteId") ?? ta.GetType().GetProperty("ClientId");
        hasClientId.ShouldBeNull();
    }
}