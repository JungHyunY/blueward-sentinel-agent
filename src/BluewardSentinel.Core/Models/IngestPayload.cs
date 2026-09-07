namespace BluewardSentinel.Core.Models;

public class IngestLogItem
{
    public string? LogLevel { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ExceptionType { get; set; }
    public string? StackTrace { get; set; }
    public string? Source { get; set; }
    public string? HostName { get; set; }
    public string? Environment { get; set; }
    public DateTime? Timestamp { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

public class IngestRequest
{
    public string? ProjectId { get; set; }
    public string? ApiKey { get; set; }
    public List<IngestLogItem> Logs { get; set; } = new();
}

public class IngestResponse
{
    public bool Success { get; set; } = true;
    public int IngestedCount { get; set; }
    public int IncidentsTriggeredCount { get; set; }
    public List<string> AffectedIncidentIds { get; set; } = new();
}
