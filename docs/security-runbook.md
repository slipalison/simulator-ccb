# Security Runbook

## SAST Pipeline Overview

| Tool | What It Finds | CI Behavior |
|------|--------------|-------------|
| **Semgrep** | Pattern-based issues: localStorage tokens, hardcoded creds, missing CSRF, insecure deserialization | ERROR → blocks merge. WARNING → Security Tab only |
| **CodeQL** | Dataflow/taint analysis: SQL injection, XSS, path traversal, weak crypto | Findings posted to Security Tab. Branch protection can block merge |

## Alert Triage Workflow

### 1. View Alerts

Go to **GitHub → Security → Code scanning alerts**

### 2. Review Each Alert

For each alert, determine:

- **Real finding**: Actual vulnerability in the code
- **False positive**: Rule incorrectly flagged safe code
- **Test-only**: Vulnerability exists only in test code (acceptable)

### 3. Dismiss False Positives

1. Click the alert
2. Click **"Dismiss alert"**
3. Select reason:
   - **False positive** — rule incorrectly flagged
   - **Used in tests** — intentional for test code
   - **Won't fix** — accepted risk with documented justification
4. **Add a comment** explaining the dismissal reason

Dismissed alerts stay dismissed across future scans (tracked by code fingerprint).

### 4. Fix Real Findings

1. Create a branch to fix the issue
2. Apply the remediation from the alert description
3. Open PR — SAST checks must pass
4. Merge

## Custom Semgrep Rules

### Current Rules

| Rule ID | Severity | Detects |
|---------|----------|---------|
| `no-localstorage-tokens` | ERROR | `localStorage` with token/auth/session keys |
| `no-dangerously-set-inner-html` | ERROR | `dangerouslySetInnerHTML` in React |
| `no-hardcoded-credentials` | ERROR | Connection strings with passwords, API key patterns |
| `no-missing-csrf` | ERROR | `[HttpPost]` without `[ValidateAntiForgeryToken]` |
| `no-raw-cpf-cnpj-comparison` | WARNING | String comparison of 11/14-digit CPF/CNPJ |
| `no-insecure-deserialization` | ERROR | `BinaryFormatter`, `TypeNameHandling.Auto/All` |

### Suppressing Findings

Use `// nosem: rule-id` with justification:

```csharp
// nosem: no-missing-csrf — Stateless JWT API, no session cookies
[HttpPost]
public IActionResult Webhook(...) { }
```

Every suppression **MUST** have a reason. Enforced via PR review.

## Branch Protection

### Required Status Checks

The `main` branch requires these CI jobs to pass before merge:

| Job | Purpose |
|-----|---------|
| `Backend (.NET 10)` | Build + test + 80% coverage |
| `Frontend Client (Vinxi)` | tsc + eslint + build |
| `Frontend Backoffice (Vinxi)` | tsc + eslint + build |
| `SAST — Semgrep` | Pattern-based security scan |
| `SAST — CodeQL` | Dataflow/taint analysis |

### Setup Instructions (Admin Only)

1. Go to **Settings → Branches → Branch protection rules**
2. Edit the `main` branch rule
3. Under **"Require status checks to pass before merging"**:
   - Enable the checkbox
   - Search and add: `SAST — Semgrep`
   - Search and add: `SAST — CodeQL`
   - Also add: `Backend (.NET 10)`, `Frontend Client (Vinxi)`, `Frontend Backoffice (Vinxi)`
4. Enable **"Require branches to be up to date before merging"**
5. Save

> **Note:** Status checks must have run at least once on `main` before they can be selected. Push the CI workflow to `main` first.

## Weekly Review Cadence

1. Open **GitHub → Security → Code scanning alerts**
2. Review new alerts since last review
3. Dismiss false positives with documented reasons
4. Create issues for real findings
5. Track open findings in project board or issue labels

## Incident Response

### If a Secret Is Committed

1. **Revoke immediately** — rotate the key/token/password
2. **Remove from git history** — `git filter-branch` or `git filter-repo`
3. **Run TruffleHog** — verify no other secrets in history:
   ```bash
   trufflehog filesystem --directory . --only-verified
   ```
4. **Add to `.gitignore`** and `.semgrepignore` if needed
5. **Document** in this runbook what happened and how it was fixed

### If a Critical Vulnerability Is Found

1. **Stop merges** — enable branch protection if not already
2. **Create issue** with severity, description, affected files
3. **Fix in priority branch** — fix → test → PR → merge
4. **Backport** if fix was on a separate branch
5. **Review SAST rules** — add a custom Semgrep rule if the pattern wasn't caught
