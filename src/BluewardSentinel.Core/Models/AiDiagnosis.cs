namespace BluewardSentinel.Core.Models;

public class AiDiagnosis
{
    public string RootCause { get; set; } = string.Empty;
    public string ImpactScope { get; set; } = string.Empty;
    public List<string> RemediationSteps { get; set; } = new();
    public string? SuggestedCodeFix { get; set; }
    public string SeverityAssessment { get; set; } = "High";
    public int ConfidencePercentage { get; set; } = 95;
    public string ModelUsed { get; set; } = "gemini-2.5-flash";
    public long LatencyMs { get; set; }
    public DateTime AnalyzedAtUtc { get; set; } = DateTime.UtcNow;
}
