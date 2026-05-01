using Microsoft.Extensions.Logging;
using NSubstitute;
using Onboarding.Application.Common;
using Onboarding.Application.Companies.Commands;
using Onboarding.Domain.Aggregates.Audit;
using Onboarding.Domain.Aggregates.CompanyAggregate;
using Onboarding.Domain.Aggregates.EmployeeAggregate;
using Onboarding.Domain.Exceptions;
using Onboarding.Domain.Repositories;
using Onboarding.Domain.ValueObjects;
using Shouldly;
using AccessGroup = Onboarding.Domain.Aggregates.EmployeeAggregate.AccessGroup;

namespace Onboarding.Domain.Tests.Application.Companies.EmployeeManagement;

public class AccessGroupCommandHandlerTests
{
    private readonly IAccessGroupRepository _accessGroupRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IKeycloakUserService _keycloakUserService;
    private readonly IAuditService _auditService;

    private readonly Guid _companyId = Guid.NewGuid();

    public AccessGroupCommandHandlerTests()
    {
        _accessGroupRepository = Substitute.For<IAccessGroupRepository>();
        _companyRepository = Substitute.For<ICompanyRepository>();
        _employeeRepository = Substitute.For<IEmployeeRepository>();
        _keycloakUserService = Substitute.For<IKeycloakUserService>();
        _auditService = Substitute.For<IAuditService>();

        _companyRepository.GetByIdAsync(_companyId, Arg.Any<CancellationToken>())
            .Returns(Onboarding.Domain.Aggregates.CompanyAggregate.Company.Register(
                "Empresa Teste", "11222333000181", "test@empresa.com", "11999999999",
                TermsAcceptance.Create("1.0", "127.0.0.1")));
    }

    // ───────────────────────────────────────────────
    // CreateAccessGroupCommandHandler
    // ───────────────────────────────────────────────

    [Fact]
    public async Task CreateAccessGroup_ValidData_CreatesGroup()
    {
        _accessGroupRepository.GetByCompanyAndNameAsync(_companyId, "financeiro", Arg.Any<CancellationToken>())
            .Returns((AccessGroup?)null);

        var sut = new CreateAccessGroupCommandHandler(
            _accessGroupRepository, _companyRepository, _keycloakUserService, _auditService,
            Substitute.For<ILogger<CreateAccessGroupCommandHandler>>());

        var command = new CreateAccessGroupCommand(
            _companyId, "financeiro",
            (IReadOnlyList<string>)["employees:read", "audit:read"],
            "sub", "admin@empresa.com", "1.2.3.4");

        var result = await sut.HandleAsync(command);

        result.Name.ShouldBe("financeiro");
        result.Permissions.ShouldContain("employees:read");
        result.Permissions.ShouldContain("audit:read");
        result.IsDefault.ShouldBeFalse();

        await _accessGroupRepository.Received(1).AddAsync(Arg.Any<AccessGroup>(), Arg.Any<CancellationToken>());
        await _keycloakUserService.Received(1).CreateGroupAsync("client", "financeiro", Arg.Any<CancellationToken>());
        await _auditService.Received(1).RecordAsync(
            "sub", "admin@empresa.com",
            ActionType.AccessGroupCreated, Arg.Any<Guid>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAccessGroup_DuplicateName_ThrowsBadRequest()
    {
        var existing = AccessGroup.Create(_companyId, "financeiro", ["employees:read"]);
        _accessGroupRepository.GetByCompanyAndNameAsync(_companyId, "financeiro", Arg.Any<CancellationToken>())
            .Returns(existing);

        var sut = new CreateAccessGroupCommandHandler(
            _accessGroupRepository, _companyRepository, _keycloakUserService, _auditService,
            Substitute.For<ILogger<CreateAccessGroupCommandHandler>>());

        var command = new CreateAccessGroupCommand(
            _companyId, "financeiro",
            (IReadOnlyList<string>)["employees:read"],
            "sub", "admin@empresa.com", "1.2.3.4");

        await Should.ThrowAsync<BadRequestException>(async () => await sut.HandleAsync(command));
    }

    [Fact]
    public async Task CreateAccessGroup_InvalidPermission_ThrowsBadRequest()
    {
        _accessGroupRepository.GetByCompanyAndNameAsync(_companyId, "custom", Arg.Any<CancellationToken>())
            .Returns((AccessGroup?)null);

        var sut = new CreateAccessGroupCommandHandler(
            _accessGroupRepository, _companyRepository, _keycloakUserService, _auditService,
            Substitute.For<ILogger<CreateAccessGroupCommandHandler>>());

        var command = new CreateAccessGroupCommand(
            _companyId, "custom",
            (IReadOnlyList<string>)["employees:read", "invalid:permission"],
            "sub", "admin@empresa.com", "1.2.3.4");

        await Should.ThrowAsync<BadRequestException>(async () => await sut.HandleAsync(command));
    }

    [Fact]
    public async Task CreateAccessGroup_CompanyNotFound_ThrowsKeyNotFound()
    {
        var sut = new CreateAccessGroupCommandHandler(
            _accessGroupRepository, _companyRepository, _keycloakUserService, _auditService,
            Substitute.For<ILogger<CreateAccessGroupCommandHandler>>());

        var command = new CreateAccessGroupCommand(
            Guid.NewGuid(), "custom",
            (IReadOnlyList<string>)["employees:read"],
            "sub", "admin@empresa.com", "1.2.3.4");

        await Should.ThrowAsync<KeyNotFoundException>(async () => await sut.HandleAsync(command));
    }

    [Fact]
    public async Task CreateAccessGroup_KeycloakFailure_StillSucceeds()
    {
        _accessGroupRepository.GetByCompanyAndNameAsync(_companyId, "custom", Arg.Any<CancellationToken>())
            .Returns((AccessGroup?)null);
        _keycloakUserService.CreateGroupAsync("client", "custom", Arg.Any<CancellationToken>())
            .Returns(Task.FromException<string>(new Exception("Keycloak unavailable")));

        var sut = new CreateAccessGroupCommandHandler(
            _accessGroupRepository, _companyRepository, _keycloakUserService, _auditService,
            Substitute.For<ILogger<CreateAccessGroupCommandHandler>>());

        var command = new CreateAccessGroupCommand(
            _companyId, "custom",
            (IReadOnlyList<string>)["employees:read"],
            "sub", "admin@empresa.com", "1.2.3.4");

        var result = await sut.HandleAsync(command);
        result.Name.ShouldBe("custom");
    }

    // ───────────────────────────────────────────────
    // UpdateAccessGroupCommandHandler
    // ───────────────────────────────────────────────

    [Fact]
    public async Task UpdateAccessGroup_UpdatePermissions_Succeeds()
    {
        var group = AccessGroup.Create(_companyId, "custom1", ["employees:read"]);
        _accessGroupRepository.GetByIdAsync(group.Id, Arg.Any<CancellationToken>())
            .Returns(group);

        var sut = new UpdateAccessGroupCommandHandler(
            _accessGroupRepository, _keycloakUserService, _auditService,
            Substitute.For<ILogger<UpdateAccessGroupCommandHandler>>());

        var command = new UpdateAccessGroupCommand(
            _companyId, group.Id, null,
            (IReadOnlyList<string>?)["employees:read", "audit:read"],
            "sub", "admin@empresa.com", "1.2.3.4");

        var result = await sut.HandleAsync(command);

        result.Permissions.ShouldContain("employees:read");
        result.Permissions.ShouldContain("audit:read");
        await _accessGroupRepository.Received(1).SaveAsync(Arg.Any<AccessGroup>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAccessGroup_DefaultGroup_ThrowsBadRequest()
    {
        var defaultGroup = AccessGroup.Create(_companyId, "admin-empresa", Permissions.All);
        _accessGroupRepository.GetByIdAsync(defaultGroup.Id, Arg.Any<CancellationToken>())
            .Returns(defaultGroup);

        var sut = new UpdateAccessGroupCommandHandler(
            _accessGroupRepository, _keycloakUserService, _auditService,
            Substitute.For<ILogger<UpdateAccessGroupCommandHandler>>());

        var command = new UpdateAccessGroupCommand(
            _companyId, defaultGroup.Id, "novo-nome",
            (IReadOnlyList<string>?)["dashboard:access"],
            "sub", "admin@empresa.com", "1.2.3.4");

        await Should.ThrowAsync<BadRequestException>(async () => await sut.HandleAsync(command));
    }

    [Fact]
    public async Task UpdateAccessGroup_WrongCompany_ThrowsInvalidOp()
    {
        var group = AccessGroup.Create(Guid.NewGuid(), "custom", ["employees:read"]);
        _accessGroupRepository.GetByIdAsync(group.Id, Arg.Any<CancellationToken>())
            .Returns(group);

        var sut = new UpdateAccessGroupCommandHandler(
            _accessGroupRepository, _keycloakUserService, _auditService,
            Substitute.For<ILogger<UpdateAccessGroupCommandHandler>>());

        var command = new UpdateAccessGroupCommand(
            _companyId, group.Id, "new-name",
            null, "sub", "admin@empresa.com", "1.2.3.4");

        await Should.ThrowAsync<InvalidOperationException>(async () => await sut.HandleAsync(command));
    }

    [Fact]
    public async Task UpdateAccessGroup_InvalidPermission_ThrowsBadRequest()
    {
        var group = AccessGroup.Create(_companyId, "custom1", ["employees:read"]);
        _accessGroupRepository.GetByIdAsync(group.Id, Arg.Any<CancellationToken>())
            .Returns(group);

        var sut = new UpdateAccessGroupCommandHandler(
            _accessGroupRepository, _keycloakUserService, _auditService,
            Substitute.For<ILogger<UpdateAccessGroupCommandHandler>>());

        var command = new UpdateAccessGroupCommand(
            _companyId, group.Id, null,
            (IReadOnlyList<string>?)["invalid:perm"],
            "sub", "admin@empresa.com", "1.2.3.4");

        await Should.ThrowAsync<BadRequestException>(async () => await sut.HandleAsync(command));
    }

    [Fact]
    public async Task UpdateAccessGroup_RenameKeycloak_SyncsGroup()
    {
        var group = AccessGroup.Create(_companyId, "old-name", ["employees:read"]);
        _accessGroupRepository.GetByIdAsync(group.Id, Arg.Any<CancellationToken>())
            .Returns(group);
        _accessGroupRepository.GetByCompanyAndNameAsync(_companyId, "new-name", Arg.Any<CancellationToken>())
            .Returns((AccessGroup?)null);
        _keycloakUserService.GetGroupByNameAsync("client", "old-name", Arg.Any<CancellationToken>())
            .Returns("keycloak-group-id-123");

        var sut = new UpdateAccessGroupCommandHandler(
            _accessGroupRepository, _keycloakUserService, _auditService,
            Substitute.For<ILogger<UpdateAccessGroupCommandHandler>>());

        var command = new UpdateAccessGroupCommand(
            _companyId, group.Id, "new-name", null,
            "sub", "admin@empresa.com", "1.2.3.4");

        var result = await sut.HandleAsync(command);
        result.Name.ShouldBe("new-name");

        await _keycloakUserService.Received(1).DeleteGroupAsync("client", "keycloak-group-id-123", Arg.Any<CancellationToken>());
        await _keycloakUserService.Received(1).CreateGroupAsync("client", "new-name", Arg.Any<CancellationToken>());
    }

    // ───────────────────────────────────────────────
    // DeleteAccessGroupCommandHandler
    // ───────────────────────────────────────────────

    [Fact]
    public async Task DeleteAccessGroup_CustomGroupNoEmployees_Succeeds()
    {
        var group = AccessGroup.Create(_companyId, "custom1", ["employees:read"]);
        _accessGroupRepository.GetByIdAsync(group.Id, Arg.Any<CancellationToken>())
            .Returns(group);
        _employeeRepository.ExistsByAccessGroupIdAsync(group.Id, Arg.Any<CancellationToken>())
            .Returns(false);
        _keycloakUserService.GetGroupByNameAsync("client", "custom1", Arg.Any<CancellationToken>())
            .Returns("kc-group-id");

        var sut = new DeleteAccessGroupCommandHandler(
            _accessGroupRepository, _employeeRepository, _keycloakUserService, _auditService,
            Substitute.For<ILogger<DeleteAccessGroupCommandHandler>>());

        var command = new DeleteAccessGroupCommand(
            _companyId, group.Id, "sub", "admin@empresa.com", "1.2.3.4");

        var result = await sut.HandleAsync(command);
        result.ShouldBe(Unit.Value);

        await _accessGroupRepository.Received(1).DeleteAsync(group.Id, Arg.Any<CancellationToken>());
        await _keycloakUserService.Received(1).DeleteGroupAsync("client", "kc-group-id", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAccessGroup_DefaultGroup_ThrowsBadRequest()
    {
        var defaultGroup = AccessGroup.Create(_companyId, "admin-empresa", Permissions.All);
        _accessGroupRepository.GetByIdAsync(defaultGroup.Id, Arg.Any<CancellationToken>())
            .Returns(defaultGroup);

        var sut = new DeleteAccessGroupCommandHandler(
            _accessGroupRepository, _employeeRepository, _keycloakUserService, _auditService,
            Substitute.For<ILogger<DeleteAccessGroupCommandHandler>>());

        var command = new DeleteAccessGroupCommand(
            _companyId, defaultGroup.Id, "sub", "admin@empresa.com", "1.2.3.4");

        await Should.ThrowAsync<BadRequestException>(async () => await sut.HandleAsync(command));
    }

    [Fact]
    public async Task DeleteAccessGroup_WithEmployees_ThrowsBadRequest()
    {
        var group = AccessGroup.Create(_companyId, "custom1", ["employees:read"]);
        _accessGroupRepository.GetByIdAsync(group.Id, Arg.Any<CancellationToken>())
            .Returns(group);
        _employeeRepository.ExistsByAccessGroupIdAsync(group.Id, Arg.Any<CancellationToken>())
            .Returns(true);

        var sut = new DeleteAccessGroupCommandHandler(
            _accessGroupRepository, _employeeRepository, _keycloakUserService, _auditService,
            Substitute.For<ILogger<DeleteAccessGroupCommandHandler>>());

        var command = new DeleteAccessGroupCommand(
            _companyId, group.Id, "sub", "admin@empresa.com", "1.2.3.4");

        var ex = await Should.ThrowAsync<BadRequestException>(async () => await sut.HandleAsync(command));
        ex.Message.ShouldContain("employees");
    }

    [Fact]
    public async Task DeleteAccessGroup_WrongCompany_ThrowsInvalidOp()
    {
        var group = AccessGroup.Create(Guid.NewGuid(), "custom1", ["employees:read"]);
        _accessGroupRepository.GetByIdAsync(group.Id, Arg.Any<CancellationToken>())
            .Returns(group);

        var sut = new DeleteAccessGroupCommandHandler(
            _accessGroupRepository, _employeeRepository, _keycloakUserService, _auditService,
            Substitute.For<ILogger<DeleteAccessGroupCommandHandler>>());

        var command = new DeleteAccessGroupCommand(
            _companyId, group.Id, "sub", "admin@empresa.com", "1.2.3.4");

        await Should.ThrowAsync<InvalidOperationException>(async () => await sut.HandleAsync(command));
    }

    [Fact]
    public async Task DeleteAccessGroup_NotFound_ThrowsKeyNotFound()
    {
        _accessGroupRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((AccessGroup?)null);

        var sut = new DeleteAccessGroupCommandHandler(
            _accessGroupRepository, _employeeRepository, _keycloakUserService, _auditService,
            Substitute.For<ILogger<DeleteAccessGroupCommandHandler>>());

        var command = new DeleteAccessGroupCommand(
            _companyId, Guid.NewGuid(), "sub", "admin@empresa.com", "1.2.3.4");

        await Should.ThrowAsync<KeyNotFoundException>(async () => await sut.HandleAsync(command));
    }

    [Fact]
    public async Task DeleteAccessGroup_KeycloakFailure_StillSucceeds()
    {
        var group = AccessGroup.Create(_companyId, "custom1", ["employees:read"]);
        _accessGroupRepository.GetByIdAsync(group.Id, Arg.Any<CancellationToken>())
            .Returns(group);
        _employeeRepository.ExistsByAccessGroupIdAsync(group.Id, Arg.Any<CancellationToken>())
            .Returns(false);
        _keycloakUserService.GetGroupByNameAsync("client", "custom1", Arg.Any<CancellationToken>())
            .Returns(Task.FromException<string?>(new Exception("Keycloak down")));

        var sut = new DeleteAccessGroupCommandHandler(
            _accessGroupRepository, _employeeRepository, _keycloakUserService, _auditService,
            Substitute.For<ILogger<DeleteAccessGroupCommandHandler>>());

        var command = new DeleteAccessGroupCommand(
            _companyId, group.Id, "sub", "admin@empresa.com", "1.2.3.4");

        var result = await sut.HandleAsync(command);
        result.ShouldBe(Unit.Value);
    }
}