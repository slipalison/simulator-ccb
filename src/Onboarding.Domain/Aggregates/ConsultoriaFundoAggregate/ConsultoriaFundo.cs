using Onboarding.Domain.Common;
using Onboarding.Domain.ValueObjects;

namespace Onboarding.Domain.Aggregates.ConsultoriaFundoAggregate;

/// <summary>
/// ConsultoriaFundo aggregate root — represents an investment fund consultancy.
/// Company-scoped per D-01 (ClienteId FK, HasQueryFilter in Infrastructure).
/// </summary>
public sealed class ConsultoriaFundo : Entity<Guid>
{
    public Guid ClienteId { get; private set; }
    public string RazaoSocial { get; private set; } = default!;
    public string? NomeFantasia { get; private set; }
    public Cnpj Cnpj { get; private set; } = default!;
    public Email? Email { get; private set; }
    public PhoneNumber? Telefone { get; private set; }
    public ConsultoriaFundoStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private ConsultoriaFundo() { }

    /// <summary>
    /// Factory method — creates a new ConsultoriaFundo with ATIVO status.
    /// </summary>
    public static ConsultoriaFundo Register(
        string razaoSocial,
        string cnpj,
        Guid clientId,
        string? nomeFantasia = null,
        string? email = null,
        string? telefone = null)
    {
        if (clientId == Guid.Empty)
            throw new ArgumentException("ClientId is required.", nameof(clientId));
        if (string.IsNullOrWhiteSpace(razaoSocial))
            throw new ArgumentException("Razão Social is required.", nameof(razaoSocial));

        return new ConsultoriaFundo
        {
            Id = Guid.NewGuid(),
            RazaoSocial = razaoSocial,
            Cnpj = Cnpj.Create(cnpj),
            ClienteId = clientId,
            NomeFantasia = nomeFantasia,
            Email = email is not null ? Email.Create(email) : null,
            Telefone = telefone is not null ? PhoneNumber.Create(telefone) : null,
            Status = ConsultoriaFundoStatus.ATIVO,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Updates ConsultoriaFundo data. RazaoSocial is required.
    /// </summary>
    public void Update(
        string razaoSocial,
        string? nomeFantasia,
        string? email,
        string? telefone,
        ConsultoriaFundoStatus status)
    {
        if (string.IsNullOrWhiteSpace(razaoSocial))
            throw new ArgumentException("Razão Social is required.", nameof(razaoSocial));

        RazaoSocial = razaoSocial;
        NomeFantasia = nomeFantasia;
        Email = email is not null ? Email.Create(email) : null;
        Telefone = telefone is not null ? PhoneNumber.Create(telefone) : null;
        Status = status;
    }
}