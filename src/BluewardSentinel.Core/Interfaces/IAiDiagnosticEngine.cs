using BluewardSentinel.Core.Models;

namespace BluewardSentinel.Core.Interfaces;

public interface IAiDiagnosticEngine
{
    Task<AiDiagnosis> DiagnoseIncidentAsync(IncidentCluster incident, List<LogEntry> recentLogs, string? modelName = null);
}
