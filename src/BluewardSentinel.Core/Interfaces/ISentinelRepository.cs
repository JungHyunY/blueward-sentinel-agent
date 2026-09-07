using BluewardSentinel.Core.Models;

namespace BluewardSentinel.Core.Interfaces;

public interface ISentinelRepository
{
    Task InitializeAsync();
    Task<List<SentinelProject>> GetProjectsAsync();
    Task<SentinelProject?> GetProjectByIdAsync(string projectId);
    Task<SentinelProject?> GetProjectByApiKeyAsync(string apiKey);
    Task<SentinelProject> CreateProjectAsync(string name, string description, string environment);
    Task DeleteProjectAsync(string projectId);

    Task IngestLogsAsync(List<LogEntry> logs);
    Task<List<LogEntry>> GetLogsAsync(string? projectId, string? logLevel, int limit = 100);

    Task<IncidentCluster> UpsertIncidentClusterAsync(IncidentCluster incident);
    Task<List<IncidentCluster>> GetIncidentsAsync(string? projectId, string? severity, string? status, string? search, string? tag = null);
    Task<IncidentCluster?> GetIncidentByIdAsync(string incidentId);
    Task UpdateIncidentStatusAsync(string incidentId, string status);
    Task SaveIncidentDiagnosisAsync(string incidentId, AiDiagnosis diagnosis);

    Task<AnalyticsMetrics> GetAnalyticsMetricsAsync(string? projectId);
}
