using NSubstitute;
using Onboarding.Application.Admin.Commands;
using Onboarding.Application.Common;
using Onboarding.Domain.Aggregates.Audit;
using Onboarding.Domain.Aggregates.EmployeeAggregate;
using Onboarding.Domain.Repositories;
using Shouldly;

namespace Onboarding.Domain.Tests.Application.Admin;

public class DeleteEmployeeHandlerTests
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IKeycloakUserService _keycloakUserService;
    private readonly IAuditService _auditService;
    private readonly DeleteEmployeeCommandHandler _sut;

    public DeleteEmployeeHandlerTests()
    {
        _employeeRepository = Substitute.For<IEmployeeRepository>();
        _keycloakUserService = Substitute.For<IKeycloakUserService>();
        _auditService = Substitute.For<IAuditService>();
        _sut = new DeleteEmployeeCommandHandler(_employeeRepository, _keycloakUserService, _auditService);
    }

    private static Employee CreateTestEmployee(Guid? companyId = null)
    {
        return Employee.Register("João Silva", "52998224725", "joao@empresa.com", "11999999999",
            companyId ?? Guid.NewGuid(), Guid.NewGuid());
    }

    [Fact]
    public async Task HandleAsync_AnonymizesEmployee_DeletesKeycloakUser_And_Audits()
    {
        // Arrange
        var employee = CreateTestEmployee();
        employee.SetKeycloakUserId("keycloak-user-123");
        _employeeRepository.GetByIdIgnoreFilterAsync(employee.Id, Arg.Any<CancellationToken>()).Returns(employee);

        var command = new DeleteEmployeeCommand(employee.Id, ActorSub: "admin-sub");

        // Act
        await _sut.HandleAsync(command);

        // Assert
        await _employeeRepository.Received(1).SaveAsync(Arg.Is<Employee>(e =>
            e.Nome == "Usuário Excluído" && e.IsDeleted), Arg.Any<CancellationToken>());
        await _keycloakUserService.Received(1).DeleteUserByEmailAsync("client", "joao@empresa.com", Arg.Any<CancellationToken>());
        await _auditService.Received(1).RecordAsync(
            "admin-sub",
            Arg.Any<string>(),
            ActionType.EmployeeDeleted,
            employee.Id,
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_IsIdempotent_OnAlreadyDeleted_Employee()
    {
        // Arrange
        var employee = CreateTestEmployee();
        employee.SetKeycloakUserId("keycloak-user-123");
        employee.Anonymize(); // Already deleted
        _employeeRepository.GetByIdIgnoreFilterAsync(employee.Id, Arg.Any<CancellationToken>()).Returns(employee);

        var command = new DeleteEmployeeCommand(employee.Id, ActorSub: "admin-sub");

        // Act
        await _sut.HandleAsync(command);

        // Assert — should NOT call SaveAsync again (Anonymize() is a no-op on already-deleted)
        await _employeeRepository.DidNotReceive().SaveAsync(Arg.Any<Employee>(), Arg.Any<CancellationToken>());
        // But should still audit the attempt
        await _auditService.Received(1).RecordAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            ActionType.EmployeeDeleted,
            employee.Id,
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ThrowsKeyNotFoundException_ForMissingEmployee()
    {
        // Arrange
        var missingId = Guid.NewGuid();
        _employeeRepository.GetByIdIgnoreFilterAsync(missingId, Arg.Any<CancellationToken>()).Returns((Employee?)null);

        var command = new DeleteEmployeeCommand(missingId, ActorSub: "admin-sub");

        // Act & Assert
        await Should.ThrowAsync<KeyNotFoundException>(() => _sut.HandleAsync(command));
    }
}