using NSubstitute;
using Onboarding.Application.Common;
using Onboarding.Application.Companies.DTOs;
using Onboarding.Application.Companies.Queries;
using Onboarding.Domain.Aggregates.EmployeeAggregate;
using Onboarding.Domain.Repositories;
using Onboarding.Domain.ValueObjects;
using Shouldly;

namespace Onboarding.Domain.Tests.Application.Companies.EmployeeManagement;

public class GetCompanyEmployeesQueryHandlerTests
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IAccessGroupRepository _accessGroupRepository;
    private readonly IKeycloakUserService _keycloakUserService;
    private readonly GetCompanyEmployeesQueryHandler _sut;

    public GetCompanyEmployeesQueryHandlerTests()
    {
        _employeeRepository = Substitute.For<IEmployeeRepository>();
        _accessGroupRepository = Substitute.For<IAccessGroupRepository>();
        _keycloakUserService = Substitute.For<IKeycloakUserService>();
        _sut = new GetCompanyEmployeesQueryHandler(_employeeRepository, _accessGroupRepository, _keycloakUserService);
    }

    private Employee CreateTestEmployee(Guid companyId, Guid? id = null)
    {
        return Employee.Register("João Silva", "52998224725", "joao@empresa.com", "11999999999", companyId, Guid.NewGuid());
    }

    [Fact]
    public async Task HandleAsync_ReturnsPaginatedEmployeeList()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var employee = CreateTestEmployee(companyId);
        var query = new GetCompanyEmployeesQuery(companyId, Page: 1, PageSize: 10, Search: null, Status: null);

        _employeeRepository.GetPagedByCompanyAsync(companyId, 1, 10, null, null, Arg.Any<CancellationToken>())
            .Returns((new List<Employee> { employee }.AsReadOnly(), 1));

        // Act
        var result = await _sut.HandleAsync(query);

        // Assert
        result.ShouldNotBeNull();
        result.Items.Count.ShouldBe(1);
        result.TotalCount.ShouldBe(1);
        result.Page.ShouldBe(1);
        result.Items[0].Id.ShouldBe(employee.Id);
        result.Items[0].Nome.ShouldBe(employee.Nome);
    }

    [Fact]
    public async Task HandleAsync_EmptyCompany_ReturnsEmptyList()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var query = new GetCompanyEmployeesQuery(companyId, Page: 1, PageSize: 10, Search: null, Status: null);

        _employeeRepository.GetPagedByCompanyAsync(companyId, 1, 10, null, null, Arg.Any<CancellationToken>())
            .Returns((new List<Employee>().AsReadOnly(), 0));

        // Act
        var result = await _sut.HandleAsync(query);

        // Assert
        result.Items.Count.ShouldBe(0);
        result.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_WithSearchFilter_PassesSearchToRepository()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var query = new GetCompanyEmployeesQuery(companyId, Page: 1, PageSize: 10, Search: "joao", Status: null);

        _employeeRepository.GetPagedByCompanyAsync(companyId, 1, 10, "joao", null, Arg.Any<CancellationToken>())
            .Returns((new List<Employee>().AsReadOnly(), 0));

        // Act
        var result = await _sut.HandleAsync(query);

        // Assert
        await _employeeRepository.Received(1).GetPagedByCompanyAsync(companyId, 1, 10, "joao", null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithStatusFilter_PassesStatusToRepository()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var query = new GetCompanyEmployeesQuery(companyId, Page: 1, PageSize: 10, Search: null, Status: "active");

        _employeeRepository.GetPagedByCompanyAsync(companyId, 1, 10, null, "active", Arg.Any<CancellationToken>())
            .Returns((new List<Employee>().AsReadOnly(), 0));

        // Act
        var result = await _sut.HandleAsync(query);

        // Assert
        await _employeeRepository.Received(1).GetPagedByCompanyAsync(companyId, 1, 10, null, "active", Arg.Any<CancellationToken>());
    }
}