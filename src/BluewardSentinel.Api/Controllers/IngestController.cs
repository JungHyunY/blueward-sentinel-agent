using BluewardSentinel.Core.Interfaces;
using BluewardSentinel.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace BluewardSentinel.Api.Controllers;

[ApiController]
[Route("api/v1/ingest")]
public class IngestController : ControllerBase
{
    private readonly ISentinelRepository _repository;
    private readonly IClusteringEngine _clusteringEngine;
    private readonly IWebhookService _webhookService;

    public IngestController(ISentinelRepository repository, IClusteringEngine clusteringEngine, IWebhookService webhookService)
    {
        _repository = repository;
        _clusteringEngine = clusteringEngine;
        _webhookService = webhookService;
    }

    [HttpPost]
    public async Task<IActionResult> IngestLogs([FromBody] IngestRequest request)
    {
        if (request.Logs == null || request.Logs.Count == 0)
        {
            return BadRequest(new { error = "수집할 로그 데이터가 비어 있습니다." });
        }

        SentinelProject? project = null;
        if (!string.IsNullOrWhiteSpace(request.ApiKey))
        {
            project = await _repository.GetProjectByApiKeyAsync(request.ApiKey);
        }
        if (project == null && !string.IsNullOrWhiteSpace(request.ProjectId))
        {
            project = await _repository.GetProjectByIdAsync(request.ProjectId);
        }
        if (project == null)
        {
            var all = await _repository.GetProjectsAsync();
            project = all.FirstOrDefault();
        }

        var projectId = project?.Id ?? "proj_default";
        var logEntries = new List<LogEntry>();
        var affectedIncidentIds = new HashSet<string>();

        foreach (var item in request.Logs)
        {
            var level = item.LogLevel ?? "INFO";
            var fp = _clusteringEngine.ComputeFingerprint(item.ExceptionType, item.StackTrace, item.Message);
            var log = new LogEntry
            {
                ProjectId = projectId,
                LogLevel = level,
                Message = item.Message,
                ExceptionType = item.ExceptionType,
                StackTrace = item.StackTrace,
                Source = item.Source,
                HostName = item.HostName,
                Environment = item.Environment ?? project?.Environment ?? "Production",
                Fingerprint = fp,
                TimestampUtc = item.Timestamp ?? DateTime.UtcNow
            };

            // If error/fatal, group into incident
            if (level.Equals("ERROR", StringComparison.OrdinalIgnoreCase) ||
                level.Equals("FATAL", StringComparison.OrdinalIgnoreCase) ||
                !string.IsNullOrWhiteSpace(item.ExceptionType))
            {
                var severity = _clusteringEngine.DetermineSeverity(level, item.ExceptionType, item.Message);
                var tags = _clusteringEngine.DetectTags(item.ExceptionType, item.StackTrace, item.Message);
                var incident = new IncidentCluster
                {
                    ProjectId = projectId,
                    Fingerprint = fp,
                    Title = $"{item.ExceptionType ?? "Error"}: {_clusteringEngine.NormalizeMessage(item.Message)}",
                    ExceptionType = item.ExceptionType ?? "SystemException",
                    Severity = severity,
                    Status = "Open",
                    OccurrenceCount = 1,
                    FirstSeenUtc = log.TimestampUtc,
                    LastSeenUtc = log.TimestampUtc,
                    SampleStackTrace = item.StackTrace ?? string.Empty,
                    SampleMessage = item.Message,
                    Tags = tags
                };

                var savedIncident = await _repository.UpsertIncidentClusterAsync(incident);
                log.IncidentId = savedIncident.Id;
                affectedIncidentIds.Add(savedIncident.Id);

                if (savedIncident.Severity == "Critical" || savedIncident.IsAnomalySpike)
                {
                    var evtType = savedIncident.IsAnomalySpike ? "Anomaly Spike" : "Critical Incident";
                    _ = _webhookService.DispatchIncidentAlertAsync(savedIncident, evtType);
                }
            }

            logEntries.Add(log);
        }

        await _repository.IngestLogsAsync(logEntries);

        return Ok(new IngestResponse
        {
            Success = true,
            IngestedCount = logEntries.Count,
            IncidentsTriggeredCount = affectedIncidentIds.Count,
            AffectedIncidentIds = affectedIncidentIds.ToList()
        });
    }

    [HttpPost("file")]
    public async Task<IActionResult> IngestFile([FromForm] IFormFile file, [FromForm] string? projectId)
    {
        if (file == null || file.Length == 0) return BadRequest("업로드할 파일이 없습니다.");

        using var reader = new StreamReader(file.OpenReadStream());
        var rawText = await reader.ReadToEndAsync();
        var lines = rawText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

        var request = new IngestRequest
        {
            ProjectId = projectId,
            Logs = new List<IngestLogItem>()
        };

        foreach (var line in lines)
        {
            var level = "INFO";
            string? exType = null;
            if (line.Contains("ERROR", StringComparison.OrdinalIgnoreCase) || line.Contains("Exception", StringComparison.OrdinalIgnoreCase))
            {
                level = "ERROR";
                var match = System.Text.RegularExpressions.Regex.Match(line, @"(\w+Exception)");
                if (match.Success) exType = match.Groups[1].Value;
            }
            else if (line.Contains("WARN", StringComparison.OrdinalIgnoreCase))
            {
                level = "WARN";
            }

            request.Logs.Add(new IngestLogItem
            {
                LogLevel = level,
                Message = line,
                ExceptionType = exType,
                Source = file.FileName,
                Timestamp = DateTime.UtcNow
            });
        }

        return await IngestLogs(request);
    }
}
