using Onboarding.Domain.Aggregates.FundoAggregate;
using Onboarding.Domain.Exceptions;
using Onboarding.Domain.ValueObjects;
using Shouldly;

namespace Onboarding.Domain.Tests.Aggregates;

public class FundoTests
{
    private static Fundo CreateValidFundo() =>
        Fundo.Register(
            nome: "Fundo Teste",
            cnpj: "11222333000181",
            clientId: Guid.NewGuid(),
            consultoriaFundoId: Guid.NewGuid(),
            custodianteId: Guid.NewGuid(),
            tipoFundo: TipoFundo.RendaFixa);

    // === Register factory ===

    [Fact]
    public void Register_ValidData_CreatesFundoWithRascunhoStatus()
    {
        var fundo = CreateValidFundo();

        fundo.ShouldNotBeNull();
        fundo.Id.ShouldNotBe(Guid.Empty);
        fundo.Nome.ShouldBe("Fundo Teste");
        fundo.Status.ShouldBe(FundoStatus.RASCUNHO);
        fundo.TipoFundo.ShouldBe(TipoFundo.RendaFixa);
        fundo.Cedentes.ShouldBeEmpty();
        fundo.TiposAtivo.ShouldBeEmpty();
    }

    [Fact]
    public void Register_ValidData_SetsCnpjAndFks()
    {
        var clientId = Guid.NewGuid();
        var consultoriaId = Guid.NewGuid();
        var custodianteId = Guid.NewGuid();

        var fundo = Fundo.Register(
            nome: "Fundo Teste",
            cnpj: "11222333000181",
            clientId: clientId,
            consultoriaFundoId: consultoriaId,
            custodianteId: custodianteId,
            tipoFundo: TipoFundo.Multimercado);

        fundo.ClienteId.ShouldBe(clientId);
        fundo.ConsultoriaFundoId.ShouldBe(consultoriaId);
        fundo.CustodianteId.ShouldBe(custodianteId);
        fundo.Cnpj.Value.ShouldBe("11222333000181");
    }

    [Fact]
    public void Register_WithOptionalFields_SetsFields()
    {
        var dataConst = new DateTimeOffset(2024, 1, 15, 0, 0, 0, TimeSpan.Zero);

        var fundo = Fundo.Register(
            nome: "Fundo Teste",
            cnpj: "11222333000181",
            clientId: Guid.NewGuid(),
            consultoriaFundoId: Guid.NewGuid(),
            custodianteId: Guid.NewGuid(),
            tipoFundo: TipoFundo.RendaFixa,
            classeAnbima: "Classe A",
            segmento: "Segmento 1",
            dataConstituicao: dataConst);

        fundo.ClasseAnbima.ShouldBe("Classe A");
        fundo.Segmento.ShouldBe("Segmento 1");
        fundo.DataConstituicao.ShouldBe(dataConst);
    }

    [Fact]
    public void Register_EmptyNome_ThrowsArgumentException()
    {
            Should.Throw<ArgumentException>(() =>
            Fundo.Register("", "11222333000181", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), TipoFundo.RendaFixa));
    }

    [Fact]
    public void Register_InvalidCnpj_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() =>
            Fundo.Register("Fundo Teste", "00000000000000", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), TipoFundo.RendaFixa));
    }

    // === TransitionTo state machine ===

    [Fact]
    public void TransitionTo_RascunhoToAtivo_Succeeds()
    {
        var fundo = CreateValidFundo();
        fundo.TransitionTo(FundoStatus.ATIVO);
        fundo.Status.ShouldBe(FundoStatus.ATIVO);
    }

    [Fact]
    public void TransitionTo_AtivoToSuspenso_Succeeds()
    {
        var fundo = CreateValidFundo();
        fundo.TransitionTo(FundoStatus.ATIVO);
        fundo.TransitionTo(FundoStatus.SUSPENSO);
        fundo.Status.ShouldBe(FundoStatus.SUSPENSO);
    }

    [Fact]
    public void TransitionTo_SuspensoToAtivo_Succeeds()
    {
        var fundo = CreateValidFundo();
        fundo.TransitionTo(FundoStatus.ATIVO);
        fundo.TransitionTo(FundoStatus.SUSPENSO);
        fundo.TransitionTo(FundoStatus.ATIVO);
        fundo.Status.ShouldBe(FundoStatus.ATIVO);
    }

    [Fact]
    public void TransitionTo_AtivoToEmLiquidacao_Succeeds()
    {
        var fundo = CreateValidFundo();
        fundo.TransitionTo(FundoStatus.ATIVO);
        fundo.TransitionTo(FundoStatus.EM_LIQUIDACAO);
        fundo.Status.ShouldBe(FundoStatus.EM_LIQUIDACAO);
    }

    [Fact]
    public void TransitionTo_EmLiquidacaoToEncerrado_Succeeds()
    {
        var fundo = CreateValidFundo();
        fundo.TransitionTo(FundoStatus.ATIVO);
        fundo.TransitionTo(FundoStatus.EM_LIQUIDACAO);
        fundo.TransitionTo(FundoStatus.ENCERRADO);
        fundo.Status.ShouldBe(FundoStatus.ENCERRADO);
    }

    [Fact]
    public void TransitionTo_SuspensoToEmLiquidacao_ViaAtivoSucceeds()
    {
        // SUSPENSO → ATIVO → EM_LIQUIDACAO (valid path)
        var fundo = CreateValidFundo();
        fundo.TransitionTo(FundoStatus.ATIVO);
        fundo.TransitionTo(FundoStatus.SUSPENSO);
        fundo.TransitionTo(FundoStatus.ATIVO);
        fundo.TransitionTo(FundoStatus.EM_LIQUIDACAO);
        fundo.Status.ShouldBe(FundoStatus.EM_LIQUIDACAO);
    }

    // === Invalid transitions ===

    [Fact]
    public void TransitionTo_RascunhoToSuspenso_ThrowsInvalidStateTransitionException()
    {
        var fundo = CreateValidFundo();
        Should.Throw<InvalidStateTransitionException>(() =>
            fundo.TransitionTo(FundoStatus.SUSPENSO));
    }

    [Fact]
    public void TransitionTo_RascunhoToEmLiquidacao_ThrowsInvalidStateTransitionException()
    {
        var fundo = CreateValidFundo();
        Should.Throw<InvalidStateTransitionException>(() =>
            fundo.TransitionTo(FundoStatus.EM_LIQUIDACAO));
    }

    [Fact]
    public void TransitionTo_EncerradoToAtivo_ThrowsInvalidStateTransitionException()
    {
        var fundo = CreateValidFundo();
        fundo.TransitionTo(FundoStatus.ATIVO);
        fundo.TransitionTo(FundoStatus.EM_LIQUIDACAO);
        fundo.TransitionTo(FundoStatus.ENCERRADO);
        Should.Throw<InvalidStateTransitionException>(() =>
            fundo.TransitionTo(FundoStatus.ATIVO));
    }

    [Fact]
    public void TransitionTo_SameStatus_ThrowsInvalidStateTransitionException()
    {
        var fundo = CreateValidFundo();
        Should.Throw<InvalidStateTransitionException>(() =>
            fundo.TransitionTo(FundoStatus.RASCUNHO));
    }

    // === Update ===

    [Fact]
    public void Update_ValidData_UpdatesFields()
    {
        var fundo = CreateValidFundo();
        var dataConst = new DateTimeOffset(2023, 6, 1, 0, 0, 0, TimeSpan.Zero);

        fundo.Update("Novo Nome", "Nova Classe", "Novo Segmento", dataConst);

        fundo.Nome.ShouldBe("Novo Nome");
        fundo.ClasseAnbima.ShouldBe("Nova Classe");
        fundo.Segmento.ShouldBe("Novo Segmento");
        fundo.DataConstituicao.ShouldBe(dataConst);
    }

    [Fact]
    public void Update_EmptyNome_ThrowsArgumentException()
    {
        var fundo = CreateValidFundo();
        Should.Throw<ArgumentException>(() =>
            fundo.Update("", null, null, null));
    }

    // === AddCedente (REL-09) ===

    [Fact]
    public void AddCedente_ValidData_AddsFundoCedente()
    {
        var fundo = CreateValidFundo();
        var cedenteId = Guid.NewGuid();
        var limite = LimiteExposicaoPercentual.Create(50m);
        var dataInicio = DateTimeOffset.UtcNow;

        fundo.AddCedente(cedenteId, limite, null, dataInicio, null);

        fundo.Cedentes.Count.ShouldBe(1);
        fundo.Cedentes[0].CedenteId.ShouldBe(cedenteId);
        fundo.Cedentes[0].LimiteExposicaoPercentual.ShouldBe(limite);
        fundo.Cedentes[0].Status.ShouldBe(FundoCedenteStatus.ATIVO);
    }

    [Fact]
    public void AddCedente_DuplicateActiveCedente_ThrowsDuplicateEntityException()
    {
        var fundo = CreateValidFundo();
        var cedenteId = Guid.NewGuid();
        var limite = LimiteExposicaoPercentual.Create(50m);
        var dataInicio = DateTimeOffset.UtcNow;

        fundo.AddCedente(cedenteId, limite, null, dataInicio, null);

        // REL-09: at most one active association per Fundo-Cedente pair
        Should.Throw<DuplicateEntityException>(() =>
            fundo.AddCedente(cedenteId, limite, null, dataInicio, null));
    }

    [Fact]
    public void AddCedente_InactiveThenActiveSameCedente_Succeeds()
    {
        // Multiple INATIVO associations with same CedenteId are allowed
        var fundo = CreateValidFundo();
        var cedenteId = Guid.NewGuid();
        var limite = LimiteExposicaoPercentual.Create(50m);
        var dataInicio = DateTimeOffset.UtcNow;

        fundo.AddCedente(cedenteId, limite, null, dataInicio, null);
        // Deactivate first
        fundo.UpdateCedente(cedenteId, limite, null, dataInicio, null, FundoCedenteStatus.INATIVO);

        // Now adding same cedente as ATIVO should succeed
        fundo.AddCedente(cedenteId, limite, null, dataInicio, null);
        fundo.Cedentes.Count.ShouldBe(2);
    }

    [Fact]
    public void AddCedente_DifferentCedentes_Succeeds()
    {
        var fundo = CreateValidFundo();
        var limite = LimiteExposicaoPercentual.Create(50m);
        var dataInicio = DateTimeOffset.UtcNow;

        fundo.AddCedente(Guid.NewGuid(), limite, null, dataInicio, null);
        fundo.AddCedente(Guid.NewGuid(), limite, null, dataInicio, null);

        fundo.Cedentes.Count.ShouldBe(2);
    }

    // === UpdateCedente ===

    [Fact]
    public void UpdateCedente_UpdatesLimitesAndStatus()
    {
        var fundo = CreateValidFundo();
        var cedenteId = Guid.NewGuid();
        var limite = LimiteExposicaoPercentual.Create(50m);
        var dataInicio = DateTimeOffset.UtcNow;

        fundo.AddCedente(cedenteId, limite, null, dataInicio, null);

        var newLimite = LimiteExposicaoPercentual.Create(75m);
        var newValor = 100000m;
        var dataFim = DateTimeOffset.UtcNow.AddDays(30);

        fundo.UpdateCedente(cedenteId, newLimite, newValor, dataInicio, dataFim, FundoCedenteStatus.INATIVO);

        var fc = fundo.Cedentes[0];
        fc.LimiteExposicaoPercentual.ShouldBe(newLimite);
        fc.LimiteExposicaoValor.ShouldBe(newValor);
        fc.DataFim.ShouldBe(dataFim);
        fc.Status.ShouldBe(FundoCedenteStatus.INATIVO);
    }

    // === RemoveCedente ===

    [Fact]
    public void RemoveCedente_ExistingCedente_RemovesFromCollection()
    {
        var fundo = CreateValidFundo();
        var cedenteId = Guid.NewGuid();
        var limite = LimiteExposicaoPercentual.Create(50m);
        var dataInicio = DateTimeOffset.UtcNow;

        fundo.AddCedente(cedenteId, limite, null, dataInicio, null);
        fundo.Cedentes.Count.ShouldBe(1);

        fundo.RemoveCedente(cedenteId);
        fundo.Cedentes.ShouldBeEmpty();
    }

    [Fact]
    public void RemoveCedente_NonExistentCedente_DoesNothing()
    {
        var fundo = CreateValidFundo();
        // Should not throw — idempotent
        fundo.RemoveCedente(Guid.NewGuid());
        fundo.Cedentes.ShouldBeEmpty();
    }

    // === AddTipoAtivo / RemoveTipoAtivo ===

    [Fact]
    public void AddTipoAtivo_ValidId_AddsFundoTipoAtivo()
    {
        var fundo = CreateValidFundo();
        var tipoAtivoId = Guid.NewGuid();

        fundo.AddTipoAtivo(tipoAtivoId);

        fundo.TiposAtivo.Count.ShouldBe(1);
        fundo.TiposAtivo[0].TipoAtivoId.ShouldBe(tipoAtivoId);
    }

    [Fact]
    public void AddTipoAtivo_DuplicateId_ThrowsDuplicateEntityException()
    {
        var fundo = CreateValidFundo();
        var tipoAtivoId = Guid.NewGuid();

        fundo.AddTipoAtivo(tipoAtivoId);
        Should.Throw<DuplicateEntityException>(() =>
            fundo.AddTipoAtivo(tipoAtivoId));
    }

    [Fact]
    public void RemoveTipoAtivo_ExistingId_RemovesFromCollection()
    {
        var fundo = CreateValidFundo();
        var tipoAtivoId = Guid.NewGuid();

        fundo.AddTipoAtivo(tipoAtivoId);
        fundo.RemoveTipoAtivo(tipoAtivoId);

        fundo.TiposAtivo.ShouldBeEmpty();
    }

    [Fact]
    public void RemoveTipoAtivo_NonExistentId_DoesNothing()
    {
        var fundo = CreateValidFundo();
        fundo.RemoveTipoAtivo(Guid.NewGuid());
        fundo.TiposAtivo.ShouldBeEmpty();
    }
}