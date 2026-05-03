using NSubstitute;
using Onboarding.Application.Common;
using Onboarding.Application.Fundos.DTOs;
using Onboarding.Application.Fundos.Queries;
using Onboarding.Domain.Aggregates.CedenteAggregate;
using Onboarding.Domain.Repositories;
using Shouldly;

namespace Onboarding.Application.Tests.Fundos.Queries;

public class ListCedenteQueryHandlerTests
{
    private readonly ICedenteRepository _repository;
    private readonly ICurrentCompanyService _currentCompanyService;
    private readonly ListCedenteQueryHandler _sut;

    private static readonly Guid CompanyId = Guid.NewGuid();

    public ListCedenteQueryHandlerTests()
    {
        _repository = Substitute.For<ICedenteRepository>();
        _currentCompanyService = Substitute.For<ICurrentCompanyService>();
        _currentCompanyService.CompanyId.Returns(CompanyId);

        _sut = new ListCedenteQueryHandler(_repository, _currentCompanyService);
    }

    [Fact]
    public async Task HandleAsync_ReturnsPaginatedResultsFilteredByCompanyId()
    {
        // Arrange
        var query = new ListCedenteQuery(Page: 1, PageSize: 10, Search: null);
        var cedentes = new List<Cedente>
        {
            Cedente.RegisterPf("52998224725", "João da Silva", CompanyId, "joao@teste.com"),
        };
        _repository.GetPagedByCompanyAsync(CompanyId, 1, 10, null, Arg.Any<CancellationToken>())
            .Returns((cedentes.AsReadOnly(), 1));

        // Act
        var result = await _sut.HandleAsync(query);

        // Assert
        result.ShouldNotBeNull();
        result.Items.Count.ShouldBe(1);
        result.TotalCount.ShouldBe(1);
        result.Page.ShouldBe(1);
        result.PageSize.ShouldBe(10);

        await _repository.Received(1).GetPagedByCompanyAsync(
            CompanyId, 1, 10, null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithSearchParameter_FiltersByNomeOrDocumento()
    {
        // Arrange
        var query = new ListCedenteQuery(Page: 1, PageSize: 20, Search: "João");
        var cedentes = new List<Cedente>();
        _repository.GetPagedByCompanyAsync(CompanyId, 1, 20, "João", Arg.Any<CancellationToken>())
            .Returns((cedentes.AsReadOnly(), 0));

        // Act
        var result = await _sut.HandleAsync(query);

        // Assert
        result.ShouldNotBeNull();
        result.Items.Count.ShouldBe(0);
        result.TotalCount.ShouldBe(0);

        await _repository.Received(1).GetPagedByCompanyAsync(
            CompanyId, 1, 20, "João", Arg.Any<CancellationToken>());
    }
}