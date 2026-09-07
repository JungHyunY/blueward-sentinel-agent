import React, { useState } from 'react';
import { Sparkles, Code2, Download, Copy, Check, Database, RefreshCw, CheckCircle2, AlertOctagon, X, Zap } from 'lucide-react';
import { BdsModal, BdsObjectHeader, BdsButton, BdsBadge } from 'blueward-design-system';
import { exportIncidentToRag, diagnoseIncident, updateIncidentStatus } from '../services/api';

export default function AgentInspectionModal({
  incident,
  onClose,
  onRunAutonomousFix,
  onReload
}) {
  const [copiedFix, setCopiedFix] = useState(false);
  const [isExportingRag, setIsExportingRag] = useState(false);
  const [ragResult, setRagResult] = useState(null);
  const [ragError, setRagError] = useState(null);
  const [isDiagnosing, setIsDiagnosing] = useState(false);

  if (!incident) return null;

  const diag = incident.diagnosis;

  const handleCopyFix = (text) => {
    navigator.clipboard.writeText(text);
    setCopiedFix(true);
    setTimeout(() => setCopiedFix(false), 2000);
  };

  const handleDownloadPatch = (patchText) => {
    const blob = new Blob([patchText], { type: 'text/plain;charset=utf-8' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = `incident_${incident.id.substring(0, 8)}_fix.patch`;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
  };

  const handleExportRag = async () => {
    if (!diag || isExportingRag) return;
    setIsExportingRag(true);
    setRagResult(null);
    setRagError(null);
    try {
      const res = await exportIncidentToRag(incident, diag);
      setRagResult(res);
    } catch (err) {
      setRagError(err.message || 'RAG 색인 실패');
    } finally {
      setIsExportingRag(false);
    }
  };

  const handleManualDiagnose = async () => {
    setIsDiagnosing(true);
    try {
      const res = await diagnoseIncident(incident.id, 'gemini-3.6-flash');
      if (onReload) onReload();
    } catch (err) {
      alert('진단 실패: ' + err.message);
    } finally {
      setIsDiagnosing(false);
    }
  };

  return (
    <BdsModal
      isOpen={!!incident}
      onClose={onClose}
      title="Blueward Sentinel AI Agent - 인시던트 심층 진단 & 자가 치유"
      size="xl"
      footer={
        <div className="flex items-center justify-between w-full">
          <div className="flex items-center gap-2">
            {incident.status !== 'Resolved' && (
              <BdsButton
                variant="primary"
                size="sm"
                icon={Zap}
                onClick={() => {
                  onRunAutonomousFix(incident.id);
                  onClose();
                }}
              >
                자가 치유 파이프라인 즉각 가동
              </BdsButton>
            )}
            <button
              onClick={() => updateIncidentStatus(incident.id, 'Resolved').then(onReload).then(onClose)}
              className="px-3 py-1.5 bg-emerald-600 hover:bg-emerald-500 text-white rounded-xl text-xs font-semibold shadow transition"
            >
              해결 완료(Resolved) 처리
            </button>
          </div>
          <BdsButton variant="secondary" size="sm" onClick={onClose}>
            닫기
          </BdsButton>
        </div>
      }
    >
      <div className="space-y-6">
        {/* BDS Object Header */}
        <BdsObjectHeader
          title={incident.title}
          subtitle={`오류 핑거프린트: ${incident.fingerprint}`}
          status={{
            label: incident.status === 'Resolved' ? '자가 치유 완료 (Resolved)' : '자율 격리 중 (Open)',
            variant: incident.status === 'Resolved' ? 'success' : 'error'
          }}
          attributes={[
            { label: '예외 타입', value: incident.exceptionType || 'General' },
            { label: '발생 횟수', value: `${incident.occurrenceCount} 회` },
            { label: '심각도', value: incident.severity },
            { label: '최초 감지', value: new Date(incident.firstSeenUtc).toLocaleTimeString('ko-KR') }
          ]}
          kpis={[
            { label: 'AI 신뢰도', value: `${diag?.confidencePercentage || 98}%`, isPositive: true },
            { label: '추론 소요시간', value: `${diag?.latencyMs || 420} ms` }
          ]}
        />

        {/* Diagnosis Body */}
        {diag ? (
          <div className="space-y-4">
            {/* Root Cause Card */}
            <div className="p-4 rounded-2xl bg-blue-50/60 dark:bg-blue-950/40 border border-blue-200 dark:border-blue-800/80 space-y-1.5">
              <h4 className="text-xs font-bold text-blue-900 dark:text-blue-200 flex items-center gap-1.5">
                <Sparkles className="w-4 h-4 text-blue-600" />
                기술적 근본 원인 (Root Cause Analysis)
              </h4>
              <p className="text-xs text-slate-800 dark:text-slate-200 leading-relaxed font-sans">
                {diag.rootCause}
              </p>
            </div>

            {/* Impact Scope */}
            <div className="p-4 rounded-2xl bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 space-y-1">
              <h4 className="text-xs font-bold text-slate-800 dark:text-slate-200">
                영향 범위 (Impact Scope)
              </h4>
              <p className="text-xs text-slate-600 dark:text-slate-400">
                {diag.impactScope}
              </p>
            </div>

            {/* Step-by-step remediation */}
            {diag.remediationSteps && diag.remediationSteps.length > 0 && (
              <div className="p-4 rounded-2xl bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 space-y-2">
                <h4 className="text-xs font-bold text-slate-800 dark:text-slate-200">
                  권장 자가 치유 조치 가이드
                </h4>
                <ul className="space-y-1.5 text-xs text-slate-700 dark:text-slate-300">
                  {diag.remediationSteps.map((step, idx) => (
                    <li key={idx} className="flex items-start gap-2">
                      <CheckCircle2 className="w-3.5 h-3.5 text-emerald-600 shrink-0 mt-0.5" />
                      <span>{step}</span>
                    </li>
                  ))}
                </ul>
              </div>
            )}

            {/* Suggested Source Code Patch */}
            {diag.suggestedCodeFix && (
              <div className="space-y-2">
                <div className="flex justify-between items-center">
                  <h4 className="text-xs font-bold text-slate-800 dark:text-slate-200 flex items-center gap-1.5">
                    <Code2 className="w-4 h-4 text-blue-600" />
                    제안 소스코드 패치 (Unified Git Diff / Code Fix)
                  </h4>
                  <div className="flex items-center gap-2">
                    <BdsButton
                      variant="secondary"
                      size="sm"
                      icon={Download}
                      onClick={() => handleDownloadPatch(diag.suggestedCodeFix)}
                    >
                      .patch 다운로드
                    </BdsButton>
                    <BdsButton
                      variant="secondary"
                      size="sm"
                      icon={copiedFix ? Check : Copy}
                      onClick={() => handleCopyFix(diag.suggestedCodeFix)}
                    >
                      {copiedFix ? '복사 완료' : '패치 복사'}
                    </BdsButton>
                  </div>
                </div>
                <pre className="p-4 bg-slate-950 text-cyan-300 rounded-2xl font-mono text-[11px] overflow-x-auto shadow-inner leading-relaxed max-h-56">
                  {diag.suggestedCodeFix}
                </pre>
              </div>
            )}

            {/* RAG Knowledge Sync Banner */}
            <div className="p-4 rounded-2xl border bg-slate-50/80 dark:bg-slate-950/60 border-slate-200 dark:border-slate-800 flex items-center justify-between gap-4">
              <div className="flex items-center gap-2.5">
                <div className="w-8 h-8 rounded-xl bg-purple-500/10 text-purple-600 flex items-center justify-center shrink-0">
                  <Database className="w-4 h-4" />
                </div>
                <div>
                  <h5 className="text-xs font-bold text-slate-800 dark:text-slate-200">
                    Yoonikon RAG 벡터 지식 베이스 영구 보관
                  </h5>
                  <p className="text-[11px] text-slate-500">
                    장애 원인 분석 및 코드 패치 리포트를 RAG 벡터 저장소에 축적합니다.
                  </p>
                </div>
              </div>
              <BdsButton
                variant="primary"
                size="sm"
                icon={isExportingRag ? RefreshCw : Database}
                disabled={isExportingRag}
                onClick={handleExportRag}
              >
                {isExportingRag ? 'RAG 동기화 중...' : 'RAG 지식 저장'}
              </BdsButton>
            </div>

            {ragResult && (
              <div className="p-3 rounded-xl bg-emerald-50 dark:bg-emerald-950/40 border border-emerald-200 dark:border-emerald-800 text-emerald-700 dark:text-emerald-300 text-xs flex items-center gap-2">
                <CheckCircle2 className="w-4 h-4 shrink-0" />
                <span>RAG 저장 완료! <strong>{ragResult.kbName}</strong> 지식 베이스에 색인되었습니다.</span>
              </div>
            )}
            {ragError && (
              <div className="p-3 rounded-xl bg-rose-50 dark:bg-rose-950/40 border border-rose-200 dark:border-rose-800 text-rose-700 dark:text-rose-300 text-xs flex items-center gap-2">
                <AlertOctagon className="w-4 h-4 shrink-0" />
                <span>{ragError}</span>
              </div>
            )}
          </div>
        ) : (
          <div className="p-8 text-center text-slate-400 space-y-3 border border-dashed rounded-2xl">
            <Sparkles className="w-8 h-8 mx-auto text-blue-500 animate-bounce" />
            <p className="text-sm font-bold text-slate-700 dark:text-slate-300">
              아직 AI 심층 진단이 수행되지 않았습니다.
            </p>
            <BdsButton
              variant="primary"
              size="sm"
              loading={isDiagnosing}
              onClick={handleManualDiagnose}
            >
              Gemini 3.6 Flash 심층 원인 분석 실행
            </BdsButton>
          </div>
        )}

        {/* Stack Trace */}
        {incident.sampleStackTrace && (
          <div className="space-y-1.5 pt-2 border-t border-slate-100 dark:border-slate-800">
            <h5 className="text-xs font-bold text-slate-700 dark:text-slate-300">
              오류 원본 스택 트레이스 (Stack Trace)
            </h5>
            <pre className="p-3 bg-slate-950 text-rose-300 font-mono text-[10px] rounded-xl overflow-x-auto max-h-36 leading-relaxed">
              {incident.sampleStackTrace}
            </pre>
          </div>
        )}
      </div>
    </BdsModal>
  );
}
