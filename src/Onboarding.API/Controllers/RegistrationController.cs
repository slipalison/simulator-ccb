using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Onboarding.API.Filters;
using Onboarding.Application.Clients.Commands;
using Onboarding.Application.Common;
using Onboarding.Domain.Exceptions;

namespace Onboarding.API.Controllers;

/// <summary>
/// POST /api/registration — PF and PJ client registration endpoint.
/// Implements BACK-05 (Controller, not Minimal API), REG-03/04 (validation),
/// REG-05 (duplicate detection), REG-06 (Keycloak user creation), SEC-08 (generic errors).
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class RegistrationController : ControllerBase
{
    private readonly ICommandHandler<RegisterClientCommand, Guid> _handler;
    private readonly IValidator<RegisterClientCommand> _validator;
    private readonly ILogger<RegistrationController> _logger;

    public RegistrationController(
        ICommandHandler<RegisterClientCommand, Guid> handler,
        IValidator<RegisterClientCommand> validator,
        ILogger<RegistrationController> logger)
    {
        _handler = handler;
        _validator = validator;
        _logger = logger;
    }

    // nosem: no-missing-csrf — Stateless JWT API, CSRF not applicable (no session cookies, Bearer token in Authorization header)
    [HttpPost]
    [Idempotent]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterClientRequest request,
        CancellationToken ct)
    {
        // Map HTTP DTO → Application command
        var command = new RegisterClientCommand(
            Nome: request.Nome ?? string.Empty,
            Cpf: request.Cpf,
            Cnpj: request.Cnpj,
            RazaoSocial: request.RazaoSocial,
            Email: request.Email,
            Phone: request.Phone,
            Password: request.Password);

        // FluentValidation — structural validation before hitting the domain
        var validation = await _validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            return UnprocessableEntity(new ValidationProblemDetails(
                validation.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())));
        }

        try
        {
            var clientId = await _handler.HandleAsync(command, ct);
            return Created($"/api/clients/{clientId}", new { id = clientId });
        }
        catch (ArgumentException ex)
        {
            // Domain value object check-digit failure (REG-03, REG-04).
            // SEC-08: log internally but return generic message — ex.Message must NOT appear in response.
            _logger.LogWarning(ex, "Domain validation failed for registration request");
            return UnprocessableEntity(new ProblemDetails
            {
                Title = "Validation failed",
                Status = StatusCodes.Status422UnprocessableEntity,
                Detail = "The provided document number is invalid."
            });
        }
        catch (DuplicateClientException ex)
        {
            // REG-05 + SEC-08: 409 Conflict with generic message — no hint about which field
            // (CPF, CNPJ, or email) caused the conflict. Do not include ex.Message.
            _logger.LogInformation(ex, "Duplicate client registration attempt");
            return Conflict(new ProblemDetails
            {
                Title = "Registration conflict",
                Status = StatusCodes.Status409Conflict,
                Detail = "A client with the provided information already exists."
            });
        }
        catch (RegistrationFailedException ex)
        {
            // Keycloak failure — compensation already ran in handler (REG-06)
            _logger.LogError(ex, "Registration failed due to Keycloak error after app_db persist");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new ProblemDetails
            {
                Title = "Registration temporarily unavailable",
                Status = StatusCodes.Status503ServiceUnavailable,
                Detail = "Please try again in a few moments."
            });
        }
    }
}
