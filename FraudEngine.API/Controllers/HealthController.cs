using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FraudEngine.API.Controllers;

/// <summary>
/// Controller for exposing system health check statuses.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class HealthController : ControllerBase
{
    private readonly HealthCheckService _healthCheckService;

    /// <summary>
    /// Initializes a new instance of the <see cref="HealthController"/> class.
    /// </summary>
    public HealthController(HealthCheckService healthCheckService)
    {
        _healthCheckService = healthCheckService;
    }

    /// <summary>
    /// Retrieves the current health status of the application.
    /// </summary>
    /// <returns>The health report.</returns>
    [HttpGet]
    public async Task<IActionResult> GetHealth()
    {
        HealthReport report = await _healthCheckService.CheckHealthAsync();

        return report.Status == HealthStatus.Healthy
            ? Ok(new { Status = report.Status.ToString(), Details = report.Entries })
            : StatusCode(503, new { Status = report.Status.ToString(), Details = report.Entries });
    }
}
