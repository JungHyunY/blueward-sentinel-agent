const API_BASE = '/api';

// In-Memory fallback store for Vercel cloud deployment (Zero-backend mode)
let mockIncidents = [
  {
    id: 'inc-sap-001',
    projectId: 'proj_sap_dx',
    fingerprint: 'a8b7c6d5e4f3',
    title: '[SAP ERP] RFC 커넥션 고갈 및 BAPI_TRANSACTION 데드락 장애',
    severity: 'Critical',
    status: 'Open',
    occurrenceCount: 18,
    firstSeenUtc: new Date(Date.now() - 3600000).toISOString(),
    lastSeenUtc: new Date().toISOString(),
    sampleMessage: 'RFC_ERROR_COMMUNICATION: Maximum number of 100 RFC work processes exceeded. BAPI_ACC_DOCUMENT_POST locked.',
    exceptionType: 'SapRfcSessionExhaustionException',
    sampleStackTrace: `   at SAP.Middleware.Connector.RfcConnection.Open()
   at Blueward.ERP.SAPConnector.ExecuteBapi(String bapiName, RfcParameterList parameters) in D:\projects\iMateV3_B6\SAPConnector.cs:line 184
   at Blueward.Financial.PostingService.PostJournalEntryAsync(JournalEntry entry) in D:\projects\iMateV3_B6\PostingService.cs:line 92`,
    diagnosis: {
      rootCause: '동시 대량 재무 전표 BAPI 트랜잭션 요청 시 RFC Work Process 커넥션 반환 누수 및 BKPF 테이블 레코드 레벨 데드락 발생',
      impactScope: '전사 ERP 회계 마감 및 온라인 결제 정산 트랜잭션 전면 지연',
      remediationSteps: [
        '1단계: SAP RFC Gateway 대기 큐 플러시 및 고갈된 RFC 세션 강제 회수',
        '2단계: BAPI 커넥션 풀 Dispose 누수 방어 로직 패치 적용',
        '3단계: BAPI_TRANSACTION_COMMIT 호출 후 세션 즉각 반환 검증'
      ],
      suggestedCodeFix: `// [Blueward AIOps Auto-Patch] Fix RFC Session Leak & Deadlock
using (var rfcConnection = _rfcPool.AcquireConnection(timeout: TimeSpan.FromSeconds(5)))
{
    try 
    {
        var result = await rfcConnection.ExecuteBapiAsync("BAPI_ACC_DOCUMENT_POST", parameters);
        await rfcConnection.ExecuteBapiAsync("BAPI_TRANSACTION_COMMIT", null);
        return result;
    }
    finally 
    {
        // 보장된 연결 해제 및 세션 풀 반환
        rfcConnection.Release();
    }
}`,
      confidencePercentage: 98,
      latencyMs: 412,
      modelUsed: 'gemini-3.6-flash'
    }
  },
  {
    id: 'inc-db-002',
    projectId: 'proj_nexus_rag',
    fingerprint: '3f2e1d0c9b8a',
    title: '[마이크로서비스] PostgreSQL Connection Pool 누수 및 대기열 폭주',
    severity: 'High',
    status: 'Resolved',
    occurrenceCount: 24,
    firstSeenUtc: new Date(Date.now() - 7200000).toISOString(),
    lastSeenUtc: new Date(Date.now() - 1800000).toISOString(),
    sampleMessage: 'Timeout waiting for available connection in pool after 15,000ms. Active: 100/100, Idle: 0, Waiting Queue: 482.',
    exceptionType: 'NpgsqlConnectionPoolTimeoutException',
    sampleStackTrace: `   at Npgsql.NpgsqlConnectorStore.AllocateConnector()
   at Npgsql.NpgsqlConnection.Open()
   at Blueward.Telemetry.OrderRepository.GetPendingOrdersAsync() in D:\projects\OrderService\OrderRepository.cs:line 64`,
    diagnosis: {
      rootCause: '주문 배치 프로세서 비동기 반복문 내에서 NpgsqlConnection 인스턴스를 using 블록 없이 열어 커넥션 풀 100개가 모두 미반환 상태로 잠김',
      impactScope: '신규 주문 생성 API 타임아웃 및 결제 대기열 누적',
      remediationSteps: [
        '1단계: DB 커넥션 풀 동적 상한을 일시적으로 150으로 확장',
        '2단계: OrderRepository.cs의 미해제 커넥션에 async using 스코프 적용',
        '3단계: 커넥션 누수 감지 인터셉터 활성화'
      ],
      suggestedCodeFix: `// [Blueward AIOps Auto-Patch] Enforce IAsyncDisposable scope
public async Task<List<Order>> GetPendingOrdersAsync()
{
    await using var conn = new NpgsqlConnection(_connectionString);
    await conn.OpenAsync();
    return await conn.QueryAsync<Order>("SELECT * FROM Orders WHERE Status = 'Pending'");
}`,
      confidencePercentage: 96,
      latencyMs: 380,
      modelUsed: 'gemini-3.6-flash'
    }
  }
];

let mockThoughtStream = [
  { id: 'th-1', agent: 'Telemetry Guard', type: 'success', timestamp: new Date(Date.now() - 180000).toISOString(), message: '마이크로서비스 클러스터 상태 감시 중 (P99 Latency: 42ms)' },
  { id: 'th-2', agent: 'Triage Agent', type: 'info', timestamp: new Date(Date.now() - 120000).toISOString(), message: 'Telemetry Stream 실시간 모니터링: 초당 4,250건 정상 수집' },
  { id: 'th-3', agent: 'RCA Reasoning Agent', type: 'info', timestamp: new Date(Date.now() - 60000).toISOString(), message: 'SAP RFC 커넥션 고갈 인시던트 원인 추론 완료 (Gemini 3.6 Flash)' }
];

export async function fetchAgentStatus() {
  try {
    const res = await fetch(`${API_BASE}/agent/status`);
    if (res.ok) return await res.json();
  } catch (err) {}

  return {
    mode: 'Autonomous Self-Healing (자율 치유 모드)',
    healthStatus: 'Optimal',
    activeAgents: [
      { name: 'Triage Agent', role: '실시간 텔레메트리 이상 징후 포착 및 심각도 분류', status: 'Online' },
      { name: 'RCA Reasoning Agent', role: '다중 소스 교차 검증 및 근본 원인 심층 추론', status: 'Online' },
      { name: 'Self-Healing Patch Agent', role: 'Unified Git Patch 생성 및 격리·복구 스크립트 실행', status: 'Online' },
      { name: 'Audit & Verification Agent', role: '복구 후 헬스체크 검증 및 RAG 지식 영구 보관', status: 'Online' }
    ],
    metrics: {
      autonomousResolutionRate: '98.4%',
      averageMttr: '38.2초 (기존 대비 94% 단축)',
      preventedDowntimeMinutes: 1420,
      totalPatchesApplied: 37
    }
  };
}

export async function fetchThoughtStream() {
  try {
    const res = await fetch(`${API_BASE}/agent/thought-stream`);
    if (res.ok) return await res.json();
  } catch (err) {}

  return mockThoughtStream;
}

export async function simulateIncident(scenario, projectId = 'proj_sap_dx') {
  try {
    const res = await fetch(`${API_BASE}/agent/simulate-incident`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ scenario, projectId })
    });
    if (res.ok) return await res.json();
  } catch (err) {}

  // Fallback for Vercel cloud preview
  const now = new Date();
  let title = '[SAP ERP] RFC 커넥션 고갈 및 BAPI_TRANSACTION 데드락 장애';
  let exType = 'SapRfcSessionExhaustionException';
  let sampleMsg = 'RFC_ERROR_COMMUNICATION: Maximum number of 100 RFC work processes exceeded. BAPI_ACC_DOCUMENT_POST locked.';

  if (scenario === 'redis_oom_cascade') {
    title = '[캐시 레이어] Redis 메모리 한도 초과 및 세션 캐스케이딩 실패';
    exType = 'RedisOutOfMemoryException';
    sampleMsg = "OOM command not allowed when used memory > 'maxmemory' (4GB limit reached). Token session store rejected writes.";
  } else if (scenario === 'db_pool_leak') {
    title = '[마이크로서비스] DB Connection Pool 고갈 및 커넥션 누수 폭주';
    exType = 'NpgsqlConnectionPoolTimeoutException';
    sampleMsg = 'Timeout waiting for available connection in pool after 15,000ms. Active: 100/100, Idle: 0, Waiting Queue: 482.';
  }

  const newInc = {
    id: 'inc-' + Math.random().toString(36).substring(2, 9),
    projectId,
    fingerprint: Math.random().toString(16).substring(2, 14),
    title,
    severity: 'Critical',
    status: 'Open',
    occurrenceCount: 14,
    firstSeenUtc: now.toISOString(),
    lastSeenUtc: now.toISOString(),
    sampleMessage: sampleMsg,
    exceptionType: exType,
    sampleStackTrace: 'at SAP.Middleware.Connector.RfcConnection.Open()\n   at Blueward.Financial.PostingService.PostJournalEntryAsync()'
  };

  mockIncidents.unshift(newInc);
  mockThoughtStream.unshift({
    id: 'th-' + Date.now(),
    agent: 'Triage Agent',
    type: 'error',
    timestamp: now.toISOString(),
    message: `🚨 [이상 징후 포착] 인시던트 '${title}' 감지됨! 실시간 다중 에이전트 자율 분석 파이프라인 트리거.`
  });

  return {
    incident: newInc,
    simulatedLogsCount: 3,
    message: '사내 경진대회 시뮬레이션 인시던트가 성공적으로 주입되었습니다.'
  };
}

export async function runAutonomousPipeline(incidentId) {
  try {
    const res = await fetch(`${API_BASE}/agent/run-autonomous-pipeline/${incidentId}`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' }
    });
    if (res.ok) return await res.json();
  } catch (err) {}

  // Fallback simulation for Vercel
  const target = mockIncidents.find(i => i.id === incidentId) || mockIncidents[0];
  target.status = 'Resolved';
  target.diagnosis = target.diagnosis || {
    rootCause: '대량 트랜잭션 급증으로 인한 리소스 한도 초과 및 세션 누수 현상',
    impactScope: '연관 마이크로서비스 처리율 일시적 저하',
    remediationSteps: [
      '1단계: 부하 분산 및 격리 조치 자동 실행',
      '2단계: 커넥션 풀 동적 확장 및 대기열 보호 패치 적용',
      '3단계: 서비스 헬스체크 정상화 확인'
    ],
    suggestedCodeFix: `// [Blueward AIOps Auto-Patch] Resource Guard Implementation
public async Task SafeExecuteAsync() {
    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
    await _semaphore.WaitAsync(cts.Token);
    try { await ProcessInternalAsync(); }
    finally { _semaphore.Release(); }
}`,
    confidencePercentage: 99,
    latencyMs: 340,
    modelUsed: 'gemini-3.6-flash'
  };

  mockThoughtStream.unshift({
    id: 'th-' + Date.now(),
    agent: 'Audit & Verification Agent',
    type: 'success',
    timestamp: new Date().toISOString(),
    message: `[사후 검증] 서비스 응답 속도 정상 회복 확인 (MTTR: 11.5초). 인시던트 '${target.title}' 상태 Resolved 종결.`
  });

  return {
    incident: target,
    stages: [
      { name: '1단계: 이상 징후 자동 탐지 (Triage)', agent: 'Triage Agent', status: 'Completed', latency: '45ms', result: 'P0 Critical 분류 (14건 누적)' },
      { name: '2단계: AI 근본 원인 추론 (RCA)', agent: 'RCA Reasoning Agent', status: 'Completed', latency: '340ms', result: target.diagnosis.rootCause },
      { name: '3단계: 자가 치유 패치 생성 (Self-Healing)', agent: 'Self-Healing Patch Agent', status: 'Completed', latency: '120ms', result: 'Unified Diff .patch 생성 및 자가 복구 완료' },
      { name: '4단계: 무결성 사후 검증 (Audit)', agent: 'Audit Agent', status: 'Completed', latency: '85ms', result: '헬스체크 통과 및 상태 Resolved 갱신' }
    ],
    remediationPatch: target.diagnosis.suggestedCodeFix,
    mttrSeconds: 11.5
  };
}

export async function fetchIncidents(projectId, severity, status, search) {
  try {
    const params = new URLSearchParams();
    if (projectId) params.append('projectId', projectId);
    if (severity) params.append('severity', severity);
    if (status) params.append('status', status);
    if (search) params.append('search', search);

    const res = await fetch(`${API_BASE}/incidents?${params.toString()}`);
    if (res.ok) return await res.json();
  } catch (err) {}

  return mockIncidents;
}

export async function fetchLogs(projectId, logLevel, limit = 100) {
  try {
    const params = new URLSearchParams();
    if (projectId) params.append('projectId', projectId);
    if (logLevel) params.append('logLevel', logLevel);
    params.append('limit', limit.toString());

    const res = await fetch(`${API_BASE}/logs?${params.toString()}`);
    if (res.ok) return await res.json();
  } catch (err) {}

  return [];
}

export async function fetchAnalyticsOverview(projectId) {
  try {
    const params = projectId ? `?projectId=${encodeURIComponent(projectId)}` : '';
    const res = await fetch(`${API_BASE}/analytics/overview${params}`);
    if (res.ok) return await res.json();
  } catch (err) {}

  return {
    totalLogs: 1842900,
    errorCount: 14,
    criticalCount: 3,
    activeIncidentsCount: 2
  };
}

export async function updateIncidentStatus(id, status) {
  try {
    const res = await fetch(`${API_BASE}/incidents/${id}/status`, {
      method: 'PATCH',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ status })
    });
    if (res.ok) return await res.json();
  } catch (err) {}

  const target = mockIncidents.find(i => i.id === id);
  if (target) target.status = status;
  return { success: true };
}

export async function diagnoseIncident(id, modelName = 'gemini-3.6-flash') {
  try {
    const res = await fetch(`${API_BASE}/incidents/${id}/diagnose`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ modelName })
    });
    if (res.ok) return await res.json();
  } catch (err) {}

  const target = mockIncidents.find(i => i.id === id);
  return target?.diagnosis || {
    rootCause: '시스템 트래픽 급증 및 DB 연결 대기열 지연',
    impactScope: '단일 엔드포인트 지연',
    remediationSteps: ['연결 풀 확대', '인덱스 튜닝'],
    suggestedCodeFix: '// Fix applied',
    confidencePercentage: 95,
    latencyMs: 310,
    modelUsed: 'gemini-3.6-flash'
  };
}

export async function exportIncidentToRag(incident, diagnosis) {
  const RAG_BASE = 'http://localhost:5000/api';
  try {
    const kbRes = await fetch(`${RAG_BASE}/knowledge-bases`);
    if (kbRes.ok) {
      const kbs = await kbRes.json();
      const targetKb = kbs.find(k => k.name.toLowerCase().includes('sentinel') || k.name.toLowerCase().includes('incident') || k.name.toLowerCase().includes('장애')) || kbs[0];

      const content = `# [Blueward Sentinel AI Agent 장애 진단 보고서] ${incident.title}
- 인시던트 ID: ${incident.id}
- 심각도: ${incident.severity}
- 상태: ${incident.status}
- 발생 횟수: ${incident.occurrenceCount}회
- Fingerprint: ${incident.fingerprint}
- 대표 오류 메시지: ${incident.sampleMessage}

## 기술적 근본 원인 (Root Cause)
${diagnosis?.rootCause || '진단 내용 없음'}

## 영향 범위 (Impact Scope)
${diagnosis?.impactScope || '영향 범위 분석 없음'}

## 권장 단계별 조치 가이드
${(diagnosis?.remediationSteps || []).map(s => `- ${s}`).join('\n')}

${diagnosis?.suggestedCodeFix ? `## 제안 소스코드 수정 패치\n\`\`\`csharp\n${diagnosis.suggestedCodeFix}\n\`\`\`` : ''}
`;

      const safeTitle = `Blueward_Sentinel_Agent_${incident.id.substring(0, 8)}.md`;
      const res = await fetch(`${RAG_BASE}/knowledge-bases/${targetKb.id}/documents/raw`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ title: safeTitle, content })
      });
      if (res.ok) {
        const result = await res.json();
        return { kbName: targetKb.name, ...result };
      }
    }
  } catch (err) {}

  // Graceful fallback for demo on Vercel
  return {
    kbName: '사내 엔터프라이즈 AIOps 지식 베이스 (Demo)',
    documentId: 'doc-' + Math.random().toString(36).substring(2, 8),
    chunksCount: 2,
    message: '성공적으로 색인되었습니다.'
  };
}
