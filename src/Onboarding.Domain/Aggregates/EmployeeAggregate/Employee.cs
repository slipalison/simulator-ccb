using Onboarding.Domain.Common;
using Onboarding.Domain.ValueObjects;

namespace Onboarding.Domain.Aggregates.EmployeeAggregate;

/// <summary>
/// Employee aggregate root — represents a PF (Pessoa Física) linked to a Company (D-01, D-02, D-04).
/// CompanyId is a Guid FK with no navigation property — EF Core configures the FK in Infrastructure.
/// </summary>
public sealed class Employee : Entity<Guid>
{
    public Cpf Cpf { get; private set; } = default!;
    public string Nome { get; private set; } = default!;
    public Email Email { get; private set; } = default!;
    public PhoneNumber Phone { get; private set; } = default!;

    /// <summary>
    /// FK to Company aggregate — no navigation property (D-02).
    /// EF Core configures company_id in employees table.
    /// </summary>
    public Guid CompanyId { get; private set; }

    /// <summary>
    /// Keycloak user ID (sub claim) — identifies the user in JWT tokens (LGPD).
    /// </summary>
    public string? KeycloakUserId { get; private set; }

    /// <summary>
    /// FK to AccessGroup — determines employee permissions (PERM-04).
    /// </summary>
    public Guid AccessGroupId { get; private set; }

    /// <summary>
    /// LGPD soft-delete support (MGMT-05).
    /// </summary>
    public DateTimeOffset? DeletedAt { get; private set; }
    public bool IsDeleted => DeletedAt.HasValue;

    private Employee() { }

    /// <summary>
    /// Factory method — creates a new Employee linked to a Company (D-04).
    /// </summary>
    public static Employee Register(
        string nome,
        string cpf,
        string email,
        string phone,
        Guid companyId,
        Guid accessGroupId)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("Nome is required.", nameof(nome));

        return new Employee
        {
            Id = Guid.NewGuid(),
            Nome = nome,
            Cpf = Cpf.Create(cpf),
            Email = Email.Create(email),
            Phone = PhoneNumber.Create(phone),
            CompanyId = companyId,
            AccessGroupId = accessGroupId
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
    /// Anonymizes all PII data for LGPD compliance. Idempotent (MGMT-05).
    /// </summary>
    public void Anonymize()
    {
        if (DeletedAt.HasValue) return;

        DeletedAt = DateTimeOffset.UtcNow;
        Nome = "Usuário Excluído";
        Email = Email.Create($"deleted-{Id}@internal.local");
        Phone = PhoneNumber.Create("+0000000000");
        Cpf = null!;
    }

    /// <summary>
    /// Updates employee data with full server-side validation.
    /// </summary>
    public void Update(string nome, string email, string phone)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("Nome is required.", nameof(nome));

        Nome = nome;
        Email = Email.Create(email);
        Phone = PhoneNumber.Create(phone);
    }

    /// <summary>
    /// Changes the employee's access group (PERM-04).
    /// </summary>
    public void SetAccessGroup(Guid accessGroupId)
    {
        AccessGroupId = accessGroupId;
    }
}