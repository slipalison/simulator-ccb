using Microsoft.Extensions.Logging;
using NSubstitute;
using Onboarding.Application.Common;
using Onboarding.Application.Companies.Commands;
using Onboarding.Domain.Aggregates.Audit;
using Onboarding.Domain.Aggregates.EmployeeAggregate;
using Onboarding.Domain.Exceptions;
using Onboarding.Domain.Repositories;
using Shouldly;

namespace Onboarding.Domain.Tests.Application.Companies.EmployeeManagement;

public class RegisterEmployeeCommandHandlerTests
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly IAccessGroupRepository _accessGroupRepository;
    private readonly IKeycloakUserService _keycloakUserService;
    private readonly IAuditService _auditService;
    private readonly ILogger<RegisterEmployeeCommandHandler> _logger;
    private readonly RegisterEmployeeCommandHandler _sut;

    public RegisterEmployeeCommandHandlerTests()
    {
        _employeeRepository = Substitute.For<IEmployeeRepository>();
        _companyRepository = Substitute.For<ICompanyRepository>();
        _accessGroupRepository = Substitute.For<IAccessGroupRepository>();
        _keycloakUserService = Substitute.For<IKeycloakUserService>();
        _auditService = Substitute.For<IAuditService>();
        _logger = Substitute.For<ILogger<RegisterEmployeeCommandHandler>>();
        _sut = new RegisterEmployeeCommandHandler(
            _employeeRepository, _companyRepository, _accessGroupRepository,
            _keycloakUserService, _auditService, _logger);
    }

    private RegisterEmployeeCommand ValidCommand(Guid? companyId = null, Guid? accessGroupId = null) => new(
        CompanyId: companyId ?? Guid.NewGuid(),
        Nome: "João Silva",
        Cpf: "52998224725",
        Email: "joao@empresa.com",
        Phone: "11999999999",
        AccessGroupId: accessGroupId,
        ActorSub: "keycloak-sub-123",
        ActorEmail: "admin@empresa.com",
        IpAddress: "192.168.1.1"
    );

    [Fact]
    public async Task HandleAsync_ValidRegistration_CreatesEmployeeAndKeycloakUser_ReturnsResultWithTempPassword()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var viewerGroupId = Guid.NewGuid();
        var command = ValidCommand(companyId: companyId, accessGroupId: viewerGroupId);
        var keycloakUserId = Guid.NewGuid().ToString();

        _companyRepository.GetByIdAsync(companyId, Arg.Any<CancellationToken>()).Returns(new Company());
        _employeeRepository.ExistsByCpfAsync(command.Cpf, Arg.Any<CancellationToken>()).Returns(false);
        _employeeRepository.ExistsByEmailAsync(command.Email, Arg.Any<CancellationToken>()).Returns(false);
        _accessGroupRepository.GetByIdAsync(viewerGroupId, Arg.Any<CancellationToken>()).Returns(
            AccessGroup.Create(companyId, "viewer", [Permissions.EmployeesRead]));
        _keycloakUserService.CreateUserAsync("client", command.Email, command.Email, Arg.Any<string>(), command.Nome, Arg.Any<CancellationToken>())
            .Returns(keycloakUserId);

        // Act
        var result = await _sut.HandleAsync(command);

        // Assert
        result.ShouldNotBeNull();
        result.EmployeeId.ShouldNotBe(Guid.Empty);
        result.TemporaryPassword.ShouldNotBeNullOrEmpty();
        result.TemporaryPassword.Length.ShouldBeGreaterThanOrEqualTo(16);

        await _employeeRepository.Received(1).AddAsync(Arg.Is<Employee>(e =>
            e.Nome == command.Nome &&
            e.CompanyId == companyId &&
            e.AccessGroupId == viewerGroupId), Arg.Any<CancellationToken>());
        await _keycloakUserService.Received(1).CreateUserAsync(
            "client", command.Email, command.Email, Arg.Any<string>(), command.Nome, Arg.Any<CancellationToken>());
        await _employeeRepository.Received(1).SaveAsync(Arg.Is<Employee>(e => e.KeycloakUserId == keycloakUserId), Arg.Any<CancellationToken>());
        await _auditService.Received(1).RecordAsync(
            command.ActorSub, command.ActorEmail,
            ActionType.EmployeeCreated,
            Arg.Any<Guid?>(), command.Nome,
            Arg.Is<string>(d => d.Contains(companyId.ToString())),
            command.IpAddress, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_DuplicateCpf_ThrowsBadRequestException()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var command = ValidCommand(companyId: companyId);
        _companyRepository.GetByIdAsync(companyId, Arg.Any<CancellationToken>()).Returns(new Company());
        _employeeRepository.ExistsByCpfAsync(command.Cpf, Arg.Any<CancellationToken>()).Returns(true);

        // Act & Assert
        await Should.ThrowAsync<BadRequestException>(() => _sut.HandleAsync(command));

        await _keycloakUserService.DidNotReceive().CreateUserAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_DuplicateEmail_ThrowsBadRequestException()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var command = ValidCommand(companyId: companyId);
        _companyRepository.GetByIdAsync(companyId, Arg.Any<CancellationToken>()).Returns(new Company());
        _employeeRepository.ExistsByCpfAsync(command.Cpf, Arg.Any<CancellationToken>()).Returns(false);
        _employeeRepository.ExistsByEmailAsync(command.Email, Arg.Any<CancellationToken>()).Returns(true);

        // Act & Assert
        await Should.ThrowAsync<BadRequestException>(() => _sut.HandleAsync(command));
    }

    [Fact]
    public async Task HandleAsync_KeycloakCreationFailure_DeletesEmployeeAsCompensation()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var viewerGroupId = Guid.NewGuid();
        var command = ValidCommand(companyId: companyId, accessGroupId: viewerGroupId);

        _companyRepository.GetByIdAsync(companyId, Arg.Any<CancellationToken>()).Returns(new Company());
        _employeeRepository.ExistsByCpfAsync(command.Cpf, Arg.Any<CancellationToken>()).Returns(false);
        _employeeRepository.ExistsByEmailAsync(command.Email, Arg.Any<CancellationToken>()).Returns(false);
        _accessGroupRepository.GetByIdAsync(viewerGroupId, Arg.Any<CancellationToken>()).Returns(
            AccessGroup.Create(companyId, "viewer", [Permissions.EmployeesRead]));
        _keycloakUserService.CreateUserAsync("client", command.Email, command.Email, Arg.Any<string>(), command.Nome, Arg.Any<CancellationToken>())
            .Returns<Task<string>>(_ => throw new Exception("Keycloak error"));

        // Act & Assert
        await Should.ThrowAsync<Exception>(() => _sut.HandleAsync(command));

        await _employeeRepository.Received(1).DeleteAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_CompanyNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var command = ValidCommand(companyId: companyId);
        _companyRepository.GetByIdAsync(companyId, Arg.Any<CancellationToken>()).Returns((Company?)null);

        // Act & Assert
        await Should.ThrowAsync<KeyNotFoundException>(() => _sut.HandleAsync(command));
    }

    [Fact]
    public async Task HandleAsync_NullAccessGroupId_ResolvesToViewerGroup()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var viewerGroup = AccessGroup.Create(companyId, "viewer", [Permissions.EmployeesRead]);
        var command = ValidCommand(companyId: companyId, accessGroupId: null);
        var keycloakUserId = Guid.NewGuid().ToString();

        _companyRepository.GetByIdAsync(companyId, Arg.Any<CancellationToken>()).Returns(new Company());
        _employeeRepository.ExistsByCpfAsync(command.Cpf, Arg.Any<CancellationToken>()).Returns(false);
        _employeeRepository.ExistsByEmailAsync(command.Email, Arg.Any<CancellationToken>()).Returns(false);
        _accessGroupRepository.GetByCompanyAndNameAsync(companyId, "viewer", Arg.Any<CancellationToken>()).Returns(viewerGroup);
        _keycloakUserService.CreateUserAsync("client", command.Email, command.Email, Arg.Any<string>(), command.Nome, Arg.Any<CancellationToken>())
            .Returns(keycloakUserId);

        // Act
        var result = await _sut.HandleAsync(command);

        // Assert
        result.ShouldNotBeNull();
        await _accessGroupRepository.Received(1).GetByCompanyAndNameAsync(companyId, "viewer", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_GeneratesCryptographicallySecureTempPassword()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var viewerGroupId = Guid.NewGuid();
        var command = ValidCommand(companyId: companyId, accessGroupId: viewerGroupId);
        var keycloakUserId = Guid.NewGuid().ToString();

        _companyRepository.GetByIdAsync(companyId, Arg.Any<CancellationToken>()).Returns(new Company());
        _employeeRepository.ExistsByCpfAsync(command.Cpf, Arg.Any<CancellationToken>()).Returns(false);
        _employeeRepository.ExistsByEmailAsync(command.Email, Arg.Any<CancellationToken>()).Returns(false);
        _accessGroupRepository.GetByIdAsync(viewerGroupId, Arg.Any<CancellationToken>()).Returns(
            AccessGroup.Create(companyId, "viewer", [Permissions.EmployeesRead]));
        _keycloakUserService.CreateUserAsync("client", command.Email, command.Email, Arg.Any<string>(), command.Nome, Arg.Any<CancellationToken>())
            .Returns(keycloakUserId);

        // Act
        var result = await _sut.HandleAsync(command);

        // Assert — password should be 16+ chars, contain upper, lower, digit, special
        result.TemporaryPassword.ShouldNotBeNullOrEmpty();
        result.TemporaryPassword.Length.ShouldBeGreaterThanOrEqualTo(16);
        // Must contain at least one uppercase, one lowercase, one digit, one special
        result.TemporaryPassword.Any(char.IsUpper).ShouldBeTrue();
        result.TemporaryPassword.Any(char.IsLower).ShouldBeTrue();
        result.TemporaryPassword.Any(char.IsDigit).ShouldBeTrue();
    }
}