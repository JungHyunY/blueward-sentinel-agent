using BluewardSentinel.Core.Models;

namespace BluewardSentinel.Core.Interfaces;

public interface IClusteringEngine
{
    string ComputeFingerprint(string? exceptionType, string? stackTrace, string message);
    string NormalizeMessage(string message);
    string DetermineSeverity(string logLevel, string? exceptionType, string message);
    List<string> DetectTags(string? exceptionType, string message, string? stackTrace);
}
