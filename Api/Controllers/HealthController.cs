using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Api.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthController(HealthCheckService healthCheckService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult> Get()
    {
        var report = await healthCheckService.CheckHealthAsync();

        return report.Status == HealthStatus.Healthy
            ? Ok(new { status = "Healthy", timestamp = DateTime.UtcNow })
            : StatusCode(503, new { status = "Unhealthy", timestamp = DateTime.UtcNow });
    }
}