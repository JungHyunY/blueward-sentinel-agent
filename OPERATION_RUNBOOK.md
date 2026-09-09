# 📖 Blueward Sentinel AI Agent 실전 운용 매뉴얼 (Operation Runbook)

이 문서는 **Blueward Sentinel AI Agent**를 로컬 환경 및 클라우드에서 실제로 기동하고, 엔터프라이즈 장애(SAP ERP, DB, Cache)를 자율적으로 진단·패치·기록하는 실전 운용 가이드입니다.

---

## 1. 시스템 아키텍처 및 포트 구성

* **로컬 풀스택 통합 포트:** `http://localhost:5060` (Kestrel 백엔드에서 정적 UI 번들 및 API 동시 서빙)
* **Vercel 클라우드 데모:** [https://sentinel-agent-ui.vercel.app](https://sentinel-agent-ui.vercel.app) (백엔드 미구동 시에도 클라이언트 독립 시뮬레이션 지원)
* **연계 포트:**
  - Sentinel Agent 백엔드 API & UI: `http://localhost:5060`
  - 사내 RAG 지식 베이스: `http://localhost:5000`

---

## 2. 환경 준비 및 사전 요구사항

* **런타임:** .NET 10.0 SDK, Node.js 20+ (UI 빌드 시 필요)
* **운영체제:** Windows 10/11, Linux, macOS
* **AI API 키 설정 (선택 사항):**
  - 시스템은 API 키가 없는 오프라인 환경에서도 **Zero-Key 룰베이스 추론 엔진**이 자동 작동합니다.
  - 실제 고도화된 LLM 추론을 사용하려면 `%APPDATA%\Blueward\ai_config.json` 또는 환경변수에 설정합니다.

---

## 3. 서비스 실행 방법 (3가지 옵션)

### 옵션 A. [권장] 로컬 단일 실행 (원클릭 풀스택)
백엔드 프로젝트 하나만 실행하면, 내부 `wwwroot`에 탑재된 React UI와 REST API가 포트 5060에서 한 번에 기동됩니다.
```bash
# 1. 터미널(PowerShell 또는 Bash)에서 백엔드 실행
dotnet run --project D:\projects\blueward-sentinel-agent\src\BluewardSentinel.Api\BluewardSentinel.Api.csproj --urls http://localhost:5060
```
* 브라우저에서 `http://localhost:5060` 접속

### 옵션 B. 프론트엔드 HMR 개발 모드
UI 컴포넌트 실시간 수정이 필요한 경우:
```bash
# 터미널 1: 백엔드 API 실행
dotnet run --project D:\projects\blueward-sentinel-agent\src\BluewardSentinel.Api\BluewardSentinel.Api.csproj --urls http://localhost:5060

# 터미널 2: 프론트엔드 Vite HMR 개발 서버 실행
cd D:\projects\blueward-sentinel-agent\src\sentinel-agent-ui
npm run dev
```
* 개발 서버 접속: `http://localhost:5061`

### 옵션 C. 클라우드 무설치 웹 데모 (시연 및 외부 공유용)
* URL: [https://sentinel-agent-ui.vercel.app](https://sentinel-agent-ui.vercel.app)
* 모바일, 태블릿, 외부 PC에서 별도 설치나 서버 기동 없이 즉시 접속하여 모든 기능을 시연할 수 있습니다.

---

## 4. 실전 운용 절차 (Step-by-Step Walkthrough)

### 1단계: 실시간 관제 대시보드 모니터링
* 접속 시 상단 **4대 핵심 KPI StatCard**(수집 텔레메트리, 활성 인시던트 수, 자율 복구율 98.4%, 평균 MTTR 38.2초)를 확인합니다.
* 우측 상단의 `[자율 치유 에이전트 가동 중]` 인디케이터가 활성화되어 있는지 확인합니다.

### 2단계: 엔터프라이즈 장애 주입 및 자율 대응 시연
* 화면 우측 상단의 **`[🚀 사내 경진대회 라이브 시연]`** 버튼을 클릭합니다.
* 3가지 엔터프라이즈 장애 시나리오 중 하나를 선택합니다:
  1. **SAP RFC Connection Pool Exhaustion & BAPI Deadlock** (사내 경진대회 1순위 시나리오)
  2. **HikariCP Database Connection Pool Leak**
  3. **Redis Cluster Out-Of-Memory & Eviction Cascade**
* **`[1-클릭 장애 주입 및 자율 치유 시연 시작]`** 버튼을 클릭합니다.

### 3단계: 4단계 Multi-Agent 파이프라인 추론 흐름 관찰
* 대시보드 상단에 **P0 긴급 경보 배너**가 발생합니다.
* 화면 하단 **Agent Thought Stream & Process Timeline**에서 4개 에이전트의 작업 단계가 실시간으로 갱신됩니다:
  - **Triage:** 초당 4,250건의 로그 중 데드락 핑거프린트 포착 및 심각도 분류 (약 1.2초)
  - **RCA Reasoning:** SAP 트랜잭션 락 인과 관계 심층 추론 (약 3.8초)
  - **Self-Healing:** 연결 풀 동적 확장 및 소스코드 패치 합성 (약 4.2초)
  - **Audit:** 복구 후 시스템 헬스체크 및 무결성 검증 (약 2.3초)
* 약 **11.5초** 만에 상태가 `자가 치유 완료 (Resolved)`로 자동 전환됩니다.

### 4단계: Unified Git Diff 패치 확인 및 실제 코드 반영
* 인시던트 데이터 테이블에서 해당 항목을 클릭하여 **인시던트 상세 모달**을 엽니다.
* 에이전트가 합성한 **Unified Git Diff (.patch)** 코드를 검토합니다.
* **`[.patch 다운로드]`** 버튼을 누르거나 생성된 패치 내용을 프로젝트에 즉시 적용합니다:
  ```bash
  # 실제 프로젝트 소스코드 디렉터리에서 패치 일괄 적용
  git apply SAP_RFC_Deadlock_Fix.patch
  ```

### 5단계: RAG 지식 베이스 영구 동기화
* 모달 우측 상단의 **`[RAG 지식 저장]`** 버튼을 클릭합니다.
* 이번 장애의 근본 원인과 해결 패치가 사내 **엔터프라이즈 RAG 지식 베이스**에 영구 보관되어, 향후 전사 엔지니어링 조직의 검색 및 유사 장애 재발 방지 지식으로 재활용됩니다.

---

## 5. 데이터 백업 및 초기화

* **SQLite 데이터베이스 파일 경로:**
  - Windows: `%APPDATA%\BluewardSentinelAgent\sentinel_agent.db`
* **데이터 초기화 방법:**
  - 서버를 종료한 후 위 `sentinel_agent.db` 파일을 삭제하고 재기동하면 초기 샘플 인시던트 데이터가 자동 재생성됩니다.
