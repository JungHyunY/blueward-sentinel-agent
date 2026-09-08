import React from 'react';
import { Database, AlertOctagon, Activity, ShieldCheck, Zap } from 'lucide-react';
import { BdsStatCard, BdsMessageStrip } from 'blueward-design-system';

export default function AgentCommandCenter({
  metrics,
  activeCriticalIncident,
  onSelectIncident
}) {
  return (
    <div className="space-y-4">
      {/* Alert Banner when critical incident is detected */}
      {activeCriticalIncident && (
        <BdsMessageStrip
          type="error"
          title="[긴급 P0 이상 징후 감지] AI 자율 치유 파이프라인이 트리거되었습니다"
          actionButton={
            <button
              onClick={() => onSelectIncident(activeCriticalIncident)}
              className="px-2.5 py-1 bg-rose-600 hover:bg-rose-500 text-white rounded-lg font-bold text-xs shadow-sm transition"
            >
              상세 분석 보기
            </button>
          }
        >
          {activeCriticalIncident.title} ({activeCriticalIncident.occurrenceCount}회 발생, {activeCriticalIncident.exceptionType})
        </BdsMessageStrip>
      )}

      {/* 4 Key StatCards */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        <BdsStatCard
          title="총 수집 텔레메트리"
          value="1,842,900 건"
          change="+24.8%"
          changeType="increase"
          changePeriod="전일 대비 수집량"
          icon={Database}
          iconColor="blue"
          sparklineData={[30, 45, 50, 70, 65, 85, 90, 110]}
        />
        <BdsStatCard
          title="이상 탐지 인시던트"
          value={`${metrics?.incidentCount || 3} 건`}
          change="100% 자동 격리"
          changeType="neutral"
          changePeriod="P0/P1 긴급 조치 완료"
          icon={AlertOctagon}
          iconColor="rose"
          sparklineData={[12, 8, 15, 6, 9, 14, 4, 3]}
        />
        <BdsStatCard
          title="AI 자율 치유율"
          value="98.4%"
          change="+12.5%"
          changeType="increase"
          changePeriod="vs 기존 수동 SRE 대응"
          icon={ShieldCheck}
          iconColor="emerald"
          progress={98}
        />
        <BdsStatCard
          title="평균 복구 시간 (MTTR)"
          value="38.2 초"
          change="-98.6%"
          changeType="decrease"
          changePeriod="기존 45분 → 38초 단축"
          icon={Zap}
          iconColor="purple"
          sparklineData={[90, 80, 60, 45, 30, 20, 10, 5]}
        />
      </div>
    </div>
  );
}
