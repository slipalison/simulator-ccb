using NSubstitute;
using Onboarding.Application.Admin.DTOs;
using Onboarding.Application.Admin.Queries;
using Onboarding.Domain.Aggregates.CompanyAggregate;
using Onboarding.Domain.Repositories;
using Onboarding.Domain.ValueObjects;
using Shouldly;

namespace Onboarding.Domain.Tests.Application.Admin;

public class GetPaginatedCompaniesHandlerTests
{
    private readonly ICompanyRepository _companyRepository;
    private readonly GetPaginatedCompaniesHandler _sut;

    public GetPaginatedCompaniesHandlerTests()
    {
        _companyRepository = Substitute.For<ICompanyRepository>();
        _sut = new GetPaginatedCompaniesHandler(
            _companyRepository,
            Substitute.For<Microsoft.Extensions.Logging.ILogger<GetPaginatedCompaniesHandler>>());
    }

    private static Company CreateTestCompany(string razaoSocial = "Empresa Teste", string cnpj = "11444777000161", string email = "empresa@teste.com")
    {
        return Company.Register(razaoSocial, cnpj, email, "11999999999",
            TermsAcceptance.Create("1.0", "127.0.0.1"));
    }

    [Fact]
    public async Task HandleAsync_ReturnsPaginatedCompanySummaryDtos()
    {
        // Arrange
        var companies = new List<Company>
        {
            CreateTestCompany("Empresa Alpha"),
            CreateTestCompany("Empresa Beta")
        };
        _companyRepository.GetPagedAsync(1, 20, null, null, Arg.Any<CancellationToken>())
            .Returns((companies.AsReadOnly(), 2));

        var query = new GetPaginatedCompaniesQuery(Page: 1, PageSize: 20);

        // Act
        var result = await _sut.HandleAsync(query);

        // Assert
        result.Items.Count.ShouldBe(2);
        result.TotalCount.ShouldBe(2);
        result.Page.ShouldBe(1);
        result.PageSize.ShouldBe(20);
    }

    [Fact]
    public async Task HandleAsync_PassSearchAndStatus_ToRepository()
    {
        // Arrange
        _companyRepository.GetPagedAsync(2, 10, "alpha", "active", Arg.Any<CancellationToken>())
            .Returns((new List<Company>().AsReadOnly(), 0));

        var query = new GetPaginatedCompaniesQuery(Page: 2, PageSize: 10, Search: "alpha", Status: "active");

        // Act
        var result = await _sut.HandleAsync(query);

        // Assert
        result.Items.Count.ShouldBe(0);
        result.TotalCount.ShouldBe(0);
        await _companyRepository.Received(1).GetPagedAsync(2, 10, "alpha", "active", Arg.Any<CancellationToken>());
    }
}