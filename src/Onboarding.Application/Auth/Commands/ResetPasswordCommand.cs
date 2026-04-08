using Microsoft.Extensions.Logging;
using Onboarding.Application.Common;
using Onboarding.Domain.Exceptions;
using Onboarding.Domain.Repositories;

namespace Onboarding.Application.Auth.Commands;

/// <summary>
/// Command: reset password using a valid token.
/// Validates token, checks password policy, updates Keycloak password, marks token as used.
/// </summary>
public sealed record ResetPasswordCommand(string Token, string NewPassword);

public sealed class ResetPasswordCommandHandler : ICommandHandler<ResetPasswordCommand, Unit>
{
    private readonly IPasswordResetTokenRepository _tokenRepo;
    private readonly IKeycloakUserService _keycloakService;
    private readonly ILogger<ResetPasswordCommandHandler> _logger;

    public ResetPasswordCommandHandler(
        IPasswordResetTokenRepository tokenRepo,
        IKeycloakUserService keycloakService,
        ILogger<ResetPasswordCommandHandler> logger)
    {
        _tokenRepo = tokenRepo;
        _keycloakService = keycloakService;
        _logger = logger;
    }

    public async Task<Unit> HandleAsync(ResetPasswordCommand command, CancellationToken ct = default)
    {
        // 1. Validate token
        var resetToken = await _tokenRepo.GetByTokenAsync(command.Token, ct);
        if (resetToken == null)
            throw new BadRequestException("Token invalido.");

        if (!resetToken.IsValid)
        {
            if (resetToken.IsExpired)
                throw new BadRequestException("Token expirado.");
            if (resetToken.IsUsed)
                throw new BadRequestException("Token ja utilizado.");
        }

        // 2. Validate password policy
        if (!IsValidPassword(command.NewPassword))
            throw new BadRequestException(
                "Senha nao atende politica de seguranca: minimo 8 caracteres, " +
                "1 maiuscula, 1 minuscula, 1 numero, 1 caractere especial.");

        // 3. Update password in Keycloak via Admin API
        var user = await _keycloakService.GetUserByEmailAsync(resetToken.Email, ct);
        if (user == null)
            throw new BadRequestException("Usuario nao encontrado no Keycloak.");

        await _keycloakService.UpdateUserPasswordAsync(user.Id, command.NewPassword, ct);

        // 4. Mark token as used
        resetToken.MarkAsUsed();
        await _tokenRepo.UpdateAsync(resetToken, ct);

        _logger.LogInformation("Password reset successful for {Email}", resetToken.Email);

        return Unit.Value;
    }

    private static bool IsValidPassword(string password)
    {
        // Min 8 chars, uppercase, lowercase, digit, special char
        return password.Length >= 8
            && password.Any(char.IsUpper)
            && password.Any(char.IsLower)
            && password.Any(char.IsDigit)
            && password.Any(ch => !char.IsLetterOrDigit(ch));
    }
}
