# CI/CD Pipeline + Cybersecurity — Features Research

**Domain:** Secure CI/CD for .NET 10 + React/Vinxi Monorepo
**Researched:** 2026-04-10
**Context:** v4.0 milestone — What security features belong in a mature pipeline?

---

## Feature Landscape

### Table Stakes (Must Have — Security is Compromised Without These)

| Feature | Description | Complexity | False Positive Rate | Integration Effort | Why Mandatory |
|---------|-------------|------------|---------------------|-------------------|---------------|
| **Dependabot** | Automated dependency updates with PRs | Low | ~0% (deterministic) | 5 min YAML config | Unpatched dependencies are the #1 attack vector |
| **Secrets Scanning (Gitleaks)** | Detect committed API keys, tokens, passwords | Low | ~5% (tunable via allowlists) | 15 min setup | Leaked secrets = immediate breach. Non-negotiable. |
| **npm audit / dotnet audit** | Built-in package vulnerability scanning | Low | ~2% | Zero (built into tooling) | Catches known vulns in direct/transitive deps |
| **SAST on PR (Semgrep)** | Fast static analysis on every PR | Low-Medium | ~15-20% (CE version) | 30 min config | Catches injection, XSS, auth bypass before merge |
| **Build Validation** | Code must compile before any merge | Low | 0% | Already in place | Broken builds block everything downstream |
| **Test Execution** | All unit tests pass on PR | Low | 0% (if tests are deterministic) | Already in place | Regression prevention |
| **Branch Protection** | Require CI pass before merge | Low | 0% | 10 min in repo settings | Without this, CI is optional = ignored |
| **Docker Image Scan (Trivy v0.69.3)** | Scan production images for CVEs | Medium | ~10% (base image CVEs may not affect app) | 1 hr setup | Container vulns can escalate to host access |

### Differentiators (Extra Confidence, Compliance-Ready)

| Feature | Description | Complexity | False Positive Rate | Integration Effort | Why Add It |
|---------|-------------|------------|---------------------|-------------------|------------|
| **CodeQL (nightly)** | Deep semantic SAST analysis | Medium-High | ~8-12% (security-extended queries) | 2-3 hr setup | Best F1 score for C# (74.4%), FedRAMP-ready, required for some compliance frameworks |
| **Checkov IaC Scan** | Scan compose.yaml, future K8s manifests | Medium | ~15% (some rules too strict) | 1 hr setup | Catches misconfigured infra (exposed ports, no resource limits, privileged containers) |
| **Dockle** | Dockerfile best practices checker | Low | ~5% | 30 min setup | Prevents anti-patterns: running as root, no HEALTHCHECK, secrets in ENV |
| **Path-Based Filtering** | Skip unaffected jobs in monorepo | Low | 0% | 30 min setup | Critical for monorepo — avoids wasting CI minutes on unchanged projects |
| **SARIF Centralization** | All scanners report to GitHub Security Tab | Medium | N/A (aggregator) | 1-2 hr per scanner | Single pane of glass for security findings, trend tracking, compliance audits |
| **Test Coverage Gate** | Block PR if coverage drops below threshold | Low | 0% (if thresholds are reasonable) | 30 min setup | Prevents coverage erosion over time |
| **Concurrency Control** | Cancel redundant PR runs | Low | 0% | 5 min YAML | Saves CI minutes, faster feedback on updated PRs |

### Anti-Features (Deliberately NOT Do These)

| Anti-Feature | Why Avoid | Better Alternative |
|--------------|-----------|-------------------|
| **Block PR on LOW-severity findings** | Creates noise fatigue, developers bypass security | Block only on CRITICAL/HIGH, warn on MEDIUM/LOW |
| **Run DAST (OWASP ZAP) in PR CI** | Requires `sleep 30` for app startup — fragile, adds 2-5 min | Run DAST in staging environment or nightly scheduled workflow |
| **Use `@main` or `@latest` for security actions** | Supply chain attack vector (Trivy March 2026 proved this) | Always pin to specific version tags or SHA commits |
| **Scan every commit to feature branches** | Wastes CI minutes, developers push WIP commits | Only scan PRs targeting main/develop, or use push triggers on main only |
| **Run all scanners sequentially** | Pipeline becomes 15-30 min, developers lose context | Run scanners in parallel, aggregate results at end |
| **Treat base image CVEs as blocking** | Alpine/Debian base images have hundreds of CVEs that don't affect your app | Filter by `--severity CRITICAL,HIGH` and only block if CVE affects your dependency tree |
| **Add 10+ scanners on day 1** | Overwhelming false positive noise, team abandons pipeline | Start with Dependabot + Gitleaks + Semgrep, add scanners incrementally |
| **Security scan without fix guidance** | Developers don't know how to remediate findings | Every scanner must link to remediation docs (SARIF includes this) |

---

## Feature Dependencies

```
Dependabot ──────────────────────────────────┐
npm/dotnet audit ────────────────────────────┤
                                              ├──→ SAST (Semgrep + CodeQL) ──→ SARIF Upload
Gitleaks ────────────────────────────────────┤
                                              │
Trivy (filesystem) ──────────────────────────┤
                                              ├──→ GitHub Security Tab
Checkov ─────────────────────────────────────┤
                                              │
Trivy (image) ──requires── Docker build ─────┘

Branch Protection ──requires── CI pass (all above)
```

**Execution order:**
1. **Parallel track A:** Build backend + Build frontend client + Build frontend backoffice
2. **Parallel track B:** Dependabot (passive) + Gitleaks + npm audit + dotnet audit
3. **After build:** Semgrep (fast, ~10s) + Trivy filesystem
4. **After Docker build:** Trivy image scan + Dockle
5. **After all above:** SARIF upload (if `always()`)
6. **Nightly (separate workflow):** CodeQL deep scan + Checkov + Kubescape

---

## Compliance Notes (LGPD)

| Requirement | How CI/CD Addresses It |
|-------------|----------------------|
| **Data protection by design** | SAST catches insecure data handling patterns |
| **Access control** | Secrets scanning prevents credential leaks |
| **Audit trail** | GitHub Security Tab retains scan history, PR-based reviews |
| **Incident response** | Dependabot auto-creates PRs for vulnerable deps |
| **Documentation** | SARIF reports serve as compliance evidence |

---

## Recommended Phased Rollout

| Phase | Scanners Added | Rationale |
|-------|---------------|-----------|
| **Phase 1: Foundation** | Dependabot, Gitleaks, npm/dotnet audit | Zero-to-minimal friction, catches biggest risks immediately |
| **Phase 2: SAST** | Semgrep on PRs | Fast feedback, catches code-level vulns |
| **Phase 3: Container** | Trivy image scan (pinned SHA), Dockle | Catches infra-level vulns |
| **Phase 4: Deep SAST** | CodeQL nightly | Best C# coverage, but too slow for PR gates |
| **Phase 5: IaC** | Checkov, Kubescape | Scans compose.yaml, future K8s manifests |
| **Phase 6: Hardening** | SARIF centralization, branch protection, coverage gates | Polish, compliance, enforcement |
