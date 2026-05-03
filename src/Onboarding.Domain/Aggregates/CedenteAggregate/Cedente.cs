using Onboarding.Domain.Common;
using Onboarding.Domain.Exceptions;
using Onboarding.Domain.ValueObjects;

namespace Onboarding.Domain.Aggregates.CedenteAggregate;

/// <summary>
/// Cedente aggregate root — represents a cedente (PF or PJ) that cedes credits to a fund.
/// Polymorphic via CedenteDocumento discriminated union (D-05, D-06).
/// Company-scoped per D-01 (ClienteId FK, HasQueryFilter in Infrastructure).
/// </summary>
public sealed class Cedente : Entity<Guid>
{
    public Guid ClienteId { get; private set; }
    public CedenteDocumento Documento { get; private set; } = default!;
    public string Nome { get; private set; } = default!;
    public Email? Email { get; private set; }
    public PhoneNumber? Telefone { get; private set; }
    public string? Endereco { get; private set; }
    public CedenteStatus Status { get; private set; }
    public List<CedenteTipoAtivo> TiposAtivo { get; private set; } = [];
    public DateTimeOffset CreatedAt { get; private set; }

    private Cedente() { }

    /// <summary>
    /// Factory method — creates a PF Cedente using CPF as document (D-05, D-06).
    /// </summary>
    public static Cedente RegisterPf(
        string cpf,
        string nome,
        Guid clientId,
        string? email = null,
        string? telefone = null,
        string? endereco = null)
    {
        if (clientId == Guid.Empty)
            throw new ArgumentException("ClientId is required.", nameof(clientId));
        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("Nome is required.", nameof(nome));

        return new Cedente
        {
            Id = Guid.NewGuid(),
            Documento = CedenteDocumento.Pf(Cpf.Create(cpf)),
            Nome = nome,
            ClienteId = clientId,
            Email = email is not null ? Email.Create(email) : null,
            Telefone = telefone is not null ? PhoneNumber.Create(telefone) : null,
            Endereco = endereco,
            Status = CedenteStatus.ATIVO,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Factory method — creates a PJ Cedente using CNPJ as document (D-05, D-06).
    /// </summary>
    public static Cedente RegisterPj(
        string cnpj,
        string razaoSocial,
        Guid clientId,
        string? email = null,
        string? telefone = null,
        string? endereco = null)
    {
        if (clientId == Guid.Empty)
            throw new ArgumentException("ClientId is required.", nameof(clientId));
        if (string.IsNullOrWhiteSpace(razaoSocial))
            throw new ArgumentException("Razão Social is required.", nameof(razaoSocial));

        return new Cedente
        {
            Id = Guid.NewGuid(),
            Documento = CedenteDocumento.Pj(Cnpj.Create(cnpj)),
            Nome = razaoSocial,
            ClienteId = clientId,
            Email = email is not null ? Email.Create(email) : null,
            Telefone = telefone is not null ? PhoneNumber.Create(telefone) : null,
            Endereco = endereco,
            Status = CedenteStatus.ATIVO,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Updates Cedente data. Nome is required.
    /// </summary>
    public void Update(
        string nome,
        string? email,
        string? telefone,
        string? endereco,
        CedenteStatus status)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("Nome is required.", nameof(nome));

        Nome = nome;
        Email = email is not null ? Email.Create(email) : null;
        Telefone = telefone is not null ? PhoneNumber.Create(telefone) : null;
        Endereco = endereco;
        Status = status;
    }

    /// <summary>
    /// Adds a CedenteTipoAtivo join record. Rejects duplicates.
    /// </summary>
    public void AddTipoAtivo(Guid tipoAtivoId)
    {
        if (TiposAtivo.Any(t => t.TipoAtivoId == tipoAtivoId))
            throw new DuplicateEntityException("CedenteTipoAtivo", tipoAtivoId.ToString());

        TiposAtivo.Add(CedenteTipoAtivo.Create(Id, tipoAtivoId));
    }

    /// <summary>
    /// Removes a CedenteTipoAtivo join record. Idempotent — does nothing if not found.
    /// </summary>
    public void RemoveTipoAtivo(Guid tipoAtivoId)
    {
        var ta = TiposAtivo.FirstOrDefault(t => t.TipoAtivoId == tipoAtivoId);
        if (ta is not null)
            TiposAtivo.Remove(ta);
    }
}