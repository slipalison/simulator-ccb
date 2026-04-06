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
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMe(CancellationToken ct)
    {
        // D-07: lookup by email claim from JWT (MapInboundClaims=false preserves "email" claim name)
        var email = User.FindFirst("email")?.Value;
        if (string.IsNullOrEmpty(email))
        {
            _logger.LogWarning("Authenticated request missing email claim in JWT");
            return Unauthorized();
        }

        var client = await _repository.GetByEmailAsync(email, ct);
        if (client is null)
        {
            // D-09: 404 with generic ProblemDetails — should not happen in normal flow
            // but must not reveal "user does not exist" (SEC-08)
            _logger.LogWarning("No client found for authenticated email {Email}", email);
            return NotFound(new ProblemDetails
            {
                Title = "Profile not found",
                Status = StatusCodes.Status404NotFound,
                Detail = "No client profile found for this account."
            });
        }

        return Ok(MapToDto(client));
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
