using Onboarding.Application.Companies.Commands;
using Shouldly;

namespace Onboarding.Application.Tests.Companies.Validators;

/// <summary>
/// Tests for RegisterEmployeeCommandValidator — exercises each validation rule branch (MGMT-01).
/// One test per rule: valid path + each invalid path.
/// </summary>
public sealed class RegisterEmployeeCommandValidatorTests
{
    private readonly RegisterEmployeeCommandValidator _validator = new();

    // Valid CPF that passes check-digit algorithm
    private const string ValidCpf = "52998224725";

    private static RegisterEmployeeCommand ValidCommand() => new(
        CompanyId: Guid.NewGuid(),
        Nome: "Alice Silva",
        Cpf: ValidCpf,
        Email: "alice@empresa.com",
        Phone: "+5511999990000",
        AccessGroupId: Guid.NewGuid(),
        ActorSub: "sub-actor",
        ActorEmail: "actor@admin.com",
        IpAddress: null);

    // -------------------------------------------------------------------------
    // Happy path
    // -------------------------------------------------------------------------

    [Fact]
    public void ValidCommand_AllFieldsValid_PassesValidation()
    {
        var result = _validator.Validate(ValidCommand());

        result.IsValid.ShouldBeTrue();
    }

    // -------------------------------------------------------------------------
    // Nome
    // -------------------------------------------------------------------------

    [Fact]
    public void Validate_EmptyNome_FailsWithNomeError()
    {
        var cmd = ValidCommand() with { Nome = "" };

        var result = _validator.Validate(cmd);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Nome");
    }

    [Fact]
    public void Validate_WhitespaceOnlyNome_FailsWithNomeError()
    {
        var cmd = ValidCommand() with { Nome = "   " };

        var result = _validator.Validate(cmd);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Nome");
    }

    // -------------------------------------------------------------------------
    // CPF
    // -------------------------------------------------------------------------

    [Fact]
    public void Validate_EmptyCpf_FailsWithCpfRequired()
    {
        var cmd = ValidCommand() with { Cpf = "" };

        var result = _validator.Validate(cmd);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Cpf" && e.ErrorMessage.Contains("required"));
    }

    [Fact]
    public void Validate_InvalidCpfFormat_FailsWithInvalidCpfMessage()
    {
        // 11 digits but wrong check digits
        var cmd = ValidCommand() with { Cpf = "12345678900" };

        var result = _validator.Validate(cmd);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Cpf" && e.ErrorMessage.Contains("Invalid CPF"));
    }

    [Fact]
    public void Validate_AllSameDigitCpf_FailsWithInvalidCpfMessage()
    {
        // e.g. 111.111.111-11 — passes length but fails domain validation
        var cmd = ValidCommand() with { Cpf = "11111111111" };

        var result = _validator.Validate(cmd);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Cpf");
    }

    [Fact]
    public void Validate_CpfWithFormatting_PassesValidation()
    {
        // Cpf.Create strips dots and dashes — formatted CPF should pass
        var cmd = ValidCommand() with { Cpf = "529.982.247-25" };

        var result = _validator.Validate(cmd);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_CpfTooShort_FailsWithInvalidCpfMessage()
    {
        var cmd = ValidCommand() with { Cpf = "1234567890" }; // 10 digits

        var result = _validator.Validate(cmd);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Cpf");
    }

    [Fact]
    public void Validate_CpfWithLetters_FailsWithInvalidCpfMessage()
    {
        var cmd = ValidCommand() with { Cpf = "ABC98224725" };

        var result = _validator.Validate(cmd);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Cpf");
    }

    // -------------------------------------------------------------------------
    // Email
    // -------------------------------------------------------------------------

    [Fact]
    public void Validate_EmptyEmail_FailsWithEmailRequired()
    {
        var cmd = ValidCommand() with { Email = "" };

        var result = _validator.Validate(cmd);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Email");
    }

    [Fact]
    public void Validate_InvalidEmailFormat_FailsWithEmailFormatError()
    {
        var cmd = ValidCommand() with { Email = "not-an-email" };

        var result = _validator.Validate(cmd);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Email");
    }

    [Fact]
    public void Validate_EmailMissingDomain_FailsValidation()
    {
        var cmd = ValidCommand() with { Email = "alice@" };

        var result = _validator.Validate(cmd);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Email");
    }

    // -------------------------------------------------------------------------
    // Phone
    // -------------------------------------------------------------------------

    [Fact]
    public void Validate_EmptyPhone_FailsWithPhoneRequired()
    {
        var cmd = ValidCommand() with { Phone = "" };

        var result = _validator.Validate(cmd);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Phone");
    }

    [Fact]
    public void Validate_WhitespaceOnlyPhone_FailsWithPhoneRequired()
    {
        var cmd = ValidCommand() with { Phone = "   " };

        var result = _validator.Validate(cmd);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Phone");
    }

    // -------------------------------------------------------------------------
    // CompanyId
    // -------------------------------------------------------------------------

    [Fact]
    public void Validate_EmptyCompanyId_FailsWithCompanyIdRequired()
    {
        var cmd = ValidCommand() with { CompanyId = Guid.Empty };

        var result = _validator.Validate(cmd);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "CompanyId");
    }

    // -------------------------------------------------------------------------
    // Multiple errors in a single invalid command
    // -------------------------------------------------------------------------

    [Fact]
    public void Validate_MultipleInvalidFields_ReportsAllErrors()
    {
        var cmd = ValidCommand() with
        {
            Nome = "",
            Email = "bad-email",
            CompanyId = Guid.Empty
        };

        var result = _validator.Validate(cmd);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Nome");
        result.Errors.ShouldContain(e => e.PropertyName == "Email");
        result.Errors.ShouldContain(e => e.PropertyName == "CompanyId");
    }

    // -------------------------------------------------------------------------
    // Error messages accuracy
    // -------------------------------------------------------------------------

    [Fact]
    public void Validate_EmptyNome_ErrorMessageContainsRequired()
    {
        var cmd = ValidCommand() with { Nome = "" };

        var result = _validator.Validate(cmd);

        result.Errors.ShouldContain(e => e.PropertyName == "Nome" && e.ErrorMessage.Contains("required"));
    }

    [Fact]
    public void Validate_EmptyPhone_ErrorMessageContainsRequired()
    {
        var cmd = ValidCommand() with { Phone = "" };

        var result = _validator.Validate(cmd);

        result.Errors.ShouldContain(e => e.PropertyName == "Phone" && e.ErrorMessage.Contains("required"));
    }

    [Fact]
    public void Validate_EmptyCompanyId_ErrorMessageContainsRequired()
    {
        var cmd = ValidCommand() with { CompanyId = Guid.Empty };

        var result = _validator.Validate(cmd);

        result.Errors.ShouldContain(e => e.PropertyName == "CompanyId" && e.ErrorMessage.Contains("required"));
    }
}
