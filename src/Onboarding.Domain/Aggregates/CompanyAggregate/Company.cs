using Onboarding.Domain.Common;
using Onboarding.Domain.ValueObjects;

namespace Onboarding.Domain.Aggregates.CompanyAggregate;

/// <summary>
/// Company aggregate root — represents a Pessoa Jurídica (PJ).
/// Replaces the obsolete Client aggregate (D-01, D-03, D-05).
/// </summary>
public sealed class Company : Entity<Guid>
{
    public Cnpj Cnpj { get; private set; } = default!;
    public string RazaoSocial { get; private set; } = default!;
    public Email Email { get; private set; } = default!;
    public PhoneNumber Phone { get; private set; } = default!;

    /// <summary>
    /// Keycloak user ID (sub claim) — identifies the user in JWT tokens without exposing PII (LGPD).
    /// </summary>
    public string? KeycloakUserId { get; private set; }

    /// <summary>
    /// Terms of use acceptance — required on registration (D-11, D-12).
    /// Stored as owned type (EF Core OwnsOne) in the companies table.
    /// </summary>
    public TermsAcceptance TermsAcceptance { get; private set; } = default!;

    /// <summary>
    /// LGPD soft-delete support (MGMT-05).
    /// </summary>
    public DateTime? DeletedAt { get; private set; }
    public bool IsDeleted => DeletedAt.HasValue;

    private Company() { }

    /// <summary>
    /// Factory method — creates a new Company with required terms acceptance (D-03, D-12).
    /// </summary>
    public static Company Register(
        string razaoSocial,
        string cnpj,
        string email,
        string phone,
        TermsAcceptance termsAcceptance)
    {
        if (termsAcceptance is null)
            throw new ArgumentException("Terms acceptance is required for company registration.", nameof(termsAcceptance));
        if (string.IsNullOrWhiteSpace(razaoSocial))
            throw new ArgumentException("Razão Social is required.", nameof(razaoSocial));

        return new Company
        {
            Id = Guid.NewGuid(),
            RazaoSocial = razaoSocial,
            Cnpj = Cnpj.Create(cnpj),
            Email = Email.Create(email),
            Phone = PhoneNumber.Create(phone),
            TermsAcceptance = termsAcceptance
        };
    }

    /// <summary>
    /// Sets the Keycloak user ID after user creation in Keycloak.
    /// </summary>
    public void SetKeycloakUserId(string keycloakUserId)
    {
        KeycloakUserId = keycloakUserId ?? throw new ArgumentNullException(nameof(keycloakUserId));
    }

    /// <summary>
    /// Anonymizes all PII data for LGPD compliance. Idempotent — calling twice does not change state further.
    /// </summary>
    public void Anonymize()
    {
        if (DeletedAt.HasValue) return; // Already deleted — idempotent guard

        DeletedAt = DateTime.UtcNow;
        RazaoSocial = "Empresa Excluída";
        Email = Email.Create($"deleted-{Id}@internal.local");
        Phone = PhoneNumber.Create("+0000000000");
        Cnpj = null!;
    }

    /// <summary>
    /// Updates company data with full server-side validation.
    /// </summary>
    public void Update(string razaoSocial, string email, string phone)
    {
        if (string.IsNullOrWhiteSpace(razaoSocial))
            throw new ArgumentException("Razão Social is required.", nameof(razaoSocial));

        RazaoSocial = razaoSocial;
        Email = Email.Create(email);
        Phone = PhoneNumber.Create(phone);
    }
}