using System.Reflection;
using Serilog.Core;
using Serilog.Events;

namespace Onboarding.API.Observability;

/// <summary>
/// Serilog IDestructuringPolicy that masks sensitive fields in logged objects.
/// Registered globally on Log.Logger — applies to all sinks (SEC-09, D-17).
/// </summary>
public sealed class SensitiveDataDestructuringPolicy : IDestructuringPolicy
{
    // Case-insensitive set of property names to redact (D-18)
    private static readonly HashSet<string> SensitivePropertyNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "password", "token", "secret", "client_secret", "authorization"
    };

    // Property names that contain CPF data with special masking rules (D-19)
    private static readonly HashSet<string> CpfPropertyNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "cpf"
    };

    // Property names that contain CNPJ data with special masking rules (D-19)
    private static readonly HashSet<string> CnpjPropertyNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "cnpj"
    };

    // Property names that contain email data with partial masking rules (D-20)
    private static readonly HashSet<string> EmailPropertyNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "email"
    };

    public bool TryDestructure(
        object value,
        ILogEventPropertyValueFactory propertyValueFactory,
        out LogEventPropertyValue result)
    {
        // Only handle non-null, non-primitive, non-string complex objects
        if (value is null || value.GetType().IsPrimitive || value is string || value.GetType().IsEnum)
        {
            result = null!;
            return false;
        }

        var properties = value.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead)
            .ToArray();

        // Only intercept objects that have at least one sensitive property
        bool hasSensitiveProperty = properties.Any(p =>
            SensitivePropertyNames.Contains(p.Name) ||
            CpfPropertyNames.Contains(p.Name) ||
            CnpjPropertyNames.Contains(p.Name) ||
            EmailPropertyNames.Contains(p.Name));

        if (!hasSensitiveProperty)
        {
            result = null!;
            return false;
        }

        var logProperties = properties.Select(prop =>
        {
            var rawValue = prop.GetValue(value);

            if (SensitivePropertyNames.Contains(prop.Name))
                return new LogEventProperty(prop.Name, new ScalarValue("[REDACTED]"));

            if (CpfPropertyNames.Contains(prop.Name) && rawValue is string)
                return new LogEventProperty(prop.Name, new ScalarValue(MaskCpf()));

            if (CnpjPropertyNames.Contains(prop.Name) && rawValue is string)
                return new LogEventProperty(prop.Name, new ScalarValue(MaskCnpj()));

            if (EmailPropertyNames.Contains(prop.Name) && rawValue is string emailStr)
                return new LogEventProperty(prop.Name, new ScalarValue(MaskEmail(emailStr)));

            return new LogEventProperty(prop.Name,
                propertyValueFactory.CreatePropertyValue(rawValue, true));
        });

        result = new StructureValue(logProperties);
        return true;
    }

    // D-19: CPF masked as ***.***.***-** regardless of actual value
    private static string MaskCpf() => "***.***.***-**";

    // D-19: CNPJ masked as **.***.***/****.***-** regardless of actual value
    private static string MaskCnpj() => "**.***.***/****.***-**";

    // D-20: Email masked as first-char***@domain — preserves domain for debugging
    public static string MaskEmail(string email)
    {
        var atIndex = email.IndexOf('@');
        if (atIndex <= 0) return "***";
        return email[0] + "***" + email[atIndex..];
    }
}
