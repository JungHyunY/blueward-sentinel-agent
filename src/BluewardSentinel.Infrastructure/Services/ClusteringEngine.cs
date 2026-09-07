using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using BluewardSentinel.Core.Interfaces;

namespace BluewardSentinel.Infrastructure.Services;

public class ClusteringEngine : IClusteringEngine
{
    private static readonly Regex GuidRegex = new(@"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}", RegexOptions.Compiled);
    private static readonly Regex NumberRegex = new(@"\d+", RegexOptions.Compiled);
    private static readonly Regex DateRegex = new(@"\d{4}-\d{2}-\d{2}[T\s]\d{2}:\d{2}:\d{2}(\.\d+)?Z?", RegexOptions.Compiled);
    private static readonly Regex LineNumberRegex = new(@":line \d+|:\d+:\d+", RegexOptions.Compiled);

    public string NormalizeMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return "UnknownError";
        var normalized = DateRegex.Replace(message, "<DATE>");
        normalized = GuidRegex.Replace(normalized, "<GUID>");
        normalized = LineNumberRegex.Replace(normalized, ":line <LINE>");
        normalized = NumberRegex.Replace(normalized, "<NUM>");
        return normalized.Trim();
    }

    public string ComputeFingerprint(string? exceptionType, string? stackTrace, string message)
    {
        var sb = new StringBuilder();
        sb.Append(exceptionType ?? "GeneralException").Append('|');

        if (!string.IsNullOrWhiteSpace(stackTrace))
        {
            var lines = stackTrace.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines.Take(4))
            {
                var cleaned = LineNumberRegex.Replace(line, "");
                cleaned = GuidRegex.Replace(cleaned, "");
                sb.Append(cleaned.Trim()).Append('|');
            }
        }
        else
        {
            sb.Append(NormalizeMessage(message));
        }

        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
        return "fp_" + Convert.ToHexString(bytes)[..16].ToLowerInvariant();
    }

    public string DetermineSeverity(string logLevel, string? exceptionType, string message)
    {
        var upperLevel = logLevel.ToUpperInvariant();
        var upperMsg = message.ToUpperInvariant();
        var upperEx = (exceptionType ?? string.Empty).ToUpperInvariant();

        if (upperLevel == "FATAL" || upperEx.Contains("OUTOFMEMORY") || upperEx.Contains("STACKOVERFLOW") || upperMsg.Contains("DEADLOCK") || upperMsg.Contains("CORRUPTION"))
        {
            return "Critical";
        }
        if (upperLevel == "ERROR" || upperEx.Contains("EXCEPTION") || upperEx.Contains("NULLREFERENCE") || upperEx.Contains("TIMEOUT") || upperEx.Contains("SQLITE") || upperEx.Contains("SQL"))
        {
            return "High";
        }
        if (upperLevel == "WARN" || upperLevel == "WARNING")
        {
            return "Medium";
        }
        return "Low";
    }

    public List<string> DetectTags(string? exceptionType, string message, string? stackTrace)
    {
        var tags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var combined = $"{(exceptionType ?? string.Empty)} {message} {(stackTrace ?? string.Empty)}".ToUpperInvariant();

        if (combined.Contains("SQL") || combined.Contains("SQLITE") || combined.Contains("DEADLOCK") || combined.Contains("DATABASE") || combined.Contains("POSTGRES") || combined.Contains("QUERY"))
        {
            tags.Add("DATABASE");
        }
        if (combined.Contains("TIMEOUT") || combined.Contains("SOCKET") || combined.Contains("CONNECTION") || combined.Contains("HTTP") || combined.Contains("NETWORK"))
        {
            tags.Add("NETWORK");
        }
        if (combined.Contains("OUTOFMEMORY") || combined.Contains("OOM") || combined.Contains("HEAP") || combined.Contains("GARBAGE") || combined.Contains("BUFFER"))
        {
            tags.Add("MEMORY_LEAK");
        }
        if (combined.Contains("NULLREFERENCE") || combined.Contains("NULLPOINTER") || combined.Contains("ARGUMENTNULL"))
        {
            tags.Add("NULL_POINTER");
        }
        if (combined.Contains("UNAUTHORIZED") || combined.Contains("FORBIDDEN") || combined.Contains("SECURITY") || combined.Contains("AUTH") || combined.Contains("TOKEN"))
        {
            tags.Add("SECURITY");
        }
        if (combined.Contains("TASKCANCELED") || combined.Contains("OPERATIONCANCELED") || combined.Contains("THREAD") || combined.Contains("SEMAPHORE"))
        {
            tags.Add("CONCURRENCY");
        }

        if (tags.Count == 0)
        {
            tags.Add("APP_LOGIC");
        }

        return tags.ToList();
    }
}
