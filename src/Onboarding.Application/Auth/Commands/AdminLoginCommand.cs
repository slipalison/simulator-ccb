namespace Onboarding.Application.Auth.Commands;

/// <summary>
/// Command: admin login with email + password.
/// ADMIN-06: triggers ROPC call via IKeycloakTokenService.ExchangePasswordAsync,
/// then validates the user has "admin" role.
/// </summary>
public sealed record AdminLoginCommand(string Email, string Password);
