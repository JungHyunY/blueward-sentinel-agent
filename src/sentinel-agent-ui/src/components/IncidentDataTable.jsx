import React from 'react';
import { AlertCircle, CheckCircle2, ChevronRight, Sparkles, Wrench } from 'lucide-react';
import { BdsBadge, BdsButton } from 'blueward-design-system';

export default function IncidentDataTable({
  incidents = [],
  onSelectIncident,
  onRunAutonomousFix,
  loading = false
}) {
  const getSeverityBadge = (sev) => {
    switch (sev?.toLowerCase()) {
      case 'critical':
        return <BdsBadge variant="danger">Critical (P0)</BdsBadge>;
      case 'high':
        return <BdsBadge variant="warning">High (P1)</BdsBadge>;
      case 'medium':
        return <BdsBadge variant="primary">Medium</BdsBadge>;
      default:
        return <BdsBadge variant="neutral">Low</BdsBadge>;
    }
  };

  const getStatusBadge = (st) => {
    if (st === 'Resolved') {
      return (
        <span className="inline-flex items-center gap-1 text-[11px] font-bold text-emerald-600 dark:text-emerald-400 bg-emerald-50 dark:bg-emerald-950/60 px-2 py-0.5 rounded-full border border-emerald-200 dark:border-emerald-800">
          <CheckCircle2 className="w-3 h-3" />
          자가 치유 완료
        </span>
      );
    }
    return (
      <span className="inline-flex items-center gap-1 text-[11px] font-bold text-rose-600 dark:text-rose-400 bg-rose-50 dark:bg-rose-950/60 px-2 py-0.5 rounded-full border border-rose-200 dark:border-rose-800 animate-pulse">
        <AlertCircle className="w-3 h-3" />
        자율 격리 중
      </span>
    );
  };

  return (
    <div className="p-6 rounded-2xl border bg-white dark:bg-slate-900 border-slate-200 dark:border-slate-800 shadow-sm space-y-4">
      <div className="flex items-center justify-between pb-3 border-b border-slate-100 dark:border-slate-800">
        <div>
          <h3 className="text-sm font-bold text-slate-900 dark:text-white">
            실시간 인시던트 클러스터 (Incident Clusters)
          </h3>
          <p className="text-xs text-slate-500">
            동일 오류 핑거프린트 기반으로 자동 군집화된 인시던트 목록입니다.
          </p>
        </div>
        <span className="text-xs font-mono text-slate-400">
          총 {incidents.length}건 감지
        </span>
      </div>

      <div className="overflow-x-auto">
        <table className="w-full text-left text-xs">
          <thead className="bg-slate-50 dark:bg-slate-950/60 border-b border-slate-200 dark:border-slate-800 text-slate-500 dark:text-slate-400 uppercase tracking-wider font-mono text-[10px]">
            <tr>
              <th className="py-3 px-4">심각도</th>
              <th className="py-3 px-4">인시던트 명칭 & 오류</th>
              <th className="py-3 px-4">예외 타입 (Fingerprint)</th>
              <th className="py-3 px-4 text-center">발생 횟수</th>
              <th className="py-3 px-4">상태</th>
              <th className="py-3 px-4 text-right">에이전트 액션</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100 dark:divide-slate-800/60">
            {incidents.length === 0 ? (
              <tr>
                <td colSpan={6} className="py-12 text-center text-slate-400">
                  현재 활성 인시던트가 없습니다. 클러스터가 정상 상태입니다.
                </td>
              </tr>
            ) : (
              incidents.map((inc) => (
                <tr
                  key={inc.id}
                  className="hover:bg-slate-50/80 dark:hover:bg-slate-800/40 transition group cursor-pointer"
                  onClick={() => onSelectIncident(inc)}
                >
                  <td className="py-3.5 px-4 whitespace-nowrap">
                    {getSeverityBadge(inc.severity)}
                  </td>
                  <td className="py-3.5 px-4 max-w-sm">
                    <p className="font-bold text-slate-900 dark:text-white truncate">
                      {inc.title}
                    </p>
                    <p className="text-[11px] text-slate-500 truncate mt-0.5">
                      {inc.sampleMessage}
                    </p>
                  </td>
                  <td className="py-3.5 px-4 font-mono text-[11px] text-blue-600 dark:text-blue-400 whitespace-nowrap">
                    {inc.exceptionType || 'GeneralException'}
                  </td>
                  <td className="py-3.5 px-4 text-center font-mono font-bold text-slate-700 dark:text-slate-300">
                    {inc.occurrenceCount}회
                  </td>
                  <td className="py-3.5 px-4 whitespace-nowrap">
                    {getStatusBadge(inc.status)}
                  </td>
                  <td className="py-3.5 px-4 text-right whitespace-nowrap" onClick={(e) => e.stopPropagation()}>
                    <div className="flex items-center justify-end gap-2">
                      <BdsButton
                        variant="secondary"
                        size="sm"
                        icon={Sparkles}
                        onClick={() => onSelectIncident(inc)}
                      >
                        AI 심층 진단
                      </BdsButton>
                      {inc.status !== 'Resolved' && (
                        <BdsButton
                          variant="primary"
                          size="sm"
                          icon={Wrench}
                          onClick={() => onRunAutonomousFix(inc.id)}
                        >
                          자가 치유
                        </BdsButton>
                      )}
                    </div>
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
