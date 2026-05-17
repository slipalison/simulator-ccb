using Onboarding.Domain.Common;

namespace Onboarding.Domain.Aggregates.FundoAggregate;

/// <summary>
/// Simple join entity inside Fundo aggregate linking Fundo to TipoAtivo.
/// No payload — just the FK relationship. Managed via Fundo.AddTipoAtivo/RemoveTipoAtivo.
/// </summary>
public class FundoTipoAtivo : Entity<Guid>
{
    public Guid FundoId { get; private set; }
    public Guid TipoAtivoId { get; private set; }

    private FundoTipoAtivo() { }

    internal static FundoTipoAtivo Create(Guid fundoId, Guid tipoAtivoId)
    {
        return new FundoTipoAtivo
        {
            Id = Guid.NewGuid(),
            FundoId = fundoId,
            TipoAtivoId = tipoAtivoId
        };
    }
}