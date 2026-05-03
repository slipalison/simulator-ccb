namespace Onboarding.Domain.ValueObjects;

/// <summary>
/// Discriminated union representing a Cedente's document — either PF (Cpf) or PJ (Cnpj).
/// Zero null risk: callers must handle both cases via Match or check IsPf/IsPj (D-06).
/// </summary>
public abstract record CedenteDocumento
{
    private CedenteDocumento() { }

    public bool IsPf => this is PessoaFisica;
    public bool IsPj => this is PessoaJuridica;

    /// <summary>
    /// Pattern-match on the document type. Both branches must be handled — no null risk.
    /// </summary>
    public TResult Match<TResult>(Func<PessoaFisica, TResult> onPf, Func<PessoaJuridica, TResult> onPj)
    {
        return this switch
        {
            PessoaFisica pf => onPf(pf),
            PessoaJuridica pj => onPj(pj),
            _ => throw new InvalidOperationException("Unreachable: CedenteDocumento has only PF and PJ variants.")
        };
    }

    /// <summary>
    /// Creates a PF variant wrapping a CPF value object.
    /// </summary>
    public static CedenteDocumento Pf(Cpf cpf) => new PessoaFisica(cpf);

    /// <summary>
    /// Creates a PJ variant wrapping a CNPJ value object.
    /// </summary>
    public static CedenteDocumento Pj(Cnpj cnpj) => new PessoaJuridica(cnpj);

    /// <summary>
    /// PF variant — natural person identified by CPF.
    /// </summary>
    public sealed record PessoaFisica(Cpf Cpf) : CedenteDocumento;

    /// <summary>
    /// PJ variant — legal entity identified by CNPJ.
    /// </summary>
    public sealed record PessoaJuridica(Cnpj Cnpj) : CedenteDocumento;
}