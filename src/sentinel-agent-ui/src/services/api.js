const API_BASE = '/api';

export async function fetchAgentStatus() {
  const res = await fetch(`${API_BASE}/agent/status`);
  if (!res.ok) throw new Error('Agent 상태 조회 실패');
  return res.json();
}

export async function fetchThoughtStream() {
  const res = await fetch(`${API_BASE}/agent/thought-stream`);
  if (!res.ok) throw new Error('Agent 생각 스트림 조회 실패');
  return res.json();
}

export async function simulateIncident(scenario, projectId = 'p-default') {
  const res = await fetch(`${API_BASE}/agent/simulate-incident`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ scenario, projectId })
  });
  if (!res.ok) throw new Error('인시던트 시뮬레이션 주입 실패');
  return res.json();
}

export async function runAutonomousPipeline(incidentId) {
  const res = await fetch(`${API_BASE}/agent/run-autonomous-pipeline/${incidentId}`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' }
  });
  if (!res.ok) throw new Error('자율 치유 파이프라인 실행 실패');
  return res.json();
}

export async function fetchIncidents(projectId, severity, status, search) {
  const params = new URLSearchParams();
  if (projectId) params.append('projectId', projectId);
  if (severity) params.append('severity', severity);
  if (status) params.append('status', status);
  if (search) params.append('search', search);

  const res = await fetch(`${API_BASE}/incidents?${params.toString()}`);
  if (!res.ok) throw new Error('인시던트 목록 조회 실패');
  return res.json();
}

export async function fetchLogs(projectId, logLevel, limit = 100) {
  const params = new URLSearchParams();
  if (projectId) params.append('projectId', projectId);
  if (logLevel) params.append('logLevel', logLevel);
  params.append('limit', limit.toString());

  const res = await fetch(`${API_BASE}/logs?${params.toString()}`);
  if (!res.ok) throw new Error('로그 조회 실패');
  return res.json();
}

export async function fetchAnalyticsOverview(projectId) {
  const params = projectId ? `?projectId=${encodeURIComponent(projectId)}` : '';
  const res = await fetch(`${API_BASE}/analytics/overview${params}`);
  if (!res.ok) throw new Error('분석 지표 조회 실패');
  return res.json();
}

export async function updateIncidentStatus(id, status) {
  const res = await fetch(`${API_BASE}/incidents/${id}/status`, {
    method: 'PATCH',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ status })
  });
  if (!res.ok) throw new Error('상태 변경 실패');
  return res.json();
}

export async function diagnoseIncident(id, modelName = 'gemini-3.6-flash') {
  const res = await fetch(`${API_BASE}/incidents/${id}/diagnose`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ modelName })
  });
  if (!res.ok) {
    const err = await res.text();
    throw new Error('AI 진단 실패: ' + err);
  }
  return res.json();
}

export async function exportIncidentToRag(incident, diagnosis) {
  const RAG_BASE = 'http://localhost:5000/api';
  const kbRes = await fetch(`${RAG_BASE}/knowledge-bases`);
  if (!kbRes.ok) {
    throw new Error('RAG 서버(포트 5000)와 통신할 수 없습니다. RAG 서비스 실행 여부를 확인하세요.');
  }
  const kbs = await kbRes.json();
  if (!kbs || kbs.length === 0) {
    throw new Error('RAG에 등록된 지식 베이스가 없습니다.');
  }

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

${incident.sampleStackTrace ? `## 오류 스택 트레이스\n\`\`\`\n${incident.sampleStackTrace}\n\`\`\`` : ''}
`;

  const safeTitle = `Blueward_Sentinel_Agent_${incident.id.substring(0, 8)}.md`;

  const res = await fetch(`${RAG_BASE}/knowledge-bases/${targetKb.id}/documents/raw`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      title: safeTitle,
      content
    })
  });

  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error(err.message || 'RAG 문서 색인 실패');
  }

  const result = await res.json();
  return { kbName: targetKb.name, ...result };
}
