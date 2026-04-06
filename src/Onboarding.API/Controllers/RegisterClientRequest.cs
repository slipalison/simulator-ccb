namespace Onboarding.API.Controllers;

/// <summary>
/// HTTP request body for POST /api/registration.
/// Separate from RegisterClientCommand to decouple HTTP concerns from Application layer.
/// </summary>
public sealed class RegisterClientRequest
{
    // PF fields
    public string? Nome { get; set; }
    public string? Cpf { get; set; }

    // PJ fields
    public string? RazaoSocial { get; set; }
    public string? Cnpj { get; set; }

    // Common fields
    public string Email { get; set; } = default!;
    public string Phone { get; set; } = default!;
    public string Password { get; set; } = default!;
}
