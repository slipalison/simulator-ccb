using Onboarding.Application.Fundos.Commands;
using Shouldly;

namespace Onboarding.Application.Tests.Fundos.Validators;

public class RegisterCedentePfCommandValidatorTests
{
    private readonly RegisterCedentePfCommandValidator _validator = new();

    private static RegisterCedentePfCommand ValidCommand() => new(
        Cpf: "52998224725",
        Nome: "João da Silva",
        Email: "joao@teste.com",
        Telefone: "11999999999",
        Endereco: "Rua Teste, 123",
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
    public void EmptyCpf_FailsValidation()
    {
        var command = ValidCommand() with { Cpf = "" };
        var result = _validator.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Cpf");
    }

    [Fact]
    public void InvalidCpfCheckDigits_FailsValidation()
    {
        // CPF with invalid check digits (all same digits)
        var command = ValidCommand() with { Cpf = "11111111111" };
        var result = _validator.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Cpf");
    }

    [Fact]
    public void InvalidCpfWrongCheckDigit_FailsValidation()
    {
        // CPF with wrong check digit
        var command = ValidCommand() with { Cpf = "52998224726" };
        var result = _validator.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Cpf");
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
    public void InvalidEmailFormat_FailsValidation()
    {
        var command = ValidCommand() with { Email = "not-an-email" };
        var result = _validator.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Email");
    }

    [Fact]
    public void NullEmail_PassesValidation()
    {
        // Email is optional
        var command = ValidCommand() with { Email = null };
        var result = _validator.Validate(command);
        result.IsValid.ShouldBeTrue();
    }
}