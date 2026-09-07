namespace BluewardSentinel.Core.Models;

public class IncidentCluster
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ProjectId { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public string Environment { get; set; } = "Production";
    public string Fingerprint { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string ExceptionType { get; set; } = string.Empty;
    public string Severity { get; set; } = "High"; // Critical, High, Medium, Low
    public string Status { get; set; } = "Open"; // Open, Investigating, Resolved, Ignored
    public int OccurrenceCount { get; set; } = 1;
    public DateTime FirstSeenUtc { get; set; } = DateTime.UtcNow;
    public DateTime LastSeenUtc { get; set; } = DateTime.UtcNow;
    public string SampleStackTrace { get; set; } = string.Empty;
    public string? SampleMessage { get; set; }
    public List<string> Tags { get; set; } = new();
    public bool IsAnomalySpike { get; set; } = false;
    public double RecentVelocityPerMin { get; set; } = 0.0;
    public AiDiagnosis? Diagnosis { get; set; }
}
