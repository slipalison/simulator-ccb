using NSubstitute;
using Onboarding.Application.Common;
using Onboarding.Application.Fundos.DTOs;
using Onboarding.Application.Fundos.Queries;
using Onboarding.Domain.Aggregates.CustodianteAggregate;
using Onboarding.Domain.Repositories;
using Shouldly;

namespace Onboarding.Application.Tests.Fundos.Queries;

public class ListCustodianteQueryHandlerTests
{
    private readonly ICustodianteRepository _repository;
    private readonly ICurrentCompanyService _currentCompanyService;
    private readonly ListCustodianteQueryHandler _sut;

    private static readonly Guid CompanyId = Guid.NewGuid();

    public ListCustodianteQueryHandlerTests()
    {
        _repository = Substitute.For<ICustodianteRepository>();
        _currentCompanyService = Substitute.For<ICurrentCompanyService>();
        _currentCompanyService.CompanyId.Returns(CompanyId);

        _sut = new ListCustodianteQueryHandler(_repository, _currentCompanyService);
    }

    [Fact]
    public async Task HandleAsync_ReturnsPaginatedResultsFilteredByCompanyId()
    {
        // Arrange
        var query = new ListCustodianteQuery(Page: 1, PageSize: 10, Search: null);
        var custodiantes = new List<Custodiante>
        {
            Custodiante.Register("Custodiante Teste", "11444777000161", CompanyId, "CUST-001"),
        };
        _repository.GetPagedByCompanyAsync(CompanyId, 1, 10, null, Arg.Any<CancellationToken>())
            .Returns((custodiantes.AsReadOnly(), 1));

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
    public async Task HandleAsync_WithSearchParameter_FiltersByRazaoSocialOrCnpj()
    {
        // Arrange
        var query = new ListCustodianteQuery(Page: 1, PageSize: 20, Search: "Custodiante Teste");
        var custodiantes = new List<Custodiante>();
        _repository.GetPagedByCompanyAsync(CompanyId, 1, 20, "Custodiante Teste", Arg.Any<CancellationToken>())
            .Returns((custodiantes.AsReadOnly(), 0));

        // Act
        var result = await _sut.HandleAsync(query);

        // Assert
        result.ShouldNotBeNull();
        result.Items.Count.ShouldBe(0);
        result.TotalCount.ShouldBe(0);

        await _repository.Received(1).GetPagedByCompanyAsync(
            CompanyId, 1, 20, "Custodiante Teste", Arg.Any<CancellationToken>());
    }
}