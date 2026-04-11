# CI/CD Pipeline + Cybersecurity — Research Summary

**Domain:** Secure CI/CD for .NET 10 + React/Vinxi Monorepo
**Synthesized:** 2026-04-10
**Research files:** STACK.md, FEATURES.md, ARCHITECTURE.md, PITFALLS.md
**Milestone:** v4.0

---

## Executive Summary

This research covers the complete CI/CD + security scanning ecosystem for a monorepo with 3 independent projects (backend .NET 10, frontend client Vinxi, frontend backoffice Vinxi) plus Docker Compose infrastructure. The research identified **industry-standard tools, architectures, and critical pitfalls** for implementing a security-hardened pipeline on GitHub Actions.

### Critical Discovery: Trivy Supply Chain Attack (March 2026)

The most significant finding is that **Trivy was compromised on 2026-03-19**. Malicious versions (v0.69.4-v0.70.0) exfiltrated SSH keys, cloud tokens, K8s configs, and crypto wallets from CI/CD pipelines. **Only Trivy v0.69.3 or earlier (SHA-pinned) is safe.** If this project ever ran unpinned Trivy, all pipeline secrets must be rotated immediately.

---

## Stack Consensus

### SAST: Use Both Semgrep and CodeQL
- **Semgrep** for PR gates (~10s scans, ~150MB RAM, 15-20% FP rate) — fast developer feedback
- **CodeQL** for nightly deep scans (74.4% F1 score, best for C#, 8-12% FP rate, 30+ min) — thorough analysis
- This dual approach is the **industry standard** for balancing speed vs depth

### SCA: Dependabot + Trivy FS
- **Dependabot** (native, zero config) for automated dependency updates
- **Trivy filesystem scanning** (SHA-pinned!) for CVE detection in lockfiles

### Container: Trivy Image + Dockle
- **Trivy image scan** (SHA-pinned!) for production image CVEs
- **Dockle** for Dockerfile best practices (USER, HEALTHCHECK, no secrets in ENV)

### Secrets: Gitleaks + TruffleHog
- **Gitleaks** (regex-based, fast, ~5% FP) for primary detection
- **TruffleHog** (active verification) as secondary for confirmed leaks

### IaC: Checkov + Kubescape
- **Checkov** for Docker Compose and future Terraform/K8s
- **Kubescape** for Kubernetes NSA/CISA compliance (future)

### What NOT to Use
- **Trivy > v0.69.3** — supply chain attack, compromised
- **SonarQube Community** — 19% detection rate (vs 46% Semgrep)
- **Unpinned action versions** — supply chain attack vector
- **DAST in PR pipeline** — fragile, slow, not reliable for gates

---

## Table Stakes (Must Have)

1. Dependabot for automated dependency updates
2. Gitleaks for secrets detection
3. npm/dotnet audit for built-in package scanning
4. Semgrep on PRs for fast SAST feedback
5. Build validation (code must compile)
6. Test execution (all tests pass)
7. Branch protection (CI pass required for merge)
8. Docker image scan (Trivy, SHA-pinned)

---

## Architecture Highlights

### Pipeline Structure
```
Path Filter → Parallel Builds (backend + client + backoffice) → Parallel Security Scanners → SARIF Upload → GitHub Security Tab
```

### Key Design Decisions
- **Path-based filtering** (`dorny/paths-filter`): Skip unaffected jobs in monorepo
- **Concurrency control**: Cancel redundant PR runs with `cancel-in-progress`
- **Parallel scanners**: Semgrep, Gitleaks, Trivy FS run simultaneously (no data dependencies)
- **Sequential only where required**: Trivy image scan needs Docker build; SARIF upload needs scanner results
- **Scheduled deep scans**: CodeQL + Checkov run weekly, not on every PR

### Target Metrics
- **Pipeline duration:** < 10 minutes for PR feedback
- **Success rate:** > 95%
- **Lead time (commit → prod):** < 1 hour

---

## Critical Pitfalls

### P1 + P8: Supply Chain Attacks (CRITICAL)
Unpinned actions allowed Trivy compromise. Must pin all actions to SHA. Must rotate secrets if unpinned Trivy ever ran.

### P2: Scanner Overload (CRITICAL)
Adding 10+ scanners on day 1 kills developer velocity. Start with 3 (Dependabot + Gitleaks + Semgrep), add incrementally.

### P3: False Positive Fatigue (HIGH)
> 30% FP rate causes developers to ignore security. Mitigate: curated rules, severity filtering, allowlists, SLAs.

### P4: Base Image CVE Paranoia (HIGH)
400+ CVEs in Alpine/Debian base images. Most don't affect your app. Filter to CRITICAL/HIGH, document ignores.

---

## Confidence Assessment

| Dimension | Confidence | Notes |
|-----------|------------|-------|
| Stack recommendations | HIGH | Multiple sources corroborate, versions verified against current releases |
| Architecture patterns | HIGH | Industry-standard patterns from mature teams |
| Pitfall identification | HIGH | Well-documented in security literature, Trivy incident is verified |
| Tool performance metrics | MEDIUM-HIGH | Benchmarks from independent sources (Konvu, DryRun), but may vary by codebase |
| Trivy safe versions | HIGH | Official project advisory, SHA-pinned versions confirmed |

---

## Implications for Roadmap

Based on research, suggested phase structure for v4.0:

### Phase 1: **CI Foundation + Secret Hygiene**
- **Addresses:** Parallel build jobs for backend + client + backoffice (FEATURES.md table stakes)
- **Avoids:** P1 (unpinned actions), P8 (unrotated secrets) — pin everything from day 1
- **Uses:** GitHub Actions, dorny/paths-filter, concurrency control (STACK.md)
- **Rationale:** Must have working builds before adding scanners. Secret hygiene is non-negotiable post-Trivy.

### Phase 2: **SAST Gate**
- **Addresses:** Semgrep on PRs for fast code-level vulnerability detection (FEATURES.md table stakes)
- **Avoids:** P2 (scanner overload) — add only 1 scanner, P3 (FP fatigue) — curated rules only
- **Uses:** Semgrep with `p/security-audit p/secrets p/owasp-top-ten` (STACK.md)
- **Rationale:** Fastest scanner (~10s), highest developer value (catches bugs before merge), lowest integration effort.

### Phase 3: **Dependency + Container Scanning**
- **Addresses:** Trivy FS + Trivy image scan + Dockle (FEATURES.md table stakes + differentiators)
- **Avoids:** P4 (base image CVE paranoia) — filter to CRITICAL/HIGH, P9 (transitive deps) — Trivy catches them
- **Uses:** Trivy v0.69.3 SHA-pinned, Dockle (STACK.md)
- **Rationale:** After code-level scanning, next highest risk is in dependencies and containers.

### Phase 4: **Deep SAST (Nightly)**
- **Addresses:** CodeQL for thorough C# semantic analysis (FEATURES.md differentiator)
- **Avoids:** P5 (sequential execution) — runs on schedule, not PR gate
- **Uses:** CodeQL with `security-extended` queries (STACK.md)
- **Rationale:** Best C# coverage but too slow for PR gates. Nightly schedule avoids blocking developers.

### Phase 5: **IaC Scanning**
- **Addresses:** Checkov + Kubescape for infrastructure security (FEATURES.md differentiator)
- **Avoids:** P6 (DAST in PR) — IaC scanning is static, fast, reliable
- **Uses:** Checkov for compose.yaml, Kubescape for future K8s (STACK.md)
- **Rationale:** Infrastructure security is critical for Docker Compose. Low FP rate, fast scans.

### Phase 6: **Hardening + Enforcement**
- **Addresses:** SARIF centralization, branch protection, coverage gates, documentation (FEATURES.md anti-features prevention)
- **Avoids:** P7 (no remediation guidance), P10 (no branch protection)
- **Uses:** github/codeql-action/upload-sarif, GitHub branch protection API
- **Rationale:** Polish phase — make all scanners visible, enforce discipline, document remediation.

### Phase Ordering Rationale
- **1→2→3→4→5→6:** Each phase adds capability without overwhelming the team
- **Scanners added incrementally:** Prevents P2 (scanner overload)
- **SARIF upload last:** Needs all scanners to exist first
- **Branch protection second-to-last:** Don't enforce what doesn't work yet
- **CodeQL nightly (Phase 4):** Separate from PR gates to avoid P5 (slow feedback)

### Research Flags for Phases
- **Phase 1:** Likely needs extra attention to secret rotation audit (P8) — check if Trivy was ever unpinned
- **Phase 3:** Trivy SHA-pinning is critical — double-check versions before implementing
- **Phase 4:** Standard patterns, unlikely to need research — CodeQL setup is well-documented
- **Phase 6:** May need team discussion on enforcement thresholds (what severity blocks merge?)

---

## Risk Register

| Risk | Severity | Mitigation |
|------|----------|------------|
| Trivy supply chain compromise | CRITICAL | SHA-pin to v0.69.3, rotate all pipeline secrets |
| Scanner false positive fatigue | HIGH | Curated rules, severity filtering, SLAs |
| Pipeline too slow (> 10 min) | HIGH | Parallel execution, path filtering, concurrency control |
| Team bypasses security for speed | HIGH | Block only on CRITICAL/HIGH, warn on MEDIUM/LOW |
| Unpinned action versions | CRITICAL | Dependabot for github-actions ecosystem, SHA-pinning policy |
| Base image CVE noise | MEDIUM | Filter by severity, document allowed CVEs, consider Distroless |
