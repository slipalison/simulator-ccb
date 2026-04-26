namespace Onboarding.Application.Companies.DTOs;

/// <summary>
/// Request DTO for POST /api/companies/registration.
/// Nullable fields allow FluentValidation to handle missing values.
/// </summary>
public sealed record RegisterCompanyRequest(
    string? RazaoSocial,
    string? Cnpj,
    string? Email,
    string? Phone,
    string? Password,
    bool? TermsAccepted);