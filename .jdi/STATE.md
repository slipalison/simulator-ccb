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
phase_status: verified
phase_verdict: APPROVED_WITH_WARNINGS
prior_phase_slug: integration-tests-fundos
prior_phase_verdict: APPROVED_WITH_WARNINGS
prior_phase_shipped_at: 2026-05-24
removed_phase: vinxi-to-vinext-migration (REMOVED 2026-05-24 via /jdi-remove-phase; artifacts archived in .jdi/archive/removed-vinxi-to-vinext-migration/; decisions D-38..D-47 retained in DECISIONS.md as history)
plan_tasks: 8
plan_waves: 5
next_step: /jdi-ship backend-csharp-quality-audit (loop converged APPROVED_WITH_WARNINGS — coverage 91.1→97.4% per-file gate met, 1687 tests green, refactor contract-preserving, D-58 prod bug fixed, 13/13 gates pass; warnings deferred in WARNINGS.md)
