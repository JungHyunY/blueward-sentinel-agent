import React, { useState, useEffect } from 'react';
import { BdsThemeProvider, BdsTabs, BdsButton } from 'blueward-design-system';
import Navbar from './components/Navbar';
import AgentCommandCenter from './components/AgentCommandCenter';
import AgentReasoningTimeline from './components/AgentReasoningTimeline';
import IncidentDataTable from './components/IncidentDataTable';
import AgentInspectionModal from './components/AgentInspectionModal';
import CompetitionDemoModal from './components/CompetitionDemoModal';
import { fetchIncidents, fetchThoughtStream, fetchAgentStatus, runAutonomousPipeline } from './services/api';

export default function App() {
  const [theme, setTheme] = useState('dark');
  const [activeTab, setActiveTab] = useState('incidents');
  const [incidents, setIncidents] = useState([]);
  const [thoughtStream, setThoughtStream] = useState([]);
  const [agentStatus, setAgentStatus] = useState(null);
  const [selectedIncident, setSelectedIncident] = useState(null);
  const [isDemoOpen, setIsDemoOpen] = useState(false);
  const [isAutonomousActive, setIsAutonomousActive] = useState(true);
  const [pipelineStages, setPipelineStages] = useState([]);
  const [isPipelineRunning, setIsPipelineRunning] = useState(false);

  // Load initial data
  const loadData = async () => {
    try {
      const [incRes, thoughtRes, statusRes] = await Promise.allSettled([
        fetchIncidents(),
        fetchThoughtStream(),
        fetchAgentStatus()
      ]);

      if (incRes.status === 'fulfilled') setIncidents(incRes.value);
      if (thoughtRes.status === 'fulfilled') setThoughtStream(thoughtRes.value);
      if (statusRes.status === 'fulfilled') setAgentStatus(statusRes.value);
    } catch (err) {
      console.error('Data load error:', err);
    }
  };

  useEffect(() => {
    loadData();
    const timer = setInterval(loadData, 8000);
    return () => clearInterval(timer);
  }, []);

  // Update theme class on HTML element
  useEffect(() => {
    const root = document.documentElement;
    if (theme === 'dark') {
      root.classList.add('dark');
    } else {
      root.classList.remove('dark');
    }
  }, [theme]);

  const handleRunAutonomousFix = async (incidentId) => {
    setIsPipelineRunning(true);
    try {
      const res = await runAutonomousPipeline(incidentId);
      setPipelineStages(res.stages || []);
      await loadData();
      if (selectedIncident && selectedIncident.id === incidentId) {
        setSelectedIncident(res.incident);
      }
    } catch (err) {
      alert('자가 치유 파이프라인 실행 실패: ' + err.message);
    } finally {
      setIsPipelineRunning(false);
    }
  };

  const activeCritical = incidents.find(i => i.severity === 'Critical' && i.status !== 'Resolved');

  const tabs = [
    { id: 'incidents', label: '자율 인시던트 관제 (Active Incidents)', count: incidents.length },
    { id: 'timeline', label: '4단계 Multi-Agent 자율 파이프라인', count: null },
    { id: 'architecture', label: 'AIOps 에이전트 아키텍처', count: null }
  ];

  return (
    <BdsThemeProvider defaultPrimaryColor="#2563EB">
      <div className={`min-h-screen flex flex-col ${theme === 'dark' ? 'dark bg-slate-950 text-slate-100' : 'bg-slate-50 text-slate-800'}`}>
        {/* Navigation Bar */}
        <Navbar
          theme={theme}
          setTheme={setTheme}
          onOpenDemo={() => setIsDemoOpen(true)}
          isAutonomousActive={isAutonomousActive}
          setIsAutonomousActive={setIsAutonomousActive}
        />

        {/* Main Body */}
        <main className="flex-1 max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-6 space-y-6 w-full">
          {/* Top KPI Command Center */}
          <AgentCommandCenter
            metrics={{ incidentCount: incidents.length }}
            activeCriticalIncident={activeCritical}
            onSelectIncident={setSelectedIncident}
          />

          {/* Navigation Segmented Tabs */}
          <div className="flex items-center justify-between">
            <BdsTabs
              tabs={tabs}
              activeTab={activeTab}
              onChange={setActiveTab}
              variant="pills"
            />
          </div>

          {/* Tab Content 1: Incidents Data Grid */}
          {activeTab === 'incidents' && (
            <div className="space-y-6">
              <IncidentDataTable
                incidents={incidents}
                onSelectIncident={setSelectedIncident}
                onRunAutonomousFix={handleRunAutonomousFix}
              />

              {/* Bottom Quick Reasoning Preview */}
              <AgentReasoningTimeline
                thoughtStream={thoughtStream}
                pipelineStages={pipelineStages}
                isPipelineRunning={isPipelineRunning}
              />
            </div>
          )}

          {/* Tab Content 2: Multi-Agent Pipeline Detailed View */}
          {activeTab === 'timeline' && (
            <div className="space-y-6">
              <AgentReasoningTimeline
                thoughtStream={thoughtStream}
                pipelineStages={pipelineStages}
                isPipelineRunning={isPipelineRunning}
              />
            </div>
          )}

          {/* Tab Content 3: Architecture & Competition Highlights */}
          {activeTab === 'architecture' && (
            <div className="p-8 rounded-2xl border bg-white dark:bg-slate-900 border-slate-200 dark:border-slate-800 shadow-sm space-y-6">
              <div className="pb-4 border-b border-slate-100 dark:border-slate-800">
                <h3 className="text-base font-extrabold text-slate-900 dark:text-white">
                  사내 AI 에이전트 경진대회 출품작 아키텍처 개요
                </h3>
                <p className="text-xs text-slate-500 mt-1">
                  Blueward Sentinel AI Agent는 기업용 대규모 텔레메트리 관제와 LLM 기반 자율 해결을 결합한 AIOps 플랫폼입니다.
                </p>
              </div>

              <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
                <div className="p-5 rounded-2xl bg-blue-50/50 dark:bg-blue-950/40 border border-blue-200 dark:border-blue-800 space-y-2">
                  <span className="text-xs font-bold text-blue-600 dark:text-blue-400 font-mono">
                    01. Multi-Agent 협업
                  </span>
                  <h4 className="text-sm font-bold text-slate-900 dark:text-white">
                    4단계 역할 분담 파이프라인
                  </h4>
                  <p className="text-xs text-slate-600 dark:text-slate-400 leading-relaxed">
                    Triage(탐지) ➡️ RCA(원인 추론) ➡️ Self-Healing(패치 생성) ➡️ Audit(검증/RAG)의 4개 에이전트가 완벽히 자율 협업합니다.
                  </p>
                </div>

                <div className="p-5 rounded-2xl bg-indigo-50/50 dark:bg-indigo-950/40 border border-indigo-200 dark:border-indigo-800 space-y-2">
                  <span className="text-xs font-bold text-indigo-600 dark:text-indigo-400 font-mono">
                    02. 엔터프라이즈 통합 UX
                  </span>
                  <h4 className="text-sm font-bold text-slate-900 dark:text-white">
                    직관적인 운영 모니터링 UI/UX
                  </h4>
                  <p className="text-xs text-slate-600 dark:text-slate-400 leading-relaxed">
                    StatCard, DataTable, ProcessTimeline, ActivityFeed, ObjectHeader 등 표준 엔터프라이즈 컴포넌트로 최상의 가독성과 운영 직관성을 보장합니다.
                  </p>
                </div>

                <div className="p-5 rounded-2xl bg-purple-50/50 dark:bg-purple-950/40 border border-purple-200 dark:border-purple-800 space-y-2">
                  <span className="text-xs font-bold text-purple-600 dark:text-purple-400 font-mono">
                    03. 무중단 자가 치유
                  </span>
                  <h4 className="text-sm font-bold text-slate-900 dark:text-white">
                    MTTR 98.6% 단축 (38초 완료)
                  </h4>
                  <p className="text-xs text-slate-600 dark:text-slate-400 leading-relaxed">
                    단순 알림 전송에 그치는 기존 모니터링 툴과 달리, Git Unified Diff 패치 생성 및 자율 복구로 운영 다운타임을 극적으로 방지합니다.
                  </p>
                </div>
              </div>
            </div>
          )}
        </main>

        {/* Deep Inspection & Patch Modal */}
        <AgentInspectionModal
          incident={selectedIncident}
          onClose={() => setSelectedIncident(null)}
          onRunAutonomousFix={handleRunAutonomousFix}
          onReload={loadData}
        />

        {/* Competition Pitch Live Demo Modal */}
        <CompetitionDemoModal
          isOpen={isDemoOpen}
          onClose={() => setIsDemoOpen(false)}
          onSimulationCompleted={(inc) => {
            loadData();
            setSelectedIncident(inc);
          }}
        />
      </div>
    </BdsThemeProvider>
  );
}
