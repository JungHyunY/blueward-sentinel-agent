import React from 'react';
import { Shield, Sparkles, Zap, Sun, Moon, Play } from 'lucide-react';
import { BdsButton, BdsBadge } from 'blueward-design-system';

export default function Navbar({
  theme,
  setTheme,
  onOpenDemo,
  isAutonomousActive,
  setIsAutonomousActive
}) {
  const isLight = theme === 'light';

  return (
    <header className="sticky top-0 z-40 w-full border-b backdrop-blur-md bg-white/90 dark:bg-slate-900/90 border-slate-200 dark:border-slate-800 transition-colors duration-200">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 h-16 flex items-center justify-between gap-4">
        {/* Brand & Subtitle */}
        <div className="flex items-center gap-3">
          <div className="w-10 h-10 rounded-2xl bg-gradient-to-tr from-blue-600 via-blue-500 to-cyan-500 flex items-center justify-center text-white shadow-lg shadow-blue-500/30">
            <Shield className="w-5 h-5" />
          </div>
          <div>
            <div className="flex items-center gap-2">
              <h1 className="text-base font-extrabold text-slate-900 dark:text-white tracking-tight">
                Blueward Sentinel <span className="text-blue-600 dark:text-blue-400">AI Agent</span>
              </h1>
              <BdsBadge variant="primary" size="sm">
                사내 AI 에이전트 경진대회 출품작
              </BdsBadge>
            </div>
            <p className="text-[11px] text-slate-500 dark:text-slate-400 font-medium">
              차세대 엔터프라이즈 자율 AIOps 모니터링 & 자가 치유 플랫폼
            </p>
          </div>
        </div>

        {/* Action Controls */}
        <div className="flex items-center gap-3">
          {/* Agent Status Diode */}
          <div className="hidden md:flex items-center gap-2 px-3 py-1.5 rounded-full bg-slate-100 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 text-xs font-mono">
            <span className="w-2.5 h-2.5 rounded-full bg-emerald-500 animate-ping" />
            <span className="w-2.5 h-2.5 rounded-full bg-emerald-500 -ml-4" />
            <span className="text-slate-700 dark:text-slate-300 font-medium">
              {isAutonomousActive ? '자율 치유 에이전트 가동 중' : '모니터링 모드'}
            </span>
          </div>

          {/* Competition Pitch Live Demo Button */}
          <BdsButton
            variant="primary"
            size="sm"
            icon={Play}
            onClick={onOpenDemo}
            className="animate-pulse shadow-lg shadow-blue-500/30 border border-blue-300/30 font-bold"
          >
            🚀 사내 경진대회 라이브 시연
          </BdsButton>

          {/* Theme Toggle */}
          <button
            onClick={() => setTheme(isLight ? 'dark' : 'light')}
            className="p-2 rounded-xl border border-slate-200 dark:border-slate-800 bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-300 hover:text-blue-600 dark:hover:text-blue-400 transition"
            title="테마 전환 (다크 / 라이트)"
          >
            {isLight ? <Moon className="w-4 h-4" /> : <Sun className="w-4 h-4" />}
          </button>
        </div>
      </div>
    </header>
  );
}
