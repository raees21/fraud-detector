using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FraudEngine.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class HealthController : ControllerBase
{
    private readonly HealthCheckService _healthCheckService;

    public HealthController(HealthCheckService healthCheckService)
    {
        _healthCheckService = healthCheckService;
    }

    [HttpGet]
    public async Task<IActionResult> GetHealth()
    {
        var report = await _healthCheckService.CheckHealthAsync();
        
        return report.Status == HealthStatus.Healthy 
            ? Ok(new { Status = report.Status.ToString(), Details = report.Entries }) 
            : StatusCode(503, new { Status = report.Status.ToString(), Details = report.Entries });
    }
}
