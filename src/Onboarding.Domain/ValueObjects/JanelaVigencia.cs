namespace Onboarding.Domain.ValueObjects;

/// <summary>
/// Value object representing a half-open date window [DataInicio, DataFim).
/// D-20: DataInicio is required; DataFim is nullable (null = infinite validity).
/// DataFim must be strictly greater than DataInicio when set.
/// </summary>
public sealed record JanelaVigencia
{
    public DateTimeOffset DataInicio { get; }
    public DateTimeOffset? DataFim { get; }

    private JanelaVigencia(DateTimeOffset dataInicio, DateTimeOffset? dataFim)
    {
        DataInicio = dataInicio;
        DataFim = dataFim;
    }

    /// <summary>
    /// Creates a JanelaVigencia. DataInicio is required; DataFim is optional.
    /// Throws ArgumentException if DataFim is set and not strictly greater than DataInicio.
    /// </summary>
    public static JanelaVigencia Create(DateTimeOffset dataInicio, DateTimeOffset? dataFim = null)
    {
        if (dataFim.HasValue && dataFim.Value <= dataInicio)
            throw new ArgumentException(
                $"DataFim must be strictly greater than DataInicio. DataInicio={dataInicio:O}, DataFim={dataFim.Value:O}",
                nameof(dataFim));

        return new JanelaVigencia(dataInicio, dataFim);
    }

    /// <summary>
    /// Returns true if the window has no end date (infinite validity).
    /// </summary>
    public bool IsInfinite => DataFim is null;

    /// <summary>
    /// Returns true if the window is currently active as of the given instant.
    /// Half-open: [DataInicio, DataFim) — DataFim itself is excluded.
    /// </summary>
    public bool IsActiveAt(DateTimeOffset instant)
        => instant >= DataInicio && (!DataFim.HasValue || instant < DataFim.Value);

    public override string ToString()
        => DataFim.HasValue
            ? $"[{DataInicio:O}, {DataFim.Value:O})"
            : $"[{DataInicio:O}, ∞)";
}
