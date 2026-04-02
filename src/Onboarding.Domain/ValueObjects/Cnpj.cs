namespace Onboarding.Domain.ValueObjects;

public sealed record Cnpj
{
    public string Value { get; }

    private Cnpj(string value) => Value = value;

    public static Cnpj Create(string? raw)
    {
        if (string.IsNullOrEmpty(raw))
            throw new ArgumentException("CNPJ não pode ser nulo ou vazio.", nameof(raw));

        var normalized = raw.Replace(".", "").Replace("/", "").Replace("-", "").ToUpperInvariant();

        if (!IsValid(normalized))
            throw new ArgumentException($"CNPJ inválido: '{raw}'", nameof(raw));

        return new Cnpj(normalized);
    }

    // Uses ASCII-48 mapping for alphanumeric backward compatibility.
    // For numeric CNPJs, CharValue('0') = '0'-48 = 0, ..., CharValue('9') = 9 — identical to digit value.
    // For alphanumeric CNPJs (July 2026 format), CharValue('A') = 65-48 = 17, etc.
    private static int CharValue(char c) => c - 48;

    private static bool IsValid(string cnpj)
    {
        if (cnpj.Length != 14) return false;

        // Reject all-same numeric sequences (e.g. 00000000000000)
        if (cnpj.All(char.IsDigit) && cnpj.Distinct().Count() == 1) return false;

        // All chars must be A-Z or 0-9
        if (!cnpj.All(c => char.IsAsciiDigit(c) || (c >= 'A' && c <= 'Z'))) return false;

        static int CalcDigit(string s, int[] weights)
        {
            var sum = weights.Select((w, i) => CharValue(s[i]) * w).Sum();
            var rem = sum % 11;
            return rem < 2 ? 0 : 11 - rem;
        }

        var w1 = new[] { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
        var w2 = new[] { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };

        var d1 = CalcDigit(cnpj, w1);
        if (d1 != cnpj[12] - '0') return false;

        var d2 = CalcDigit(cnpj, w2);
        return d2 == cnpj[13] - '0';
    }

    public override string ToString() => Value;
}
