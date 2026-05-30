using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Onboarding.Application.Services;

namespace Onboarding.Infrastructure.Services;

/// <summary>Thin HTTP wrapper — tested via integration tests.</summary>
[ExcludeFromCodeCoverage]
public sealed class ResendEmailService : IEmailService
{
    private readonly HttpClient _httpClient;
    private readonly string _fromEmail;
    private readonly ILogger<ResendEmailService> _logger;

    public ResendEmailService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<ResendEmailService> logger)
    {
        _httpClient = httpClient;
        _fromEmail = configuration["Email:FromAddress"] ?? "onboarding@example.com";
        _logger = logger;
    }

    public async Task SendPasswordResetEmailAsync(string email, string resetLink, CancellationToken ct = default)
    {
        var payload = new
        {
            from = _fromEmail,
            to = new[] { email },
            subject = "Recuperacao de Senha",
            html = $@"
                <h1>Recuperacao de Senha</h1>
                <p>Clique no link abaixo para redefinir sua senha:</p>
                <a href='{resetLink}'>Redefinir Senha</a>
                <p>Este link expira em 15 minutos.</p>
                <p>Se voce nao solicitou esta recuperacao, ignore este email.</p>
            "
        };

        var response = await _httpClient.PostAsJsonAsync("emails", payload, ct);
        response.EnsureSuccessStatusCode();

        _logger.LogInformation("Password reset email sent to {Email}", MaskEmail(email));
    }

    // SEC-01: mask email for log output — preserves domain, hides local part after first char
    private static string MaskEmail(string email)
    {
        var at = email.IndexOf('@');
        return at > 0 ? email[0] + "***" + email[at..] : "***";
    }
}
