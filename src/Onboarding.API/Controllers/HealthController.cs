using Microsoft.AspNetCore.Mvc;

namespace Onboarding.API.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet("/healthz2")]
    public IActionResult Get() => Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
}
