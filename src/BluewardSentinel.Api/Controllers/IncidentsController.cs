using BluewardSentinel.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BluewardSentinel.Api.Controllers;

[ApiController]
[Route("api/incidents")]
[Route("api/v1/incidents")]
public class IncidentsController : ControllerBase
{
    private readonly ISentinelRepository _repository;
    private readonly IAiDiagnosticEngine _diagnosticEngine;

    public IncidentsController(ISentinelRepository repository, IAiDiagnosticEngine diagnosticEngine)
    {
        _repository = repository;
        _diagnosticEngine = diagnosticEngine;
    }

    [HttpGet]
    public async Task<IActionResult> GetIncidents([FromQuery] string? projectId, [FromQuery] string? severity, [FromQuery] string? status, [FromQuery] string? search, [FromQuery] string? tag)
    {
        var list = await _repository.GetIncidentsAsync(projectId, severity, status, search, tag);
        return Ok(list);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetIncident(string id)
    {
        var item = await _repository.GetIncidentByIdAsync(id);
        if (item == null) return NotFound("인시던트를 찾을 수 없습니다.");
        return Ok(item);
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(string id, [FromBody] UpdateStatusRequest req)
    {
        await _repository.UpdateIncidentStatusAsync(id, req.Status);
        return Ok(new { success = true });
    }

    [HttpPost("{id}/diagnose")]
    public async Task<IActionResult> DiagnoseIncident(string id, [FromQuery] string? modelName)
    {
        var incident = await _repository.GetIncidentByIdAsync(id);
        if (incident == null) return NotFound("인시던트를 찾을 수 없습니다.");

        var recentLogs = await _repository.GetLogsAsync(incident.ProjectId, "ERROR", 10);
        var diagnosis = await _diagnosticEngine.DiagnoseIncidentAsync(incident, recentLogs, modelName);
        await _repository.SaveIncidentDiagnosisAsync(id, diagnosis);

        return Ok(diagnosis);
    }
}

public class UpdateStatusRequest
{
    public string Status { get; set; } = "Resolved";
}
