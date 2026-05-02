namespace Onboarding.Application.Companies.Commands;

/// <summary>
/// Command: register a new PJ company with terms acceptance (REG-01, REG-02).
/// </summary>
public sealed record RegisterCompanyCommand(
    string RazaoSocial,
    string Cnpj,
    string Email,
    string Phone,
    string Password,
    bool TermsAccepted,
    string TermsVersion,
    string? IpAddress);