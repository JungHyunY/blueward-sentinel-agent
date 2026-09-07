using BluewardSentinel.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BluewardSentinel.Api.Controllers;

[ApiController]
[Route("api/logs")]
[Route("api/v1/logs")]
public class LogsController : ControllerBase
{
    private readonly ISentinelRepository _repository;

    public LogsController(ISentinelRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<IActionResult> GetLogs([FromQuery] string? projectId, [FromQuery] string? logLevel, [FromQuery] int limit = 100)
    {
        var list = await _repository.GetLogsAsync(projectId, logLevel, limit);
        return Ok(list);
    }

    [HttpGet("stream")]
    public async Task StreamLogs([FromQuery] string? projectId, [FromQuery] string? logLevel, CancellationToken cancellationToken)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";

        var lastTimestamp = DateTime.UtcNow.AddMinutes(-5);

        while (!cancellationToken.IsCancellationRequested)
        {
            var logs = await _repository.GetLogsAsync(projectId, logLevel, limit: 30);
            var newLogs = logs.Where(l => l.TimestampUtc > lastTimestamp).OrderBy(l => l.TimestampUtc).ToList();

            if (newLogs.Count > 0)
            {
                lastTimestamp = newLogs.Max(l => l.TimestampUtc);
                var json = System.Text.Json.JsonSerializer.Serialize(newLogs, new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                });
                await Response.WriteAsync($"data: {json}\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }
            else
            {
                await Response.WriteAsync(": keepalive\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }

            await Task.Delay(2000, cancellationToken);
        }
    }
}
