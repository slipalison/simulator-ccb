# Reviewers routing (per-project reviewers, run by /jdi-verify)

3 reviewers — all 3 run on every `/jdi-verify` regardless of phase content. Backend and frontend reviewers are OBLIGATED to execute Playwright regression suite (project mandate — regression testing is NOT optional). Security reviewer plays Playwright OPTIONALLY when relevant to auth/security flow validation.

| Agent | File glob | Trigger | Playwright | Blocks ship? |
|---|---|---|---|---|
| jdi-reviewer-onboarding-keycloak-backend-csharp | `**/*.{cs,csproj,sln,slnx}` | /jdi-verify (always) | MANDATORY (G7) | yes, if BLOCKED |
| jdi-reviewer-onboarding-keycloak-frontend-vinext | `frontend/**/*.{ts,tsx,jsx,js,css,scss,html,mjs,cjs}` | /jdi-verify (always) | MANDATORY (G5 + G6) | yes, if BLOCKED |
| jdi-reviewer-onboarding-keycloak-security | `{.github/workflows/**,.semgrep/**,Dockerfile*,docker-compose*.yml,infra/**,keycloak/**,**/Security/**,**/Permission*,**/Auth*}` | /jdi-verify (always) | optional | yes, if BLOCKED |

## Verdict aggregation (3 reviewers → single ship gate)
1. If ANY reviewer returns `BLOCKED` → phase BLOCKED. Ship blocked.
2. Else if ANY reviewer returns `APPROVED_WITH_WARNINGS` → ship allowed but warnings surfaced in `/jdi-ship` confirmation.
3. Else (all 3 APPROVED) → ship clean.

## Combined REVIEW.md
Reviewers append (not overwrite) to `.jdi/phases/{NN-slug}/REVIEW.md` in order: backend → frontend → security. The 3 sections are independent verdicts; the aggregator computes the final ship-gate verdict.

## Cache artifacts (gitignored)
All reviewers write captures to `.jdi/cache/phase-{NN}-*` (HAR, screenshots, scan reports). `.gitignore` has `.jdi/cache/`.
