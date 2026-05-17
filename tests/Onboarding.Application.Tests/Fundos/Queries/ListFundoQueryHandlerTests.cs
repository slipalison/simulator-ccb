using NSubstitute;
using Onboarding.Application.Common;
using Onboarding.Application.Fundos.DTOs;
using Onboarding.Application.Fundos.Queries;
using Onboarding.Domain.Aggregates.FundoAggregate;
using Onboarding.Domain.Repositories;
using Shouldly;

namespace Onboarding.Application.Tests.Fundos.Queries;

public class ListFundoQueryHandlerTests
{
    private readonly IFundoRepository _repository;
    private readonly ICurrentCompanyService _currentCompanyService;
    private readonly ListFundoQueryHandler _sut;

    private static readonly Guid CompanyId = Guid.NewGuid();

    public ListFundoQueryHandlerTests()
    {
        _repository = Substitute.For<IFundoRepository>();
        _currentCompanyService = Substitute.For<ICurrentCompanyService>();
        _currentCompanyService.CompanyId.Returns(CompanyId);

        _sut = new ListFundoQueryHandler(_repository, _currentCompanyService);
    }

    [Fact]
    public async Task HandleAsync_ReturnsPaginatedResultsFilteredByCompanyId()
    {
        // Arrange
        var query = new ListFundoQuery(Page: 1, PageSize: 10, Search: null);
        var fundos = new List<Fundo>
        {
            Fundo.Register("Fundo 1", "11444777000161", CompanyId, Guid.NewGuid(), Guid.NewGuid(), TipoFundo.RendaFixa),
        };
        _repository.GetPagedByCompanyAsync(CompanyId, 1, 10, null, Arg.Any<CancellationToken>())
            .Returns((fundos.AsReadOnly(), 1));

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
    public async Task HandleAsync_WithSearchParameter_FiltersByNomeOrCnpj()
    {
        // Arrange
        var query = new ListFundoQuery(Page: 1, PageSize: 20, Search: "Fundo Teste");
        var fundos = new List<Fundo>();
        _repository.GetPagedByCompanyAsync(CompanyId, 1, 20, "Fundo Teste", Arg.Any<CancellationToken>())
            .Returns((fundos.AsReadOnly(), 0));

        // Act
        var result = await _sut.HandleAsync(query);

        // Assert
        result.ShouldNotBeNull();
        result.Items.Count.ShouldBe(0);
        result.TotalCount.ShouldBe(0);

        await _repository.Received(1).GetPagedByCompanyAsync(
            CompanyId, 1, 20, "Fundo Teste", Arg.Any<CancellationToken>());
    }
}