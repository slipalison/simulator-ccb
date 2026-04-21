# Security Overview

Central navigation point for all security documentation in the Onboarding project.

## Quick Links

| Document | Purpose | Audience |
|----------|---------|----------|
| [CI Pipeline Architecture](ci-pipeline.md) | Multi-stage CI details, security tool rationale, results location | All contributors, DevOps |
| [Security Policy](../.github/SECURITY.md) | Vulnerability reporting, supported versions, response timeline | All contributors, security researchers |
| [Security Runbook](security-runbook.md) | Daily operations, alert response, false positive handling, escalation | Developers, tech leads |
| [Branch Protection](branch-protection.md) | CI gating setup, required status checks, GitHub UI instructions | Repository administrators |
| [IaC Policies](iac-policies.md) | Docker Compose + K8s security rules, suppression workflow | Infrastructure developers |
| [Secrets Incident Response](secrets-incident-response.md) | Secret revocation, credential rotation, escalation procedures | All contributors |
| [Compliance Mapping](compliance-mapping.md) | OWASP Top 10, LGPD, CIS Docker Benchmark mapping | Compliance officers, auditors |
| [Security Audit Checklist](security-audit-checklist.md) | PR review, onboarding, quarterly review, pre-release checks | PR reviewers, new contributors |

## CI Security Pipeline

This project runs **13 security checks** across a multi-stage pipeline (Build → Tests → Security).

See [CI Pipeline Architecture](ci-pipeline.md) for complete documentation including:
- Why each tool was chosen and what alternatives were rejected
- What each tool detects and what it generates
- Where to find results in the GitHub UI
- Multi-stage dependency diagram and timeline

**Quick reference:**

| # | Job | Category | Tool | Blocks Merge |
|---|-----|----------|------|--------------|
| 1 | Backend › Tests | Build/Test | coverlet.msbuild | Coverage < 80% |
| 2 | Frontend Client › Tests | Frontend | tsc + eslint | Any failure |
| 3 | Frontend Backoffice › Tests | Frontend | tsc + eslint | Any failure |
| 4 | Security › SAST — Semgrep | SAST | Custom + registry rules | ERROR findings |
| 5 | Security › SAST — CodeQL | SAST | Dataflow/taint analysis | Security alerts |
| 6 | Security › SCA — Trivy | SCA | Trivy filesystem | CRITICAL/HIGH CVEs |
| 7 | Security › Container — Trivy | Container | Trivy image scan | CRITICAL/HIGH CVEs |
| 8 | Security › Container — Dockle | Container | CIS Docker Benchmarks | ERROR findings |
| 9 | Security › IaC — Checkov | IaC | Docker Compose scanning | HIGH misconfigs |
| 10 | Security › Secrets — Gitleaks | Secrets | Pattern-based detection | Any detection |
| 11 | Security › Secrets — TruffleHog | Secrets | Active verification | Any verified secret |
| 12 | Security › SBOM — Syft | SBOM | SPDX + CycloneDX | Never (informational) |
| 13 | Security › DAST — OWASP ZAP | DAST | Baseline scan | Never (informational) |

Additionally, **Dependabot** runs weekly to create PRs for dependency updates.

## Security Posture

| Framework | Coverage | Details |
|-----------|----------|---------|
| OWASP Top 10 | 9/10 automated | A04 (Insecure Design) via architecture review |
| LGPD Art. 46-50 | Full coverage | All requirements mapped to automated controls |
| CIS Docker Benchmarks | 9/9 enforced | Dockle + Checkov + Gitleaks |

See [Compliance Mapping](compliance-mapping.md) for detailed mappings.

## Getting Started

- **New contributor**: Start with [Security Audit Checklist](security-audit-checklist.md) → Onboarding section
- **PR reviewer**: Use [Security Audit Checklist](security-audit-checklist.md) → PR Review section
- **Security researcher**: See [Security Policy](../.github/SECURITY.md) for responsible disclosure
- **Ops on-call**: See [Security Runbook](security-runbook.md) for alert response procedures
