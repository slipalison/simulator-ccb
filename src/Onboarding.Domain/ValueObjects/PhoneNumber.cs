namespace Onboarding.Domain.ValueObjects;

public sealed record PhoneNumber
{
    public string Value { get; }

    private PhoneNumber(string value) => Value = value;

    public static PhoneNumber Create(string? raw)
    {
        if (raw is null)
            throw new ArgumentException("Telefone não pode ser nulo.", nameof(raw));

        var digits = new string(raw.Where(char.IsDigit).ToArray());

        if (digits.Length < 8 || digits.Length > 15)
            throw new ArgumentException(
                $"Telefone inválido — deve ter entre 8 e 15 dígitos, obtido {digits.Length}: '{raw}'",
                nameof(raw));

        return new PhoneNumber(digits);
    }

    public override string ToString() => Value;
}
