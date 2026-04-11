namespace Onboarding.Application.Auth.DTOs;

/// <summary>
/// Response returned after successful admin login.
/// Contains refresh token for cookie and admin user info.
/// </summary>
public sealed record AdminSessionResponse(
    string RefreshToken,
    int RefreshExpiresIn,
    string AdminName,
    string AdminEmail);
