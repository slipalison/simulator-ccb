using Onboarding.Application.Common;
using Onboarding.Domain.Aggregates.ClientAggregate;
using Onboarding.Domain.Exceptions;
using Onboarding.Domain.Repositories;

namespace Onboarding.Application.Clients.Commands;

public sealed class RegisterClientCommandHandler
    : ICommandHandler<RegisterClientCommand, Guid>
{
    private readonly IClientRepository _repository;
    private readonly IKeycloakUserService _keycloakUserService;

    public RegisterClientCommandHandler(
        IClientRepository repository,
        IKeycloakUserService keycloakUserService)
    {
        _repository = repository;
        _keycloakUserService = keycloakUserService;
    }

    public async Task<Guid> HandleAsync(
        RegisterClientCommand command, CancellationToken ct = default)
    {
        // 1. Duplicate detection — fail fast before any write (REG-05)
        // Normalize document strings to match the stored normalized values.
        if (!string.IsNullOrEmpty(command.Cpf))
        {
            var normalizedCpf = command.Cpf.Replace(".", "").Replace("-", "");
            if (await _repository.ExistsByCpfAsync(normalizedCpf, ct))
                throw new DuplicateClientException("CPF already registered.");
        }

        if (!string.IsNullOrEmpty(command.Cnpj))
        {
            var normalizedCnpj = command.Cnpj
                .Replace(".", "").Replace("/", "").Replace("-", "");
            if (await _repository.ExistsByCnpjAsync(normalizedCnpj, ct))
                throw new DuplicateClientException("CNPJ already registered.");
        }

        if (await _repository.ExistsByEmailAsync(command.Email, ct))
            throw new DuplicateClientException("Email already registered.");

        // 2. Build domain aggregate — value objects validate format + check digits here (REG-03, REG-04)
        var client = command.Cpf is not null
            ? Client.RegisterPessoaFisica(
                command.Nome, command.Cpf, command.Email, command.Phone)
            : Client.RegisterPessoaJuridica(
                command.RazaoSocial!, command.Cnpj!, command.Email, command.Phone);

        // 3. Persist to app_db first (architectural decision from STATE.md)
        await _repository.AddAsync(client, ct);

        // 4. Create Keycloak user — compensate if it fails (REG-06)
        try
        {
            await _keycloakUserService.CreateUserAsync(
                username: command.Email,
                email: command.Email,
                password: command.Password,
                firstName: command.Nome,
                ct: ct);
        }
        catch (Exception ex) when (IsTransientKeycloakError(ex))
        {
            // Compensation: delete the persisted row — Keycloak is the auth source of truth
            await _repository.DeleteAsync(client.Id, ct);
            throw new RegistrationFailedException(
                "User registration failed due to an internal error. Please try again.", ex);
        }

        return client.Id;
    }

    /// <summary>
    /// Returns true for exceptions that indicate a Keycloak infrastructure failure or
    /// unexpected error. We compensate (delete DB row) for any error after DB persist.
    /// Does NOT catch ArgumentException (which would be a programming error).
    /// </summary>
    private static bool IsTransientKeycloakError(Exception ex) =>
        ex is not ArgumentException;
}
