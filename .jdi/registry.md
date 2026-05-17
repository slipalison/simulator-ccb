# Specialist registry — audit trail

## R-1 (2026-05-11)
**Type:** specialist (doer + reviewer)
**Slug:** onboarding-keycloak-backend-csharp
**Stack:** Backend C# / .NET 10 + EF Core + Keycloak
**File glob:** `**/*.{cs,csproj,sln,slnx}`
**Files:**
- `.jdi/agents/jdi-doer-onboarding-keycloak-backend-csharp.md`
- `.jdi/agents/jdi-reviewer-onboarding-keycloak-backend-csharp.md`
**Created by:** /jdi-bootstrap (manual generation — subagent had no AskUserQuestion access)
**Playwright:** mandatory in reviewer (G7 regression suite)

## R-2 (2026-05-11)
**Type:** specialist (doer + reviewer)
**Slug:** onboarding-keycloak-frontend-vinext
**Stack:** Frontend React 19 + Vinxi 0.5 → Vinext (Cloudflare fork) migration target
**File glob:** `frontend/**/*.{ts,tsx,jsx,js,css,scss,html,mjs,cjs}`
**Files:**
- `.jdi/agents/jdi-doer-onboarding-keycloak-frontend-vinext.md`
- `.jdi/agents/jdi-reviewer-onboarding-keycloak-frontend-vinext.md`
**Created by:** /jdi-bootstrap
**Playwright:** mandatory in reviewer (G5 client + G6 backoffice)
**Note:** Migration Vinxi→Vinext tracked as Phase 53 in ROADMAP.md (user-decided).

## R-3 (2026-05-11)
**Type:** specialist (doer + reviewer) — cross-cutting
**Slug:** onboarding-keycloak-security
**Stack:** Security pipeline (13 tools) + Keycloak hardening + multi-tenant + secrets
**File glob:** `{.github/workflows/**,.semgrep/**,Dockerfile*,docker-compose*.yml,infra/**,keycloak/**,**/Security/**,**/Permission*,**/Auth*,**/.env*}`
**Auto-trigger keywords:** security, CVE, vulnerability, secret, token, keycloak, hardening, semgrep, codeql, trivy, zap, container, SAST, DAST
**Files:**
- `.jdi/agents/jdi-doer-onboarding-keycloak-security.md`
- `.jdi/agents/jdi-reviewer-onboarding-keycloak-security.md`
**Created by:** /jdi-bootstrap
**Playwright:** optional (used for CSP/headers validation when relevant)
