using NSubstitute;
using Onboarding.Application.Common;
using Onboarding.Application.Fundos.DTOs;
using Onboarding.Application.Fundos.Queries;
using Onboarding.Domain.Aggregates.ConsultoriaFundoAggregate;
using Onboarding.Domain.Repositories;
using Shouldly;

namespace Onboarding.Application.Tests.Fundos.Queries;

public class ListConsultoriaFundoQueryHandlerTests
{
    private readonly IConsultoriaFundoRepository _repository;
    private readonly ICurrentCompanyService _currentCompanyService;
    private readonly ListConsultoriaFundoQueryHandler _sut;

    private static readonly Guid CompanyId = Guid.NewGuid();

    public ListConsultoriaFundoQueryHandlerTests()
    {
        _repository = Substitute.For<IConsultoriaFundoRepository>();
        _currentCompanyService = Substitute.For<ICurrentCompanyService>();
        _currentCompanyService.CompanyId.Returns(CompanyId);

        _sut = new ListConsultoriaFundoQueryHandler(_repository, _currentCompanyService);
    }

    [Fact]
    public async Task HandleAsync_ReturnsPaginatedResultsFilteredByCompanyId()
    {
        // Arrange
        var query = new ListConsultoriaFundoQuery(Page: 1, PageSize: 10, Search: null);
        var consultorias = new List<ConsultoriaFundo>
        {
            ConsultoriaFundo.Register("Consultoria Teste", "11444777000161", CompanyId),
        };
        _repository.GetPagedByCompanyAsync(CompanyId, 1, 10, null, Arg.Any<CancellationToken>())
            .Returns((consultorias.AsReadOnly(), 1));

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
        var query = new ListConsultoriaFundoQuery(Page: 1, PageSize: 20, Search: "Consultoria Teste");
        var consultorias = new List<ConsultoriaFundo>();
        _repository.GetPagedByCompanyAsync(CompanyId, 1, 20, "Consultoria Teste", Arg.Any<CancellationToken>())
            .Returns((consultorias.AsReadOnly(), 0));

        // Act
        var result = await _sut.HandleAsync(query);

        // Assert
        result.ShouldNotBeNull();
        result.Items.Count.ShouldBe(0);
        result.TotalCount.ShouldBe(0);

        await _repository.Received(1).GetPagedByCompanyAsync(
            CompanyId, 1, 20, "Consultoria Teste", Arg.Any<CancellationToken>());
    }
}