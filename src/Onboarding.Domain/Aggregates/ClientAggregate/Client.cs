using Onboarding.Domain.Common;
using Onboarding.Domain.ValueObjects;

namespace Onboarding.Domain.Aggregates.ClientAggregate;

public sealed class Client : Entity<Guid>
{
    public string Name { get; private set; } = default!;
    public Email Email { get; private set; } = default!;
    public PhoneNumber Phone { get; private set; } = default!;
    public ClientType Type { get; private set; }

    // PF-specific (null for PJ)
    public Cpf? Cpf { get; private set; }

    // PJ-specific (null for PF)
    public Cnpj? Cnpj { get; private set; }
    public string? RazaoSocial { get; private set; }

    // LGPD soft-delete support
    public DateTime? DeletedAt { get; private set; }
    public bool IsDeleted => DeletedAt.HasValue;

    // Protected parameterless constructor: used by EF Core to materialize entities
    // from the database without invoking factory methods. External code must use
    // the static factory methods (RegisterPessoaFisica / RegisterPessoaJuridica)
    // which enforce all domain invariants.
    // CS0628: Warning suppressed — protected constructor in sealed class is an
    // intentional EF Core convention pattern required for entity materialization.
#pragma warning disable CS0628
    protected Client() { }
#pragma warning restore CS0628

    public static Client RegisterPessoaFisica(
        string nome,
        string cpf,
        string email,
        string phone)
    {
        return new Client
        {
            Id = Guid.NewGuid(),
            Name = nome ?? throw new ArgumentNullException(nameof(nome)),
            Cpf = ValueObjects.Cpf.Create(cpf),
            Email = ValueObjects.Email.Create(email),
            Phone = PhoneNumber.Create(phone),
            Type = ClientType.PessoaFisica
        };
    }

    public static Client RegisterPessoaJuridica(
        string razaoSocial,
        string cnpj,
        string email,
        string phone)
    {
        return new Client
        {
            Id = Guid.NewGuid(),
            Name = razaoSocial ?? throw new ArgumentNullException(nameof(razaoSocial)),
            Cnpj = ValueObjects.Cnpj.Create(cnpj),
            Email = ValueObjects.Email.Create(email),
            Phone = PhoneNumber.Create(phone),
            Type = ClientType.PessoaJuridica,
            RazaoSocial = razaoSocial
        };
    }

    /// <summary>
    /// Anonymizes all PII data for LGPD compliance. Idempotent — calling twice does not change state further.
    /// </summary>
    public void Anonymize()
    {
        if (DeletedAt.HasValue) return; // Already deleted — idempotent guard

        DeletedAt = DateTime.UtcNow;
        Name = "Usuário Excluído";
        Email = Email.Create($"deleted-{Id}@internal.local");
        Phone = PhoneNumber.Create("+0000000000");
        Cpf = null;
        Cnpj = null;
        RazaoSocial = null;
    }

    /// <summary>
    /// Updates user data with full server-side validation.
    /// </summary>
    public void Update(string name, string? razaoSocial, string email, string phone)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty.", nameof(name));

        Name = name;
        RazaoSocial = razaoSocial;
        Email = Email.Create(email);
        Phone = PhoneNumber.Create(phone);
    }
}
