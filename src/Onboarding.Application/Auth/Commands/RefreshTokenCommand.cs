using Onboarding.Application.Auth.DTOs;
using Onboarding.Application.Common;

namespace Onboarding.Application.Auth.Commands;

/// <summary>
/// Command: exchange refresh token for a new Keycloak token pair.
/// AUTH-04: backend exposes POST /api/auth/refresh — frontend triggers when token is near expiry.
/// </summary>
public sealed record RefreshTokenCommand(string RefreshToken);

public sealed class RefreshTokenCommandHandler : ICommandHandler<RefreshTokenCommand, TokenResponse>
{
    private readonly IKeycloakTokenService _tokenService;

    public RefreshTokenCommandHandler(IKeycloakTokenService tokenService)
    {
        _tokenService = tokenService;
    }

    public async Task<TokenResponse> HandleAsync(RefreshTokenCommand command, CancellationToken ct = default)
    {
        // IKeycloakTokenService throws KeycloakAuthException on failure — caller (AuthController) maps to 401
        return await _tokenService.RefreshTokenAsync(command.RefreshToken, ct);
    }
}
