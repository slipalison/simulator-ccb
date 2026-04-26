using NSubstitute;
using Onboarding.Application.Admin.DTOs;
using Onboarding.Application.Admin.Queries;
using Onboarding.Domain.Aggregates.CompanyAggregate;
using Onboarding.Domain.Aggregates.EmployeeAggregate;
using Onboarding.Domain.Repositories;
using Onboarding.Domain.ValueObjects;
using Shouldly;

namespace Onboarding.Domain.Tests.Application.Admin;

public class GetPaginatedEmployeesHandlerTests
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly GetPaginatedEmployeesHandler _sut;

    public GetPaginatedEmployeesHandlerTests()
    {
        _employeeRepository = Substitute.For<IEmployeeRepository>();
        _companyRepository = Substitute.For<ICompanyRepository>();
        _sut = new GetPaginatedEmployeesHandler(_employeeRepository, _companyRepository);
    }

    private static Employee CreateTestEmployee(string nome = "João Silva", string cpf = "52998224725", string email = "joao@empresa.com", Guid? companyId = null, Guid? accessGroupId = null)
    {
        var cGuid = companyId ?? Guid.NewGuid();
        var aGuid = accessGroupId ?? Guid.NewGuid();
        return Employee.Register(nome, cpf, email, "11999999999", cGuid, aGuid);
    }

    [Fact]
    public async Task HandleAsync_ReturnsEmployeesFromAllCompanies_WhenCompanyIdIsNull()
    {
        // Arrange — admin query with no company filter
        var employees = new List<Employee>
        {
            CreateTestEmployee(nome: "João"),
            CreateTestEmployee(nome: "Maria")
        }.AsReadOnly();

        _employeeRepository.GetPagedAllAsync(1, 20, null, null, Arg.Any<CancellationToken>())
            .Returns((employees, 2));

        var query = new GetPaginatedEmployeesQuery(Page: 1, PageSize: 20);

        // Act
        var result = await _sut.HandleAsync(query);

        // Assert
        result.Items.Count.ShouldBe(2);
        result.TotalCount.ShouldBe(2);
        result.Page.ShouldBe(1);
    }

    [Fact]
    public async Task HandleAsync_ReturnsEmployeesFilteredByCompanyId_WhenCompanyIdProvided()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var employees = new List<Employee>
        {
            CreateTestEmployee(nome: "João", companyId: companyId)
        }.AsReadOnly();

        _employeeRepository.GetPagedByCompanyAsync(companyId, 1, 20, null, null, Arg.Any<CancellationToken>())
            .Returns((employees, 1));

        var query = new GetPaginatedEmployeesQuery(Page: 1, PageSize: 20, CompanyId: companyId);

        // Act
        var result = await _sut.HandleAsync(query);

        // Assert
        result.Items.Count.ShouldBe(1);
        result.TotalCount.ShouldBe(1);
        await _employeeRepository.Received(1).GetPagedByCompanyAsync(companyId, 1, 20, null, null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_AppliesSearchAndStatusFilters()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var employees = new List<Employee>().AsReadOnly();

        _employeeRepository.GetPagedByCompanyAsync(companyId, 1, 20, "joao", "active", Arg.Any<CancellationToken>())
            .Returns((employees, 0));

        var query = new GetPaginatedEmployeesQuery(Page: 1, PageSize: 20, Search: "joao", Status: "active", CompanyId: companyId);

        // Act
        var result = await _sut.HandleAsync(query);

        // Assert — passes search and status to repository
        await _employeeRepository.Received(1).GetPagedByCompanyAsync(companyId, 1, 20, "joao", "active", Arg.Any<CancellationToken>());
        result.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_IncludesCompanyRazaoSocial_InDto()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var company = Company.Register("Empresa Alpha", "11444777000161", "alpha@empresa.com", "11999999999",
            TermsAcceptance.Create("1.0", "127.0.0.1"));
        var employee = CreateTestEmployee(nome: "João", companyId: companyId);

        _employeeRepository.GetPagedAllAsync(1, 20, null, null, Arg.Any<CancellationToken>())
            .Returns((new List<Employee> { employee }.AsReadOnly(), 1));
        _companyRepository.GetByIdAsync(companyId, Arg.Any<CancellationToken>()).Returns(company);

        var query = new GetPaginatedEmployeesQuery(Page: 1, PageSize: 20);

        // Act
        var result = await _sut.HandleAsync(query);

        // Assert
        result.Items[0].CompanyRazaoSocial.ShouldBe("Empresa Alpha");
    }
}