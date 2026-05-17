namespace Onboarding.Domain.Aggregates.FundoAggregate;

/// <summary>
/// Classification of fund types per CVM regulations.
/// </summary>
public enum TipoFundo
{
    RendaFixa = 1,
    RendaVariavel = 2,
    Cambial = 3,
    Multimercado = 4,
    Outros = 5
}