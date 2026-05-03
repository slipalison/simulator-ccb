using Onboarding.Domain.Common;
using Onboarding.Domain.ValueObjects;

namespace Onboarding.Domain.Aggregates.CustodianteAggregate;

/// <summary>
/// Custodiante aggregate root — represents a financial custodian institution.
/// Company-scoped per D-01 (ClienteId FK, HasQueryFilter in Infrastructure).
/// </summary>
public sealed class Custodiante : Entity<Guid>
{
    public Guid ClienteId { get; private set; }
    public string RazaoSocial { get; private set; } = default!;
    public string? CodigoInterno { get; private set; }
    public Cnpj Cnpj { get; private set; } = default!;
    public Email? Email { get; private set; }
    public PhoneNumber? Telefone { get; private set; }
    public CustodianteStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private Custodiante() { }

    /// <summary>
    /// Factory method — creates a new Custodiante with ATIVO status.
    /// </summary>
    public static Custodiante Register(
        string razaoSocial,
        string cnpj,
        Guid clientId,
        string? codigoInterno = null,
        string? email = null,
        string? telefone = null)
    {
        if (string.IsNullOrWhiteSpace(razaoSocial))
            throw new ArgumentException("Razão Social is required.", nameof(razaoSocial));

        return new Custodiante
        {
            Id = Guid.NewGuid(),
            RazaoSocial = razaoSocial,
            Cnpj = Cnpj.Create(cnpj),
            ClienteId = clientId,
            CodigoInterno = codigoInterno,
            Email = email is not null ? Email.Create(email) : null,
            Telefone = telefone is not null ? PhoneNumber.Create(telefone) : null,
            Status = CustodianteStatus.ATIVO,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Updates Custodiante data. RazaoSocial is required.
    /// </summary>
    public void Update(
        string razaoSocial,
        string? codigoInterno,
        string? email,
        string? telefone,
        CustodianteStatus status)
    {
        if (string.IsNullOrWhiteSpace(razaoSocial))
            throw new ArgumentException("Razão Social is required.", nameof(razaoSocial));

        RazaoSocial = razaoSocial;
        CodigoInterno = codigoInterno;
        Email = email is not null ? Email.Create(email) : null;
        Telefone = telefone is not null ? PhoneNumber.Create(telefone) : null;
        Status = status;
    }
}