import React from 'react';
import { Cpu, Terminal, CheckCircle2, AlertTriangle, ArrowRight } from 'lucide-react';
import { BdsProcessTimeline, BdsActivityFeed, BdsBadge } from 'blueward-design-system';

export default function AgentReasoningTimeline({
  thoughtStream = [],
  pipelineStages = [],
  isPipelineRunning = false
}) {
  // Map stages to BdsProcessTimeline format
  const timelineSteps = pipelineStages.length > 0
    ? pipelineStages.map((s, idx) => ({
        id: idx + 1,
        title: s.name,
        actor: s.agent,
        status: s.status === 'Completed' ? 'completed' : 'in_progress',
        message: `${s.result} (소요시간: ${s.latency})`,
        timestamp: '실시간 자율 처리'
      }))
    : [
        { id: 1, title: '1단계: 이상 징후 자동 탐지 (Triage)', actor: 'Triage Agent', status: 'completed', message: '로그 패턴 임계치 초과 및 심각도 P0 분류 완료', timestamp: '상시 활성' },
        { id: 2, title: '2단계: AI 근본 원인 추론 (RCA)', actor: 'RCA Reasoning Agent', status: 'completed', message: '다중 소스 교차 검증 및 스택트레이스 심층 분석', timestamp: '상시 활성' },
        { id: 3, title: '3단계: 자가 치유 패치 생성 (Self-Healing)', actor: 'Self-Healing Patch Agent', status: 'completed', message: 'Unified Diff .patch 및 격리 스크립트 합성', timestamp: '상시 활성' },
        { id: 4, title: '4단계: 무결성 사후 검증 (Audit)', actor: 'Audit & Verification Agent', status: 'completed', message: '헬스체크 통과 확인 및 RAG 지식 베이스 영구 저장', timestamp: '상시 활성' }
      ];

  // Map thoughtStream to BdsActivityFeed format
  const feedItems = thoughtStream.map((t, idx) => ({
    id: t.id || idx,
    user: t.agent || 'AI Sentinel',
    action: t.message,
    time: t.timestamp ? new Date(t.timestamp).toLocaleTimeString('ko-KR') : '방금 전',
    type: t.type || 'info'
  }));

  return (
    <div className="grid grid-cols-1 lg:grid-cols-12 gap-6">
      {/* 4-Stage Autonomous Multi-Agent Pipeline */}
      <div className="lg:col-span-7">
        <div className="p-6 rounded-2xl border bg-white dark:bg-slate-900 border-slate-200 dark:border-slate-800 shadow-sm">
          <div className="flex items-center justify-between mb-4 pb-3 border-b border-slate-100 dark:border-slate-800">
            <div>
              <h3 className="text-sm font-bold text-slate-900 dark:text-white flex items-center gap-2">
                <Cpu className="w-4 h-4 text-blue-600 dark:text-blue-400" />
                4단계 AIOps Multi-Agent 자율 해결 파이프라인
              </h3>
              <p className="text-xs text-slate-500 mt-0.5">
                사내 경진대회 핵심: 4개 전문 에이전트가 협업하여 38초 만에 무중단 자율 복구를 완성합니다.
              </p>
            </div>
            {isPipelineRunning && (
              <span className="px-2.5 py-1 rounded-full text-xs font-mono font-bold bg-blue-50 text-blue-600 dark:bg-blue-950 dark:text-blue-400 border border-blue-200 dark:border-blue-800 animate-pulse">
                파이프라인 추론 중...
              </span>
            )}
          </div>

          <BdsProcessTimeline steps={timelineSteps} orientation="vertical" />
        </div>
      </div>

      {/* Live Agent Thought Stream (Chain-of-Thought) */}
      <div className="lg:col-span-5">
        <BdsActivityFeed
          title="Live Agent CoT Thought Stream (추론 스트림)"
          items={feedItems}
          maxHeight="420px"
        />
      </div>
    </div>
  );
}
