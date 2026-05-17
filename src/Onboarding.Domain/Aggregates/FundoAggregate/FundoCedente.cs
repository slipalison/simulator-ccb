using Onboarding.Domain.Common;
using Onboarding.Domain.ValueObjects;

namespace Onboarding.Domain.Aggregates.FundoAggregate;

/// <summary>
/// Join entity inside Fundo aggregate representing the association between
/// a Fundo and a Cedente, with exposure limits and date ranges.
/// Managed via Fundo.AddCedente/UpdateCedente/RemoveCedente (D-08).
/// </summary>
public class FundoCedente : Entity<Guid>
{
    public Guid FundoId { get; private set; }
    public Guid CedenteId { get; private set; }
    public LimiteExposicaoPercentual LimiteExposicaoPercentual { get; private set; } = default!;
    public decimal? LimiteExposicaoValor { get; private set; }
    public DateTimeOffset DataInicio { get; private set; }
    public DateTimeOffset? DataFim { get; private set; }
    public FundoCedenteStatus Status { get; private set; }

    private FundoCedente() { }

    internal static FundoCedente Create(
        Guid fundoId,
        Guid cedenteId,
        LimiteExposicaoPercentual limitePercentual,
        decimal? limiteValor,
        DateTimeOffset dataInicio,
        DateTimeOffset? dataFim)
    {
        return new FundoCedente
        {
            Id = Guid.NewGuid(),
            FundoId = fundoId,
            CedenteId = cedenteId,
            LimiteExposicaoPercentual = limitePercentual,
            LimiteExposicaoValor = limiteValor,
            DataInicio = dataInicio,
            DataFim = dataFim,
            Status = FundoCedenteStatus.ATIVO
        };
    }

    internal void UpdateDetails(
        LimiteExposicaoPercentual limitePercentual,
        decimal? limiteValor,
        DateTimeOffset dataInicio,
        DateTimeOffset? dataFim,
        FundoCedenteStatus status)
    {
        LimiteExposicaoPercentual = limitePercentual;
        LimiteExposicaoValor = limiteValor;
        DataInicio = dataInicio;
        DataFim = dataFim;
        Status = status;
    }
}