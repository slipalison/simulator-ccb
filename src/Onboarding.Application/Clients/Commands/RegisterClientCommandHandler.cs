using Onboarding.Application.Common;
using Onboarding.Domain.Aggregates.CompanyAggregate;
using Onboarding.Domain.Exceptions;
using Onboarding.Domain.Repositories;

namespace Onboarding.Application.Clients.Commands;

public sealed class RegisterClientCommandHandler
    : ICommandHandler<RegisterClientCommand, Guid>
{
    private readonly ICompanyRepository _repository;
    private readonly IKeycloakUserService _keycloakUserService;

    public RegisterClientCommandHandler(
        ICompanyRepository repository,
        IKeycloakUserService keycloakUserService)
    {
        _repository = repository;
        _keycloakUserService = keycloakUserService;
    }

    public async Task<Guid> HandleAsync(
        RegisterClientCommand command, CancellationToken ct = default)
    {
        // 1. Duplicate detection — fail fast before any write (REG-02, REG-05)
        var normalizedCnpj = command.Cnpj?
            .Replace(".", "").Replace("/", "").Replace("-", "") ?? "";

        if (!string.IsNullOrEmpty(normalizedCnpj))
        {
            if (await _repository.ExistsByCnpjAsync(normalizedCnpj, ct))
                throw new DuplicateCompanyException("CNPJ already registered.");
        }

        if (await _repository.ExistsByEmailAsync(command.Email!, ct))
            throw new DuplicateCompanyException("Email already registered.");

        // 2. Build domain aggregate — value objects validate format + check digits (REG-03, REG-04)
        var terms = TermsAcceptance.Create(TermsAcceptance.CurrentVersion, "0.0.0.0");
        var company = Company.Register(
            command.RazaoSocial!, command.Cnpj!, command.Email!, command.Phone!, terms);

        // 3. Persist to app_db first (architectural decision from STATE.md)
        await _repository.AddAsync(company, ct);

        // 4. Create Keycloak user — compensate if it fails (REG-06)
        try
        {
            var keycloakUserId = await _keycloakUserService.CreateUserAsync("client",
                username: command.Email!,
                email: command.Email!,
                password: command.Password!,
                firstName: command.RazaoSocial,
                ct: ct);

            // Store Keycloak user ID for JWT sub-based profile lookup (LGPD: no email in tokens)
            company.SetKeycloakUserId(keycloakUserId);
            await _repository.SaveAsync(company, ct);
        }
        catch (DuplicateKeycloakUserException ex)
        {
            await _repository.DeleteAsync(company.Id, ct);
            throw new DuplicateCompanyException("A company with the provided information already exists.", ex);
        }
        catch (Exception ex) when (IsTransientKeycloakError(ex))
        {
            await _repository.DeleteAsync(company.Id, ct);
            throw new RegistrationFailedException(
                "User registration failed due to an internal error. Please try again.", ex);
        }

        return company.Id;
    }

    /// <summary>
    /// Returns true for exceptions that indicate a Keycloak infrastructure failure or
    /// unexpected error. We compensate (delete DB row) for any error after DB persist.
    /// Does NOT catch ArgumentException (which would be a programming error).
    /// </summary>
    private static bool IsTransientKeycloakError(Exception ex) =>
        ex is not ArgumentException;
}
