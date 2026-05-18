namespace Onboarding.Application.Auth.DTOs;

/// <summary>
/// Response shape for GET /api/auth/me.
/// Extends the token fields with a flat permissions array so the frontend
/// can gate UI elements without an extra round-trip (frontend-client-fundos, iter 6).
/// Permissions are resolved server-side by ClientClaimsMiddleware from the DB
/// AccessGroup lookup — not derived from JWT claims in the controller.
/// </summary>
public sealed record MeResponse(
    string AccessToken,
    int ExpiresIn,
    string TokenType,
    string Scope,
    IReadOnlyList<string> Permissions);
