project_slug: onboarding-keycloak
schema_version: 2
adopted: true
specialists_ready: true
specialists_count: 3
specialists:
  - jdi-doer-onboarding-keycloak-backend-csharp + jdi-reviewer-onboarding-keycloak-backend-csharp
  - jdi-doer-onboarding-keycloak-frontend-vinext + jdi-reviewer-onboarding-keycloak-frontend-vinext
  - jdi-doer-onboarding-keycloak-security + jdi-reviewer-onboarding-keycloak-security
current_phase: 54
current_phase_slug: backend-csharp-quality-audit
phase_status: in_progress
phase_verdict: n/a
prior_phase_slug: integration-tests-fundos
prior_phase_verdict: APPROVED_WITH_WARNINGS
prior_phase_shipped_at: 2026-05-24
removed_phase: vinxi-to-vinext-migration (REMOVED 2026-05-24 via /jdi-remove-phase; artifacts archived in .jdi/archive/removed-vinxi-to-vinext-migration/; decisions D-38..D-47 retained in DECISIONS.md as history)
plan_tasks: 8
plan_waves: 5
next_step: (loop running) W1-W3 done+verified; W4 coverage merged 96.6%; W4 EXPOSED + FIXED real prod bug (admin search 500, value-converter cols, FromSql 7 sites, D-58, commit a63c258, Integration 217/0). REMAINING: 4 latent search sites need integration tests (Custodiante/Fundo/ListAdminFundo/Employee 72-76%) → then T-8 consolidation (COVERAGE-FINAL+WARNINGS) + live Playwright E2E + /jdi-verify
