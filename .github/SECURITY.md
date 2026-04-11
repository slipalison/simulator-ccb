# Security Policy

## Supported Versions

| Version | Supported |
|---------|-----------|
| `main` branch | ✅ Yes |
| Feature branches | Best effort |

## Reporting a Vulnerability

Please report security vulnerabilities **privately** through one of these channels:

1. **GitHub Security Advisories** — Go to **Security** → **Advisories** → **New draft security advisory**
2. **Direct message** to repository maintainer

**Do NOT** report vulnerabilities via public issues, pull requests, or discussions.

### What to Include

- Description of the vulnerability
- Steps to reproduce (with code examples if possible)
- Potential impact (data exposure, unauthorized access, etc.)
- Affected component (API, frontend, infrastructure, etc.)
- Suggested fix (if any)

### Response Timeline

| Stage | Timeline | Action |
|-------|----------|--------|
| **Acknowledgment** | Within 48 hours | Confirm receipt and initial assessment |
| **Assessment** | Within 1 week | Determine severity and scope |
| **Fix** | Within 2 weeks for critical issues | Develop and test remediation |
| **Public disclosure** | After fix deployed | Publish advisory, credit reporter |

## Security Measures

This project uses automated security scanning in CI/CD. Every pull request runs the following checks:

| Tool | Category | Scans For | Blocks Merge |
|------|----------|-----------|--------------|
| **Semgrep** | SAST | Code patterns (XSS, CSRF, insecure deserialization, hardcoded creds) | ✅ ERROR findings |
| **CodeQL** | SAST | Dataflow and taint analysis (injection, path traversal, etc.) | ✅ Alerts |
| **Trivy (fs)** | SCA | Dependency vulnerabilities (CVEs in NuGet, npm packages) | ✅ CRITICAL/HIGH |
| **Trivy (image)** | Container | Container image layer vulnerabilities | ✅ CRITICAL/HIGH |
| **Dockle** | Container | Docker image best practices (CIS Benchmarks) | ✅ ERROR findings |
| **Checkov** | IaC | Docker Compose misconfigurations (privileged mode, secrets, caps) | ✅ CRITICAL/HIGH |
| **Gitleaks** | Secrets | Hardcoded secrets via pattern matching (full git history) | ✅ Any detection |
| **TruffleHog** | Secrets | Active credential verification (confirms secrets are valid) | ✅ Any verified secret |
| **Dependabot** | SCA | Automated dependency update PRs for vulnerable packages | ⚠️ Advisory |

### SARIF Categories in GitHub Security Tab

All security findings appear in **GitHub Security Tab** → **Code scanning alerts**, categorized by tool:

| Category | Tool | Description |
|----------|------|-------------|
| `semgrep` | Semgrep | Custom + registry rule findings |
| `codeql` | CodeQL | Dataflow/taint analysis results |
| `trivy` | Trivy (fs) | Dependency CVEs |
| `trivy-image` | Trivy (image) | Container layer CVEs |
| `checkov` | Checkov | IaC misconfigurations |

Secret scanning alerts (Gitleaks/TruffleHog) appear under **Secret scanning alerts** in the Security Tab.

## Security Best Practices for Contributors

1. **Never commit secrets** — Use `.env` files (gitignored) for local development
2. **Use Value Objects** — CPF, CNPJ, and other sensitive data should use domain VOs
3. **Validate input** — All user input must be validated server-side
4. **Use parameterized queries** — EF Core handles this, but raw SQL must use parameters
5. **Run security scans locally** before pushing — see `CONTRIBUTING.md`

## Compliance

This project follows security best practices aligned with:

- **OWASP Top 10** — Web application security risks
- **LGPD (Lei Geral de Proteção de Dados)** — Brazilian data protection law
- **CIS Docker Benchmarks** — Container security hardening
- **NSA Kubernetes Hardening Guide** — Future K8s deployment security
