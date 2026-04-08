namespace Onboarding.Domain.Aggregates.PasswordReset;

/// <summary>
/// Represents a password reset token issued during forgot-password flow.
/// Tokens are single-use and expire after 15 minutes.
/// </summary>
public class PasswordResetToken
{
    public Guid Id { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string Token { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public bool IsUsed { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    public bool IsValid => !IsUsed && !IsExpired;

    private PasswordResetToken() { }

    public static PasswordResetToken Create(string email)
    {
        return new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            Email = email,
            Token = Guid.NewGuid().ToString("N"),
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            CreatedAt = DateTime.UtcNow,
            IsUsed = false
        };
    }

    public void MarkAsUsed()
    {
        IsUsed = true;
    }
}
