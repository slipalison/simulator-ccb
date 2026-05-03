using Onboarding.Domain.Aggregates.FundoAggregate;
using Onboarding.Domain.ValueObjects;
using Shouldly;

namespace Onboarding.Domain.Tests.Aggregates;

public class FundoCedenteTests
{
    // === FundoCedenteStatus enum ===

    [Fact]
    public void FundoCedenteStatus_HasAtivoAndInativo()
    {
        var values = Enum.GetValues<FundoCedenteStatus>();
        values.Length.ShouldBe(2);
        values.ShouldContain(FundoCedenteStatus.ATIVO);
        values.ShouldContain(FundoCedenteStatus.INATIVO);
    }

    // === FundoCedente creation via Fundo aggregate ===

    [Fact]
    public void AddCedente_WithLimitePercentual_SetsLimite()
    {
        var fundo = Fundo.Register("Fundo", "11222333000181", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), TipoFundo.RendaFixa);
        var cedenteId = Guid.NewGuid();
        var limite = LimiteExposicaoPercentual.Create(30m);
        var dataInicio = DateTimeOffset.UtcNow;

        fundo.AddCedente(cedenteId, limite, 50000m, dataInicio, null);

        var fc = fundo.Cedentes[0];
        fc.LimiteExposicaoPercentual.Value.ShouldBe(30m);
        fc.LimiteExposicaoPercentual.IsUnlimited.ShouldBeFalse();
        fc.LimiteExposicaoValor.ShouldBe(50000m);
        fc.DataInicio.ShouldBe(dataInicio);
        fc.DataFim.ShouldBeNull();
        fc.Status.ShouldBe(FundoCedenteStatus.ATIVO);
    }

    [Fact]
    public void AddCedente_WithUnlimitedLimite_SetsSentinelValue()
    {
        var fundo = Fundo.Register("Fundo", "11222333000181", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), TipoFundo.RendaFixa);
        var cedenteId = Guid.NewGuid();
        var limite = LimiteExposicaoPercentual.Create(-1m); // sentinel = unlimited (REL-08)
        var dataInicio = DateTimeOffset.UtcNow;

        fundo.AddCedente(cedenteId, limite, null, dataInicio, null);

        var fc = fundo.Cedentes[0];
        fc.LimiteExposicaoPercentual.IsUnlimited.ShouldBeTrue();
        fc.LimiteExposicaoValor.ShouldBeNull();
    }

    [Fact]
    public void AddCedente_WithDataFim_SetsDataFim()
    {
        var fundo = Fundo.Register("Fundo", "11222333000181", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), TipoFundo.RendaFixa);
        var cedenteId = Guid.NewGuid();
        var limite = LimiteExposicaoPercentual.Create(100m);
        var dataInicio = DateTimeOffset.UtcNow;
        var dataFim = dataInicio.AddYears(1);

        fundo.AddCedente(cedenteId, limite, null, dataInicio, dataFim);

        var fc = fundo.Cedentes[0];
        fc.DataFim.ShouldBe(dataFim);
    }

    // === UpdateCedente details ===

    [Fact]
    public void UpdateCedente_Deactivate_SetsStatusToInativo()
    {
        var fundo = Fundo.Register("Fundo", "11222333000181", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), TipoFundo.RendaFixa);
        var cedenteId = Guid.NewGuid();
        var limite = LimiteExposicaoPercentual.Create(50m);
        var dataInicio = DateTimeOffset.UtcNow;

        fundo.AddCedente(cedenteId, limite, null, dataInicio, null);
        fundo.UpdateCedente(cedenteId, limite, null, dataInicio, null, FundoCedenteStatus.INATIVO);

        fundo.Cedentes[0].Status.ShouldBe(FundoCedenteStatus.INATIVO);
    }

    // === Fundamental TipoAtivo entity ===

    [Fact]
    public void FundoTipoAtivo_SetsTipoAtivoId()
    {
        var fundo = Fundo.Register("Fundo", "11222333000181", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), TipoFundo.RendaFixa);
        var tipoAtivoId = Guid.NewGuid();

        fundo.AddTipoAtivo(tipoAtivoId);

        fundo.TiposAtivo[0].TipoAtivoId.ShouldBe(tipoAtivoId);
        fundo.TiposAtivo[0].FundoId.ShouldBe(fundo.Id);
    }
}