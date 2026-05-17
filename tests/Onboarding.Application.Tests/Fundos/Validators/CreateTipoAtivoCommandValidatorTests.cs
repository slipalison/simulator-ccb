using Onboarding.Application.Fundos.Commands;
using Onboarding.Domain.Aggregates.TipoAtivoAggregate;
using Shouldly;

namespace Onboarding.Application.Tests.Fundos.Validators;

public class CreateTipoAtivoCommandValidatorTests
{
    private readonly CreateTipoAtivoCommandValidator _validator = new();

    private static CreateTipoAtivoCommand ValidCommand() => new(
        Codigo: "RF-001",
        Descricao: "Título Público Federal",
        Categoria: TipoAtivoCategoria.RendaFixa,
        Subcategoria: "TPF",
        OrdemExibicao: 1
    );

    [Fact]
    public void ValidCommand_PassesValidation()
    {
        var command = ValidCommand();
        var result = _validator.Validate(command);
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void EmptyCodigo_FailsValidation()
    {
        var command = ValidCommand() with { Codigo = "" };
        var result = _validator.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Codigo");
    }

    [Fact]
    public void EmptyDescricao_FailsValidation()
    {
        var command = ValidCommand() with { Descricao = "" };
        var result = _validator.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Descricao");
    }

    [Fact]
    public void InvalidCategoriaEnum_FailsValidation()
    {
        var command = ValidCommand() with { Categoria = (TipoAtivoCategoria)999 };
        var result = _validator.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Categoria");
    }
}