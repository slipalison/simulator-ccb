---
phase: 01-infrastructure
plan: 02
subsystem: infra
tags: [keycloak, docker-compose, realm-import, postgres, brute-force, password-policy]

# Dependency graph
requires:
  - phase: 01-infrastructure
    plan: 01
    provides: compose.yaml with Keycloak service, volume mounts, and healthcheck infrastructure

provides:
  - Keycloak onboarding realm imported on first boot via --import-realm flag
  - onboarding-app public client (directAccessGrantsEnabled, no secret)
  - onboarding-api-admin confidential client (serviceAccountsEnabled, secret=dev-admin-secret)
  - Brute force protection (failureFactor=5, waitIncrementSeconds=30, maxFailureWaitSeconds=900)
  - Password policy (length 8, uppercase, lowercase, digits, specialChars)
  - Access token lifespan 300s (5 minutes), SSO session max 8h
  - Smoke-tested: realm HTTP 200 confirmed via Admin API on first boot

affects:
  - 01-03 (API scaffold needs known client IDs and service account secret for JwtBearer config)
  - Phase 5 (Registration API uses onboarding-api-admin service account to create Keycloak users)
  - Phase 9 (Login UI authenticates via onboarding-app ROPC grant)

# Tech tracking
tech-stack:
  added:
    - Keycloak 26.1 realm import JSON (onboarding-realm.json)
  patterns:
    - Realm import via --import-realm flag on first boot (idempotent: skips if realm already exists)
    - clientScopeMappings for service account role assignment in realm JSON
    - sslRequired=external for dev/prod parity (HTTP allowed on loopback, HTTPS required externally)

key-files:
  created:
    - keycloak/onboarding-realm.json
  modified: []

key-decisions:
  - "clientScopeMappings used (not scopeMappings) for service account role binding in realm JSON — confirmed correct for Keycloak import format"
  - "sslRequired=external chosen over none — allows HTTP on localhost while enforcing HTTPS for external traffic"
  - "standardFlowEnabled=false on both clients — neither needs Authorization Code flow in v1; ROPC only"
  - "Realm import is idempotent — Keycloak skips re-import if realm already exists in keycloak_db volume"

patterns-established:
  - "Keycloak realm JSON: all config in single file, no env var interpolation (Keycloak import does not support it)"
  - "Client secrets are literal strings in realm JSON matching .env values exactly"
  - "Smoke test pattern: docker compose up --wait, Admin API token, GET /admin/realms/{name}, docker compose down"

requirements-completed:
  - INFRA-05

# Metrics
duration: 8min
completed: 2026-04-01
---

# Phase 01 Plan 02: Keycloak Realm Import and Smoke Test Summary

**Keycloak onboarding realm provisioned via JSON import with two clients, brute force protection, password policy, and service account role bindings — smoke-tested healthy on first boot**

## Performance

- **Duration:** ~8 min (Task 1 completed prior session; Task 2 smoke test ~3 min including image pull)
- **Started:** 2026-04-01T18:13:39Z (Task 1 prior session)
- **Completed:** 2026-04-01T18:20:00Z
- **Tasks:** 2 of 2
- **Files modified:** 1 (keycloak/onboarding-realm.json)

## Accomplishments

- Created `keycloak/onboarding-realm.json` with complete realm configuration: two clients, brute force protection (failureFactor=5, waitIncrementSeconds=30), password policy (length 8 + uppercase + lowercase + digits + specialChars), access token lifespan 300s, clientScopeMappings assigning manage-users + view-users to onboarding-api-admin service account
- Smoke-tested Keycloak start: all three infrastructure services (app_db, keycloak_db, keycloak) reached healthy status via `docker compose up --wait`
- Verified realm imported: Admin API `GET /admin/realms/onboarding` returned HTTP 200
- Verified both clients: onboarding-app (publicClient=true, directAccessGrantsEnabled=true), onboarding-api-admin (publicClient=false, serviceAccountsEnabled=true)
- Verified port security: keycloak_db has no host port binding; app_db and keycloak bind to 127.0.0.1 only
- Stopped cleanly with volumes preserved for Plan 03 continuity

## Task Commits

Each task was committed atomically:

1. **Task 1: Create onboarding-realm.json with full realm configuration** - `959970d` (feat)
2. **Task 2: Smoke-test Keycloak boots and realm is imported** - verification only, no file changes; recorded in plan metadata commit

**Plan metadata:** (docs commit — see final commit hash)

## Files Created/Modified

- `keycloak/onboarding-realm.json` - Complete Keycloak realm definition auto-imported on first boot. Contains both clients, brute force config, password policy, token lifetimes, clientScopeMappings for service account.

## Decisions Made

- **clientScopeMappings vs scopeMappings:** Used `clientScopeMappings` (keyed by source client `realm-management`) to assign manage-users and view-users roles to the onboarding-api-admin service account. This is the correct Keycloak import field for this pattern.
- **sslRequired=external:** HTTPS required for non-loopback requests; HTTP allowed on localhost. Correct for local dev without self-signed cert complexity.
- **standardFlowEnabled=false on both clients:** v1 uses ROPC grant only. Authorization Code flow not needed until v2 security hardening.
- **Realm import idempotency:** Keycloak's --import-realm skips realms that already exist in keycloak_db volume. Volumes are preserved across `docker compose down` (without -v) so re-import does not happen on subsequent starts.

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

- `python3` not available in shell environment on Windows — switched to `node -e` for JSON parsing in curl pipeline. Functionally identical result.

## User Setup Required

None - no external service configuration required. All credentials are in `.env` (gitignored).

## Next Phase Readiness

- Plan 03 (API scaffold) can proceed immediately
- Known values for Plan 03 wiring:
  - Keycloak realm: `onboarding`
  - Public client ID: `onboarding-app`
  - Confidential client ID: `onboarding-api-admin`, secret: `dev-admin-secret` (matches KC_ADMIN_CLIENT_SECRET in .env)
  - Token endpoint: `http://keycloak:8080/realms/onboarding/protocol/openid-connect/token` (internal Docker network)
  - Admin API base: `http://keycloak:8080/admin/realms/onboarding` (internal Docker network)
- keycloak_db volume preserved — Keycloak will skip realm re-import on next start
- No blockers for Plan 03

---
*Phase: 01-infrastructure*
*Completed: 2026-04-01*
