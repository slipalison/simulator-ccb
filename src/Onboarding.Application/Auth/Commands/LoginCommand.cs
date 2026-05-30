using Microsoft.Extensions.Logging;
using Onboarding.Application.Auth.DTOs;
using Onboarding.Application.Common;

namespace Onboarding.Application.Auth.Commands;

/// <summary>
/// Command: exchange email + password for a Keycloak token pair.
/// AUTH-02: triggers ROPC call via IKeycloakTokenService.ExchangePasswordAsync.
/// </summary>
public sealed record LoginCommand(string Email, string Password);

public sealed class LoginCommandHandler : ICommandHandler<LoginCommand, TokenResponse>
{
    private readonly IKeycloakTokenService _tokenService;
    private readonly ILogger<LoginCommandHandler> _logger;

    public LoginCommandHandler(
        IKeycloakTokenService tokenService,
        ILogger<LoginCommandHandler> logger)
    {
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<TokenResponse> HandleAsync(LoginCommand command, CancellationToken ct = default)
    {
        // IKeycloakTokenService throws KeycloakAuthException on failure — caller (AuthController) maps to 401
        var tokens = await _tokenService.ExchangePasswordAsync(command.Email, command.Password, ct);
        _logger.LogInformation("Login successful");
        return tokens;
    }
}
