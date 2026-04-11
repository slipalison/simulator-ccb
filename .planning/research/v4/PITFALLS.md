# CI/CD Pipeline + Cybersecurity — Pitfalls Research

**Domain:** Secure CI/CD for .NET 10 + React/Vinxi Monorepo
**Researched:** 2026-04-10
**Context:** v4.0 milestone — What do teams get wrong when adding security scanning?

---

## Pitfalls Catalog

### P1: Supply Chain Attack via Unpinned Actions
**Severity:** 🔴 CRITICAL — Can compromise entire pipeline

**What happens:** Teams use `@main`, `@latest`, or unpinned action versions. Attackers compromise popular actions (Trivy March 2026: malicious versions stole SSH keys, cloud tokens, K8s configs, crypto wallets from 75 hijacked tags).

**Warning signs:**
- Workflow YAML uses `uses: some/action@main` or `@master`
- No SHA pinning (`@sha256:abc123`) in any action reference
- Actions with high download counts but no verified publisher badge

**Prevention strategy:**
- **Always pin to SHA or specific version tag:** `aquasecurity/trivy-action@57a97c7` (not `@master`)
- Audit all existing action references after Trivy incident
- Use Dependabot for `github-actions` ecosystem to get version update PRs
- **CRITICAL:** If Trivy was ever run unpinned, rotate ALL secrets the pipeline had access to

**Phase to address:** Phase 1 (foundation) — this must be right from day 1

**Impact on developer velocity:** None (pinning is a one-time config decision)

---

### P2: Scanner Overload — Too Many Tools on Day 1
**Severity:** 🔴 CRITICAL — Kills developer velocity, team abandons pipeline

**What happens:** Team adds 10+ scanners simultaneously. Pipeline takes 30 minutes. Hundreds of false positives. Developers start ignoring security alerts. Eventually someone disables scanning to "unblock" a release.

**Warning signs:**
- Pipeline duration > 10 minutes for PR feedback
- > 50 open security findings in GitHub Security Tab
- Developers complaining about "security blocking releases"
- Security findings with no remediation action for > 2 weeks

**Prevention strategy:**
- **Start with 3 scanners:** Dependabot + Gitleaks + Semgrep (covers deps, secrets, code)
- Add 1-2 scanners per phase, not all at once
- Each new scanner runs in "warn mode" (exit-code: 0) for first 2 weeks to establish baseline
- After baseline, switch to "block mode" (exit-code: 1) for CRITICAL/HIGH only
- Set SLA: CRITICAL findings fixed within 24h, HIGH within 1 week

**Phase to address:** Phased rollout (Phases 1-6 over multiple sprints)

**Impact on developer velocity:** High if done wrong (team abandons pipeline). Low if done right (incremental addition).

---

### P3: False Positive Fatigue
**Severity:** 🟠 HIGH — Developers stop trusting security tools

**What happens:** Scanners flag benign patterns as vulnerabilities. Developer spends 30 min investigating each finding. Most are false positives. Team stops taking security alerts seriously.

**Warning signs:**
- > 30% of security findings are marked "false positive" or "won't fix"
- Developers creating blanket allowlists instead of targeted suppressions
- Security dashboard has 200+ open findings, most stale

**Prevention strategy:**
- **Semgrep CE false positive rate: ~15-20%.** Mitigate by:
  - Using curated rule packs (`p/security-audit` not `p/default`)
  - Adding `.semgrepignore` for test files, generated code
  - Using `# nosemgrep` comments with justification (require PR review for suppressions)
- **Trivy false positive rate: ~10%.** Mitigate by:
  - Filtering to `--severity CRITICAL,HIGH` only
  - Using `.trivyignore` for CVEs that don't affect your attack surface
  - Don't block on base image CVEs that your app doesn't trigger
- **CodeQL false positive rate: ~8-12%.** Mitigate by:
  - Using `security-extended` queries (not `security-and-quality` — too noisy)
  - Running nightly only (not on PRs) to avoid blocking workflow

**Phase to address:** Phase 2 (Semgrep), Phase 4 (CodeQL), Phase 6 (hardening)

**Impact on developer velocity:** High — each false positive costs 15-30 min of investigation time

---

### P4: Base Image CVE Paranoia
**Severity:** 🟠 HIGH — Wastes time on unfixable problems

**What happens:** Trivy reports 400+ CVEs in Alpine/Debian base image. Team spends days trying to fix them. Most are in utilities the app never uses (e.g., `libxml2` CVE in an image that only runs a .NET binary).

**Warning signs:**
- Security report shows hundreds of MEDIUM/LOW CVEs
- CVEs are in system libraries, not application dependencies
- Team debates whether to switch from Alpine to Distroless vs. just accepting the risk

**Prevention strategy:**
- Filter by severity: `--severity CRITICAL,HIGH` (ignores noise)
- Use `.trivyignore` for CVEs that don't affect your attack surface (document why)
- Consider **Distroless images** for production (no shell, no package manager, minimal attack surface)
- **Right answer:** If CVE is in a library your app never calls, it's not a vulnerability — document and ignore

**Phase to address:** Phase 3 (container scanning)

**Impact on developer velocity:** Medium-High — can burn days on unfixable CVEs

---

### P5: Sequential Scanner Execution
**Severity:** 🟡 MEDIUM — Wastes CI minutes, slow feedback

**What happens:** Pipeline runs scanners one after another: build → Semgrep → Gitleaks → Trivy → SARIF upload. Total time: 20+ minutes. Developer loses context waiting for results.

**Warning signs:**
- Workflow YAML has `needs:` chains between scanners that don't depend on each other
- Total pipeline duration > 10 minutes
- Scanners waiting on each other without data dependency

**Prevention strategy:**
- **Run all independent scanners in parallel:** Semgrep, Gitleaks, Trivy FS have no dependencies on each other
- Only chain: Trivy image scan (needs Docker build), SARIF upload (needs scanner results)
- Use `concurrency` with `cancel-in-progress: true` to abort redundant runs on PR updates
- Target: **< 10 min total pipeline duration** for PR feedback

**Phase to address:** Phase 1 (build structure), Phase 7 (SARIF aggregation)

**Impact on developer velocity:** Medium — slow pipelines cause context switching and frustration

---

### P6: DAST in PR Pipeline
**Severity:** 🟡 MEDIUM — Fragile, slow, unreliable

**What happens:** Team adds OWASP ZAP DAST scanning to PR pipeline. ZAP needs the app running. App needs database. Database needs migrations. `sleep 30` isn't enough. Pipeline flakes. Developer rage-quits.

**Warning signs:**
- Workflow has `sleep 30` or `sleep 60` before DAST step
- DAST step flakes intermittently ("sometimes it works, sometimes it doesn't")
- DAST requires spinning up PostgreSQL, Keycloak, etc. in CI

**Prevention strategy:**
- **DAST belongs in staging or nightly workflows, NOT PR gates**
- PR gates should be fast, deterministic, and reliable
- Run DAST against a deployed staging environment instead
- If DAST must run in CI: use health checks, not `sleep`

**Phase to address:** Out of scope for v4.0 (DAST is a v5+ concern)

**Impact on developer velocity:** High — fragile DAST blocks merges unpredictably

---

### P7: Security Findings Without Remediation Guidance
**Severity:** 🟡 MEDIUM — Developers don't know how to fix issues

**What happens:** Scanner reports "SQL Injection risk" at line 42. Developer doesn't know what the fix should be. Asks in team chat. No one knows. Finding sits open for months.

**Warning signs:**
- Security findings without links to documentation
- Generic error messages ("Potential vulnerability detected")
- No team knowledge base for common findings

**Prevention strategy:**
- Use scanners that output SARIF format (includes remediation links by spec)
- Create a `SECURITY.md` in repo with common finding remediation guides
- Require PR review for `# nosemgrep` suppressions (forces knowledge sharing)
- Schedule monthly "security triage" session (30 min, team reviews open findings)

**Phase to address:** Phase 7 (SARIF centralization + documentation)

**Impact on developer velocity:** Medium — unactionable findings waste investigation time

---

### P8: Not Rotating Secrets After Trivy Incident
**Severity:** 🔴 CRITICAL — Assumes pipeline was never compromised

**What happens:** Team had unpinned Trivy action before March 2026. Malicious version ran. Pipeline secrets (GitHub tokens, deploy keys, cloud credentials) were exfiltrated. Team updates Trivy version but doesn't rotate secrets. Attacker still has access.

**Warning signs:**
- Trivy or any action was unpinned during the compromise window
- Secrets used in pipeline haven't been rotated since the incident
- No audit of what secrets were available to compromised actions

**Prevention strategy:**
- **If Trivy ran unpinned at any point: rotate ALL pipeline secrets immediately**
- Review GitHub Actions logs for suspicious activity during compromise window
- Block C2 domain: `scan.aquasecurtiy.org` and IP `45.148.10.212`
- Verify Trivy binaries with `cosign` (signature must be before 2026-03-19)
- Adopt principle: assume compromise, rotate everything

**Phase to address:** Phase 1 (immediate — before adding any scanners)

**Impact on developer velocity:** Low (one-time rotation effort) but critical for security

---

### P9: Ignoring Transitive Dependency Vulnerabilities
**Severity:** 🟠 HIGH — Supply chain attacks come through transitive deps

**What happens:** Team audits direct dependencies but ignores transitive ones. A transitive dependency (`log4j`-style) has a critical CVE. Team doesn't know it's even in their dependency tree.

**Warning signs:**
- Only running `npm audit` without `--production` flag (misses dev deps that bundle into prod)
- Not running `dotnet list package --vulnerable`
- Dependabot configured but ignoring transitive dependencies

**Prevention strategy:**
- **Dependabot catches transitive deps automatically** — ensure it's enabled for all ecosystems
- Trivy filesystem scanning catches transitive vulnerabilities in lockfiles
- Run `npm audit --audit-level=high` (not just `npm audit`)
- Run `dotnet list package --vulnerable` in CI

**Phase to address:** Phase 1 (Dependabot), Phase 3 (Trivy FS)

**Impact on developer velocity:** Low (automated detection)

---

### P10: No Branch Protection After CI Setup
**Severity:** 🟠 HIGH — CI exists but can be bypassed

**What happens:** Team builds comprehensive CI pipeline but forgets to enable branch protection. Developer can force-push to main or merge without passing CI. Security scanning is optional = ignored.

**Warning signs:**
- GitHub repo settings don't require status checks to pass
- "Require branches to be up to date before merging" is unchecked
- No admin-protected branches (admins can bypass)

**Prevention strategy:**
- **Enable branch protection immediately after Phase 7 (SARIF upload works):**
  - Require pull request reviews before merging
  - Require status checks to pass (select all CI jobs)
  - Require branches to be up to date before merging
  - Include administrators (no exceptions)
  - Restrict pushes (only via PRs)

**Phase to address:** Phase 8 (hardening)

**Impact on developer velocity:** None — just enforces existing CI discipline

---

## Pitfall Severity Summary

| ID | Pitfall | Severity | Developer Velocity Impact | Phase |
|----|---------|----------|--------------------------|-------|
| P1 | Unpinned actions (supply chain) | 🔴 CRITICAL | None (one-time config) | Phase 1 |
| P2 | Scanner overload (too many day 1) | 🔴 CRITICAL | High (team abandons pipeline) | Phases 1-6 |
| P3 | False positive fatigue | 🟠 HIGH | High (15-30 min per FP) | Phases 2-6 |
| P4 | Base image CVE paranoia | 🟠 HIGH | Medium-High (days on unfixable) | Phase 3 |
| P5 | Sequential scanner execution | 🟡 MEDIUM | Medium (slow feedback) | Phases 1, 7 |
| P6 | DAST in PR pipeline | 🟡 MEDIUM | High (fragile blocks) | Out of scope v4 |
| P7 | No remediation guidance | 🟡 MEDIUM | Medium (investigation time) | Phase 7 |
| P8 | Not rotating secrets post-Trivy | 🔴 CRITICAL | Low (one-time effort) | Phase 1 |
| P9 | Ignoring transitive deps | 🟠 HIGH | Low (automated) | Phases 1, 3 |
| P10 | No branch protection | 🟠 HIGH | None (enforcement only) | Phase 8 |

---

## Balance: Security vs Developer Experience (Small Team)

**For a small team (1-3 devs), the right balance is:**

| Principle | Implementation |
|-----------|---------------|
| **Fast feedback** | Semgrep on PRs (~10s), not CodeQL (30+ min) |
| **Block only on critical** | CRITICAL/HIGH block merge, MEDIUM/LOW warn |
| **Automate the boring stuff** | Dependabot auto-creates update PRs |
| **Don't boil the ocean** | 3 scanners first, add more as team matures |
| **Security as enabler, not blocker** | Scanners help find bugs, not prevent shipping |
| **Scheduled deep scans** | CodeQL nightly, not on every PR |
| **Document once, reference always** | `SECURITY.md` with common finding fixes |

**What NOT to optimize for:**
- ❌ 100% CVE coverage (diminishing returns, most CVEs don't affect you)
- ❌ Zero false positives (impossible, tune to < 20%)
- ❌ Scanning everything (scan what matters, ignore noise)
- ❌ Perfect compliance on day 1 (mature over phases, not all at once)
