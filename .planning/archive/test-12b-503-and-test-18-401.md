# Debug Session: Test 12b (503) + Test 18 (401 ValidIssuer)

**Started:** 2026-04-07
**Status:** ✅ RESOLVED — All 22 UAT tests passing

## Root Causes Found

### Bug 1: Test 12b → 503 (expected 409) — FIXED

**Root cause:** Keycloak retorna 409 para email duplicado, mas o handler tratava TODAS as exceções do Keycloak como erro transitório (`RegistrationFailedException` → 503).

**Fix applied (3 files):**
1. `src/Onboarding.Domain/Exceptions/DuplicateKeycloakUserException.cs` — NOVA exceção para colisões no Keycloak
2. `src/Onboarding.Infrastructure/Keycloak/KeycloakUserService.cs` — Detecta 409 do Keycloak e lança `DuplicateKeycloakUserException`
3. `src/Onboarding.Application/Clients/Commands/RegisterClientCommandHandler.cs` — Catch específico para `DuplicateKeycloakUserException` → compensa DB → lança `DuplicateClientException` → 409

### Bug 2: Test 18 → 401 (expected 200) — FIXED

**Root cause:** OIDC discovery retorna `jwks_uri: http://localhost:8180/...` (via KC_HOSTNAME), inacessível de dentro do container da API.

**Fix applied (2 files):**
1. `src/Onboarding.API/Observability/HostnameRewriteHandler.cs` — NOVO DelegatingHandler que reescreve `localhost:8180` → `keycloak:8080`
2. `src/Onboarding.API/Program.cs` — Configura `options.Backchannel` com `HostnameRewriteHandler`

### Bug 3: Realm JSON clientPolicies config — FIXED

**Root cause:** Keycloak 26.x requer `"config": {}` na condição `any-client` das client policies.

**Fix applied:**
1. `keycloak/onboarding-realm.json` — Adicionado `"config": {}` na condição `any-client`

### Bug 4: KeycloakAdminClientOptions Resource causing 403 — FIXED

**Root cause:** `Resource = adminClientId` em `KeycloakAdminClientOptions` causava HTTP 403 Forbidden no Keycloak Admin API.

**Fix applied:**
1. `src/Onboarding.Infrastructure/DependencyInjection.cs` — Removido `Resource = adminClientId` de `KeycloakAdminClientOptions`

## Additional Fixes (Infrastructure)

- Service account roles `manage-users` e `view-users` do `realm-management` client atribuídas ao service account user do `onboarding-api-admin` via Admin API
- Note: Estas roles precisam ser atribuídas manualmente após cada `docker compose down -v` até que o realm JSON seja atualizado com o mapeamento correto de service account roles

## Test Results

```
22 passou | 0 falhou | 8 ignorado (brute force)
```

All tests passing:
- ✅ Test 10: PF Registration (201)
- ✅ Test 11: PJ Registration (201)
- ✅ Test 12a: Duplicate CPF Detection (409)
- ✅ Test 12b: Duplicate Email Detection (409)
- ✅ Test 13: Invalid CPF (422)
- ✅ Test 14a: Idempotency First (201)
- ✅ Test 14b: Idempotency Same Key (201)
- ✅ Test 13b: Invalid CNPJ (400)
- ✅ Test 15: Login (200)
- ✅ Test 16: Login Invalid (401)
- ✅ Test 16b: Login Non-existent Email (401)
- ✅ Test 17: Protected Route without Token (401)
- ✅ Test 18: Protected Route with Token (200)
- ✅ Test 19: Token Refresh (200)
- ✅ Test 19b: Token Refresh Invalid (401)
- ✅ Test 15b: Login Missing Email (422)
- ✅ Test 15c: Login Missing Password (400)
- ✅ Test 9a: Liveness (200)
- ✅ Test 9b: Readiness (200)
- ✅ Keycloak Realm Discovery (200)
- ✅ Keycloak Health (200)
- ✅ Grafana UI (200)
