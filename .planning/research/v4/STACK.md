# CI/CD Pipeline + Cybersecurity — Stack Research

**Domain:** Secure CI/CD for .NET 10 + React/Vinxi Monorepo
**Researched:** 2026-04-10
**Context:** v4.0 milestone — GitHub Actions with parallel builds + comprehensive security scanning

---

## Stack Consensus

### CI/CD Platform

| Component | Package/Action | Version | Rationale | Confidence | Tier |
|-----------|---------------|---------|-----------|------------|------|
| CI/CD Platform | GitHub Actions | Native | Built into repo, parallel matrix support, SARIF integration | HIGH | Free (public repos) |
| Checkout | actions/checkout | v4 | Standard checkout, requires `fetch-depth: 0` for secrets scanning | HIGH | Free |
| .NET Setup | actions/setup-dotnet | v4 | Native .NET 10 support, lockfile-based caching | HIGH | Free |
| Node Setup | actions/setup-node | v4 | Native Node 22+ support, pnpm/npm caching | HIGH | Free |
| Path Filter | dorny/paths-filter | v3 | Skip unaffected jobs in monorepo (backend vs frontend vs infra) | HIGH | Free |
| Concurrency | native (`concurrency:`) | Native | Cancel redundant PR runs with `cancel-in-progress: true` | HIGH | Free |
| SARIF Upload | github/codeql-action/upload-sarif | v3 | Centralized security findings in GitHub Security Tab | HIGH | Free |

### SAST (Static Application Security Testing)

| Component | Package/Action | Version | Rationale | Confidence | Tier |
|-----------|---------------|---------|-----------|------------|------|
| Primary SAST (.NET) | github/codeql-action | v3 | Best F1 score (74.4%) for C#, GitHub-native, security-extended queries | HIGH | Free (public), $30/mo/committer (private) |
| Fast SAST (PR gate) | semgrep/semgrep-action | v1 | ~10s scans, 150MB RAM, easy YAML rules, good for PR feedback loop | HIGH | Free (10 collaborators/50 repos), $30/mo beyond |
| Semgrep Config | `p/security-audit p/secrets p/owasp-top-ten` | — | Pre-built rule packs covering OWASP, secrets, common vulns | HIGH | Free |

**Key finding:** Use **both** — Semgrep in PRs for instant feedback (< 10s), CodeQL in nightly scheduled scans for deep semantic analysis (30+ min runs). This is the industry-standard pattern for balancing speed vs depth.

**⚠️ CRITICAL — Trivy Supply Chain Attack (March 2026):**
On 2026-03-19, Trivy was compromised via credential theft. Malicious versions (v0.69.4-v0.70.0) stole SSH keys, cloud tokens, K8s configs, crypto wallets. **Only use Trivy v0.69.3 or earlier, pinned by SHA:**
- `trivy-action@57a97c7` (safe version: v0.35.0)
- CLI: `v0.69.3` or earlier
- `setup-trivy@3fb12ec` (safe version: v0.2.6)

### SCA (Software Composition Analysis)

| Component | Package/Action | Version | Rationale | Confidence | Tier |
|-----------|---------------|---------|-----------|------------|------|
| Dependency Updates | Dependabot (native) | Native | Zero-config, weekly updates, PR-based updates, grouping support | HIGH | Free |
| Dependency Scanning | aquasecurity/trivy-action | v0.35.0 (SHA: `57a97c7`) | Scans filesystem dependencies, outputs SARIF, severity filtering | HIGH | Free, open source |
| OWASP Dep Check | dependency-check/Dependency-Check_Action | main | Additional CVE database, `--failOnCVSS 7` threshold | MEDIUM | Free, open source |
| npm audit | npm ci + npm audit | Native | Quick check for high-severity npm vulns, `--audit-level=high` | HIGH | Free |

### Container Scanning

| Component | Package/Action | Version | Rationale | Confidence | Tier |
|-----------|---------------|---------|-----------|------------|------|
| Image Scanning | aquasecurity/trivy-action | v0.35.0 (SHA: `57a97c7`) | Industry standard for container image scanning, SARIF output | HIGH | Free, open source |
| Docker Best Practices | aquasecurity/dockle | v0.4.x (CLI) | Checks Dockerfile best practices (USER, HEALTHCHECK, secrets in ENV) | MEDIUM | Free, open source |

### IaC Scanning

| Component | Package/Action | Version | Rationale | Confidence | Tier |
|-----------|---------------|---------|-----------|------------|------|
| IaC Scanning | bridgecrewio/checkov-action | v3.x | Scans Docker Compose, Terraform, K8s manifests, SARIF output | HIGH | Free (open source), paid cloud |
| K8s Scanning | armosec/kubescape | v3.x | Kubernetes security policies, NSA/CISA compliance checks | MEDIUM | Free (open source), paid cloud |

### Secrets Scanning

| Component | Package/Action | Version | Rationale | Confidence | Tier |
|-----------|---------------|---------|-----------|------------|------|
| Primary | gitleaks/gitleaks-action | v2 | Fast, regex-based secret detection, `.gitleaks.toml` config for allowlists | HIGH | Free, open source |
| Secondary | trufflesecurity/trufflehog | main | Active verification of found secrets (not just regex), `--only-verified --fail` | MEDIUM | Free tier, paid enterprise |

### What NOT to Use

| Tool | Why Avoid | Alternative |
|------|-----------|-------------|
| **Trivy > v0.69.3** | Supply chain attack (March 2026) — compromised versions steal secrets | Pin to v0.69.3 or SHA `57a97c7` |
| **SonarQube Community** | Low detection rate (19% in DryRun benchmark vs 46% for Semgrep) | Semgrep or CodeQL |
| **Snyk Free** | Limited to 1 contributor, proprietary | Trivy + Dependabot (open source) |
| **actions/checkout@v3** | Older version, v4 has better perf and security | checkout@v4 |
| **Unpinned action versions** | Vulnerable to supply chain attacks (see Trivy incident) | Always pin to SHA or specific version tags |
| **DAST in PR CI** | OWASP ZAP action needs `sleep 30` — fragile, slow, not reliable for PR gates | DAST in staging/nightly only |
| **FOSSA** | Moved to commercial model, limited free tier | Dependabot + Trivy (free, open source) |

---

## GitHub Actions Workflow Configuration Patterns

### Path-Based Conditional Execution
```yaml
jobs:
  backend-build:
    if: ${{ needs.filter.outputs.backend == 'true' }}
    runs-on: ubuntu-latest
```

### SARIF Integration for All Scanners
```yaml
- uses: github/codeql-action/upload-sarif@v3
  with:
    sarif_file: results.sarif
  if: always()  # Upload even if scan fails
```

### Concurrency Control
```yaml
concurrency:
  group: ci-${{ github.ref }}
  cancel-in-progress: true
```

---

## Sources
- [CI/CD Pipeline Best Practices 2026 — ZTABS](https://ztabs.co/blog/ci-cd-pipeline-best-practices)
- [GitHub Actions Security Scanning — OneUptime](https://oneuptime.com/blog/post/2025-12-20-security-scanning-github-actions/view)
- [Semgrep vs CodeQL Comparison 2026 — Konvu](https://konvu.com/compare/semgrep-vs-codeql)
- [Trivy Security Incident #10425 — GitHub](https://github.com/aquasecurity/trivy/discussions/10425)
- [Trivy Compromised Analysis — omedia.dev](https://omedia.dev/blog/trivy-github-actions-compromised-full-malware Payload-analysis)
- [GitHub Actions Security Scanner Analysis — arXiv](https://arxiv.org/html/2601.14455v1)
