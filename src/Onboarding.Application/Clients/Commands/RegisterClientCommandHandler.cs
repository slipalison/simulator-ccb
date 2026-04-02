using Onboarding.Application.Common;
using Onboarding.Domain.Aggregates.ClientAggregate;
using Onboarding.Domain.Repositories;

namespace Onboarding.Application.Clients.Commands;

public sealed class RegisterClientCommandHandler
    : ICommandHandler<RegisterClientCommand, Guid>
{
    private readonly IClientRepository _repository;

    public RegisterClientCommandHandler(IClientRepository repository)
        => _repository = repository;

    public async Task<Guid> HandleAsync(
        RegisterClientCommand command, CancellationToken ct = default)
    {
        var client = command.Cpf is not null
            ? Client.RegisterPessoaFisica(
                command.Nome, command.Cpf, command.Email, command.Phone)
            : Client.RegisterPessoaJuridica(
                command.RazaoSocial!, command.Cnpj!, command.Email, command.Phone);

        await _repository.AddAsync(client, ct);

        // TODO Phase 5: forward command.Password to IKeycloakUserService.CreateUserAsync
        // Password is not stored in the domain — Keycloak owns auth credentials.

        return client.Id;
    }
}
