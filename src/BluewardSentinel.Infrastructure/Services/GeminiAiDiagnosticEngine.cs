using System.Diagnostics;
using System.Text;
using System.Text.Json;
using BluewardSentinel.Core.Interfaces;
using BluewardSentinel.Core.Models;

namespace BluewardSentinel.Infrastructure.Services;

public class GeminiAiDiagnosticEngine : IAiDiagnosticEngine
{
    public static string? RuntimeApiKey { get; set; }

    private readonly HttpClient _httpClient;

    public GeminiAiDiagnosticEngine(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<AiDiagnosis> DiagnoseIncidentAsync(IncidentCluster incident, List<LogEntry> recentLogs, string? modelName = null)
    {
        var sw = Stopwatch.StartNew();
        var model = string.IsNullOrWhiteSpace(modelName) ? "gemini-3.6-flash" : modelName;
        if (model.Contains("2.5") || model.Contains("2.0") || model.Contains("1.5"))
        {
            model = "gemini-3.6-flash";
        }

        var apiKey = RuntimeApiKey ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? "";
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            var bluewardConfig = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Blueward", "ai_config.json");
            var sharedConfig = File.Exists(bluewardConfig) ? bluewardConfig : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Yoonikon", "ai_config.json");
            if (File.Exists(sharedConfig))
            {
                try
                {
                    using var doc = JsonDocument.Parse(File.ReadAllText(sharedConfig));
                    if (doc.RootElement.TryGetProperty("apiKey", out var k))
                    {
                        apiKey = k.GetString() ?? "";
                    }
                }
                catch {}
            }
        }
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NoCodeRagSolution");
            var keyTxt = Path.Combine(appData, "gemini_key.txt");
            if (File.Exists(keyTxt))
            {
                apiKey = File.ReadAllText(keyTxt).Trim();
            }
        }
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            var configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NoCodeRagSolution", "gemini_config.json");
            if (File.Exists(configPath))
            {
                try
                {
                    using var doc = JsonDocument.Parse(File.ReadAllText(configPath));
                    if (doc.RootElement.TryGetProperty("ApiKey", out var k))
                    {
                        apiKey = k.GetString() ?? "";
                    }
                }
                catch {}
            }
        }

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return new AiDiagnosis
            {
                RootCause = $"[오프라인 휴리스틱 진단] 감지된 예외 유형: {incident.ExceptionType}. {incident.SampleMessage}",
                ImpactScope = "단일 마이크로서비스 또는 엔드포인트 요청 실패",
                RemediationSteps = new List<string>
                {
                    "1. 해당 예외 발생 지점의 Null 참조 또는 연결 타임아웃 방어 로직 확인",
                    "2. 데이터베이스 커넥션 풀 및 외래키 제약조건 무결성 검증",
                    "3. 입력 파라미터 유효성 검증 필터 추가"
                },
                SuggestedCodeFix = "// 예외 처리 방어 로직 추가\nif (data == null) throw new ArgumentNullException(nameof(data));",
                SeverityAssessment = incident.Severity,
                ConfidencePercentage = 80,
                ModelUsed = "Heuristic-Engine",
                LatencyMs = sw.ElapsedMilliseconds,
                AnalyzedAtUtc = DateTime.UtcNow
            };
        }

        var logContext = new StringBuilder();
        logContext.AppendLine($"인시던트 제목: {incident.Title}");
        logContext.AppendLine($"예외 타입: {incident.ExceptionType}");
        logContext.AppendLine($"발생 횟수: {incident.OccurrenceCount}회 (최초: {incident.FirstSeenUtc:u}, 최근: {incident.LastSeenUtc:u})");
        logContext.AppendLine($"스택 트레이스:\n{incident.SampleStackTrace}");
        logContext.AppendLine("\n[주변 연관 로그]");
        foreach (var l in recentLogs.Take(5))
        {
            logContext.AppendLine($"[{l.TimestampUtc:HH:mm:ss}] [{l.LogLevel}] {l.Message}");
        }

        var prompt = @$"당신은 엔터프라이즈 장애 관제 플랫폼 'Blueward Sentinel'의 수석 SRE/AI 아키텍트입니다.
아래 시스템 장애 인시던트 로그를 심층 분석하여 정확한 원인과 소스코드 레벨 해결책을 제시하세요.

반드시 아래 JSON 포맷으로만 답변하세요 (다른 설명 금지):
{{
  ""rootCause"": ""1~2문장으로 명확한 기술적 근본 원인 설명"",
  ""impactScope"": ""장애로 인해 영향받는 시스템/사용자 범위"",
  ""remediationSteps"": [
    ""1단계: 즉각 조치 사항"",
    ""2단계: 근본적인 코드/인프라 수정 사항"",
    ""3단계: 재발 방지 모니터링""
  ],
  ""suggestedCodeFix"": ""수정해야 할 구체적인 소스코드 블록 (C#, Python, Java, SQL 등)"",
  ""severityAssessment"": ""Critical 또는 High 또는 Medium 또는 Low"",
  ""confidencePercentage"": 95
}}

로그 내용:
{logContext}";

        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";
        var body = new
        {
            contents = new[]
            {
                new { parts = new[] { new { text = prompt } } }
            },
            generationConfig = new
            {
                temperature = 0.1,
                response_mime_type = "application/json"
            }
        };

        var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(url, content);
        sw.Stop();

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync();
            throw new Exception($"Gemini API 호출 실패 ({response.StatusCode}): {err}");
        }

        var json = await response.Content.ReadAsStringAsync();
        using var parsed = JsonDocument.Parse(json);
        var rawText = parsed.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString() ?? "{}";

        try
        {
            var diag = JsonSerializer.Deserialize<AiDiagnosis>(rawText, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                       ?? new AiDiagnosis();
            diag.ModelUsed = model;
            diag.LatencyMs = sw.ElapsedMilliseconds;
            diag.AnalyzedAtUtc = DateTime.UtcNow;
            return diag;
        }
        catch
        {
            return new AiDiagnosis
            {
                RootCause = rawText,
                ImpactScope = "서비스 레벨 영향",
                RemediationSteps = new List<string> { "로그 세부 내용 참조" },
                SeverityAssessment = incident.Severity,
                ConfidencePercentage = 90,
                ModelUsed = model,
                LatencyMs = sw.ElapsedMilliseconds,
                AnalyzedAtUtc = DateTime.UtcNow
            };
        }
    }
}
