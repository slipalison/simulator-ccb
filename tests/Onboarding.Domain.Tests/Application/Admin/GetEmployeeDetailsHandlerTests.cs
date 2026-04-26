using NSubstitute;
using Onboarding.Application.Admin.DTOs;
using Onboarding.Application.Admin.Queries;
using Onboarding.Domain.Aggregates.CompanyAggregate;
using Onboarding.Domain.Aggregates.EmployeeAggregate;
using Onboarding.Domain.Repositories;
using Onboarding.Domain.ValueObjects;
using Shouldly;

namespace Onboarding.Domain.Tests.Application.Admin;

public class GetEmployeeDetailsHandlerTests
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly GetEmployeeDetailsHandler _sut;

    public GetEmployeeDetailsHandlerTests()
    {
        _employeeRepository = Substitute.For<IEmployeeRepository>();
        _companyRepository = Substitute.For<ICompanyRepository>();
        _sut = new GetEmployeeDetailsHandler(_employeeRepository, _companyRepository);
    }

    private static Employee CreateTestEmployee(string nome = "João Silva", string cpf = "52998224725", string email = "joao@empresa.com", Guid? companyId = null, Guid? accessGroupId = null)
    {
        var cGuid = companyId ?? Guid.NewGuid();
        var aGuid = accessGroupId ?? Guid.NewGuid();
        return Employee.Register(nome, cpf, email, "11999999999", cGuid, aGuid);
    }

    [Fact]
    public async Task HandleAsync_ReturnsEmployeeSummaryDto_ForExistingEmployee()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var employee = CreateTestEmployee(nome: "João Silva", cpf: "52998224725", email: "joao@empresa.com", companyId: companyId);
        var company = Company.Register("Empresa Alpha", "11444777000161", "alpha@empresa.com", "11999999999",
            TermsAcceptance.Create("1.0", "127.0.0.1"));

        _employeeRepository.GetByIdIgnoreFilterAsync(employee.Id, Arg.Any<CancellationToken>()).Returns(employee);
        _companyRepository.GetByIdAsync(companyId, Arg.Any<CancellationToken>()).Returns(company);

        var query = new GetEmployeeDetailsQuery(employee.Id);

        // Act
        var result = await _sut.HandleAsync(query);

        // Assert
        result.ShouldNotBeNull();
        result.Nome.ShouldBe("João Silva");
        result.Email.ShouldBe("joao@empresa.com");
        result.CompanyRazaoSocial.ShouldBe("Empresa Alpha");
        result.CompanyId.ShouldBe(companyId);
    }

    [Fact]
    public async Task HandleAsync_ThrowsKeyNotFoundException_ForMissingEmployee()
    {
        // Arrange
        var missingId = Guid.NewGuid();
        _employeeRepository.GetByIdIgnoreFilterAsync(missingId, Arg.Any<CancellationToken>()).Returns((Employee?)null);

        var query = new GetEmployeeDetailsQuery(missingId);

        // Act & Assert
        await Should.ThrowAsync<KeyNotFoundException>(() => _sut.HandleAsync(query));
    }
}