namespace Onboarding.Domain.ValueObjects;

public sealed record Cpf
{
    public string Value { get; }

    private Cpf(string value) => Value = value;

    public static Cpf Create(string? raw)
    {
        var digits = raw?.Replace(".", "").Replace("-", "") ?? "";
        if (!IsValid(digits))
            throw new ArgumentException($"CPF inválido: '{raw}'", nameof(raw));
        return new Cpf(digits);
    }

    private static bool IsValid(string digits)
    {
        if (digits.Length != 11) return false;
        if (!digits.All(char.IsDigit)) return false;
        if (digits.Distinct().Count() == 1) return false;

        static int CalcDigit(string d, int[] weights)
        {
            var sum = weights.Select((w, i) => (d[i] - '0') * w).Sum();
            var rem = sum % 11;
            return rem < 2 ? 0 : 11 - rem;
        }

        var w1 = new[] { 10, 9, 8, 7, 6, 5, 4, 3, 2 };
        var w2 = new[] { 11, 10, 9, 8, 7, 6, 5, 4, 3, 2 };

        var d1 = CalcDigit(digits, w1);
        if (d1 != digits[9] - '0') return false;

        var d2 = CalcDigit(digits, w2);
        return d2 == digits[10] - '0';
    }

    public override string ToString() => Value;
}
