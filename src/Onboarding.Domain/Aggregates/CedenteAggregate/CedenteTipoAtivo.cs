using Onboarding.Domain.Common;

namespace Onboarding.Domain.Aggregates.CedenteAggregate;

/// <summary>
/// Simple join entity inside Cedente aggregate linking Cedente to TipoAtivo.
/// No payload — just the FK relationship. Managed via Cedente.AddTipoAtivo/RemoveTipoAtivo.
/// </summary>
public sealed class CedenteTipoAtivo : Entity<Guid>
{
    public Guid CedenteId { get; private set; }
    public Guid TipoAtivoId { get; private set; }

    private CedenteTipoAtivo() { }

    internal static CedenteTipoAtivo Create(Guid cedenteId, Guid tipoAtivoId)
    {
        return new CedenteTipoAtivo
        {
            Id = Guid.NewGuid(),
            CedenteId = cedenteId,
            TipoAtivoId = tipoAtivoId
        };
    }
}