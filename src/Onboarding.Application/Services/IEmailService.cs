namespace Onboarding.Application.Services;

public interface IEmailService
{
    Task SendPasswordResetEmailAsync(string email, string resetLink, CancellationToken ct = default);
}
