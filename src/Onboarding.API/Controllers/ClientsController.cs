using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Onboarding.Domain.Aggregates.ClientAggregate;
using Onboarding.Domain.Repositories;

namespace Onboarding.API.Controllers;

/// <summary>
/// Client profile endpoints.
/// GET /api/clients/me — AUTH-03: protected route returns profile of authenticated client.
/// D-06: [Authorize] without explicit policy — JWT signature + expiry only.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class ClientsController : ControllerBase
{
    private readonly IClientRepository _repository;
    private readonly ILogger<ClientsController> _logger;

    public ClientsController(
        IClientRepository repository,
        ILogger<ClientsController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>GET /api/clients/me — returns the authenticated client's profile.</summary>
    [HttpGet("me")]
    [Authorize(AuthenticationSchemes = "BearerClient")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMe(CancellationToken ct)
    {
        // Primary: use "sub" (Keycloak user ID) — opaque, stable, non-personal identifier
        var keycloakSub = User.FindFirst("sub")?.Value;
        if (!string.IsNullOrEmpty(keycloakSub))
        {
            var client = await _repository.GetByKeycloakSubAsync(keycloakSub, ct);
            if (client is not null) return Ok(MapToDto(client));

            // sub present but client not in DB — authenticated user without profile
            _logger.LogWarning("Authenticated user with sub {Sub} not found in database", keycloakSub);
            return NotFound();
        }

        // Fallback: use "name" claim (present in Keycloak tokens via profile scope).
        // Keycloak appends " -" to the name when emailVerified is false, so strip it.
        var rawName = User.FindFirst("name")?.Value;
        if (!string.IsNullOrEmpty(rawName))
        {
            var name = rawName.EndsWith(" -") ? rawName[..^2] : rawName;
            var clientByName = await _repository.GetByNameAsync(name, ct);
            if (clientByName is not null) return Ok(MapToDto(clientByName));

            _logger.LogWarning("Authenticated user with name {Name} not found in database", name);
            return NotFound();
        }

        _logger.LogWarning("Authenticated request missing both 'sub' and 'name' claims in JWT");
        return Unauthorized();
    }

    private static ClientProfileDto MapToDto(Client client) => new(
        Id: client.Id,
        Name: client.Name,
        Email: client.Email.Value,
        Phone: client.Phone.Value,
        Type: client.Type.ToString(),
        Cpf: client.Cpf?.Value,
        Cnpj: client.Cnpj?.Value,
        RazaoSocial: client.RazaoSocial);
}

/// <summary>Read-only profile DTO — returned by GET /api/clients/me (PROF-02 wiring point).</summary>
public sealed record ClientProfileDto(
    Guid Id,
    string Name,
    string Email,
    string Phone,
    string Type,
    string? Cpf,
    string? Cnpj,
    string? RazaoSocial);
