namespace BluewardSentinel.Core.Models;

public class AnalyticsMetrics
{
    public int TotalLogsIngested { get; set; }
    public int TotalIncidentsCount { get; set; }
    public int CriticalIncidentsCount { get; set; }
    public int HighIncidentsCount { get; set; }
    public int ResolvedIncidentsCount { get; set; }
    public double ErrorRatePercentage { get; set; }
    public List<TopExceptionMetric> TopExceptions { get; set; } = new();
    public List<HourlyVolumeMetric> HourlyVolumes { get; set; } = new();
    public List<ProjectHealthMetric> ProjectHealths { get; set; } = new();
}

public class TopExceptionMetric
{
    public string ExceptionType { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class HourlyVolumeMetric
{
    public string HourLabel { get; set; } = string.Empty;
    public int ErrorCount { get; set; }
    public int TotalCount { get; set; }
}

public class ProjectHealthMetric
{
    public string ProjectId { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public int TotalLogs { get; set; }
    public int ErrorCount { get; set; }
    public int IncidentCount { get; set; }
    public string HealthStatus { get; set; } = "Healthy"; // Healthy, Warning, Critical
}
