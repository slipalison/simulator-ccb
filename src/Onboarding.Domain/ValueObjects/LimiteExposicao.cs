namespace Onboarding.Domain.ValueObjects;

/// <summary>
/// Value object encapsulating exposure limits for a relationship aggregate.
/// At least one of Percentual or Valor must be provided (PLAN T-1 default rule; reviewer may adjust).
/// Percentual: 0-100 decimal (null = no percentage cap).
/// Valor: positive decimal (null = no value cap).
/// </summary>
public sealed record LimiteExposicao
{
    /// <summary>
    /// Exposure limit as a percentage (0-100). Null means no percentage cap.
    /// </summary>
    public decimal? Percentual { get; }

    /// <summary>
    /// Exposure limit as an absolute monetary value. Null means no value cap.
    /// </summary>
    public decimal? Valor { get; }

    private LimiteExposicao(decimal? percentual, decimal? valor)
    {
        Percentual = percentual;
        Valor = valor;
    }

    /// <summary>
    /// Creates a LimiteExposicao. At least one of percentual or valor must be provided.
    /// Percentual must be in [0, 100] when set. Valor must be > 0 when set.
    /// </summary>
    public static LimiteExposicao Create(decimal? percentual, decimal? valor)
    {
        if (percentual is null && valor is null)
            throw new ArgumentException(
                "At least one of Percentual or Valor must be provided for LimiteExposicao.");

        if (percentual.HasValue && (percentual.Value < 0m || percentual.Value > 100m))
            throw new ArgumentException(
                $"LimiteExposicao Percentual must be between 0 and 100. Value: {percentual.Value}",
                nameof(percentual));

        if (valor.HasValue && valor.Value <= 0m)
            throw new ArgumentException(
                $"LimiteExposicao Valor must be greater than 0. Value: {valor.Value}",
                nameof(valor));

        return new LimiteExposicao(percentual, valor);
    }

    /// <summary>
    /// Creates a LimiteExposicao with only a percentage limit.
    /// </summary>
    public static LimiteExposicao FromPercentual(decimal percentual)
        => Create(percentual, null);

    /// <summary>
    /// Creates a LimiteExposicao with only a value limit.
    /// </summary>
    public static LimiteExposicao FromValor(decimal valor)
        => Create(null, valor);

    /// <summary>
    /// Returns true if both Percentual and Valor are set.
    /// </summary>
    public bool HasBothLimits => Percentual.HasValue && Valor.HasValue;

    public override string ToString()
    {
        if (HasBothLimits)
            return $"{Percentual!.Value}% / R${Valor!.Value:N2}";
        if (Percentual.HasValue)
            return $"{Percentual.Value}%";
        return $"R${Valor!.Value:N2}";
    }
}
