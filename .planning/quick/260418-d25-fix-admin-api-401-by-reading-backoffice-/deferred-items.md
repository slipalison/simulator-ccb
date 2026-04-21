# Deferred Items — quick-260418-d25

## Out-of-scope discoveries (not fixed)

### 1. Onboarding.Integration.Tests fail without Docker daemon

- **Tests:** `RegistrationIntegrationTests.PostPf_ValidPayload_CreatesUserInKeycloak`, `RegistrationIntegrationTests.PostPf_KeycloakDown_NoOrphanedRowInAppDb`
- **Symptom:** `System.IO.IOException: All pipe instances are busy` / `NamedPipeClientStream.ConnectInternal` while Testcontainers tries to spin up Keycloak.
- **Root cause:** Docker Desktop / Docker engine is not running on the executor host. Pre-existing environmental failure; not caused by this task.
- **Why deferred:** Outside the plan's `<files>` and unrelated to the AdminSessionMiddleware/AdminAuthController fix. Same tests failed identically before this change.
- **Action item:** Run `docker compose up -d` (or start Docker Desktop) before running the integration suite.

### 2. Frontend backoffice test references removed `loginAdmin` symbol

- **File:** `frontend/backoffice/src/tests/admin-api.test.ts`
- **Symptom:** Imports `loginAdmin` and asserts `POST /api/admin/auth/login`, but `frontend/backoffice/src/lib/admin-api.ts` no longer exports `loginAdmin` (already migrated to ACF before this task).
- **Why deferred:** Frontend test file is outside the plan's `<files>` (plan is .NET-only). The file was already stale before this task — its removal is independent cleanup.
- **Action item:** Open a follow-up to delete or rewrite `admin-api.test.ts` to cover the ACF-based admin-api surface (`getAdminMe`, `logoutAdmin`, `listUsers`, `createAdmin`, `getAdministrators`, etc.).
