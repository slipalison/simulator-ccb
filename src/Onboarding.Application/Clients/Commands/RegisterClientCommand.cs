namespace Onboarding.Application.Clients.Commands;

public sealed record RegisterClientCommand(
    string Nome,
    string? Cpf,
    string? Cnpj,
    string? RazaoSocial,
    string? Email,
    string? Phone,
    // Password is NOT stored in the domain. It is forwarded to Keycloak Admin API
    // in Phase 5 (RegisterClientCommandHandler will call IKeycloakUserService).
    // The Client aggregate intentionally has no Password property.
    string? Password);
