using Microsoft.Extensions.Logging;
using NSubstitute;
using Onboarding.Application.Common;
using Onboarding.Application.Companies.Commands;
using Onboarding.Domain.Aggregates.Audit;
using Onboarding.Domain.Aggregates.CompanyAggregate;
using Onboarding.Domain.Aggregates.EmployeeAggregate;
using Onboarding.Domain.Exceptions;
using Onboarding.Domain.Repositories;
using Shouldly;

namespace Onboarding.Domain.Tests.Application.Companies;

public class RegisterCompanyCommandHandlerTests
{
    private readonly ICompanyRepository _companyRepository;
    private readonly IAccessGroupRepository _accessGroupRepository;
    private readonly IKeycloakUserService _keycloakUserService;
    private readonly IAuditService _auditService;
    private readonly ILogger<RegisterCompanyCommandHandler> _logger;
    private readonly RegisterCompanyCommandHandler _sut;

    public RegisterCompanyCommandHandlerTests()
    {
        _companyRepository = Substitute.For<ICompanyRepository>();
        _accessGroupRepository = Substitute.For<IAccessGroupRepository>();
        _keycloakUserService = Substitute.For<IKeycloakUserService>();
        _auditService = Substitute.For<IAuditService>();
        _logger = Substitute.For<ILogger<RegisterCompanyCommandHandler>>();
        _sut = new RegisterCompanyCommandHandler(
            _companyRepository, _accessGroupRepository, _keycloakUserService, _auditService, _logger);
    }

    private RegisterCompanyCommand ValidCommand() => new(
        RazaoSocial: "Empresa Teste LTDA",
        Cnpj: "11444777000161",
        Email: "empresa@teste.com",
        Phone: "11999999999",
        Password: "Senha@123",
        TermsAccepted: true,
        TermsVersion: "1.0",
        IpAddress: "192.168.1.1"
    );

    [Fact]
    public async Task HandleAsync_ValidRegistration_CreatesCompanyAndKeycloakUserAndSeedsAccessGroups()
    {
        // Arrange
        var command = ValidCommand();
        var keycloakUserId = Guid.NewGuid().ToString();
        _companyRepository.ExistsByCnpjAsync(command.Cnpj, Arg.Any<CancellationToken>()).Returns(false);
        _companyRepository.ExistsByEmailAsync(command.Email, Arg.Any<CancellationToken>()).Returns(false);
        _keycloakUserService.CreateUserAsync("client", command.Email, command.Email, command.Password, command.RazaoSocial, Arg.Any<CancellationToken>())
            .Returns(keycloakUserId);

        // Act
        var result = await _sut.HandleAsync(command);

        // Assert
        result.ShouldNotBeNull();
        result.CompanyId.ShouldNotBe(Guid.Empty);
        result.KeycloakUserId.ShouldBe(keycloakUserId);

        await _companyRepository.Received(1).AddAsync(Arg.Is<Company>(c =>
            c.RazaoSocial == command.RazaoSocial &&
            c.Email.Value == command.Email.ToLowerInvariant()), Arg.Any<CancellationToken>());
        await _keycloakUserService.Received(1).CreateUserAsync("client", command.Email, command.Email, command.Password, command.RazaoSocial, Arg.Any<CancellationToken>());
        await _companyRepository.Received(1).SaveAsync(Arg.Any<Company>(), Arg.Any<CancellationToken>());
        await _accessGroupRepository.Received(1).AddRangeAsync(Arg.Is<IEnumerable<AccessGroup>>(groups =>
            groups.Count() == 3), Arg.Any<CancellationToken>());
        await _auditService.Received(1).RecordAsync(
            Arg.Any<string>(),
            command.Email,
            ActionType.CompanyRegistered,
            Arg.Any<Guid?>(),
            command.RazaoSocial,
            Arg.Is<string>(d => d.Contains(command.Cnpj)),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<Guid?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ValidRegistration_CreatesKeycloakGroupsForDefaultAccessGroups()
    {
        // Arrange
        var command = ValidCommand();
        var keycloakUserId = Guid.NewGuid().ToString();
        _companyRepository.ExistsByCnpjAsync(command.Cnpj, Arg.Any<CancellationToken>()).Returns(false);
        _companyRepository.ExistsByEmailAsync(command.Email, Arg.Any<CancellationToken>()).Returns(false);
        _keycloakUserService.CreateUserAsync("client", command.Email, command.Email, command.Password, command.RazaoSocial, Arg.Any<CancellationToken>())
            .Returns(keycloakUserId);

        // Act
        var result = await _sut.HandleAsync(command);

        // Assert — CreateGroupAsync called for each default group (D-13)
        await _keycloakUserService.Received(1).CreateGroupAsync("client", "admin-empresa", Arg.Any<CancellationToken>());
        await _keycloakUserService.Received(1).CreateGroupAsync("client", "viewer", Arg.Any<CancellationToken>());
        await _keycloakUserService.Received(1).CreateGroupAsync("client", "dashboard", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_KeycloakGroupCreationFailure_StillCompletesRegistration()
    {
        // Arrange — Keycloak group creation fails but DB group creation succeeds (D-13 eventual consistency)
        var command = ValidCommand();
        var keycloakUserId = Guid.NewGuid().ToString();
        _companyRepository.ExistsByCnpjAsync(command.Cnpj, Arg.Any<CancellationToken>()).Returns(false);
        _companyRepository.ExistsByEmailAsync(command.Email, Arg.Any<CancellationToken>()).Returns(false);
        _keycloakUserService.CreateUserAsync("client", command.Email, command.Email, command.Password, command.RazaoSocial, Arg.Any<CancellationToken>())
            .Returns(keycloakUserId);
        _keycloakUserService.CreateGroupAsync("client", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<Task<string>>(_ => throw new Exception("Keycloak group creation failed"));

        // Act — should NOT throw despite Keycloak group failure
        var result = await _sut.HandleAsync(command);

        // Assert — company still created, groups still seeded in DB
        result.ShouldNotBeNull();
        await _accessGroupRepository.Received(1).AddRangeAsync(Arg.Is<IEnumerable<AccessGroup>>(groups =>
            groups.Count() == 3), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_DuplicateCnpj_ThrowsDuplicateCompanyException()
    {
        // Arrange
        var command = ValidCommand();
        _companyRepository.ExistsByCnpjAsync(command.Cnpj, Arg.Any<CancellationToken>()).Returns(true);

        // Act & Assert
        await Should.ThrowAsync<DuplicateCompanyException>(() => _sut.HandleAsync(command));

        await _keycloakUserService.DidNotReceive().CreateUserAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_DuplicateEmail_ThrowsDuplicateCompanyException()
    {
        // Arrange
        var command = ValidCommand();
        _companyRepository.ExistsByCnpjAsync(command.Cnpj, Arg.Any<CancellationToken>()).Returns(false);
        _companyRepository.ExistsByEmailAsync(command.Email, Arg.Any<CancellationToken>()).Returns(true);

        // Act & Assert
        await Should.ThrowAsync<DuplicateCompanyException>(() => _sut.HandleAsync(command));

        await _keycloakUserService.DidNotReceive().CreateUserAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_KeycloakCreationFailure_DeletesCompanyAsCompensation()
    {
        // Arrange
        var command = ValidCommand();
        _companyRepository.ExistsByCnpjAsync(command.Cnpj, Arg.Any<CancellationToken>()).Returns(false);
        _companyRepository.ExistsByEmailAsync(command.Email, Arg.Any<CancellationToken>()).Returns(false);
        _keycloakUserService.CreateUserAsync("client", command.Email, command.Email, command.Password, command.RazaoSocial, Arg.Any<CancellationToken>())
            .Returns<Task<string>>(_ => throw new Exception("Keycloak error"));

        // Act & Assert
        await Should.ThrowAsync<Exception>(() => _sut.HandleAsync(command));

        await _companyRepository.Received(1).DeleteAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_SetsKeycloakUserIdOnCompany()
    {
        // Arrange
        var command = ValidCommand();
        var keycloakUserId = Guid.NewGuid().ToString();
        _companyRepository.ExistsByCnpjAsync(command.Cnpj, Arg.Any<CancellationToken>()).Returns(false);
        _companyRepository.ExistsByEmailAsync(command.Email, Arg.Any<CancellationToken>()).Returns(false);
        _keycloakUserService.CreateUserAsync("client", command.Email, command.Email, command.Password, command.RazaoSocial, Arg.Any<CancellationToken>())
            .Returns(keycloakUserId);

        // Act
        var result = await _sut.HandleAsync(command);

        // Assert
        result.KeycloakUserId.ShouldBe(keycloakUserId);
        await _companyRepository.Received(1).SaveAsync(Arg.Is<Company>(c => c.KeycloakUserId == keycloakUserId), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_RecordsAuditWithCompanyRegisteredActionType()
    {
        // Arrange
        var command = ValidCommand();
        var keycloakUserId = Guid.NewGuid().ToString();
        _companyRepository.ExistsByCnpjAsync(command.Cnpj, Arg.Any<CancellationToken>()).Returns(false);
        _companyRepository.ExistsByEmailAsync(command.Email, Arg.Any<CancellationToken>()).Returns(false);
        _keycloakUserService.CreateUserAsync("client", command.Email, command.Email, command.Password, command.RazaoSocial, Arg.Any<CancellationToken>())
            .Returns(keycloakUserId);

        // Act
        await _sut.HandleAsync(command);

        // Assert
        await _auditService.Received(1).RecordAsync(
            Arg.Any<string>(),
            command.Email,
            ActionType.CompanyRegistered,
            Arg.Any<Guid?>(),
            command.RazaoSocial,
            Arg.Is<string>(d => d.Contains(command.Cnpj)),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<Guid?>(),
            Arg.Any<CancellationToken>());
    }
}