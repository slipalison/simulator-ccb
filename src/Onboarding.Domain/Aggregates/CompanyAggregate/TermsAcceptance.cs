namespace Onboarding.Domain.Aggregates.CompanyAggregate;

/// <summary>
/// Value object representing a user's acceptance of terms of use.
/// Required on Company registration per REG-04 / D-12.
/// </summary>
public sealed record TermsAcceptance
{
    public DateTimeOffset AcceptedAt { get; }
    public string TermsVersion { get; }
    public string IpAddress { get; }

    private TermsAcceptance(DateTimeOffset acceptedAt, string termsVersion, string ipAddress)
    {
        AcceptedAt = acceptedAt;
        TermsVersion = termsVersion;
        IpAddress = ipAddress;
    }

    /// <summary>
    /// Creates a new TermsAcceptance with the current UTC timestamp.
    /// Throws if termsVersion or ipAddress is null/empty (D-12).
    /// </summary>
    public static TermsAcceptance Create(string termsVersion, string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(termsVersion))
            throw new ArgumentException("Terms version is required.", nameof(termsVersion));
        if (string.IsNullOrWhiteSpace(ipAddress))
            throw new ArgumentException("IP address is required for terms acceptance.", nameof(ipAddress));

        return new TermsAcceptance(DateTimeOffset.UtcNow, termsVersion, ipAddress);
    }

    /// <summary>
    /// Current version of the terms of use (D-13).
    /// </summary>
    public static readonly string CurrentVersion = "1.0";
}