# Compliance Mapping

This document maps the project's security controls to industry standards and regulatory requirements.

## OWASP Top 10 (2021)

| ID | Category | Tools | Coverage | Notes |
|----|----------|-------|----------|-------|
| **A01** | Broken Access Control | CodeQL, Semgrep | ✅ | Dataflow analysis for authorization bypass, CSRF via `no-missing-csrf` rule |
| **A02** | Cryptographic Failures | Gitleaks, TruffleHog, Semgrep | ✅ | Secret detection (JWT keys, signing keys), `no-hardcoded-credentials` rule |
| **A03** | Injection | CodeQL, Semgrep | ✅ | SQL injection via CodeQL taint analysis, parameterized query enforcement |
| **A04** | Insecure Design | — | ⚠️ | Architecture-level — addressed by DDD design and Keycloak auth |
| **A05** | Security Misconfiguration | Checkov, Dockle, Kubescape | ✅ | Docker Compose checks, CIS Benchmarks, K8s hardening (future) |
| **A06** | Vulnerable and Outdated Components | Trivy, Dependabot | ✅ | Dependency CVE scanning, automated update PRs |
| **A07** | Identification and Authentication Failures | Semgrep, CodeQL | ✅ | Keycloak integration, `no-localstorage-tokens` rule, session management |
| **A08** | Software and Data Integrity Failures | Semgrep, CodeQL | ✅ | `no-insecure-deserialization` rule, CodeQL integrity checks |
| **A09** | Security Logging and Monitoring Failures | — | ⚠️ | Observability (Phase 4) covers logging; alert response in security runbook |
| **A10** | Server-Side Request Forgery (SSRF) | CodeQL | ✅ | CodeQL SSRF queries in `security-extended` suite |

## LGPD (Lei Geral de Proteção de Dados — Lei 13.709/2018)

| Article | Requirement | Security Control | Evidence |
|---------|-------------|-----------------|----------|
| **Art. 46** | Security of personal data | Full CI security pipeline | 12 automated checks on every change |
| **Art. 46** | Protection against unauthorized access | Keycloak auth, RBAC | Admin roles, JWT validation |
| **Art. 46** | Encryption of sensitive data | HTTPS, secure secrets management | No hardcoded secrets (Gitleaks/TruffleHog) |
| **Art. 47** | Incident notification | `docs/secrets-incident-response.md` | Defined revocation + escalation procedures |
| **Art. 48** | Breach communication | Incident response escalation Level 3 | CTO notification for production exposures |
| **Art. 50** | Security policies | `docs/security-runbook.md`, `docs/iac-policies.md` | Documented procedures, regular reviews |

**CPF/CNPJ Protection:**
- Domain Value Objects (`Cpf`, `Cnpj`) enforce validation at the type level
- Semgrep rule `no-raw-cpf-cnpj-comparison` detects string comparison bypasses
- PII is scrubbed from logs and audit snapshots (verified by Semgrep + CodeQL)

## CIS Docker Benchmarks

| Check | Description | Tool | Enforcement |
|-------|-------------|------|-------------|
| **4.1** | Image should use specific tag, not `latest` | Dockle (`CIS-DI-0003`) | CI blocks on ERROR |
| **4.2** | Container should run as non-root | Dockle (`CIS-DI-0001`) | CI warns (MEDIUM) |
| **4.3** | HEALTHCHECK should be present | Dockle (`CIS-DI-0006`) | CI warns (LOW) |
| **4.6** | No `--cap-add ALL` | Checkov (`CKV_DOCKER_4`) | CI blocks on CRITICAL |
| **4.7** | No `privileged: true` | Checkov (`CKV_DOCKER_3`) | CI blocks on CRITICAL |
| **4.9** | No secrets in ENV | Checkov (`CKV_DOCKER_5`) | CI blocks on HIGH |
| **4.10** | No `network_mode: host` | Checkov (`CKV_DOCKER_8`) | CI blocks on HIGH |
| **5.1** | No `ADD` instruction (use COPY) | Dockle (`CIS-DI-0009`) | CI blocks on FATAL |
| **5.15** | No secrets in Dockerfile | Gitleaks | CI blocks on any detection |

## Coverage Summary

| Framework | Coverage | Notes |
|-----------|----------|-------|
| **OWASP Top 10** | 9/10 automated, 1 architectural | A04 (Insecure Design) addressed by DDD architecture |
| **LGPD Art. 46-50** | Full coverage | All requirements mapped to controls |
| **CIS Docker Benchmarks** | 9/9 checks enforced | Automated via Dockle + Checkov + Gitleaks |

## Gaps and Future Improvements

| Gap | Planned Resolution | Timeline |
|-----|-------------------|----------|
| A04: Insecure Design not automated | Architecture review process | Ongoing |
| A09: Logging monitoring not automated | Alert thresholds in Grafana/Loki | Next milestone |
| API rate limiting | Middleware in API layer | Future phase |
| Dependency license scanning | FOSSA or similar tool | Future phase |
| Penetration testing | Third-party assessment | Before production launch |
