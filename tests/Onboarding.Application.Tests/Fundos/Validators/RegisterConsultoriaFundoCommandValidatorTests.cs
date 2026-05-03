using Onboarding.Application.Fundos.Commands;
using Shouldly;

namespace Onboarding.Application.Tests.Fundos.Validators;

public class RegisterConsultoriaFundoCommandValidatorTests
{
    private readonly RegisterConsultoriaFundoCommandValidator _validator = new();

    private static RegisterConsultoriaFundoCommand ValidCommand() => new(
        RazaoSocial: "Consultoria Teste LTDA",
        Cnpj: "11444777000161",
        NomeFantasia: "Consultoria Teste",
        Email: "consultoria@teste.com",
        Telefone: "11999999999"
    );

    [Fact]
    public void ValidCommand_PassesValidation()
    {
        var command = ValidCommand();
        var result = _validator.Validate(command);
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void EmptyRazaoSocial_FailsValidation()
    {
        var command = ValidCommand() with { RazaoSocial = "" };
        var result = _validator.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "RazaoSocial");
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
        // CNPJ with invalid check digits (all zeros)
        var command = ValidCommand() with { Cnpj = "00000000000000" };
        var result = _validator.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Cnpj");
    }

    [Fact]
    public void InvalidCnpjFormat_FailsValidation()
    {
        // CNPJ with wrong check digit
        var command = ValidCommand() with { Cnpj = "11444777000162" };
        var result = _validator.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Cnpj");
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

    [Fact]
    public void EmptyEmail_PassesValidation()
    {
        // Empty email is allowed (.EmailAddress().When(...) pattern skips empty strings)
        var command = ValidCommand() with { Email = "" };
        var result = _validator.Validate(command);
        // FluentValidation EmailAddress validator does not fail on empty when When condition skips it
        result.IsValid.ShouldBeTrue();
    }
}