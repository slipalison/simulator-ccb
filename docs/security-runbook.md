# Security Runbook

Central reference for security operations in the Onboarding project.

## Quick Reference

| What | Where | How |
|------|-------|-----|
| **Security alerts** | GitHub → Security → Code scanning alerts | Review by category |
| **Secret alerts** | GitHub → Security → Secret scanning alerts | Revoke immediately |
| **Dependency alerts** | GitHub → Security → Dependabot alerts | Review and merge PRs |
| **CI status** | GitHub → Actions | Check job logs for failures |
| **Security policy** | `.github/SECURITY.md` | Vulnerability reporting |

## Alert Response by Tool

### Semgrep (SAST — Code Patterns)

**Where:** GitHub Security Tab → Code scanning alerts → `semgrep` category

**Response:**
1. Read the finding: file, line, rule ID, severity
2. If **real issue**: Fix the code pattern (e.g., add `[ValidateAntiForgeryToken]`)
3. If **false positive**: Add `// nosem: rule-id` with justification
4. If **acceptable risk**: Dismiss as "Won't fix" with documented reason

**Common findings:**
- `no-missing-csrf`: Add `[ValidateAntiForgeryToken]` to POST endpoints
- `no-hardcoded-credentials`: Move secrets to environment variables or Key Vault
- `no-localstorage-tokens`: Use httpOnly cookies instead of localStorage

**Local check:** `semgrep scan --config .semgrep/ --error --metrics off .`

---

### CodeQL (SAST — Dataflow/Taint)

**Where:** GitHub Security Tab → Code scanning alerts → `codeql` category

**Response:**
1. Review the dataflow path: source → sink
2. If **real vulnerability**: Add input validation, parameterized queries, or sanitization
3. If **false positive**: Dismiss with reason (e.g., "input is already validated upstream")

**Common findings:**
- SQL injection: Use EF Core LINQ (not raw SQL) or parameterized queries
- XSS: Sanitize user input before rendering, use proper encoding
- Path traversal: Validate file paths against allowed directories

**Note:** CodeQL `security-extended` may produce lower-confidence findings. Review carefully before dismissing.

---

### Trivy (SCA — Dependency CVEs)

**Where:** GitHub Security Tab → Code scanning alerts → `trivy` category

**Response:**
1. Check the CVE: severity, affected package, fix version
2. **Update the dependency** to the patched version
3. If **no fix available**: Add to `.trivyignore` with justification and monitor
4. If **transitive dependency**: Update the parent package or wait for upstream fix

**Local check:** `docker run --rm -v $(pwd):/root aquasec/trivy fs --scanners vuln --severity CRITICAL,HIGH .`

---

### Trivy Image (Container CVEs)

**Where:** GitHub Security Tab → Code scanning alerts → `trivy-image` category

**Response:**
1. Check the CVE: which OS package or application layer is affected
2. **Update the base image** (e.g., `postgres:16-alpine` → newer patch version)
3. **Rebuild the Docker image** — new layers may pull fixed packages
4. If **no fix available**: Document acceptance of risk in `.trivyignore`

**Note:** Container CVEs are often OS-level (Alpine, Debian) and resolve when base images are updated.

---

### Dockle (Container Lint)

**Where:** GitHub Actions logs → `Container Lint — Dockle` job

**Response:**
1. Read the CIS Benchmark check ID and description
2. Fix the Dockerfile or compose.yaml (e.g., add `HEALTHCHECK`, remove `ADD`)
3. If **acceptable deviation**: Document in `docs/iac-policies.md`

**Common findings:**
- `CIS-DI-0001`: Add `USER nonroot:nonroot` to Dockerfile
- `CIS-DI-0006`: Add `HEALTHCHECK` instruction
- `CIS-DI-0003`: Replace `latest` tag with specific version

---

### Checkov (IaC — Compose/Dockerfile)

**Where:** GitHub Security Tab → Code scanning alerts → `checkov` category

**Response:**
1. Check the CKV_DOCKER ID and description
2. Fix the `compose.yaml` misconfiguration
3. If **suppression needed**: Add to `.checkov.yml` skip-check list with justification

**Common findings:**
- `CKV_DOCKER_3`: Remove `privileged: true` (use specific capabilities)
- `CKV_DOCKER_5`: Move secrets from ENV to `.env` file
- `CKV_DOCKER_6`: Add memory limits to services

---

### Gitleaks (Secrets — Pattern)

**Where:** GitHub Security Tab → Secret scanning alerts **AND** CI failure

**Response (IMMEDIATE):**
1. **Revoke the secret** — it is compromised
2. **Rotate the credential** — generate new secret, update all consumers
3. **Remove from git history** if merged to main (BFG Repo-Cleaner)
4. **Document the incident** — see `docs/secrets-incident-response.md`

**DO NOT just remove the line from code.** The secret must be rotated.

**Local check:** `gitleaks detect --config .gitleaks.toml --source . --verbose`

---

### TruffleHog (Secrets — Active Verification)

**Where:** GitHub Security Tab → Secret scanning alerts **AND** CI failure

**Response:**
1. TruffleHog only reports **verified active** secrets (credential worked)
2. **Revoke immediately** — this is a confirmed exposure
3. Follow incident response: `docs/secrets-incident-response.md`
4. Root cause: How was the active secret committed?

**Local check:** `trufflehog filesystem --directory . --only-verified --fail`

---

## False Positive Handling

### When is something a false positive?

- **Test fixtures**: Mock credentials in test code (e.g., `"test-secret-123"` in `*Tests.cs`)
- **Documentation**: Example code in README or docs (clearly marked as examples)
- **Already mitigated**: The finding is valid but the risk is controlled by another layer
- **Intentional design**: The pattern is required for functionality (e.g., public webhook endpoint without CSRF)

### How to suppress

| Tool | Suppression Method |
|------|-------------------|
| Semgrep | `// nosem: rule-id — justification` in code |
| CodeQL | Dismiss in GitHub Security Tab with reason |
| Trivy (fs) | Add CVE ID to `.trivyignore` with comment |
| Trivy (image) | Same as above, or update base image |
| Dockle | Document acceptable deviation in `docs/iac-policies.md` |
| Checkov | Add check ID to `.checkov.yml` skip-check list |
| Gitleaks | Add fingerprint to `.gitleaksignore` |
| TruffleHog | No suppression — verified secrets cannot be false positives |

### Quarterly Suppression Review

Every quarter (first Monday of Jan/Apr/Jul/Oct):
1. Review all suppressions across all tools
2. Remove suppressions that are no longer valid
3. Check if suppressed findings now have fixes available
4. Update documentation if patterns have changed

---

## Weekly Security Review

**When:** Monday 10:00 AM, 30 minutes
**Who:** Tech lead + rotating developer

### Agenda

1. **New alerts** (5 min) — Review new findings since last review
2. **Open findings** (10 min) — Status of unresolved alerts
3. **False positives** (5 min) — Dismiss confirmed FPs, update suppressions
4. **Dependabot PRs** (5 min) — Review and merge pending dependency updates
5. **Action items** (5 min) — Assign owners for open findings

### Output

- Updated suppression lists (`.trivyignore`, `.checkov.yml`, `.gitleaksignore`)
- Action items with owners and deadlines
- Security metrics (alerts this week, resolution rate)

---

## Escalation Matrix

| Level | Who | When | Examples |
|-------|-----|------|----------|
| **Level 1** | Developer who wrote the code | Within 24 hours | Semgrep finding in own PR, Dockle lint failure |
| **Level 2** | Tech lead / security champion | Within 48 hours | CodeQL dataflow finding, Checkov CRITICAL, Trivy HIGH CVE with no fix |
| **Level 3** | CTO / security team | Immediately | Production credentials leaked, active secret in public repo, data breach risk |

### Escalation Triggers for Level 3

- Database credentials committed to public repository
- Keycloak admin secret exposed
- JWT signing key leaked (all sessions compromised)
- Any finding with LGPD/GDPR implications
- Third-party API keys exposed publicly

---

## Tool Reference

### Config Files

| Tool | Config File | Location |
|------|-------------|----------|
| Semgrep | `.semgrep/*.yaml`, `.semgrepignore` | Project root |
| CodeQL | `codeql-config.yml` | `.github/codeql/` |
| Trivy (fs) | `.trivyignore` | Project root |
| Checkov | `.checkov.yml` | Project root |
| Gitleaks | `.gitleaks.toml`, `.gitleaksignore` | Project root |

### Local Commands

```bash
# Semgrep
semgrep scan --config .semgrep/ --error --metrics off .

# Gitleaks
gitleaks detect --config .gitleaks.toml --source . --verbose

# TruffleHog
trufflehog filesystem --directory . --only-verified --fail

# Trivy (dependency scan)
docker run --rm -v $(pwd):/root aquasec/trivy fs --scanners vuln --severity CRITICAL,HIGH .

# Checkov
checkov -f compose.yaml --framework dockerfile_compose --compact
```

### Related Documents

- Security Policy: `.github/SECURITY.md`
- IaC Policies: `docs/iac-policies.md`
- Secrets Incident Response: `docs/secrets-incident-response.md`
- Branch Protection: `docs/branch-protection.md`
