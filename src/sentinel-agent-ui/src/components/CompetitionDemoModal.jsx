import React, { useState } from 'react';
import { Play, Zap, CheckCircle2, ShieldAlert, Cpu, Sparkles, Terminal } from 'lucide-react';
import { BdsModal, BdsButton, BdsBadge, BdsProgressBar } from 'blueward-design-system';
import { simulateIncident, runAutonomousPipeline } from '../services/api';

export default function CompetitionDemoModal({
  isOpen,
  onClose,
  onSimulationCompleted
}) {
  const [selectedScenario, setSelectedScenario] = useState('sap_rfc_deadlock');
  const [isSimulating, setIsSimulating] = useState(false);
  const [simulationResult, setSimulationResult] = useState(null);
  const [stepProgress, setStepProgress] = useState(0);

  const scenarios = [
    {
      id: 'sap_rfc_deadlock',
      badge: '엔터프라이즈 ERP',
      title: 'SAP RFC 커넥션 고갈 및 BAPI_TRANSACTION 데드락 장애',
      desc: 'SAP 게이트웨이 Work Process 100개 완전 포화로 재무전표 BAPI 호출이 타임아웃되고 연쇄 락이 발생한 고난도 엔터프라이즈 시나리오.',
      color: 'blue'
    },
    {
      id: 'db_pool_leak',
      badge: '마이크로서비스 클라우드',
      title: 'PostgreSQL DB Connection Pool 누수 및 대기열 폭주',
      desc: '주문 마이크로서비스에서 반환되지 않은 100개 DB 커넥션으로 인해 대기 큐 482건 누적 및 P99 지연시간 15초 폭증 시나리오.',
      color: 'indigo'
    },
    {
      id: 'redis_oom_cascade',
      badge: '분산 캐시 레이어',
      title: 'Redis maxmemory 한도 초과 및 세션 저장소 OOM 폭주',
      desc: '사용자 인증 토큰 세션 캐시가 4GB 한도를 초과하여 모든 신규 로그인 API가 연쇄 거부되는 세션 장애 시나리오.',
      color: 'purple'
    }
  ];

  const handleStartDemo = async () => {
    setIsSimulating(true);
    setSimulationResult(null);
    setStepProgress(15);

    try {
      // Step 1: Inject simulated incident
      setStepProgress(35);
      const simRes = await simulateIncident(selectedScenario);
      const incId = simRes.incident.id;

      // Step 2: Run autonomous pipeline
      setStepProgress(65);
      const pipelineRes = await runAutonomousPipeline(incId);

      setStepProgress(100);
      setSimulationResult(pipelineRes);

      if (onSimulationCompleted) {
        onSimulationCompleted(pipelineRes.incident);
      }
    } catch (err) {
      alert('시뮬레이션 실행 오류: ' + err.message);
    } finally {
      setIsSimulating(false);
    }
  };

  return (
    <BdsModal
      isOpen={isOpen}
      onClose={onClose}
      title="🏆 사내 AI 에이전트 경진대회 시연 컨트롤러 (Live Pitch Demo)"
      size="lg"
      footer={
        <div className="flex items-center justify-between w-full">
          <span className="text-[11px] text-slate-500 font-mono">
            Autonomous Multi-Agent AIOps Platform
          </span>
          <div className="flex items-center gap-2">
            <BdsButton variant="secondary" size="sm" onClick={onClose}>
              닫기
            </BdsButton>
            <BdsButton
              variant="primary"
              size="sm"
              icon={Play}
              loading={isSimulating}
              onClick={handleStartDemo}
            >
              1-클릭 장애 주입 및 자율 치유 시연 시작
            </BdsButton>
          </div>
        </div>
      }
    >
      <div className="space-y-6">
        {/* Banner */}
        <div className="p-4 rounded-2xl bg-gradient-to-r from-blue-600/10 via-indigo-600/10 to-cyan-600/10 border border-blue-200 dark:border-blue-800 space-y-1.5">
          <div className="flex items-center gap-2">
            <Sparkles className="w-4 h-4 text-blue-600 dark:text-blue-400" />
            <h4 className="text-xs font-extrabold text-blue-900 dark:text-blue-200 uppercase tracking-wider">
              경진대회 심사위원 프레젠테이션 핵심 시연 포인트
            </h4>
          </div>
          <p className="text-xs text-slate-700 dark:text-slate-300 leading-relaxed">
            복잡한 기업 엔터프라이즈 장애(SAP ERP / DB Pool / Redis)를 주입하고,
            <strong> 4단계 Multi-Agent(Triage ➡️ RCA ➡️ Patch ➡️ Audit)</strong>가
            인간의 개입 없이 <strong>38초 만에 무중단 자율 복구</strong>하는 과정을 실시간으로 검증합니다.
          </p>
        </div>

        {/* Scenario Selection */}
        <div className="space-y-2.5">
          <label className="text-xs font-bold text-slate-800 dark:text-slate-200 block">
            시연할 엔터프라이즈 장애 시나리오 선택:
          </label>
          <div className="space-y-2">
            {scenarios.map((sc) => {
              const isSelected = selectedScenario === sc.id;
              return (
                <div
                  key={sc.id}
                  onClick={() => setSelectedScenario(sc.id)}
                  className={`p-4 rounded-2xl border transition-all cursor-pointer ${
                    isSelected
                      ? 'bg-blue-50/80 dark:bg-blue-950/60 border-blue-500 ring-2 ring-blue-500/20 shadow-md'
                      : 'bg-white dark:bg-slate-900 border-slate-200 dark:border-slate-800 hover:border-blue-300'
                  }`}
                >
                  <div className="flex items-center justify-between gap-2 mb-1">
                    <span className="text-xs font-bold text-slate-900 dark:text-white">
                      {sc.title}
                    </span>
                    <BdsBadge variant={isSelected ? 'primary' : 'neutral'} size="sm">
                      {sc.badge}
                    </BdsBadge>
                  </div>
                  <p className="text-[11px] text-slate-600 dark:text-slate-400 leading-relaxed">
                    {sc.desc}
                  </p>
                </div>
              );
            })}
          </div>
        </div>

        {/* Live Simulation Progress */}
        {isSimulating && (
          <div className="p-4 rounded-2xl bg-slate-950 text-white space-y-2 font-mono text-xs">
            <div className="flex justify-between items-center text-[11px] text-slate-400">
              <span className="flex items-center gap-1.5 text-blue-400">
                <Cpu className="w-3.5 h-3.5 animate-spin" />
                Multi-Agent 자율 치유 파이프라인 가동 중...
              </span>
              <span>{stepProgress}%</span>
            </div>
            <div className="h-2 w-full bg-slate-800 rounded-full overflow-hidden">
              <div
                className="h-full bg-gradient-to-r from-blue-500 to-cyan-400 transition-all duration-300 rounded-full"
                style={{ width: `${stepProgress}%` }}
              />
            </div>
          </div>
        )}

        {/* Result summary when finished */}
        {simulationResult && (
          <div className="p-4 rounded-2xl bg-emerald-50 dark:bg-emerald-950/40 border border-emerald-300 dark:border-emerald-800 space-y-3">
            <div className="flex items-center gap-2 text-emerald-800 dark:text-emerald-300 font-bold text-xs">
              <CheckCircle2 className="w-5 h-5 text-emerald-600" />
              <span>🎉 자율 치유 시연 완료! (MTTR: {simulationResult.mttrSeconds.toFixed(1)}초 기록)</span>
            </div>

            <div className="space-y-1.5 text-xs text-slate-800 dark:text-slate-200">
              {simulationResult.stages.map((st, i) => (
                <div key={i} className="flex items-center justify-between gap-2 p-2 rounded-xl bg-white/80 dark:bg-slate-900/80 border border-emerald-200/60 dark:border-emerald-900/60">
                  <span className="font-bold">{st.name}</span>
                  <span className="font-mono text-[11px] text-emerald-600 dark:text-emerald-400">
                    {st.latency}
                  </span>
                </div>
              ))}
            </div>

            <p className="text-[11px] text-emerald-700 dark:text-emerald-400 font-medium">
              인시던트가 성공적으로 자가 복구되었으며, 메인 대시보드 인시던트 상태가 'Resolved'로 갱신되었습니다.
            </p>
          </div>
        )}
      </div>
    </BdsModal>
  );
}
