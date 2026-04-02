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

    // Private constructor: prevents external construction; EF Core uses parameterless constructor
    private Client() { }

    // EF Core entry point — required to allow EF Core to instantiate this entity
    // without invoking factory methods. Without this, EF Core cannot materialize
    // Client instances from the database.
    protected Client(bool _) { }

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
}
