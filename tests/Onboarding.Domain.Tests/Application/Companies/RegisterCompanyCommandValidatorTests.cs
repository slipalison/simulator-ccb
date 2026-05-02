using FluentValidation;
using Onboarding.Application.Companies.Commands;
using Shouldly;

namespace Onboarding.Domain.Tests.Application.Companies;

public class RegisterCompanyCommandValidatorTests
{
    private readonly RegisterCompanyCommandValidator _validator = new();

    private RegisterCompanyCommand ValidCommand() => new(
        RazaoSocial: "Empresa Teste LTDA",
        Cnpj: "11444777000161",
        Email: "empresa@teste.com",
        Phone: "11999999999",
        Password: "Senha@123",
        TermsAccepted: true,
        TermsVersion: "1.0",
        IpAddress: "192.168.1.1"
    );

    [Fact]
    public void ValidCommand_PassesValidation()
    {
        var command = ValidCommand();
        var result = _validator.Validate(command);
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void TermsAccepted_False_FailsValidation()
    {
        var command = ValidCommand() with { TermsAccepted = false };
        var result = _validator.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "TermsAccepted");
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
    public void InvalidCnpj_FailsValidation()
    {
        var command = ValidCommand() with { Cnpj = "00000000000000" };
        var result = _validator.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Cnpj");
    }

    [Fact]
    public void EmptyEmail_FailsValidation()
    {
        var command = ValidCommand() with { Email = "" };
        var result = _validator.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Email");
    }

    [Fact]
    public void InvalidEmail_FailsValidation()
    {
        var command = ValidCommand() with { Email = "not-an-email" };
        var result = _validator.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Email");
    }

    [Fact]
    public void EmptyPhone_FailsValidation()
    {
        var command = ValidCommand() with { Phone = "" };
        var result = _validator.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Phone");
    }

    [Fact]
    public void Password_TooShort_FailsValidation()
    {
        var command = ValidCommand() with { Password = "Ab1@" };
        var result = _validator.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Password" && e.ErrorMessage.Contains("8"));
    }

    [Fact]
    public void Password_NoUppercase_FailsValidation()
    {
        var command = ValidCommand() with { Password = "senha@123" };
        var result = _validator.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Password");
    }

    [Fact]
    public void Password_NoLowercase_FailsValidation()
    {
        var command = ValidCommand() with { Password = "SENHA@123" };
        var result = _validator.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Password");
    }

    [Fact]
    public void Password_NoDigit_FailsValidation()
    {
        var command = ValidCommand() with { Password = "Senha@teste" };
        var result = _validator.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Password");
    }

    [Fact]
    public void Password_NoSpecialChar_FailsValidation()
    {
        var command = ValidCommand() with { Password = "Senha1234" };
        var result = _validator.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Password");
    }

    [Fact]
    public void EmptyTermsVersion_FailsValidation()
    {
        var command = ValidCommand() with { TermsVersion = "" };
        var result = _validator.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "TermsVersion");
    }
}