# Security Overview

Central navigation point for all security documentation in the Onboarding project.

## Quick Links

| Document | Purpose | Audience |
|----------|---------|----------|
| [Security Policy](../.github/SECURITY.md) | Vulnerability reporting, supported versions, response timeline | All contributors, security researchers |
| [Security Runbook](security-runbook.md) | Daily operations, alert response, false positive handling, escalation | Developers, tech leads |
| [Branch Protection](branch-protection.md) | CI gating setup, required status checks, GitHub UI instructions | Repository administrators |
| [IaC Policies](iac-policies.md) | Docker Compose + K8s security rules, suppression workflow | Infrastructure developers |
| [Secrets Incident Response](secrets-incident-response.md) | Secret revocation, credential rotation, escalation procedures | All contributors |
| [Compliance Mapping](compliance-mapping.md) | OWASP Top 10, LGPD, CIS Docker Benchmark mapping | Compliance officers, auditors |
| [Security Audit Checklist](security-audit-checklist.md) | PR review, onboarding, quarterly review, pre-release checks | PR reviewers, new contributors |

## CI Security Pipeline

This project runs **12 independent security checks** on every push and pull request:

| # | Job | Category | Tool | Blocks Merge |
|---|-----|----------|------|--------------|
| 1 | Backend (.NET 10) | Build/Test | coverlet.msbuild | Coverage < 80% |
| 2 | Frontend Client (Vinxi) | Frontend | tsc + eslint + build | Any failure |
| 3 | Frontend Backoffice (Vinxi) | Frontend | tsc + eslint + build | Any failure |
| 4 | SAST — Semgrep | SAST | Semgrep custom + registry rules | ERROR findings |
| 5 | SAST — CodeQL | SAST | Dataflow/taint analysis | Security alerts |
| 6 | SCA — Trivy | SCA | Trivy filesystem scan | CRITICAL/HIGH CVEs |
| 7 | Container Scan — Trivy Image | Container | Trivy image scan | CRITICAL/HIGH CVEs |
| 8 | Container Lint — Dockle | Container | CIS Docker Benchmarks | ERROR findings |
| 9 | IaC — Checkov | IaC | Docker Compose scanning | CRITICAL/HIGH misconfigs |
| 10 | IaC — Kubescape | IaC | K8s manifest scanning | HIGH findings (when active) |
| 11 | Secrets — Gitleaks | Secrets | Pattern-based secret detection | Any detection |
| 12 | Secrets — TruffleHog | Secrets | Active credential verification | Any verified secret |

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
