using Microsoft.Extensions.Logging;
using NSubstitute;
using Onboarding.Application.Admin.Commands;
using Onboarding.Application.Common;
using Onboarding.Domain.Aggregates.Audit;
using Onboarding.Domain.Aggregates.CompanyAggregate;
using Onboarding.Domain.Repositories;
using Onboarding.Domain.ValueObjects;
using Shouldly;

namespace Onboarding.Domain.Tests.Application.Admin;

public class UpdateCompanyCommandHandlerTests
{
    private readonly ICompanyRepository _companyRepository;
    private readonly IKeycloakUserService _keycloakUserService;
    private readonly IAuditService _auditService;
    private readonly ILogger<UpdateCompanyCommandHandler> _logger;
    private readonly UpdateCompanyCommandHandler _sut;

    public UpdateCompanyCommandHandlerTests()
    {
        _companyRepository = Substitute.For<ICompanyRepository>();
        _keycloakUserService = Substitute.For<IKeycloakUserService>();
        _auditService = Substitute.For<IAuditService>();
        _logger = Substitute.For<ILogger<UpdateCompanyCommandHandler>>();
        _sut = new UpdateCompanyCommandHandler(_companyRepository, _keycloakUserService, _auditService, _logger);
    }

    private static Company CreateTestCompany()
    {
        return Company.Register("Empresa Original", "11444777000161", "original@teste.com", "11999999999",
            TermsAcceptance.Create("1.0", "127.0.0.1"));
    }

    [Fact]
    public async Task HandleAsync_UpdatesCompanyAndSyncsEmailToKeycloak_WhenEmailChanges()
    {
        // Arrange
        var company = CreateTestCompany();
        company.SetKeycloakUserId("keycloak-user-123");
        var command = new UpdateCompanyCommand(company.Id, "Empresa Atualizada", "nova@teste.com", "11888888888");
        _companyRepository.GetByIdAsync(command.CompanyId, Arg.Any<CancellationToken>()).Returns(company);

        // Act
        await _sut.HandleAsync(command);

        // Assert
        await _companyRepository.Received(1).SaveAsync(Arg.Is<Company>(c =>
            c.RazaoSocial == "Empresa Atualizada" &&
            c.Email.Value == "nova@teste.com"), Arg.Any<CancellationToken>());
        await _keycloakUserService.Received(1).UpdateAdminUserAsync(
            "client", "keycloak-user-123", "Empresa Atualizada", "nova@teste.com", Arg.Any<CancellationToken>());
        await _auditService.Received(1).RecordAsync(
            Arg.Any<string>(), Arg.Any<string>(), ActionType.CompanyUpdated,
            command.CompanyId, "Empresa Atualizada", Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_DoesNotSyncKeycloak_WhenEmailUnchanged()
    {
        // Arrange
        var company = CreateTestCompany();
        company.SetKeycloakUserId("keycloak-user-123");
        var command = new UpdateCompanyCommand(company.Id, "Empresa Atualizada", "original@teste.com", "11888888888");
        _companyRepository.GetByIdAsync(command.CompanyId, Arg.Any<CancellationToken>()).Returns(company);

        // Act
        await _sut.HandleAsync(command);

        // Assert — same email, no Keycloak sync
        await _keycloakUserService.DidNotReceive().UpdateAdminUserAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ThrowsKeyNotFoundException_WhenCompanyNotFound()
    {
        // Arrange
        var missingId = Guid.NewGuid();
        var command = new UpdateCompanyCommand(missingId, "Empresa", "email@teste.com", "11999999999");
        _companyRepository.GetByIdAsync(missingId, Arg.Any<CancellationToken>()).Returns((Company?)null);

        // Act & Assert
        await Should.ThrowAsync<KeyNotFoundException>(() => _sut.HandleAsync(command));
    }

    [Fact]
    public async Task HandleAsync_ContinuesWithoutRethrow_WhenKeycloakSyncFails()
    {
        // Arrange
        var company = CreateTestCompany();
        company.SetKeycloakUserId("keycloak-user-123");
        var command = new UpdateCompanyCommand(company.Id, "Empresa Atualizada", "nova@teste.com", "11888888888");
        _companyRepository.GetByIdAsync(command.CompanyId, Arg.Any<CancellationToken>()).Returns(company);
        _keycloakUserService.UpdateAdminUserAsync("client", "keycloak-user-123", "Empresa Atualizada", "nova@teste.com", Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new Exception("Keycloak sync failed")));

        // Act — should NOT throw
        await Should.NotThrowAsync(() => _sut.HandleAsync(command));

        // Assert — DB update still happened
        await _companyRepository.Received(1).SaveAsync(Arg.Any<Company>(), Arg.Any<CancellationToken>());
    }
}