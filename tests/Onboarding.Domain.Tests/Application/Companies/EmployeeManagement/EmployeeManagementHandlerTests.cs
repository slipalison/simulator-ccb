using Microsoft.Extensions.Logging;
using NSubstitute;
using Onboarding.Application.Common;
using Onboarding.Application.Companies.Commands;
using Onboarding.Domain.Aggregates.Audit;
using Onboarding.Domain.Aggregates.EmployeeAggregate;
using Onboarding.Domain.Repositories;
using Shouldly;

namespace Onboarding.Domain.Tests.Application.Companies.EmployeeManagement;

public class ToggleEmployeeStatusHandlerTests
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IKeycloakUserService _keycloakUserService;
    private readonly IAuditService _auditService;
    private readonly ILogger<ToggleEmployeeStatusCommandHandler> _logger;
    private readonly ToggleEmployeeStatusCommandHandler _sut;

    public ToggleEmployeeStatusHandlerTests()
    {
        _employeeRepository = Substitute.For<IEmployeeRepository>();
        _keycloakUserService = Substitute.For<IKeycloakUserService>();
        _auditService = Substitute.For<IAuditService>();
        _logger = Substitute.For<ILogger<ToggleEmployeeStatusCommandHandler>>();
        _sut = new ToggleEmployeeStatusCommandHandler(_employeeRepository, _keycloakUserService, _auditService, _logger);
    }

    private static Employee CreateTestEmployee(Guid companyId, Guid? id = null)
    {
        return Employee.Register("João Silva", "52998224725", "joao@empresa.com", "11999999999", companyId, Guid.NewGuid());
    }

    [Fact]
    public async Task HandleAsync_BlockEmployee_CallsBlockUserAndLogoutAndAudit()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var employee = CreateTestEmployee(companyId);
        employee.SetKeycloakUserId("keycloak-user-id-123");
        var command = new ToggleEmployeeStatusCommand(employee.Id, companyId, Activate: false, ActorSub: "sub", ActorEmail: "admin@empresa.com", IpAddress: "1.1.1.1");

        _employeeRepository.GetByIdAsync(employee.Id, Arg.Any<CancellationToken>()).Returns(employee);

        // Act
        await _sut.HandleAsync(command);

        // Assert
        await _keycloakUserService.Received(1).BlockUserAsync("client", "keycloak-user-id-123", Arg.Any<CancellationToken>());
        await _keycloakUserService.Received(1).LogoutAllSessionsAsync("client", "keycloak-user-id-123", Arg.Any<CancellationToken>());
        await _auditService.Received(1).RecordAsync("sub", "admin@empresa.com", ActionType.EmployeeBlocked, employee.Id, employee.Nome, Arg.Any<string>(), "1.1.1.1", Arg.Any<string?>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_UnblockEmployee_CallsUnblockUserAndAudit()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var employee = CreateTestEmployee(companyId);
        employee.SetKeycloakUserId("keycloak-user-id-123");
        var command = new ToggleEmployeeStatusCommand(employee.Id, companyId, Activate: true, ActorSub: "sub", ActorEmail: "admin@empresa.com", IpAddress: "1.1.1.1");

        _employeeRepository.GetByIdAsync(employee.Id, Arg.Any<CancellationToken>()).Returns(employee);

        // Act
        await _sut.HandleAsync(command);

        // Assert
        await _keycloakUserService.Received(1).UnblockUserAsync("client", "keycloak-user-id-123", Arg.Any<CancellationToken>());
        await _auditService.Received(1).RecordAsync("sub", "admin@empresa.com", ActionType.EmployeeUnblocked, employee.Id, employee.Nome, Arg.Any<string>(), "1.1.1.1", Arg.Any<string?>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_EmployeeNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var command = new ToggleEmployeeStatusCommand(Guid.NewGuid(), Guid.NewGuid(), Activate: true, ActorSub: "sub", ActorEmail: "admin@empresa.com", IpAddress: "1.1.1.1");
        _employeeRepository.GetByIdAsync(command.EmployeeId, Arg.Any<CancellationToken>()).Returns((Employee?)null);

        // Act & Assert
        await Should.ThrowAsync<KeyNotFoundException>(() => _sut.HandleAsync(command));
    }

    [Fact]
    public async Task HandleAsync_CompanyIdMismatch_ThrowsInvalidOperationException()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var employee = CreateTestEmployee(companyId);
        var wrongCompanyId = Guid.NewGuid();
        var command = new ToggleEmployeeStatusCommand(employee.Id, wrongCompanyId, Activate: true, ActorSub: "sub", ActorEmail: "admin@empresa.com", IpAddress: "1.1.1.1");

        _employeeRepository.GetByIdAsync(employee.Id, Arg.Any<CancellationToken>()).Returns(employee);

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(() => _sut.HandleAsync(command));
    }
}

public class ResetEmployeePasswordHandlerTests
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IKeycloakUserService _keycloakUserService;
    private readonly IAuditService _auditService;
    private readonly ResetEmployeePasswordCommandHandler _sut;

    public ResetEmployeePasswordHandlerTests()
    {
        _employeeRepository = Substitute.For<IEmployeeRepository>();
        _keycloakUserService = Substitute.For<IKeycloakUserService>();
        _auditService = Substitute.For<IAuditService>();
        _sut = new ResetEmployeePasswordCommandHandler(_employeeRepository, _keycloakUserService, _auditService);
    }

    private static Employee CreateTestEmployee(Guid companyId, Guid? id = null)
    {
        return Employee.Register("João Silva", "52998224725", "joao@empresa.com", "11999999999", companyId, Guid.NewGuid());
    }

    [Fact]
    public async Task HandleAsync_ResetsPasswordAndReturnsTempPassword()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var employee = CreateTestEmployee(companyId);
        employee.SetKeycloakUserId("keycloak-user-id-456");
        var command = new ResetEmployeePasswordCommand(employee.Id, companyId, ActorSub: "sub", ActorEmail: "admin@empresa.com", IpAddress: "1.1.1.1");

        _employeeRepository.GetByIdAsync(employee.Id, Arg.Any<CancellationToken>()).Returns(employee);

        // Act
        var result = await _sut.HandleAsync(command);

        // Assert
        result.ShouldNotBeNull();
        result.TemporaryPassword.ShouldNotBeNullOrEmpty();
        result.TemporaryPassword.Length.ShouldBeGreaterThanOrEqualTo(16);
        await _keycloakUserService.Received(1).ResetPasswordAsTemporaryAsync("client", "keycloak-user-id-456", Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _auditService.Received(1).RecordAsync("sub", "admin@empresa.com", ActionType.EmployeePasswordReset, employee.Id, employee.Nome, Arg.Any<string>(), "1.1.1.1", Arg.Any<string?>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_CompanyIdMismatch_ThrowsInvalidOperationException()
    {
        var companyId = Guid.NewGuid();
        var employee = CreateTestEmployee(companyId);
        var command = new ResetEmployeePasswordCommand(employee.Id, Guid.NewGuid(), ActorSub: "sub", ActorEmail: "admin@empresa.com", IpAddress: "1.1.1.1");

        _employeeRepository.GetByIdAsync(employee.Id, Arg.Any<CancellationToken>()).Returns(employee);

        await Should.ThrowAsync<InvalidOperationException>(() => _sut.HandleAsync(command));
    }
}

public class UpdateEmployeeHandlerTests
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IKeycloakUserService _keycloakUserService;
    private readonly IAuditService _auditService;
    private readonly UpdateEmployeeCommandHandler _sut;

    public UpdateEmployeeHandlerTests()
    {
        _employeeRepository = Substitute.For<IEmployeeRepository>();
        _keycloakUserService = Substitute.For<IKeycloakUserService>();
        _auditService = Substitute.For<IAuditService>();
        _sut = new UpdateEmployeeCommandHandler(_employeeRepository, _keycloakUserService, _auditService);
    }

    private static Employee CreateTestEmployee(Guid companyId) =>
        Employee.Register("João Silva", "52998224725", "joao@empresa.com", "11999999999", companyId, Guid.NewGuid());

    [Fact]
    public async Task HandleAsync_UpdatesEmployeeAndKeycloakAndAudits()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var employee = CreateTestEmployee(companyId);
        employee.SetKeycloakUserId("keycloak-user-id-789");
        var command = new UpdateEmployeeCommand(employee.Id, companyId, Nome: "João Updated", Email: "joao.updated@empresa.com", Phone: "11888888888", ActorSub: "sub", ActorEmail: "admin@empresa.com", IpAddress: "1.1.1.1");

        _employeeRepository.GetByIdAsync(employee.Id, Arg.Any<CancellationToken>()).Returns(employee);

        // Act
        await _sut.HandleAsync(command);

        // Assert
        await _employeeRepository.Received(1).SaveAsync(employee, Arg.Any<CancellationToken>());
        await _keycloakUserService.Received(1).UpdateAdminUserAsync("client", "keycloak-user-id-789", "João Updated", "joao.updated@empresa.com", Arg.Any<CancellationToken>());
        await _auditService.Received(1).RecordAsync("sub", "admin@empresa.com", ActionType.EmployeeEdited, employee.Id, "João Updated", Arg.Any<string>(), "1.1.1.1", Arg.Any<string?>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_EmployeeNotFound_ThrowsKeyNotFoundException()
    {
        var command = new UpdateEmployeeCommand(Guid.NewGuid(), Guid.NewGuid(), "Nome", "email@test.com", "11999999999", "sub", "admin@empresa.com", "1.1.1.1");
        _employeeRepository.GetByIdAsync(command.EmployeeId, Arg.Any<CancellationToken>()).Returns((Employee?)null);

        await Should.ThrowAsync<KeyNotFoundException>(() => _sut.HandleAsync(command));
    }
}

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

    private static Employee CreateTestEmployee(Guid companyId) =>
        Employee.Register("João Silva", "52998224725", "joao@empresa.com", "11999999999", companyId, Guid.NewGuid());

    [Fact]
    public async Task HandleAsync_AnonymizesEmployeeAndDeletesKeycloakUserAndAudits()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var employee = CreateTestEmployee(companyId);
        employee.SetKeycloakUserId("keycloak-user-id-del");
        var originalEmail = employee.Email.Value;
        var command = new DeleteEmployeeCommand(employee.Id, companyId, ActorSub: "sub", ActorEmail: "admin@empresa.com", IpAddress: "1.1.1.1");

        _employeeRepository.GetByIdAsync(employee.Id, Arg.Any<CancellationToken>()).Returns(employee);

        // Act
        await _sut.HandleAsync(command);

        // Assert
        employee.IsDeleted.ShouldBeTrue();
        employee.Nome.ShouldBe("Usuário Excluído");
        await _employeeRepository.Received(1).SaveAsync(employee, Arg.Any<CancellationToken>());
        await _keycloakUserService.Received(1).DeleteUserByEmailAsync("client", originalEmail, Arg.Any<CancellationToken>());
        await _auditService.Received(1).RecordAsync("sub", "admin@empresa.com", ActionType.EmployeeDeleted, employee.Id, Arg.Any<string>(), Arg.Any<string>(), "1.1.1.1", Arg.Any<string?>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_AlreadyDeletedEmployee_IsIdempotent()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var employee = CreateTestEmployee(companyId);
        employee.SetKeycloakUserId("keycloak-user-id-del");
        var command = new DeleteEmployeeCommand(employee.Id, companyId, ActorSub: "sub", ActorEmail: "admin@empresa.com", IpAddress: "1.1.1.1");

        // First deletion
        _employeeRepository.GetByIdAsync(employee.Id, Arg.Any<CancellationToken>()).Returns(employee);
        await _sut.HandleAsync(command);

        // Verify first call
        employee.IsDeleted.ShouldBeTrue();

        // Reset mock calls for second call
        _keycloakUserService.ClearReceivedCalls();
        _auditService.ClearReceivedCalls();
        _employeeRepository.ClearReceivedCalls();

        // Second deletion on same (already anonymized) employee
        // The handler detects IsDeleted and skips second Anonymize + Keycloak delete
        await _sut.HandleAsync(command);

        // Assert — should NOT call DeleteUserByEmailAsync again on second call
        // (it tries with the anonymized email "deleted-..." which is best-effort)
        await _employeeRepository.DidNotReceive().SaveAsync(Arg.Any<Employee>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_CompanyIdMismatch_ThrowsInvalidOperationException()
    {
        var companyId = Guid.NewGuid();
        var employee = CreateTestEmployee(companyId);
        var command = new DeleteEmployeeCommand(employee.Id, Guid.NewGuid(), ActorSub: "sub", ActorEmail: "admin@empresa.com", IpAddress: "1.1.1.1");

        _employeeRepository.GetByIdAsync(employee.Id, Arg.Any<CancellationToken>()).Returns(employee);

        await Should.ThrowAsync<InvalidOperationException>(() => _sut.HandleAsync(command));
    }
}

public class ChangeEmployeeAccessGroupHandlerTests
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IAccessGroupRepository _accessGroupRepository;
    private readonly IKeycloakUserService _keycloakUserService;
    private readonly IAuditService _auditService;
    private readonly ILogger<ChangeEmployeeAccessGroupCommandHandler> _logger;
    private readonly ChangeEmployeeAccessGroupCommandHandler _sut;

    public ChangeEmployeeAccessGroupHandlerTests()
    {
        _employeeRepository = Substitute.For<IEmployeeRepository>();
        _accessGroupRepository = Substitute.For<IAccessGroupRepository>();
        _keycloakUserService = Substitute.For<IKeycloakUserService>();
        _auditService = Substitute.For<IAuditService>();
        _logger = Substitute.For<ILogger<ChangeEmployeeAccessGroupCommandHandler>>();
        _sut = new ChangeEmployeeAccessGroupCommandHandler(_employeeRepository, _accessGroupRepository, _keycloakUserService, _auditService, _logger);
    }

    private static Employee CreateTestEmployee(Guid companyId) =>
        Employee.Register("João Silva", "52998224725", "joao@empresa.com", "11999999999", companyId, Guid.NewGuid());

    [Fact]
    public async Task HandleAsync_ChangesAccessGroupAndSavesAndAudits()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var employee = CreateTestEmployee(companyId);
        var newGroupId = Guid.NewGuid();
        var newGroup = AccessGroup.Create(companyId, "admin-empresa", [Permissions.EmployeesRead, Permissions.EmployeesWrite]);
        // Force the new group's Id to match what we're looking for
        var command = new ChangeEmployeeAccessGroupCommand(employee.Id, companyId, newGroupId, ActorSub: "sub", ActorEmail: "admin@empresa.com", IpAddress: "1.1.1.1");

        _employeeRepository.GetByIdAsync(employee.Id, Arg.Any<CancellationToken>()).Returns(employee);
        _accessGroupRepository.GetByIdAsync(newGroupId, Arg.Any<CancellationToken>()).Returns(newGroup);

        // Act
        await _sut.HandleAsync(command);

        // Assert
        employee.AccessGroupId.ShouldBe(newGroupId);
        await _employeeRepository.Received(1).SaveAsync(employee, Arg.Any<CancellationToken>());
        await _auditService.Received(1).RecordAsync("sub", "admin@empresa.com", ActionType.AccessGroupChanged, employee.Id, employee.Nome, Arg.Any<string>(), "1.1.1.1", Arg.Any<string?>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ChangesAccessGroup_SyncsKeycloakGroupMembership()
    {
        // Arrange (D-15: add to new group + remove from old group in Keycloak)
        var companyId = Guid.NewGuid();
        var previousAccessGroupId = Guid.NewGuid();
        var newGroupId = Guid.NewGuid();
        var employee = Employee.Register("João Silva", "52998224725", "joao@empresa.com", "11999999999", companyId, previousAccessGroupId);
        employee.SetKeycloakUserId("keycloak-user-id-999");

        var previousGroup = AccessGroup.Create(companyId, "viewer", [Permissions.EmployeesRead]);
        var newGroup = AccessGroup.Create(companyId, "admin-empresa", Permissions.All);
        var command = new ChangeEmployeeAccessGroupCommand(employee.Id, companyId, newGroupId, ActorSub: "sub", ActorEmail: "admin@empresa.com", IpAddress: "1.1.1.1");

        _employeeRepository.GetByIdAsync(employee.Id, Arg.Any<CancellationToken>()).Returns(employee);
        _accessGroupRepository.GetByIdAsync(newGroupId, Arg.Any<CancellationToken>()).Returns(newGroup);
        _accessGroupRepository.GetByIdAsync(previousAccessGroupId, Arg.Any<CancellationToken>()).Returns(previousGroup);
        _keycloakUserService.GetGroupByNameAsync("client", "admin-empresa", Arg.Any<CancellationToken>()).Returns("new-group-id");
        _keycloakUserService.GetGroupByNameAsync("client", "viewer", Arg.Any<CancellationToken>()).Returns("old-group-id");

        // Act
        await _sut.HandleAsync(command);

        // Assert — AddUserToGroupAsync for new group, RemoveUserFromGroupAsync for old group
        await _keycloakUserService.Received(1).AddUserToGroupAsync("client", "keycloak-user-id-999", "new-group-id", Arg.Any<CancellationToken>());
        await _keycloakUserService.Received(1).RemoveUserFromGroupAsync("client", "keycloak-user-id-999", "old-group-id", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_KeycloakSyncFailure_StillCompletesDbUpdate()
    {
        // Arrange — Keycloak sync fails but DB update succeeds (D-15 eventual consistency)
        var companyId = Guid.NewGuid();
        var previousAccessGroupId = Guid.NewGuid();
        var newGroupId = Guid.NewGuid();
        var employee = Employee.Register("João Silva", "52998224725", "joao@empresa.com", "11999999999", companyId, previousAccessGroupId);
        employee.SetKeycloakUserId("keycloak-user-id-999");

        var newGroup = AccessGroup.Create(companyId, "admin-empresa", Permissions.All);
        var command = new ChangeEmployeeAccessGroupCommand(employee.Id, companyId, newGroupId, ActorSub: "sub", ActorEmail: "admin@empresa.com", IpAddress: "1.1.1.1");

        _employeeRepository.GetByIdAsync(employee.Id, Arg.Any<CancellationToken>()).Returns(employee);
        _accessGroupRepository.GetByIdAsync(newGroupId, Arg.Any<CancellationToken>()).Returns(newGroup);
        _keycloakUserService.GetGroupByNameAsync("client", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<Task<string?>>(_ => throw new Exception("Keycloak unavailable"));

        // Act — should NOT throw despite Keycloak failure
        await _sut.HandleAsync(command);

        // Assert — DB update still happened
        employee.AccessGroupId.ShouldBe(newGroupId);
        await _employeeRepository.Received(1).SaveAsync(employee, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_AccessGroupFromDifferentCompany_ThrowsInvalidOperationException()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var otherCompanyId = Guid.NewGuid();
        var employee = CreateTestEmployee(companyId);
        var newGroupId = Guid.NewGuid();
        var wrongGroup = AccessGroup.Create(otherCompanyId, "viewer", [Permissions.EmployeesRead]);
        var command = new ChangeEmployeeAccessGroupCommand(employee.Id, companyId, newGroupId, ActorSub: "sub", ActorEmail: "admin@empresa.com", IpAddress: "1.1.1.1");

        _employeeRepository.GetByIdAsync(employee.Id, Arg.Any<CancellationToken>()).Returns(employee);
        _accessGroupRepository.GetByIdAsync(newGroupId, Arg.Any<CancellationToken>()).Returns(wrongGroup);

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(() => _sut.HandleAsync(command));
    }

    [Fact]
    public async Task HandleAsync_EmployeeNotFound_ThrowsKeyNotFoundException()
    {
        var command = new ChangeEmployeeAccessGroupCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "sub", "admin@empresa.com", "1.1.1.1");
        _employeeRepository.GetByIdAsync(command.EmployeeId, Arg.Any<CancellationToken>()).Returns((Employee?)null);

        await Should.ThrowAsync<KeyNotFoundException>(() => _sut.HandleAsync(command));
    }
}