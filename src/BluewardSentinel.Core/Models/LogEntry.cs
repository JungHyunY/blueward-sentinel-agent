namespace BluewardSentinel.Core.Models;

public class LogEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ProjectId { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public string? IncidentId { get; set; }
    public string LogLevel { get; set; } = "INFO"; // INFO, WARN, ERROR, FATAL
    public string Message { get; set; } = string.Empty;
    public string? ExceptionType { get; set; }
    public string? StackTrace { get; set; }
    public string? Source { get; set; }
    public string? HostName { get; set; }
    public string? Environment { get; set; }
    public string? Fingerprint { get; set; }
    public string? MetadataJson { get; set; }
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
}
