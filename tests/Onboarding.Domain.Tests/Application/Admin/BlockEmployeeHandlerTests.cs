using NSubstitute;
using Onboarding.Application.Admin.Commands;
using Onboarding.Application.Common;
using Onboarding.Domain.Aggregates.Audit;
using Onboarding.Domain.Aggregates.EmployeeAggregate;
using Onboarding.Domain.Repositories;
using Shouldly;

namespace Onboarding.Domain.Tests.Application.Admin;

public class BlockEmployeeHandlerTests
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IKeycloakUserService _keycloakUserService;
    private readonly IAuditService _auditService;
    private readonly BlockEmployeeCommandHandler _sut;

    public BlockEmployeeHandlerTests()
    {
        _employeeRepository = Substitute.For<IEmployeeRepository>();
        _keycloakUserService = Substitute.For<IKeycloakUserService>();
        _auditService = Substitute.For<IAuditService>();
        _sut = new BlockEmployeeCommandHandler(_employeeRepository, _keycloakUserService, _auditService);
    }

    private static Employee CreateTestEmployee(Guid? companyId = null)
    {
        return Employee.Register("João Silva", "52998224725", "joao@empresa.com", "11999999999",
            companyId ?? Guid.NewGuid(), Guid.NewGuid());
    }

    [Fact]
    public async Task HandleAsync_BlocksEmployee_InKeycloak_And_RevokesSessions()
    {
        // Arrange
        var employee = CreateTestEmployee();
        employee.SetKeycloakUserId("keycloak-user-123");
        _employeeRepository.GetByIdIgnoreFilterAsync(employee.Id, Arg.Any<CancellationToken>()).Returns(employee);

        var command = new BlockEmployeeCommand(employee.Id, ActorSub: "admin-sub");

        // Act
        await _sut.HandleAsync(command);

        // Assert
        await _keycloakUserService.Received(1).BlockUserAsync("client", "keycloak-user-123", Arg.Any<CancellationToken>());
        await _keycloakUserService.Received(1).LogoutAllSessionsAsync("client", "keycloak-user-123", Arg.Any<CancellationToken>());
        await _auditService.Received(1).RecordAsync(
            "admin-sub",
            Arg.Any<string>(),
            ActionType.EmployeeBlocked,
            employee.Id,
            employee.Nome,
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

        var command = new BlockEmployeeCommand(missingId, ActorSub: "admin-sub");

        // Act & Assert
        await Should.ThrowAsync<KeyNotFoundException>(() => _sut.HandleAsync(command));
    }
}

public class UnblockEmployeeHandlerTests
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IKeycloakUserService _keycloakUserService;
    private readonly IAuditService _auditService;
    private readonly UnblockEmployeeCommandHandler _sut;

    public UnblockEmployeeHandlerTests()
    {
        _employeeRepository = Substitute.For<IEmployeeRepository>();
        _keycloakUserService = Substitute.For<IKeycloakUserService>();
        _auditService = Substitute.For<IAuditService>();
        _sut = new UnblockEmployeeCommandHandler(_employeeRepository, _keycloakUserService, _auditService);
    }

    private static Employee CreateTestEmployee(Guid? companyId = null)
    {
        return Employee.Register("João Silva", "52998224725", "joao@empresa.com", "11999999999",
            companyId ?? Guid.NewGuid(), Guid.NewGuid());
    }

    [Fact]
    public async Task HandleAsync_UnblocksEmployee_InKeycloak_And_Audits()
    {
        // Arrange
        var employee = CreateTestEmployee();
        employee.SetKeycloakUserId("keycloak-user-456");
        _employeeRepository.GetByIdIgnoreFilterAsync(employee.Id, Arg.Any<CancellationToken>()).Returns(employee);

        var command = new UnblockEmployeeCommand(employee.Id, ActorSub: "admin-sub");

        // Act
        await _sut.HandleAsync(command);

        // Assert
        await _keycloakUserService.Received(1).UnblockUserAsync("client", "keycloak-user-456", Arg.Any<CancellationToken>());
        await _keycloakUserService.DidNotReceive().LogoutAllSessionsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _auditService.Received(1).RecordAsync(
            "admin-sub",
            Arg.Any<string>(),
            ActionType.EmployeeUnblocked,
            employee.Id,
            employee.Nome,
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

        var command = new UnblockEmployeeCommand(missingId, ActorSub: "admin-sub");

        // Act & Assert
        await Should.ThrowAsync<KeyNotFoundException>(() => _sut.HandleAsync(command));
    }
}