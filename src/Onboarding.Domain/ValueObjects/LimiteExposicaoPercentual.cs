namespace Onboarding.Domain.ValueObjects;

/// <summary>
/// Value object representing an exposure limit percentage.
/// Sentinel value -1 means "unlimited" (no cap). Valid percentages are 0-100.
/// </summary>
public sealed record LimiteExposicaoPercentual
{
    public decimal Value { get; }

    /// <summary>
    /// True when Value == -1 (unlimited exposure, no percentage cap).
    /// </summary>
    public bool IsUnlimited => Value == -1m;

    private LimiteExposicaoPercentual(decimal value) => Value = value;

    /// <summary>
    /// Creates a LimiteExposicaoPercentual. Accepts -1 (unlimited) or 0-100.
    /// Rejects values below -1, above 100, or fractional negatives like -0.5.
    /// </summary>
    public static LimiteExposicaoPercentual Create(decimal value)
    {
        if (value == -1m)
            return new LimiteExposicaoPercentual(value);

        if (value < 0m || value > 100m)
            throw new ArgumentException(
                $"Limite de exposição deve ser -1 (ilimitado) ou entre 0 e 100. Valor recebido: {value}",
                nameof(value));

        return new LimiteExposicaoPercentual(value);
    }

    public override string ToString() => IsUnlimited ? "Ilimitado" : $"{Value}%";
}