# Security Audit Checklist

Checklists for PR reviewers and new contributors.

## PR Review Checklist

Every pull request MUST pass the following checks before merging:

### Automated CI Checks (Required — 11 status checks)

- [ ] `Backend (.NET 10)` — Build succeeds, all tests pass, coverage ≥ 80%
- [ ] `Frontend Client (Vinxi)` — TypeScript OK, ESLint clean, build succeeds
- [ ] `Frontend Backoffice (Vinxi)` — TypeScript OK, ESLint clean, build succeeds
- [ ] `SAST — Semgrep` — No ERROR findings (or dismissed with justification)
- [ ] `SAST — CodeQL` — No critical dataflow findings
- [ ] `SCA — Trivy` — No CRITICAL/HIGH dependency CVEs (or `.trivyignore` documented)
- [ ] `Container Scan — Trivy Image` — No CRITICAL/HIGH image CVEs
- [ ] `Container Lint — Dockle` — No CIS Benchmark ERROR findings
- [ ] `IaC — Checkov` — No CRITICAL/HIGH Compose misconfigs
- [ ] `Secrets — Gitleaks` — No hardcoded secrets detected
- [ ] `Secrets — TruffleHog` — No active credentials verified

### Manual Review Items

- [ ] **No new suppressions without justification**
  - `// nosem:` comments include reason
  - `.gitleaksignore` additions have documented fingerprints
  - `.trivyignore` entries explain why CVE is acceptable
- [ ] **No secrets committed**
  - Check for `.env` file changes (should be gitignored)
  - Verify connection strings use config variables, not hardcoded values
  - Confirm test fixtures use mock credentials (not real ones)
- [ ] **Input validation on new endpoints**
  - New controllers/validators use FluentValidation
  - CPF/CNPJ handled via Value Objects (not raw strings)
  - PII scrubbed from logs and responses
- [ ] **Authorization enforced**
  - New endpoints have `[Authorize]` attributes
  - Admin endpoints require `admin` role
  - CSRF validation on POST/PUT/DELETE (or documented exception)
- [ ] **Dependencies updated intentionally**
  - Dependabot PRs reviewed for breaking changes
  - No unexplained new package additions
- [ ] **Tests cover new functionality**
  - New domain logic has unit tests
  - New endpoints have integration tests
  - Coverage threshold maintained

### Security-Specific Review (for security-related changes)

- [ ] Changes reviewed by tech lead or security champion
- [ ] No new attack surface without documented threat model
- [ ] Cryptographic operations use approved algorithms
- [ ] Error messages don't leak sensitive information
- [ ] Audit logging captures security-relevant events

---

## New Contributor Onboarding Checklist

Before making your first contribution, complete the following:

### Reading

- [ ] Read `CONTRIBUTING.md` — Development setup, code quality, commit messages
- [ ] Read `.github/SECURITY.md` — Security policy, vulnerability reporting
- [ ] Read `docs/security-runbook.md` — Security operations and alert response
- [ ] Read `docs/iac-policies.md` — Infrastructure as Code security rules

### Environment Setup

- [ ] Install development prerequisites (.NET 10, Node.js 22, Docker)
- [ ] Install security tools:
  ```bash
  # Semgrep
  pip install semgrep
  # Gitleaks
  brew install gitleaks  # macOS / winget install gitleaks  # Windows
  ```
- [ ] Clone repository and run local checks:
  ```bash
  dotnet restore Onboarding.slnx
  dotnet build Onboarding.slnx --configuration Release
  dotnet test Onboarding.slnx --configuration Release
  ```

### Security Awareness

- [ ] Understand that **secrets must never be committed** — use `.env` files
- [ ] Know the difference between test fixtures (mock secrets) and real credentials
- [ ] Understand the secret revocation process (`docs/secrets-incident-response.md`)
- [ ] Know where to find security alerts (GitHub Security Tab)
- [ ] Know who to contact for security questions (tech lead / security champion)

### First Contribution

- [ ] Run `semgrep scan --config .semgrep/ --error --metrics off .` locally before first push
- [ ] Run `gitleaks detect --config .gitleaks.toml --source .` locally before first push
- [ ] Open a PR and verify all 11 CI checks pass
- [ ] Ensure PR template checklist is completed (includes security items)

---

## Quarterly Security Review Checklist

Performed on the first Monday of January, April, July, October.

- [ ] Review all suppressions across all tools (`.trivyignore`, `.checkov.yml`, `.gitleaksignore`, `.semgrep/*`)
- [ ] Check if suppressed findings now have fixes available
- [ ] Review Dependabot alert trends (are vulnerability PRs increasing?)
- [ ] Update `docs/compliance-mapping.md` if new requirements apply
- [ ] Review and update `docs/security-runbook.md` if procedures changed
- [ ] Verify branch protection rules are still correctly configured
- [ ] Test local security tools still work with latest versions
- [ ] Document any security incidents from the quarter and lessons learned

---

## Pre-Release Security Checklist

Before any production deployment:

- [ ] All CI checks passing on release branch
- [ ] No open CRITICAL or HIGH findings in GitHub Security Tab
- [ ] Dependabot PRs merged (no known vulnerable dependencies)
- [ ] Penetration testing completed (third-party)
- [ ] Incident response procedures tested (tabletop exercise)
- [ ] Key rotation completed (JWT, API keys, DB passwords)
- [ ] Security runbook reviewed and updated
- [ ] Compliance mapping reviewed (any new requirements?)
- [ ] Legal/compliance sign-off obtained (LGPD)
