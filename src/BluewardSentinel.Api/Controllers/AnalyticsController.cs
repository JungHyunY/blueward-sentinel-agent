using BluewardSentinel.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BluewardSentinel.Api.Controllers;

[ApiController]
[Route("api/analytics")]
[Route("api/v1/analytics")]
public class AnalyticsController : ControllerBase
{
    private readonly ISentinelRepository _repository;

    public AnalyticsController(ISentinelRepository repository)
    {
        _repository = repository;
    }

    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview([FromQuery] string? projectId)
    {
        var metrics = await _repository.GetAnalyticsMetricsAsync(projectId);
        return Ok(metrics);
    }
}
