using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Onboarding.Application.Common;
using Onboarding.Application.Services;
using Onboarding.Domain.Aggregates.PasswordReset;
using Onboarding.Domain.Exceptions;
using Onboarding.Domain.Repositories;

namespace Onboarding.Application.Auth.Commands;

/// <summary>
/// Command: initiate forgot password flow — create reset token and send email.
/// Returns Unit. Always returns success (no info disclosure about email existence).
/// </summary>
public sealed record ForgotPasswordCommand(string Email);

public sealed class ForgotPasswordCommandHandler : ICommandHandler<ForgotPasswordCommand, Unit>
{
    private readonly IPasswordResetTokenRepository _tokenRepo;
    private readonly IKeycloakUserService _keycloakService;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ForgotPasswordCommandHandler> _logger;

    public ForgotPasswordCommandHandler(
        IPasswordResetTokenRepository tokenRepo,
        IKeycloakUserService keycloakService,
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<ForgotPasswordCommandHandler> logger)
    {
        _tokenRepo = tokenRepo;
        _keycloakService = keycloakService;
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<Unit> HandleAsync(ForgotPasswordCommand command, CancellationToken ct = default)
    {
        // 1. Check if email exists (without revealing it)
        var userExists = await _keycloakService.UserExistsByEmailAsync("client", command.Email, ct);

        // 2. If exists, create token + send email
        if (userExists)
        {
            // Rate limiting check: max 3 per hour
            var recentTokens = await _tokenRepo.CountRecentTokensAsync(
                command.Email, TimeSpan.FromHours(1), ct);

            if (recentTokens >= 3)
                throw new RateLimitExceededException("Maximo 3 resets por hora.");

            // Create token
            var token = PasswordResetToken.Create(command.Email);
            await _tokenRepo.AddAsync(token, ct);

            // Send email via Resend.com — use configurable frontend base URL
            var frontendBaseUrl = _configuration["Frontend:BaseUrl"] ?? "http://localhost:5173";
            frontendBaseUrl = frontendBaseUrl.TrimEnd('/');
            var resetLink = $"{frontendBaseUrl}/reset-password?token={token.Token}";
            await _emailService.SendPasswordResetEmailAsync(command.Email, resetLink, ct);

            _logger.LogInformation("Password reset email sent to {Email}", command.Email);
        }
        else
        {
            // Same log for non-existing email (no info disclosure)
            _logger.LogInformation(
                "Password reset requested for non-existing email: {Email}", command.Email);
        }

        // Always return success (no info disclosure)
        return Unit.Value;
    }
}
