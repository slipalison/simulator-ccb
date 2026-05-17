namespace Onboarding.Domain.Aggregates.TipoAtivoAggregate;

/// <summary>
/// Classification of asset types per CVM regulation.
/// </summary>
public enum TipoAtivoCategoria
{
    RendaFixa = 1,
    RendaVariavel = 2,
    Derivativo = 3,
    Misto = 4,
    Outros = 5
}