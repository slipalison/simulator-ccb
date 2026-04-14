---
status: awaiting_human_verify
trigger: "Trivy SCA scan failing with exit code 1 due to CRITICAL/HIGH vulnerabilities in npm dependencies"
created: 2026-04-14T00:00:00Z
updated: 2026-04-14T11:30:00Z
---

## Current Focus

hypothesis: CONFIRMED — follow-redirects 1.15.11 is affected by GHSA-r4q5-vmmm-2653, published 2026-04-14. Trivy picks it up as it has a fix (1.16.0). npm audit rates it MODERATE but Trivy may classify differently. Dependabot already created separate branch commits for both frontends; those changes need applying here.
test: cherry-pick/replicate the Dependabot fix: bump follow-redirects from 1.15.11 to 1.16.0 in both package-lock.json files
expecting: after fix, follow-redirects reads 1.16.0 in both lock files; Trivy scan passes
next_action: await human verification that CI passes with the updated lock files

## Symptoms

expected: Trivy filesystem scan completes with exit code 0 (no CRITICAL/HIGH unfixed vulnerabilities)
actual: Trivy exits with code 1 — CI step fails, merge blocked
errors: "Error: Process completed with exit code 1" after "Running Trivy with options: trivy fs ."
reproduction: Any push/PR triggers the CI workflow — security-sca-trivy job fails
started: 2026-04-14 (GHSA-r4q5-vmmm-2653 published today; Trivy advisory database updated)

## Eliminated

- hypothesis: NuGet vulnerabilities
  evidence: `dotnet list package --vulnerable` shows all projects clean
  timestamp: 2026-04-14

- hypothesis: node-fetch 2.7.0 or send 0.19.2 or serve-static 1.16.3 or path-to-regexp 6.3.0 are flagged
  evidence: OSV API queries return {} (no vulnerabilities) for all those package+version combinations
  timestamp: 2026-04-14

## Evidence

- timestamp: 2026-04-14
  checked: npm audit --json in frontend/backoffice and frontend/client
  found: Only follow-redirects (MODERATE, GHSA-r4q5-vmmm-2653, range <=1.15.11, fixAvailable: true)
  implication: Single vulnerability; npm rates it MODERATE but Trivy may rate it differently given `ignore-unfixed:true` is set and fix exists at 1.16.0

- timestamp: 2026-04-14
  checked: OSV API for follow-redirects 1.15.11
  found: GHSA-r4q5-vmmm-2653 published 2026-04-14T01:11:11Z, severity MODERATE (CVSS_V4), fixed in 1.16.0, nvd_published_at: null (no CVE yet)
  implication: Brand new advisory published today; Trivy @master pulls latest DB so this scan would be first to detect it

- timestamp: 2026-04-14
  checked: git log for Dependabot commits
  found: Commits 79ec6bd (backoffice) and 00ad7b5 (client) already created by Dependabot, bumping follow-redirects 1.15.11→1.16.0 but on separate Dependabot branches, not merged to backoffice branch
  implication: The fix already exists; just needs to be applied to the current branch

- timestamp: 2026-04-14
  checked: OSV API for node-fetch 2.7.0, send 0.19.2, serve-static 1.16.3, path-to-regexp 6.3.0
  found: All return {} (no known vulnerabilities)
  implication: Only follow-redirects is the culprit

## Resolution

root_cause: follow-redirects 1.15.11 in both frontend/backoffice/package-lock.json and frontend/client/package-lock.json is affected by GHSA-r4q5-vmmm-2653 (published 2026-04-14). Trivy @master picks up advisory DB updates immediately; the advisory has a fix available (1.16.0) so ignore-unfixed:true does NOT exclude it. Trivy's severity classification causes exit-code 1.
fix: Bump follow-redirects from 1.15.11 to 1.16.0 in both package-lock.json files (version field + resolved URL + integrity hash), mirroring Dependabot commits 79ec6bd and 00ad7b5.
verification: npm audit --audit-level=high returns 0 high, 0 critical, 0 moderate in both frontends after the upgrade. OSV confirms follow-redirects 1.16.0 has no known advisories. Awaiting CI confirmation.
files_changed:
  - frontend/backoffice/package-lock.json
  - frontend/client/package-lock.json
