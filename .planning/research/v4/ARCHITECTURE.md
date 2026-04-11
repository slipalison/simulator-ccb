# CI/CD Pipeline + Cybersecurity — Architecture Research

**Domain:** Secure CI/CD for .NET 10 + React/Vinxi Monorepo
**Researched:** 2026-04-10
**Context:** v4.0 milestone — How to structure parallel CI/CD with security scanning

---

## Workflow Architecture

### High-Level Pipeline Structure

```
┌─────────────────────────────────────────────────────────────────┐
│                        GitHub Actions                            │
│                                                                  │
│  ┌──────────────┐                                                │
│  │ Path Filter   │ ← dorny/paths-filter                          │
│  │ (backend,     │                                                │
│  │  client,      │                                                │
│  │  backoffice)  │                                                │
│  └──────┬───────┘                                                │
│         │                                                         │
│  ┌──────▼──────────────────────────────────────────────┐         │
│  │              PARALLEL BUILD JOBS                     │         │
│  │                                                      │         │
│  │  ┌──────────┐  ┌──────────┐  ┌──────────┐          │         │
│  │  │ Backend   │  │ Client   │  │ Backoffice│          │         │
│  │  │ .NET 10   │  │ Vinxi    │  │ Vinxi     │          │         │
│  │  │          │  │          │  │           │          │         │
│  │  │ restore   │  │ restore  │  │ restore   │          │         │
│  │  │ build     │  │ build    │  │ build     │          │         │
│  │  │ test      │  │ lint     │  │ lint      │          │         │
│  │  │ coverage  │  │ typechk  │  │ typechk   │          │         │
│  │  └────┬─────┘  └────┬─────┘  └─────┬─────┘          │         │
│  └───────┼─────────────┼─────────────┼─────────────────┘         │
│          │             │             │                             │
│  ┌───────▼─────────────▼─────────────▼─────────────────┐         │
│  │           SECURITY SCANNING (PARALLEL)               │         │
│  │                                                      │         │
│  │  ┌──────────┐  ┌──────────┐  ┌──────────┐          │         │
│  │  │ Semgrep  │  │ Gitleaks │  │ Trivy FS │          │         │
│  │  │ (SAST)   │  │(Secrets) │  │ (SCA)    │          │         │
│  │  └──────────┘  └──────────┘  └──────────┘          │         │
│  │                                                      │         │
│  │  ┌──────────┐  ┌──────────┐                         │         │
│  │  │ Trivy    │  │ Dockle   │  (after Docker build)   │         │
│  │  │ (Image)  │  │          │                          │         │
│  │  └──────────┘  └──────────┘                         │         │
│  └───────────────────────┬──────────────────────────────┘         │
│                          │                                         │
│  ┌───────────────────────▼──────────────────────────────┐         │
│  │           SARIF UPLOAD (if: always())                 │         │
│  │                                                      │         │
│  │  All scanner results → GitHub Security Tab            │         │
│  └──────────────────────────────────────────────────────┘         │
└──────────────────────────────────────────────────────────────────┘

SEPARATE WORKFLOW (scheduled: weekly):
┌──────────────────────────────────────────────────────────────────┐
│  ┌──────────┐  ┌──────────┐  ┌──────────┐                       │
│  │ CodeQL   │  │ Checkov  │  │ Kubescape│                       │
│  │ (deep)   │  │ (IaC)    │  │ (K8s)    │                       │
│  └──────────┘  └──────────┘  └──────────┘                       │
└──────────────────────────────────────────────────────────────────┘
```

---

## Job Matrix Configuration

### Workflow Triggers

```yaml
on:
  pull_request:
    branches: [main]
    paths:
      - 'src/**'
      - 'frontend/client/**'
      - 'frontend/backoffice/**'
      - 'compose.yaml'
      - 'Dockerfile*'
      - '.github/workflows/**'
  push:
    branches: [main]
  schedule:
    # CodeQL + Checkov run weekly on Sunday at 6am UTC
    - cron: '0 6 * * 0'
  workflow_dispatch:  # Manual trigger for debugging
```

### Build Jobs Matrix

```yaml
jobs:
  # Phase 1: Path Filtering
  filter:
    runs-on: ubuntu-latest
    outputs:
      backend: ${{ steps.filter.outputs.backend }}
      client: ${{ steps.filter.outputs.client }}
      backoffice: ${{ steps.filter.outputs.backoffice }}
      infra: ${{ steps.filter.outputs.infra }}
    steps:
      - uses: dorny/paths-filter@v3
        with:
          filters: |
            backend:
              - 'src/**'
              - '**.cs'
              - '**.csproj'
            client:
              - 'frontend/client/**'
            backoffice:
              - 'frontend/backoffice/**'
            infra:
              - 'compose.yaml'
              - 'Dockerfile*'
              - 'keycloak/**'

  # Phase 2: Parallel Builds
  backend-build:
    needs: filter
    if: needs.filter.outputs.backend == 'true'
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
          cache: true
          cache-dependency-path: '**/packages.lock.json'
      - run: dotnet restore
      - run: dotnet build --no-restore --configuration Release
      - run: dotnet test --no-build --configuration Release --logger trx
      - run: dotnet test --no-build --configuration Release --collect:"XPlat Code Coverage"

  client-build:
    needs: filter
    if: needs.filter.outputs.client == 'true'
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-node@v4
        with:
          node-version: '22'
          cache: 'npm'
          cache-dependency-path: 'frontend/client/package-lock.json'
      - run: npm ci
        working-directory: frontend/client
      - run: npm run lint
        working-directory: frontend/client
      - run: npx tsc --noEmit
        working-directory: frontend/client
      - run: npm run build
        working-directory: frontend/client

  backoffice-build:
    needs: filter
    if: needs.filter.outputs.backoffice == 'true'
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-node@v4
        with:
          node-version: '22'
          cache: 'npm'
          cache-dependency-path: 'frontend/backoffice/package-lock.json'
      - run: npm ci
        working-directory: frontend/backoffice
      - run: npm run lint
        working-directory: frontend/backoffice
      - run: npx tsc --noEmit
        working-directory: frontend/backoffice
      - run: npm run build
        working-directory: frontend/backoffice
```

### Security Scanning Jobs

```yaml
  semgrep-scan:
    needs: filter
    runs-on: ubuntu-latest
    permissions:
      security-events: write
      contents: read
    steps:
      - uses: actions/checkout@v4
      - uses: semgrep/semgrep-action@v1
        with:
          config: >-
            p/security-audit
            p/secrets
            p/owasp-top-ten
        env:
          SEMGREP_RULES: >-
            p/csharp
            p/javascript
            p/typescript

  gitleaks-scan:
    needs: filter
    runs-on: ubuntu-latest
    permissions:
      security-events: write
      contents: read
    steps:
      - uses: actions/checkout@v4
        with:
          fetch-depth: 0  # Required for full history scan
      - uses: gitleaks/gitleaks-action@v2
        with:
          config-path: .gitleaks.toml
        env:
          GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}

  trivy-filesystem:
    needs: filter
    runs-on: ubuntu-latest
    permissions:
      security-events: write
      contents: read
    steps:
      - uses: actions/checkout@v4
      - uses: aquasecurity/trivy-action@57a97c7  # SHA-pinned (v0.35.0)
        with:
          scan-type: fs
          scan-ref: '.'
          severity: CRITICAL,HIGH
          exit-code: 1
          format: sarif
          output: trivy-fs-results.sarif

  trivy-image:
    needs: backend-build  # Requires Docker build first
    runs-on: ubuntu-latest
    if: needs.filter.outputs.infra == 'true'
    permissions:
      security-events: write
      contents: read
    steps:
      - uses: actions/checkout@v4
      - run: docker compose build
      - uses: aquasecurity/trivy-action@57a97c7  # SHA-pinned (v0.35.0)
        with:
          image-ref: 'onboarding-api:latest'
          severity: CRITICAL,HIGH
          exit-code: 1
          format: sarif
          output: trivy-image-results.sarif

  # SARIF Upload (runs even if scanners fail)
  upload-sarif:
    needs: [semgrep-scan, gitleaks-scan, trivy-filesystem, trivy-image]
    if: always()
    runs-on: ubuntu-latest
    permissions:
      security-events: write
      contents: read
    steps:
      - uses: github/codeql-action/upload-sarif@v3
        with:
          sarif_file: trivy-fs-results.sarif
      - uses: github/codeql-action/upload-sarif@v3
        with:
          sarif_file: trivy-image-results.sarif
```

---

## Parallel vs Sequential Execution Plan

| Stage | Execution | Rationale |
|-------|-----------|-----------|
| Path Filter | First, single job | Determines which downstream jobs run |
| Backend Build | Parallel with client/backoffice | Independent projects, no dependencies |
| Client Build | Parallel with backend/backoffice | Independent projects, no dependencies |
| Backoffice Build | Parallel with backend/client | Independent projects, no dependencies |
| Semgrep | Parallel with other scanners | Independent analysis |
| Gitleaks | Parallel with other scanners | Independent analysis |
| Trivy FS | Parallel with other scanners | Independent analysis |
| Trivy Image | Sequential after backend build | Requires Docker image to exist |
| SARIF Upload | Sequential after all scanners | Aggregates all results |
| CodeQL (nightly) | Separate scheduled workflow | Too slow for PR gates (30+ min) |

---

## Data Flow

```
Developer pushes code
    │
    ▼
GitHub Actions triggered (PR/push/schedule)
    │
    ├──→ Path Filter → determines which jobs run
    │
    ├──→ Build Jobs (parallel) → pass/fail status
    │
    ├──→ Security Scanners (parallel) → SARIF files
    │
    ├──→ SARIF Upload → GitHub Security Tab
    │
    └──→ Branch Protection → blocks merge if any job fails
              │
              ├── CRITICAL/HIGH vulns → BLOCK merge
              └── MEDIUM/LOW vulns → WARN only
```

---

## Suggested Build Order for Implementation

| Phase | What to Implement | Dependency | Estimated Effort |
|-------|-------------------|------------|-----------------|
| **1** | Basic CI: path filter + 3 build jobs | None | 2-3 hours |
| **2** | Dependabot config (`.github/dependabot.yml`) | None | 30 min |
| **3** | Gitleaks integration | Phase 1 | 1 hour |
| **4** | Semgrep integration | Phase 1 | 1-2 hours |
| **5** | Trivy filesystem scanning | Phase 1 | 1 hour |
| **6** | Trivy image scanning (SHA-pinned!) | Phase 1 + Dockerfile | 1-2 hours |
| **7** | SARIF upload aggregation | Phases 3-6 | 1 hour |
| **8** | Branch protection rules | Phase 7 | 30 min (UI config) |
| **9** | CodeQL nightly workflow | Phase 7 | 2-3 hours |
| **10** | Checkov IaC scanning | Phase 1 | 1-2 hours |
| **11** | Dockle integration | Phase 6 | 30 min |
| **12** | Coverage gates | Phase 1 | 30 min |

**Why this order:**
- Phases 1-2 are foundational (no scanning yet, just builds + dep management)
- Phases 3-5 add the highest-impact, lowest-friction scanners first
- Phase 6 adds container scanning (requires Docker build to work)
- Phase 7 centralizes findings (makes all previous scanners visible)
- Phases 8+ are polish and enforcement

---

## Reporting Flow

```
Scanner Results (SARIF format)
    │
    ▼
github/codeql-action/upload-sarif@v3
    │
    ├──→ GitHub Security Tab (findings list)
    │       ├── Severity filtering (CRITICAL, HIGH, MEDIUM, LOW)
    │       ├── Code snippets with vulnerability location
    │       └── Remediation guidance links
    │
    ├──→ Dependabot Alerts (dependency vulns)
    │       ├── Auto-created PRs for vulnerable packages
    │       └── Severity scoring
    │
    └──→ PR Status Checks (pipeline status)
            ├── Green = all passed, safe to merge
            └── Red = build failure or critical vuln, block merge
```

---

## Security Boundaries

| Boundary | Enforcement Mechanism |
|----------|----------------------|
| **Secrets in repo** | Gitleaks blocks commits with secrets |
| **Vulnerable dependencies** | Dependabot alerts + Trivy FS blocks CRITICAL/HIGH |
| **Code vulnerabilities** | Semgrep blocks CRITICAL/HIGH in PR, CodeQL flags in nightly |
| **Container vulnerabilities** | Trivy image scan blocks CRITICAL/HIGH |
| **IaC misconfigurations** | Checkov warns (enforcement in later phase) |
| **Merge protection** | Branch protection requires all CI jobs to pass |
