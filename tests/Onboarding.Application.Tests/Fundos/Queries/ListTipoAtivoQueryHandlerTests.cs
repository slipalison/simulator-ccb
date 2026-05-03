using NSubstitute;
using Onboarding.Application.Common;
using Onboarding.Application.Fundos.DTOs;
using Onboarding.Application.Fundos.Queries;
using Onboarding.Domain.Aggregates.TipoAtivoAggregate;
using Onboarding.Domain.Repositories;
using Shouldly;

namespace Onboarding.Application.Tests.Fundos.Queries;

public class ListTipoAtivoQueryHandlerTests
{
    private readonly ITipoAtivoRepository _repository;
    private readonly ListTipoAtivoQueryHandler _sut;

    public ListTipoAtivoQueryHandlerTests()
    {
        _repository = Substitute.For<ITipoAtivoRepository>();
        _sut = new ListTipoAtivoQueryHandler(_repository);
    }

    [Fact]
    public async Task HandleAsync_ReturnsPaginatedResultsWithNoCompanyFilter()
    {
        // Arrange — TipoAtivo is global per D-03, no CompanyId filter
        var query = new ListTipoAtivoQuery(Page: 1, PageSize: 10, Search: null);
        var tiposAtivo = new List<TipoAtivo>
        {
            TipoAtivo.Register("RF-001", "Título Público Federal", TipoAtivoCategoria.RendaFixa),
        };
        _repository.GetPagedAsync(1, 10, null, Arg.Any<CancellationToken>())
            .Returns((tiposAtivo.AsReadOnly(), 1));

        // Act
        var result = await _sut.HandleAsync(query);

        // Assert
        result.ShouldNotBeNull();
        result.Items.Count.ShouldBe(1);
        result.TotalCount.ShouldBe(1);
        result.Page.ShouldBe(1);
        result.PageSize.ShouldBe(10);

        // Verify NO companyId parameter in repository call (global scope)
        await _repository.Received(1).GetPagedAsync(1, 10, null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithSearchParameter_FiltersByDescricaoOrCodigo()
    {
        // Arrange
        var query = new ListTipoAtivoQuery(Page: 1, PageSize: 20, Search: "RF-001");
        var tiposAtivo = new List<TipoAtivo>();
        _repository.GetPagedAsync(1, 20, "RF-001", Arg.Any<CancellationToken>())
            .Returns((tiposAtivo.AsReadOnly(), 0));

        // Act
        var result = await _sut.HandleAsync(query);

        // Assert
        result.ShouldNotBeNull();
        result.Items.Count.ShouldBe(0);
        result.TotalCount.ShouldBe(0);

        await _repository.Received(1).GetPagedAsync(1, 20, "RF-001", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_MapsDtoFieldsCorrectly()
    {
        // Arrange
        var query = new ListTipoAtivoQuery(Page: 1, PageSize: 10, Search: null);
        var tipoAtivo = TipoAtivo.Register("RF-001", "Título Público", TipoAtivoCategoria.RendaFixa, "TPF", 1);
        var tiposAtivo = new List<TipoAtivo> { tipoAtivo };
        _repository.GetPagedAsync(1, 10, null, Arg.Any<CancellationToken>())
            .Returns((tiposAtivo.AsReadOnly(), 1));

        // Act
        var result = await _sut.HandleAsync(query);

        // Assert
        var dto = result.Items[0];
        dto.Codigo.ShouldBe("RF-001");
        dto.Descricao.ShouldBe("Título Público");
        dto.Categoria.ShouldBe(TipoAtivoCategoria.RendaFixa);
        dto.Subcategoria.ShouldBe("TPF");
        dto.Status.ShouldBe(TipoAtivoStatus.ATIVO);
        dto.OrdemExibicao.ShouldBe(1);
    }
}