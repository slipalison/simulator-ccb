namespace Onboarding.Domain.ValueObjects;

public sealed record Email
{
    public string Value { get; }

    private Email(string value) => Value = value;

    public static Email Create(string? raw)
    {
        if (string.IsNullOrEmpty(raw))
            throw new ArgumentException("Email não pode ser nulo ou vazio.", nameof(raw));

        var parts = raw.Split('@');
        if (parts.Length != 2)
            throw new ArgumentException($"Email inválido: '{raw}'", nameof(raw));

        var localPart = parts[0];
        var domainPart = parts[1];

        if (localPart.Length == 0)
            throw new ArgumentException($"Email inválido — parte local vazia: '{raw}'", nameof(raw));

        var dotIndex = domainPart.IndexOf('.');
        if (dotIndex < 1 || dotIndex >= domainPart.Length - 1)
            throw new ArgumentException($"Email inválido — domínio sem ponto válido: '{raw}'", nameof(raw));

        return new Email(raw.ToLowerInvariant());
    }

    public override string ToString() => Value;
}
