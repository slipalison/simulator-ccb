using NSubstitute;
using Onboarding.Application.Admin.DTOs;
using Onboarding.Application.Admin.Queries;
using Onboarding.Domain.Aggregates.CompanyAggregate;
using Onboarding.Domain.Repositories;
using Onboarding.Domain.ValueObjects;
using Shouldly;

namespace Onboarding.Domain.Tests.Application.Admin;

public class GetCompanyDetailsHandlerTests
{
    private readonly ICompanyRepository _companyRepository;
    private readonly GetCompanyDetailsHandler _sut;

    public GetCompanyDetailsHandlerTests()
    {
        _companyRepository = Substitute.For<ICompanyRepository>();
        _sut = new GetCompanyDetailsHandler(
            _companyRepository,
            Substitute.For<Microsoft.Extensions.Logging.ILogger<GetCompanyDetailsHandler>>());
    }

    private static Company CreateTestCompany()
    {
        return Company.Register("Empresa Detalhe", "11444777000161", "detalhe@teste.com", "11999999999",
            TermsAcceptance.Create("1.0", "127.0.0.1"));
    }

    [Fact]
    public async Task HandleAsync_ReturnsCompanySummaryDto_ForExistingCompany()
    {
        // Arrange
        var company = CreateTestCompany();
        var companyId = company.Id;
        _companyRepository.GetByIdAsync(companyId, Arg.Any<CancellationToken>()).Returns(company);

        var query = new GetCompanyDetailsQuery(companyId);

        // Act
        var result = await _sut.HandleAsync(query);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(companyId);
        result.RazaoSocial.ShouldBe("Empresa Detalhe");
        result.Email.ShouldBe("detalhe@teste.com");
    }

    [Fact]
    public async Task HandleAsync_ThrowsKeyNotFoundException_ForMissingCompany()
    {
        // Arrange
        var missingId = Guid.NewGuid();
        _companyRepository.GetByIdAsync(missingId, Arg.Any<CancellationToken>()).Returns((Company?)null);

        var query = new GetCompanyDetailsQuery(missingId);

        // Act & Assert
        await Should.ThrowAsync<KeyNotFoundException>(() => _sut.HandleAsync(query));
    }
}