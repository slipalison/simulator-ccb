using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Onboarding.Application.Clients.Commands;
using Onboarding.Application.Common;
using Onboarding.Domain.Aggregates.ClientAggregate;
using Onboarding.Domain.Exceptions;
using Onboarding.Domain.Repositories;
using Shouldly;

namespace Onboarding.Domain.Tests.Application.Commands;

public class RegisterClientCommandHandlerTests
{
    private readonly IClientRepository _repo;
    private readonly IKeycloakUserService _keycloakService;
    private readonly ICommandHandler<RegisterClientCommand, Guid> _handler;

    public RegisterClientCommandHandlerTests()
    {
        _repo = Substitute.For<IClientRepository>();
        _repo.AddAsync(Arg.Any<Client>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _keycloakService = Substitute.For<IKeycloakUserService>();
        _keycloakService
            .CreateUserAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
                             Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("fake-keycloak-id");
        _handler = new RegisterClientCommandHandler(_repo, _keycloakService);
    }

    [Fact]
    public async Task HandleAsync_PessoaFisica_CreatesClientAndReturnsGuid()
    {
        // Arrange
        var command = new RegisterClientCommand(
            Nome: "João Silva",
            Cpf: "529.982.247-25",
            Cnpj: null,
            RazaoSocial: null,
            Email: "joao@example.com",
            Phone: "11999998888",
            Password: "Str0ng@Pass");

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.ShouldNotBe(Guid.Empty);
        await _repo.Received(1).AddAsync(
            Arg.Is<Client>(c => c.Type == ClientType.PessoaFisica),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_PessoaJuridica_CreatesClientAndReturnsGuid()
    {
        // Arrange
        var command = new RegisterClientCommand(
            Nome: "Empresa Ltda",
            Cpf: null,
            Cnpj: "11.222.333/0001-81",
            RazaoSocial: "Empresa Ltda",
            Email: "contato@empresa.com",
            Phone: "1133334444",
            Password: "Str0ng@Pass");

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.ShouldNotBe(Guid.Empty);
        await _repo.Received(1).AddAsync(
            Arg.Is<Client>(c => c.Type == ClientType.PessoaJuridica),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_InvalidCpf_ThrowsArgumentException()
    {
        // Arrange
        var command = new RegisterClientCommand(
            Nome: "João Silva",
            Cpf: "000.000.000-00",
            Cnpj: null,
            RazaoSocial: null,
            Email: "joao@example.com",
            Phone: "11999998888",
            Password: "Str0ng@Pass");

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(() => _handler.HandleAsync(command));
        await _repo.DidNotReceive().AddAsync(Arg.Any<Client>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_NullNome_ThrowsArgumentNullException()
    {
        // Arrange
        var command = new RegisterClientCommand(
            Nome: null!,
            Cpf: "529.982.247-25",
            Cnpj: null,
            RazaoSocial: null,
            Email: "joao@example.com",
            Phone: "11999998888",
            Password: "Str0ng@Pass");

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() => _handler.HandleAsync(command));
    }

    [Fact]
    public void Password_NotStoredInDomain_ClientHasNoPasswordProperty()
    {
        typeof(Client).GetProperty("Password").ShouldBeNull();
    }

    // REG-05: duplicate CPF → DuplicateClientException before AddAsync
    [Fact]
    public async Task HandleAsync_DuplicateCpf_ThrowsDuplicateClientExceptionWithoutPersisting()
    {
        // Arrange
        _repo.ExistsByCpfAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
             .Returns(true);

        var command = new RegisterClientCommand(
            Nome: "João Silva",
            Cpf: "529.982.247-25",
            Cnpj: null,
            RazaoSocial: null,
            Email: "joao@example.com",
            Phone: "11999998888",
            Password: "Str0ng@Pass");

        // Act & Assert
        await Should.ThrowAsync<DuplicateClientException>(
            () => _handler.HandleAsync(command));
        await _repo.DidNotReceive().AddAsync(Arg.Any<Client>(), Arg.Any<CancellationToken>());
    }

    // REG-05: duplicate email → DuplicateClientException before AddAsync
    [Fact]
    public async Task HandleAsync_DuplicateEmail_ThrowsDuplicateClientExceptionWithoutPersisting()
    {
        // Arrange
        _repo.ExistsByCpfAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
             .Returns(false);
        _repo.ExistsByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
             .Returns(true);

        var command = new RegisterClientCommand(
            Nome: "Maria Silva",
            Cpf: "529.982.247-25",
            Cnpj: null,
            RazaoSocial: null,
            Email: "maria@example.com",
            Phone: "11999998888",
            Password: "Str0ng@Pass");

        // Act & Assert
        await Should.ThrowAsync<DuplicateClientException>(
            () => _handler.HandleAsync(command));
        await _repo.DidNotReceive().AddAsync(Arg.Any<Client>(), Arg.Any<CancellationToken>());
    }

    // REG-06: Keycloak failure → DeleteAsync called (compensation), RegistrationFailedException thrown
    [Fact]
    public async Task HandleAsync_KeycloakFails_CompensatesWithDeleteAndThrowsRegistrationFailedException()
    {
        // Arrange
        _repo.ExistsByCpfAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _repo.ExistsByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _repo.DeleteAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _keycloakService
            .CreateUserAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
                             Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Keycloak unreachable"));

        var command = new RegisterClientCommand(
            Nome: "João Silva",
            Cpf: "529.982.247-25",
            Cnpj: null,
            RazaoSocial: null,
            Email: "joao@example.com",
            Phone: "11999998888",
            Password: "Str0ng@Pass");

        // Act & Assert
        await Should.ThrowAsync<RegistrationFailedException>(() => _handler.HandleAsync(command));
        await _repo.Received(1).DeleteAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    // REG-06: successful path → IKeycloakUserService.CreateUserAsync called with correct email
    [Fact]
    public async Task HandleAsync_PessoaFisica_CallsKeycloakCreateUser()
    {
        // Arrange
        _repo.ExistsByCpfAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _repo.ExistsByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        var command = new RegisterClientCommand(
            Nome: "João Silva",
            Cpf: "529.982.247-25",
            Cnpj: null,
            RazaoSocial: null,
            Email: "joao@example.com",
            Phone: "11999998888",
            Password: "Str0ng@Pass");

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.ShouldNotBe(Guid.Empty);
        await _keycloakService.Received(1).CreateUserAsync(
            username: "joao@example.com",
            email: "joao@example.com",
            password: "Str0ng@Pass",
            firstName: "João Silva",
            ct: Arg.Any<CancellationToken>());
    }
}
