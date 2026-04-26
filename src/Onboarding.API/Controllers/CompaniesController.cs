using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Onboarding.Domain.Aggregates.CompanyAggregate;
using Onboarding.Domain.Repositories;

namespace Onboarding.API.Controllers;

/// <summary>
/// Company profile endpoints.
/// GET /api/companies/me — AUTH-03: protected route returns profile of authenticated company.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class CompaniesController : ControllerBase
{
    private readonly ICompanyRepository _repository;
    private readonly ILogger<CompaniesController> _logger;

    public CompaniesController(
        ICompanyRepository repository,
        ILogger<CompaniesController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>GET /api/companies/me — returns the authenticated company's profile.</summary>
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
            var company = await _repository.GetByKeycloakSubAsync(keycloakSub, ct);
            if (company is not null) return Ok(MapToDto(company));

            _logger.LogWarning("Authenticated user with sub {Sub} not found in database", keycloakSub);
            return NotFound();
        }

        _logger.LogWarning("Authenticated request missing 'sub' claim in JWT");
        return Unauthorized();
    }

    private static CompanyProfileDto MapToDto(Company company) => new(
        Id: company.Id,
        RazaoSocial: company.RazaoSocial,
        Email: company.Email.Value,
        Phone: company.Phone.Value,
        Cnpj: company.Cnpj?.Value);
}

/// <summary>Read-only profile DTO — returned by GET /api/companies/me.</summary>
public sealed record CompanyProfileDto(
    Guid Id,
    string RazaoSocial,
    string Email,
    string Phone,
    string? Cnpj);