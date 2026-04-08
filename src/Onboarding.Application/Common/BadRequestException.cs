namespace Onboarding.Application.Common;

/// <summary>
/// Thrown when a request fails validation or business rule checks at the application level.
/// Maps to HTTP 400 Bad Request.
/// </summary>
public sealed class BadRequestException : Exception
{
    public BadRequestException(string message) : base(message) { }
}
