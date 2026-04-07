namespace Onboarding.API.Observability;

/// <summary>
/// HttpMessageHandler that rewrites request URIs from an external hostname
/// (e.g. localhost:8180) to an internal Docker network hostname (e.g. keycloak:8080).
/// 
/// Used by the JWT Bearer Backchannel to fetch the JWKS from Keycloak when the
/// OIDC discovery metadata returns a jwks_uri with the KC_HOSTNAME (external)
/// which is unreachable from inside the container.
/// </summary>
public sealed class HostnameRewriteHandler : DelegatingHandler
{
    private readonly string _internalHost;
    private readonly string _externalHost;

    public HostnameRewriteHandler(string internalHost, string externalHost)
        : this(internalHost, externalHost, new HttpClientHandler())
    {
    }

    public HostnameRewriteHandler(string internalHost, string externalHost, HttpMessageHandler innerHandler)
        : base(innerHandler)
    {
        _internalHost = internalHost;
        _externalHost = externalHost;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Rewrite the URI: replace external hostname with internal hostname
        if (request.RequestUri != null && request.RequestUri.ToString().Contains(_externalHost))
        {
            var newUri = request.RequestUri.ToString().Replace(
                _externalHost, _internalHost, StringComparison.OrdinalIgnoreCase);
            request.RequestUri = new Uri(newUri);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
