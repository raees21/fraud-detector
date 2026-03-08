using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;

namespace FraudEngine.API.Controllers;

/// <summary>
/// Controller for exposing system health check statuses.
/// </summary>
public class HealthController : ApiControllerBase
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
    [AllowAnonymous]
    public async Task<IActionResult> GetHealth()
    {
        HealthReport report = await _healthCheckService.CheckHealthAsync();

        return report.Status == HealthStatus.Healthy
            ? Ok(new { Status = report.Status.ToString() })
            : StatusCode(503, new { Status = report.Status.ToString() });
    }
}
