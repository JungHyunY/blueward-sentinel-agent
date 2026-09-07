using Microsoft.AspNetCore.Mvc;
using BluewardSentinel.Core.Interfaces;
using BluewardSentinel.Core.Models;
using System.Text.Json;

namespace BluewardSentinel.Api.Controllers;

[ApiController]
[Route("api/agent")]
public class AgentSimulationController : ControllerBase
{
    private readonly ISentinelRepository _repository;
    private readonly IAiDiagnosticEngine _diagnosticEngine;
    private readonly IClusteringEngine _clusteringEngine;

    private static readonly List<object> _thoughtHistory = new()
    {
        new { id = "th-1", agent = "Triage Agent", type = "info", timestamp = DateTime.UtcNow.AddMinutes(-5).ToString("o"), message = "Telemetry Stream 실시간 모니터링 중: 초당 4,250건 정상 수집" },
        new { id = "th-2", agent = "Telemetry Guard", type = "success", timestamp = DateTime.UtcNow.AddMinutes(-3).ToString("o"), message = "마이크로서비스 클러스터 상태 양호 (P99 Latency: 42ms)" }
    };

    public AgentSimulationController(
        ISentinelRepository repository,
        IAiDiagnosticEngine diagnosticEngine,
        IClusteringEngine clusteringEngine)
    {
        _repository = repository;
        _diagnosticEngine = diagnosticEngine;
        _clusteringEngine = clusteringEngine;
    }

    [HttpGet("status")]
    public IActionResult GetAgentStatus()
    {
        return Ok(new
        {
            mode = "Autonomous Self-Healing (자율 치유 모드)",
            healthStatus = "Optimal",
            activeAgents = new[]
            {
                new { name = "Triage Agent", role = "실시간 텔레메트리 이상 징후 포착 및 심각도 분류", status = "Online" },
                new { name = "RCA Reasoning Agent", role = "다중 소스 교차 검증 및 근본 원인 심층 추론", status = "Online" },
                new { name = "Self-Healing Patch Agent", role = "Unified Git Patch 생성 및 격리·복구 스크립트 실행", status = "Online" },
                new { name = "Audit & Verification Agent", role = "복구 후 헬스체크 검증 및 RAG 지식 영구 보관", status = "Online" }
            },
            metrics = new
            {
                autonomousResolutionRate = "98.4%",
                averageMttr = "38.2초 (기존 대비 94% 단축)",
                preventedDowntimeMinutes = 1420,
                totalPatchesApplied = 37
            }
        });
    }

    [HttpGet("thought-stream")]
    public IActionResult GetThoughtStream()
    {
        return Ok(_thoughtHistory.TakeLast(20).Reverse().ToList());
    }

    [HttpPost("simulate-incident")]
    public async Task<IActionResult> SimulateIncident([FromBody] SimulationRequest req)
    {
        var scenario = req?.Scenario ?? "db_pool_leak";
        var projects = await _repository.GetProjectsAsync();
        var targetProj = projects.FirstOrDefault(p => p.Id == req?.ProjectId) ?? projects.FirstOrDefault();
        var projectId = targetProj?.Id ?? "proj_sap_dx";

        string title, exType, sampleMsg, stackTrace;
        List<LogEntry> logs = new();

        var now = DateTime.UtcNow;

        if (scenario == "sap_rfc_deadlock")
        {
            title = "[SAP ERP] RFC 커넥션 고갈 및 BAPI_TRANSACTION 데드락 장애";
            exType = "SapRfcSessionExhaustionException";
            sampleMsg = "RFC_ERROR_COMMUNICATION: Maximum number of 100 RFC work processes exceeded. BAPI_ACC_DOCUMENT_POST locked.";
            stackTrace = @"   at SAP.Middleware.Connector.RfcConnection.Open()
   at Blueward.ERP.SAPConnector.ExecuteBapi(String bapiName, RfcParameterList parameters) in D:\projects\iMateV3_B6\SAPConnector.cs:line 184
   at Blueward.Financial.PostingService.PostJournalEntryAsync(JournalEntry entry) in D:\projects\iMateV3_B6\PostingService.cs:line 92";

            logs.Add(new LogEntry { ProjectId = projectId, LogLevel = "Warning", Message = "SAP RFC Gateway work process queue utilization > 92%", TimestampUtc = now.AddSeconds(-20) });
            logs.Add(new LogEntry { ProjectId = projectId, LogLevel = "Error", Message = "BAPI_ACC_DOCUMENT_POST timeout after 30000ms. Deadlock detected on table BKPF.", TimestampUtc = now.AddSeconds(-10) });
            logs.Add(new LogEntry { ProjectId = projectId, LogLevel = "Critical", Message = sampleMsg, ExceptionType = exType, StackTrace = stackTrace, TimestampUtc = now });
        }
        else if (scenario == "redis_oom_cascade")
        {
            title = "[캐시 레이어] Redis 메모리 한도 초과 및 세션 캐스케이딩 실패";
            exType = "RedisOutOfMemoryException";
            sampleMsg = "OOM command not allowed when used memory > 'maxmemory' (4GB limit reached). Token session store rejected writes.";
            stackTrace = @"   at StackExchange.Redis.ConnectionMultiplexer.Execute()
   at Blueward.Auth.SessionManager.SetSessionAsync(String token, UserSession session) in D:\projects\BluewardAuth\SessionManager.cs:line 128
   at Microsoft.AspNetCore.Authentication.TokenValidationMiddleware.Invoke()";

            logs.Add(new LogEntry { ProjectId = projectId, LogLevel = "Warning", Message = "Redis memory reached 95% of 4096MB allocation", TimestampUtc = now.AddSeconds(-25) });
            logs.Add(new LogEntry { ProjectId = projectId, LogLevel = "Critical", Message = sampleMsg, ExceptionType = exType, StackTrace = stackTrace, TimestampUtc = now });
        }
        else
        {
            title = "[마이크로서비스] DB Connection Pool 고갈 및 커넥션 누수 폭주";
            exType = "NpgsqlConnectionPoolTimeoutException";
            sampleMsg = "Timeout waiting for available connection in pool after 15,000ms. Active: 100/100, Idle: 0, Waiting Queue: 482.";
            stackTrace = @"   at Npgsql.NpgsqlConnectorStore.AllocateConnector()
   at Npgsql.NpgsqlConnection.Open()
   at Blueward.Telemetry.OrderRepository.GetPendingOrdersAsync() in D:\projects\OrderService\OrderRepository.cs:line 64
   at Blueward.Telemetry.OrderProcessor.ProcessBatchAsync() in D:\projects\OrderService\OrderProcessor.cs:line 112";

            logs.Add(new LogEntry { ProjectId = projectId, LogLevel = "Warning", Message = "PostgreSQL pool active connections reached 98/100 limit", TimestampUtc = now.AddSeconds(-30) });
            logs.Add(new LogEntry { ProjectId = projectId, LogLevel = "Error", Message = "Query timeout after 15000ms in OrderRepository.cs:64", TimestampUtc = now.AddSeconds(-15) });
            logs.Add(new LogEntry { ProjectId = projectId, LogLevel = "Critical", Message = sampleMsg, ExceptionType = exType, StackTrace = stackTrace, TimestampUtc = now });
        }

        // Ingest into repository
        await _repository.IngestLogsAsync(logs);

        // Cluster into incident
        var fp = _clusteringEngine.ComputeFingerprint(exType, stackTrace, sampleMsg);
        var cluster = new IncidentCluster
        {
            Id = Guid.NewGuid().ToString("N"),
            ProjectId = projectId,
            Fingerprint = fp,
            Title = title,
            Severity = "Critical",
            Status = "Open",
            FirstSeenUtc = now,
            LastSeenUtc = now,
            OccurrenceCount = 14,
            SampleMessage = sampleMsg,
            ExceptionType = exType,
            SampleStackTrace = stackTrace
        };
        cluster = await _repository.UpsertIncidentClusterAsync(cluster);

        _thoughtHistory.Add(new
        {
            id = Guid.NewGuid().ToString("N"),
            agent = "Triage Agent",
            type = "error",
            timestamp = DateTime.UtcNow.ToString("o"),
            message = $"🚨 [이상 징후 포착] 인시던트 '{title}' 감지됨! 실시간 다중 에이전트 자율 분석 파이프라인 트리거."
        });

        return Ok(new
        {
            incident = cluster,
            simulatedLogsCount = logs.Count,
            message = "사내 경진대회 시뮬레이션 인시던트가 성공적으로 주입되었습니다."
        });
    }

    [HttpPost("run-autonomous-pipeline/{incidentId}")]
    public async Task<IActionResult> RunAutonomousPipeline(string incidentId)
    {
        var cluster = await _repository.GetIncidentByIdAsync(incidentId);
        if (cluster == null)
        {
            return NotFound(new { message = "인시던트를 찾을 수 없습니다." });
        }

        var recentLogs = await _repository.GetLogsAsync(cluster.ProjectId, null, 15);
        var sw = System.Diagnostics.Stopwatch.StartNew();

        // 1. Triage Agent
        var triageThought = new
        {
            id = Guid.NewGuid().ToString("N"),
            agent = "Triage Agent",
            type = "info",
            timestamp = DateTime.UtcNow.ToString("o"),
            message = $"[Triage] Fingerprint '{cluster.Fingerprint.Substring(0, Math.Min(8, cluster.Fingerprint.Length))}' 분석 완료. 심각도: CRITICAL, 우선순위: P0 지정."
        };
        _thoughtHistory.Add(triageThought);

        // 2. RCA Agent
        var diag = await _diagnosticEngine.DiagnoseIncidentAsync(cluster, recentLogs, "gemini-3.6-flash");
        cluster.Diagnosis = diag;
        await _repository.SaveIncidentDiagnosisAsync(cluster.Id, diag);

        var rcaThought = new
        {
            id = Guid.NewGuid().ToString("N"),
            agent = "RCA Reasoning Agent",
            type = "info",
            timestamp = DateTime.UtcNow.ToString("o"),
            message = $"[RCA 추론] 근본 원인 도출: {diag.RootCause}"
        };
        _thoughtHistory.Add(rcaThought);

        // 3. Self-Healing Agent
        var patchThought = new
        {
            id = Guid.NewGuid().ToString("N"),
            agent = "Self-Healing Patch Agent",
            type = "success",
            timestamp = DateTime.UtcNow.ToString("o"),
            message = $"[자가 치유] Unified Git Diff 패치 생성 완료. 임시 커넥션 풀 동적 확장 및 대기열 보호 스크립트 실행 완료."
        };
        _thoughtHistory.Add(patchThought);

        // 4. Audit Agent
        cluster.Status = "Resolved";
        await _repository.UpdateIncidentStatusAsync(cluster.Id, "Resolved");

        var auditThought = new
        {
            id = Guid.NewGuid().ToString("N"),
            agent = "Audit & Verification Agent",
            type = "success",
            timestamp = DateTime.UtcNow.ToString("o"),
            message = $"[사후 검증] 마이크로서비스 응답 속도 정상 회복 확인 (MTTR: {sw.ElapsedMilliseconds / 1000.0:F1}초). 상태 'Resolved' 종결."
        };
        _thoughtHistory.Add(auditThought);

        sw.Stop();

        return Ok(new
        {
            incident = cluster,
            stages = new[]
            {
                new { name = "1단계: 이상 징후 자동 탐지 (Triage)", agent = "Triage Agent", status = "Completed", latency = "45ms", result = $"P0 Critical 분류 ({cluster.OccurrenceCount}건 누적)" },
                new { name = "2단계: AI 근본 원인 추론 (RCA)", agent = "RCA Reasoning Agent", status = "Completed", latency = $"{diag.LatencyMs}ms", result = diag.RootCause },
                new { name = "3단계: 자가 치유 패치 생성 (Self-Healing)", agent = "Self-Healing Patch Agent", status = "Completed", latency = "120ms", result = "Unified Diff .patch 생성 및 자가 복구 완료" },
                new { name = "4단계: 무결성 사후 검증 (Audit)", agent = "Audit Agent", status = "Completed", latency = "85ms", result = "헬스체크 통과 및 상태 Resolved 갱신" }
            },
            remediationPatch = diag.SuggestedCodeFix,
            mttrSeconds = sw.ElapsedMilliseconds / 1000.0
        });
    }
}

public class SimulationRequest
{
    public string? Scenario { get; set; }
    public string? ProjectId { get; set; }
}
