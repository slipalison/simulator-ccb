using Onboarding.Application.Fundos.Commands;
using Onboarding.Domain.Aggregates.FundoAggregate;
using Shouldly;

namespace Onboarding.Application.Tests.Fundos.Validators;

public class RegisterFundoCommandValidatorTests
{
    private readonly RegisterFundoCommandValidator _validator = new();

    private static RegisterFundoCommand ValidCommand() => new(
        Nome: "Fundo Teste",
        Cnpj: "11444777000161",
        ConsultoriaFundoId: Guid.NewGuid(),
        CustodianteId: Guid.NewGuid(),
        TipoFundo: TipoFundo.RendaFixa,
        ClasseAnbima: "Classe A",
        Segmento: "Segmento 1",
        DataConstituicao: null,
        ActorSub: "test-sub-123",
        ActorEmail: "actor@teste.com"
    );

    [Fact]
    public void ValidCommand_PassesValidation()
    {
        var command = ValidCommand();
        var result = _validator.Validate(command);
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void EmptyNome_FailsValidation()
    {
        var command = ValidCommand() with { Nome = "" };
        var result = _validator.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Nome");
    }

    [Fact]
    public void EmptyCnpj_FailsValidation()
    {
        var command = ValidCommand() with { Cnpj = "" };
        var result = _validator.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Cnpj");
    }

    [Fact]
    public void InvalidCnpjCheckDigits_FailsValidation()
    {
        // CNPJ with invalid check digits
        var command = ValidCommand() with { Cnpj = "00000000000000" };
        var result = _validator.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Cnpj");
    }

    [Fact]
    public void EmptyConsultoriaFundoId_FailsValidation()
    {
        var command = ValidCommand() with { ConsultoriaFundoId = Guid.Empty };
        var result = _validator.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "ConsultoriaFundoId");
    }

    [Fact]
    public void EmptyCustodianteId_FailsValidation()
    {
        var command = ValidCommand() with { CustodianteId = Guid.Empty };
        var result = _validator.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "CustodianteId");
    }

    [Fact]
    public void InvalidTipoFundoEnum_FailsValidation()
    {
        var command = ValidCommand() with { TipoFundo = (TipoFundo)999 };
        var result = _validator.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "TipoFundo");
    }
}