namespace Onboarding.API.Configuration;

/// <summary>
/// Cookie configuration settings — loaded via IOptions&lt;CookieSettings&gt;.
/// </summary>
public sealed class CookieSettings
{
    /// <summary>
    /// Whether cookies should require HTTPS (Secure flag).
    /// true in production, false in local development.
    /// </summary>
    public bool Secure { get; set; } = true;
}
