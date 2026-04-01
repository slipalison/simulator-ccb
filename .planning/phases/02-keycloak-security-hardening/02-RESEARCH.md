# Phase 2: Keycloak Security Hardening — Research

**Researched:** 2026-04-01
**Domain:** Keycloak 26.x security hardening — realm configuration, OIDC attack surfaces, Docker Compose access control
**Confidence:** HIGH (core brute force / password policy / redirect URI findings verified against official Keycloak docs; MEDIUM for request_uri mechanism details which changed significantly across versions)

---

## Summary

Phase 2 hardens the Keycloak 26.1 instance provisioned in Phase 1 against seven documented attack surfaces (SEC-01 through SEC-07). The hardening work is almost entirely configuration — realm JSON updates plus Docker Compose environment variable additions — with no new application code required.

The realm JSON created in Phase 1 already has brute force protection and password policy fields set correctly. Phase 2 must verify those values are correct, tighten the `redirectUris` wildcard on `onboarding-app` to an exact URI, add a `clientPolicies` block to enforce the no-wildcard rule at policy level, address the `request_uri` SSRF surface, and verify the service account's role scope is precisely `manage-users` + `view-users` (no broader permissions).

The primary change requiring care is the `redirectUris` wildcard: Phase 1 intentionally left `http://localhost:5173/*` as a placeholder with a note that Phase 2 would tighten it. That tightening is safe because the `onboarding-app` client uses ROPC (no authorization code redirect in the grant flow) — the redirect URI field only matters if Authorization Code Flow is ever used. Registering an exact URI is low-risk and the correct security posture.

**Primary recommendation:** Update `keycloak/onboarding-realm.json` with exact redirect URIs, add `clientPolicies` to enforce no wildcards, add the `request_uri` disable flag to the Keycloak startup command in `compose.yaml`, and write shell-based acceptance tests that verify each SEC-0X requirement against the running Keycloak instance via its Admin REST API.

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| SEC-01 | Brute force protection: max 5 failures, 30s wait, escalating | Already in realm JSON. Research confirms field names: `bruteForceProtected`, `failureFactor=5`, `waitIncrementSeconds=30`. Verify via GET /admin/realms/onboarding + attack-detection API. |
| SEC-02 | Password policy: min 8 chars, uppercase, lowercase, digit, special | Already in realm JSON as `passwordPolicy` string. Research confirms correct format. Verify by attempting to create user with weak password via Admin API (expect 400). |
| SEC-03 | Redirect URIs exact: no wildcards registered on any client | Phase 1 left `http://localhost:5173/*` on onboarding-app. Must replace with exact URI. Add `clientPolicies` block with `secure-redirect-uris-enforcer`. Verify with GET /admin/realms/onboarding/clients. |
| SEC-04 | SSRF protection: disable request_uri parameter | Requires startup flag `--spi-login-protocol-openid-connect-suppress-logout-confirmation-screen` pattern — exact flag is `KC_SPI_LOGIN_PROTOCOL__OPENID_CONNECT__REQUEST_URI_ENABLED=false` env var (LOW confidence — see Open Questions). Alternative: network egress control. |
| SEC-05 | Admin console inaccessible from any IP except 127.0.0.1 in dev | Port binding `127.0.0.1:8180:8080` already in compose.yaml. Admin console runs on port 8080 (same as main server). The loopback binding IS the access restriction for dev. Verify by attempting connection from another host fails. |
| SEC-06 | HTTPS enforcement configured (HTTP only for local dev) | `sslRequired: "external"` already in realm JSON — correct value for dev. Means HTTPS required for non-loopback. No compose.yaml change needed for dev. Document the production upgrade path. |
| SEC-07 | Service account least privilege: manage-users only | `clientScopeMappings` assigns `manage-users` + `view-users`. Research confirms this is correct minimum. `view-users` is needed to read user data. No `realm-admin` or broader roles. Verify via Admin API service account role query. |
</phase_requirements>

---

## Project Constraints (from CLAUDE.md)

- **Tech Stack**: .NET 10 + React/Vinxi + PostgreSQL + Keycloak — stack locked
- **Infra**: Everything runs in Docker Compose locally
- **Security**: Keycloak must be hardened against documented vulnerabilities — this IS the phase
- **API Style**: Controllers ASP.NET (no Minimal API) — not relevant to this phase
- **Observability**: Serilog + OpenTelemetry mandatory — not relevant to this phase (no .NET code in Phase 2)
- **Keycloak image**: `quay.io/keycloak/keycloak:26.1` — locked
- **Realm**: `onboarding` — locked
- **Public client**: `onboarding-app` with Direct Access Grants — locked
- **Confidential client**: `onboarding-api-admin` with Service Account — locked
- **Port binding**: `127.0.0.1:8180:8080` — already in compose.yaml

---

## Standard Stack

### What Phase 2 Modifies

| Artifact | Location | Change Type |
|----------|----------|-------------|
| Realm configuration | `keycloak/onboarding-realm.json` | Tighten redirectUris, add clientPolicies |
| Compose configuration | `compose.yaml` | Add KC_SPI env var for request_uri disable |
| Acceptance tests | `tests/keycloak-hardening/` | New shell script or .http file verifying all SEC-0X |

### No New Packages

Phase 2 is pure infrastructure configuration. No NuGet packages, npm packages, or Docker images change.

---

## Architecture Patterns

### Hardening Layer: What Lives Where

```
keycloak/
└── onboarding-realm.json       # All realm-level security config

compose.yaml                    # Startup flags that are not realm-level

tests/
└── keycloak-hardening/
    └── verify-hardening.sh     # Acceptance tests (curl + jq/python3)
```

### Pattern 1: Realm JSON as Single Source of Truth

**What:** All security policy that Keycloak can express in realm configuration belongs in `onboarding-realm.json`, not the Admin Console. The file is the source of truth; the Admin Console is read-only for ops.

**When to use:** For any setting that appears in a `RealmRepresentation` or `ClientRepresentation` field.

**Example — current brute force fields (already correct in Phase 1):**
```json
{
  "bruteForceProtected": true,
  "permanentLockout": false,
  "failureFactor": 5,
  "waitIncrementSeconds": 30,
  "maxFailureWaitSeconds": 900,
  "minimumQuickLoginWaitSeconds": 60,
  "quickLoginCheckMilliSeconds": 1000,
  "maxDeltaTimeSeconds": 43200
}
```

**Example — exact redirect URI (Phase 2 change):**
```json
{
  "clientId": "onboarding-app",
  "redirectUris": [
    "http://localhost:5173/login/callback"
  ],
  "webOrigins": [
    "http://localhost:5173"
  ]
}
```

> Note: Since `onboarding-app` uses ROPC (Direct Access Grants only, `standardFlowEnabled: false`), the redirect URI is never exercised in the auth flow. However, registering an exact URI instead of a wildcard is correct security posture and required by SEC-03.

### Pattern 2: clientPolicies for No-Wildcard Enforcement

**What:** Keycloak 24+ introduced `secure-redirect-uris-enforcer` as a client policy executor. Adding it to the realm JSON prevents any future client from being registered with wildcard redirect URIs.

**When to use:** When you want policy enforcement beyond manually checking each client's config.

**Structure in realm JSON:**
```json
{
  "clientProfiles": {
    "profiles": [
      {
        "name": "no-wildcard-redirects",
        "description": "Prevents wildcard redirect URIs on all clients",
        "executors": [
          {
            "executor": "secure-redirect-uris-enforcer",
            "configuration": {
              "allow-wildcard-in-redirect-uri": false,
              "allow-open-redirect": false,
              "allow-http-scheme": true,
              "allow-ipv4-loopback-address": true,
              "allow-ipv6-loopback-address": true,
              "oauth-2-1-compliant": false
            }
          }
        ]
      }
    ]
  },
  "clientPolicies": {
    "policies": [
      {
        "name": "enforce-no-wildcard-redirects",
        "description": "Apply no-wildcard profile to all clients",
        "enabled": true,
        "conditions": [
          {
            "condition": "any-client"
          }
        ],
        "profiles": ["no-wildcard-redirects"]
      }
    ]
  }
}
```

> Confidence: MEDIUM. The `clientPolicies`/`clientProfiles` structure is confirmed from Keycloak 24.0.0 release notes and community examples. The exact field names inside the executor's `configuration` map need live validation because official docs don't publish the exact JSON schema for each executor. The key field to test is `allow-wildcard-in-redirect-uri`.

### Pattern 3: SPI Startup Flag for request_uri Disable

**What:** The `request_uri` SSRF attack surface (CVE-2020-10770) was patched in Keycloak 13+, but the parameter itself can still be accepted. For Keycloak 26.x in dev mode (`start-dev`), the SPI provider configuration pattern uses double-dash environment variables.

**Mechanism:** Keycloak 26.x uses Quarkus-based configuration. SPI overrides follow the pattern:
```
KC_SPI_{PROVIDER_TYPE}__{PROVIDER_NAME}__{SETTING}=value
```
Note the double underscore between segments.

**For login protocol openid-connect:**
```
CLI flag:  --spi-login-protocol-openid-connect-suppress-logout-confirmation-screen
Env var:   KC_SPI_LOGIN_PROTOCOL__OPENID_CONNECT__SUPPRESS_LOGOUT_CONFIRMATION_SCREEN
```

The `request_uri` specific flag follows the same pattern but its exact name in Keycloak 26.x is unverified (see Open Questions). The safest approach for this phase:

**Approach A — SPI flag (preferred if flag name confirmed):**
```yaml
environment:
  KC_SPI_LOGIN_PROTOCOL__OPENID_CONNECT__REQUEST_URI_ENABLED: "false"
```

**Approach B — Network isolation (defense in depth, always valid):**
The Keycloak container already cannot reach external addresses because `keycloak_db` has no host port binding and the Docker network `onboarding-net` is isolated. The `request_uri` attack requires Keycloak to make outbound HTTP calls. Network-level prevention is already partially in place.

**Approach C — PAR-only policy (future-proof):**
Add a client policy requiring Pushed Authorization Requests (PAR) — if PAR is required, `request_uri` in the standard OIDC endpoint is irrelevant. However, this conflicts with ROPC grant usage.

**Recommendation for Phase 2:** Use Approach A as primary (needs live testing to confirm flag name), document Approach B as defense in depth. Do not implement Approach C (incompatible with ROPC v1 design).

### Pattern 4: Admin Console Access Restriction

**What:** In the current `compose.yaml`, Keycloak is bound to `127.0.0.1:8180:8080`. This means the admin console (which runs on the same port 8080 internally, exposed as 8180 externally) is only reachable from the Docker host's loopback address.

**Key distinction:** Port 8080 inside the container serves BOTH the OIDC endpoints AND the admin console. Port 9000 inside the container serves management (health/metrics) only. The host binding `127.0.0.1:8180:8080` restricts ALL access to port 8080 — including admin console — to the local machine.

**What SEC-05 requires:**
> Admin console inaccessible from any IP except 127.0.0.1 in dev environment.

This is already satisfied by the port binding. No additional configuration is required. The verification task for SEC-05 is to confirm the binding is correct and document why it works.

**Production note:** In production, a reverse proxy must block `/admin/*` paths from the public internet. The `KC_HOSTNAME_ADMIN` env var can point admin to a separate internal hostname that is not DNS-published externally.

### Pattern 5: Service Account Role Verification

**What:** The `clientScopeMappings` in Phase 1 assigned `manage-users` and `view-users` from `realm-management` to the `onboarding-api-admin` service account. SEC-07 requires verification that NO broader permissions (like `realm-admin`) are attached.

**Admin API query pattern:**
```bash
# Step 1: Get client internal ID
CLIENT_ID=$(curl -s -H "Authorization: Bearer $TOKEN" \
  "http://localhost:8180/admin/realms/onboarding/clients?clientId=onboarding-api-admin" \
  | python3 -c "import json,sys; print(json.load(sys.stdin)[0]['id'])")

# Step 2: Get service account user
SA_USER_ID=$(curl -s -H "Authorization: Bearer $TOKEN" \
  "http://localhost:8180/admin/realms/onboarding/clients/$CLIENT_ID/service-account-user" \
  | python3 -c "import json,sys; print(json.load(sys.stdin)['id'])")

# Step 3: Get role mappings
curl -s -H "Authorization: Bearer $TOKEN" \
  "http://localhost:8180/admin/realms/onboarding/users/$SA_USER_ID/role-mappings"
  | python3 -c "
import json,sys
data = json.load(sys.stdin)
client_roles = data.get('clientMappings', {})
realm_mgmt = client_roles.get('realm-management', {}).get('mappings', [])
role_names = [r['name'] for r in realm_mgmt]
print('Roles:', role_names)
allowed = {'manage-users', 'view-users'}
extra = set(role_names) - allowed
print('PASS' if not extra else f'FAIL - extra roles: {extra}')
"
```

### Anti-Patterns to Avoid

- **Adding `realm-admin` role to service account**: Grants full realm control. The `manage-users` role is sufficient to create/update/delete users. Never grant `realm-admin` to the API service account.
- **Wildcard `*` in redirectUris on confidential clients**: Even with a secret, wildcards on confidential clients enable open redirect attacks.
- **Using `KC_HOSTNAME_STRICT=false` in production**: Necessary for dev, but must be reverted for production. Document this explicitly.
- **Relying on realm JSON import to re-apply clientPolicies**: Keycloak skips realm re-import if the realm already exists in the database volume. Phase 2 must either destroy the volume or use the Admin API to apply changes to a running instance.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Brute force rate limiting | Custom login attempt counter | Keycloak `bruteForceProtected` realm setting | Keycloak tracks per-user, per-IP, handles distributed scenarios |
| Password complexity checking | Custom validator | Keycloak `passwordPolicy` realm setting | Handles policy at auth layer, not API layer |
| Admin console IP allowlist | iptables rules or custom proxy | Docker port binding `127.0.0.1:8180:8080` | Simpler, composable, correct for dev |
| Redirect URI validation | Custom middleware | Keycloak `secure-redirect-uris-enforcer` client policy | Enforced at token issuance, not app layer |
| Service account token scoping | Custom JWT claims filtering | Keycloak role assignment (`manage-users` only) | Enforced at identity provider level |

---

## Common Pitfalls

### Pitfall 1: Realm JSON Re-Import Does NOT Update Existing Realm

**What goes wrong:** Developer updates `onboarding-realm.json` and restarts Keycloak. The changes don't take effect. The old security settings remain.

**Why it happens:** Keycloak's `--import-realm` flag is idempotent — if the realm already exists in `keycloak_db`, the import is skipped entirely. This was documented as a decision in Phase 1.

**How to avoid:**
- For Phase 2 development: Run `docker compose down -v` (destroys volumes) then `docker compose up --wait` to force re-import. This is safe during development.
- For production updates: Use the Keycloak Admin REST API with `PUT /admin/realms/onboarding` to apply changes to the live realm. Do NOT rely on re-import in production.
- Alternatively: Use `kcadm.sh` commands inside the container to apply targeted changes.

**Warning signs:** Verification script shows old `redirectUris` with wildcards after compose restart without `-v`.

### Pitfall 2: clientPolicies JSON Structure Is Version-Sensitive

**What goes wrong:** The `clientPolicies`/`clientProfiles` JSON structure in realm export has subtle differences between Keycloak versions. A structure valid in 24.x may be silently ignored or error on 26.1.

**Why it happens:** The client policies feature was introduced in Keycloak 18 and has had schema evolution. The executor configuration key names are not publicly documented in a versioned schema.

**How to avoid:**
- After adding `clientPolicies` to realm JSON, force a volume-clean restart and verify the policy appears in the Admin Console under Realm Settings > Client Policies.
- Test with an actual wildcard redirect URI attempt after policy is applied: it should be rejected with a Keycloak error.
- If `clientPolicies` import fails silently, apply via `kcadm.sh update realms/onboarding -s clientPolicies=...` as a fallback.

**Warning signs:** Wildcard URI test in acceptance script is accepted by Keycloak after policy should have been applied.

### Pitfall 3: request_uri SPI Flag Name Uncertainty in 26.x

**What goes wrong:** The SPI environment variable to disable `request_uri` handling is documented for older Keycloak versions but the exact variable name for 26.x (Quarkus-based) uses a different naming convention than legacy WildFly-based versions.

**Why it happens:** Keycloak migrated from WildFly to Quarkus in version 17. SPI configuration moved from standalone.xml `<spi>` elements to environment variable naming with double-underscore separators. The old `--spi-login-protocol-openid-connect-request-uri-enabled=false` flag may or may not map correctly to Keycloak 26.x's Quarkus SPI resolution.

**How to avoid:**
- Test the flag in isolation: start Keycloak with the flag and check startup logs for SPI warnings.
- If the flag is not recognized, fall back to the defense-in-depth approach: network isolation already prevents egress.
- Check Keycloak 26.x startup output — it lists all applied SPI overrides.

**Warning signs:** Keycloak starts but logs show "Unrecognized configuration key" for the SPI env var.

### Pitfall 4: admin console on 127.0.0.1 vs container-internal access

**What goes wrong:** The Keycloak admin console being accessible from `127.0.0.1:8180` on the Docker host is treated as "not restricted" — but in the context of SEC-05, this IS the correct restriction for the dev environment.

**Why it happens:** Confusion between "accessible from localhost" (correct) and "accessible from the network" (blocked). SEC-05 says "inaccessible from any IP except 127.0.0.1" — this is exactly what `127.0.0.1:8180:8080` achieves.

**How to avoid:**
- Document clearly in the phase plan that SEC-05 is satisfied by the existing port binding.
- The acceptance test for SEC-05 is: verify `docker compose ps keycloak` shows `127.0.0.1:8180->8080/tcp` (not `0.0.0.0:8180`).

### Pitfall 5: `view-users` Role Is Required Alongside `manage-users`

**What goes wrong:** SEC-07 says "manage-users only" but removing `view-users` from the service account breaks API calls that read user data (e.g., checking if a user exists before registration). The .NET Registration API will receive 403 errors when trying to query users.

**Why it happens:** `manage-users` grants create/update/delete on users but does NOT grant read. `view-users` is the separate read permission.

**How to avoid:**
- Keep `view-users` assigned alongside `manage-users` — this IS least privilege for the use case.
- The SEC-07 requirement "manage-users only" should be interpreted as "only user-management roles, not realm-admin or manage-realm."
- Explicitly document this in the plan.

---

## Code Examples

### Acceptance Test: Verify All SEC-0X Requirements

```bash
#!/usr/bin/env bash
# File: tests/keycloak-hardening/verify-hardening.sh
# Usage: ./verify-hardening.sh
# Requires: curl, python3, jq (optional)

set -euo pipefail

KC_BASE="http://localhost:8180"
KC_REALM="onboarding"
ADMIN_USER="admin"
ADMIN_PASS="${KC_ADMIN_PASSWORD:-Admin@Keycloak2026!}"
PASS=0
FAIL=0

log_pass() { echo "PASS: $1"; PASS=$((PASS+1)); }
log_fail() { echo "FAIL: $1"; FAIL=$((FAIL+1)); }

# Obtain admin token
TOKEN=$(curl -s -X POST "$KC_BASE/realms/master/protocol/openid-connect/token" \
  -d "client_id=admin-cli&grant_type=password&username=$ADMIN_USER&password=$ADMIN_PASS" \
  | python3 -c "import json,sys; d=json.load(sys.stdin); print(d.get('access_token','FAILED'))")

if [ "$TOKEN" = "FAILED" ]; then
  echo "ERROR: Cannot obtain admin token"
  exit 1
fi

# GET realm settings
REALM=$(curl -s -H "Authorization: Bearer $TOKEN" "$KC_BASE/admin/realms/$KC_REALM")

# SEC-01: Brute force protection
BF=$(echo "$REALM" | python3 -c "import json,sys; d=json.load(sys.stdin); print(d.get('bruteForceProtected'))")
FF=$(echo "$REALM" | python3 -c "import json,sys; d=json.load(sys.stdin); print(d.get('failureFactor'))")
WI=$(echo "$REALM" | python3 -c "import json,sys; d=json.load(sys.stdin); print(d.get('waitIncrementSeconds'))")
[ "$BF" = "True" ] && [ "$FF" = "5" ] && [ "$WI" = "30" ] \
  && log_pass "SEC-01: bruteForceProtected=true, failureFactor=5, waitIncrementSeconds=30" \
  || log_fail "SEC-01: BF=$BF FF=$FF WI=$WI (expected True/5/30)"

# SEC-02: Password policy
PP=$(echo "$REALM" | python3 -c "import json,sys; d=json.load(sys.stdin); print(d.get('passwordPolicy',''))")
echo "$PP" | grep -q "length(8)" && echo "$PP" | grep -q "upperCase(1)" && \
  echo "$PP" | grep -q "lowerCase(1)" && echo "$PP" | grep -q "digits(1)" && \
  echo "$PP" | grep -q "specialChars(1)" \
  && log_pass "SEC-02: passwordPolicy contains all required components" \
  || log_fail "SEC-02: passwordPolicy missing components: $PP"

# SEC-03: No wildcards in redirectUris
CLIENTS=$(curl -s -H "Authorization: Bearer $TOKEN" "$KC_BASE/admin/realms/$KC_REALM/clients")
WILDCARD_COUNT=$(echo "$CLIENTS" | python3 -c "
import json,sys
clients = json.load(sys.stdin)
count = sum(1 for c in clients for uri in c.get('redirectUris', []) if '*' in uri)
print(count)
")
[ "$WILDCARD_COUNT" = "0" ] \
  && log_pass "SEC-03: No wildcard redirect URIs found in any client" \
  || log_fail "SEC-03: Found $WILDCARD_COUNT redirect URIs with wildcards"

# SEC-05: Admin console port binding
PORT_BINDING=$(docker compose ps keycloak 2>/dev/null | grep "8180")
echo "$PORT_BINDING" | grep -q "127.0.0.1:8180" \
  && log_pass "SEC-05: Keycloak bound to 127.0.0.1:8180 (not 0.0.0.0)" \
  || log_fail "SEC-05: Port binding is NOT restricted to 127.0.0.1"

# SEC-06: SSL required external
SSL=$(echo "$REALM" | python3 -c "import json,sys; d=json.load(sys.stdin); print(d.get('sslRequired'))")
[ "$SSL" = "external" ] \
  && log_pass "SEC-06: sslRequired=external (HTTPS required for non-loopback)" \
  || log_fail "SEC-06: sslRequired=$SSL (expected 'external')"

# SEC-07: Service account roles — manage-users + view-users only
CLIENT_INT_ID=$(echo "$CLIENTS" | python3 -c "
import json,sys
clients = json.load(sys.stdin)
for c in clients:
    if c.get('clientId') == 'onboarding-api-admin':
        print(c['id'])
        break
")
SA_USER_ID=$(curl -s -H "Authorization: Bearer $TOKEN" \
  "$KC_BASE/admin/realms/$KC_REALM/clients/$CLIENT_INT_ID/service-account-user" \
  | python3 -c "import json,sys; print(json.load(sys.stdin)['id'])")

ROLE_MAPPINGS=$(curl -s -H "Authorization: Bearer $TOKEN" \
  "$KC_BASE/admin/realms/$KC_REALM/users/$SA_USER_ID/role-mappings")

EXTRA_ROLES=$(echo "$ROLE_MAPPINGS" | python3 -c "
import json,sys
data = json.load(sys.stdin)
client_mappings = data.get('clientMappings', {})
realm_mgmt = client_mappings.get('realm-management', {}).get('mappings', [])
role_names = set(r['name'] for r in realm_mgmt)
allowed = {'manage-users', 'view-users'}
extra = role_names - allowed
print(extra if extra else 'none')
")
[ "$EXTRA_ROLES" = "none" ] \
  && log_pass "SEC-07: Service account has only manage-users + view-users (no realm-admin)" \
  || log_fail "SEC-07: Service account has extra roles: $EXTRA_ROLES"

# Summary
echo ""
echo "Results: $PASS passed, $FAIL failed"
[ "$FAIL" = "0" ] && exit 0 || exit 1
```

### Verifying Brute Force Behavior Live

```bash
# After starting Keycloak and creating a test user:
# Try 6 bad logins — 6th should be rejected due to lockout (failureFactor=5)
for i in {1..6}; do
  HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" \
    -X POST "http://localhost:8180/realms/onboarding/protocol/openid-connect/token" \
    -d "client_id=onboarding-app&grant_type=password&username=testuser&password=wrongpassword")
  echo "Attempt $i: HTTP $HTTP_CODE"
done
# Expect: attempts 1-5 return 401, attempt 6 returns 401 with "Account is not fully set up"
# or check user lock status via:
# GET /admin/realms/onboarding/attack-detection/brute-force/users/{userId}
# Response: {"disabled": true, "numFailures": 5, ...}
```

### Applying realm JSON Changes to Existing Volume

```bash
# Option 1: Full re-import (destroys and recreates data — OK in dev)
docker compose down -v
docker compose up app_db keycloak_db keycloak -d --wait

# Option 2: Admin API PATCH (preserves data — preferred for incremental changes)
TOKEN=$(curl -s -X POST "http://localhost:8180/realms/master/protocol/openid-connect/token" \
  -d "client_id=admin-cli&grant_type=password&username=admin&password=Admin@Keycloak2026!" \
  | python3 -c "import json,sys; print(json.load(sys.stdin)['access_token'])")

# Apply a specific change (e.g., tighten redirect URI on onboarding-app):
CLIENT_ID=$(curl -s -H "Authorization: Bearer $TOKEN" \
  "http://localhost:8180/admin/realms/onboarding/clients?clientId=onboarding-app" \
  | python3 -c "import json,sys; print(json.load(sys.stdin)[0]['id'])")

curl -s -X PUT "http://localhost:8180/admin/realms/onboarding/clients/$CLIENT_ID" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"redirectUris": ["http://localhost:5173/login/callback"]}'
```

---

## What Requires Realm JSON vs Startup Flags

| Security Control | Mechanism | Location |
|-----------------|-----------|----------|
| Brute force protection | Realm JSON fields | `onboarding-realm.json` |
| Password policy | Realm JSON `passwordPolicy` string | `onboarding-realm.json` |
| Redirect URI exact match (client config) | Realm JSON `clients[].redirectUris` | `onboarding-realm.json` |
| Redirect URI no-wildcard policy | Realm JSON `clientPolicies` | `onboarding-realm.json` |
| SSL required mode | Realm JSON `sslRequired` | `onboarding-realm.json` |
| Service account role binding | Realm JSON `clientScopeMappings` | `onboarding-realm.json` |
| request_uri SSRF disable | SPI startup env var | `compose.yaml` environment |
| Admin console IP restriction | Docker port binding | `compose.yaml` ports (already done) |
| Access token lifespan | Realm JSON `accessTokenLifespan` | `onboarding-realm.json` (already done) |

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| WildFly `standalone.xml` SPI config | Quarkus env var `KC_SPI_*` pattern | Keycloak 17 (2021) | SPI flags use double-underscore naming, not XML |
| Brute force: temporary OR permanent only | Temporary with escalation to permanent | Keycloak 24 (2024) | New `maximumTemporaryLockouts` field available |
| `KEYCLOAK_ADMIN` env var | `KC_BOOTSTRAP_ADMIN_USERNAME` | Keycloak 26 | Old var silently ignored — Phase 1 already uses new name |
| Wildcard redirect URIs broadly allowed | `secure-redirect-uris-enforcer` client policy | Keycloak 24 (2024) | Can now enforce no-wildcard at realm policy level |
| request_uri SSRF: open by default | Fixed in Keycloak 13 (validation tightened) | Keycloak 13 (2021) | 26.x still accepts the parameter — SPI flag for full disable is unconfirmed |

**Deprecated/outdated:**
- `sslRequired: "none"`: Never use. Keycloak 26 still accepts this value but it disables all HTTPS enforcement.
- `standardFlowEnabled: true` without explicit redirect URI list: Always explicitly set redirect URIs; never rely on "empty = allow all" behavior (which Keycloak does NOT do, but confusion exists).

---

## Runtime State Inventory

> This phase is a hardening/configuration change — not a rename/migration. The Runtime State Inventory applies for a limited scope: the existing Keycloak volume state from Phase 1.

| Category | Items Found | Action Required |
|----------|-------------|-----------------|
| Stored data (Keycloak DB volume) | `keycloak_data` Docker volume contains the imported `onboarding` realm from Phase 1 with `redirectUris: ["http://localhost:5173/*"]` and no `clientPolicies` | Re-import requires `docker compose down -v` OR Admin API PATCH |
| Live service config | Keycloak container is stopped (Phase 1 ended with `docker compose down`). No live state to update. | Start fresh with updated realm JSON |
| OS-registered state | None — no Task Scheduler, systemd, or pm2 entries for this service | None |
| Secrets/env vars | `.env` has `KC_ADMIN_PASSWORD`, `KC_ADMIN_CLIENT_SECRET` — unchanged by Phase 2 | None |
| Build artifacts | None — no compiled artifacts for Keycloak config | None |

**Key finding:** Since Phase 1 ended with `docker compose down` (not `down -v`), the `keycloak_data` volume still exists on disk with the Phase 1 realm configuration. Phase 2 must use `docker compose down -v` before `up --wait` to force realm re-import with the updated JSON.

---

## Open Questions

1. **Exact env var name for disabling request_uri in Keycloak 26.x**
   - What we know: The pattern is `KC_SPI_{TYPE}__{PROVIDER}__{SETTING}` for Quarkus-based Keycloak. For the openid-connect login protocol SPI, settings exist (verified: `KC_SPI_LOGIN_PROTOCOL__OPENID_CONNECT__ADD_REQ_PARAMS_FAIL_FAST`).
   - What's unclear: Whether a `REQUEST_URI_ENABLED` or similar setting exists as a named SPI option in 26.1, or whether request_uri validation is now internal-only (fixed in code, not configurable).
   - Recommendation: Test `KC_SPI_LOGIN_PROTOCOL__OPENID_CONNECT__REQUEST_URI_ENABLED=false` at startup. If Keycloak logs an unrecognized key warning, fall back to documenting defense-in-depth (network isolation) as the mitigation for SEC-04.

2. **clientPolicies JSON schema for 26.1**
   - What we know: `clientPolicies` and `clientProfiles` exist as realm JSON fields since Keycloak 18+, and the `secure-redirect-uris-enforcer` executor was added in 24.0.
   - What's unclear: Whether `allow-wildcard-in-redirect-uri` is the exact key name in the executor configuration for 26.1, or whether the field was renamed.
   - Recommendation: After first import attempt, check the Admin Console under Realm Settings > Client Policies to verify the policy was imported and contains the expected executor configuration.

3. **ROPC and redirect URI validation**
   - What we know: `onboarding-app` uses ROPC (`directAccessGrantsEnabled: true`, `standardFlowEnabled: false`). The redirect URI is technically never used in the ROPC flow.
   - What's unclear: Whether setting an exact redirect URI on a ROPC-only client could cause issues (some Keycloak versions validate the field is non-empty even for non-standard flows).
   - Recommendation: If an exact URI causes issues, keeping `redirectUris: []` (empty) on a ROPC-only client is also valid — the `secure-redirect-uris-enforcer` policy would only apply when redirect URIs are actually evaluated.

---

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| Docker Compose v2 | All Keycloak ops | To verify at runtime | v2.x | — |
| Python3 | Acceptance test scripts | To verify at runtime | 3.x | Use `jq` instead |
| curl | Acceptance test scripts | To verify at runtime | — | — |
| Keycloak 26.1 container | All hardening | Pulled in Phase 1 | 26.1 | — |

> Environment availability requires running Keycloak to be verified. Phase 2 executes after Phase 1 infrastructure is known working, so all dependencies should be available.

---

## Validation Architecture

> `workflow.nyquist_validation` is `true` in `.planning/config.json` — this section is required.

### Test Framework

| Property | Value |
|----------|-------|
| Framework | Shell scripts (curl + python3) against Keycloak Admin REST API |
| Config file | `tests/keycloak-hardening/verify-hardening.sh` (Wave 0 creation) |
| Quick run command | `bash tests/keycloak-hardening/verify-hardening.sh` |
| Full suite command | `docker compose down -v && docker compose up app_db keycloak_db keycloak -d --wait && bash tests/keycloak-hardening/verify-hardening.sh` |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| SEC-01 | bruteForceProtected=true, failureFactor=5, waitIncrementSeconds=30 | API assertion | `bash tests/keycloak-hardening/verify-hardening.sh` (SEC-01 block) | Wave 0 |
| SEC-02 | passwordPolicy contains length(8) + all character classes | API assertion | `bash tests/keycloak-hardening/verify-hardening.sh` (SEC-02 block) | Wave 0 |
| SEC-03 | No client has wildcard `*` in redirectUris | API assertion | `bash tests/keycloak-hardening/verify-hardening.sh` (SEC-03 block) | Wave 0 |
| SEC-04 | request_uri SSRF mitigation active | Startup log check or network test | Manual check of Keycloak startup logs for SPI override confirmation | Wave 0 |
| SEC-05 | Keycloak port bound to 127.0.0.1:8180, not 0.0.0.0 | Docker state check | `docker compose ps keycloak \| grep "127.0.0.1:8180"` | Wave 0 |
| SEC-06 | sslRequired=external in realm | API assertion | `bash tests/keycloak-hardening/verify-hardening.sh` (SEC-06 block) | Wave 0 |
| SEC-07 | Service account has only manage-users + view-users | API role query | `bash tests/keycloak-hardening/verify-hardening.sh` (SEC-07 block) | Wave 0 |

### Sampling Rate
- **Per task commit:** `bash tests/keycloak-hardening/verify-hardening.sh` (requires Keycloak running)
- **Per wave merge:** Full suite command (clean volume restart + verification)
- **Phase gate:** All 7 SEC checks PASS before `/gsd:verify-work`

### Wave 0 Gaps
- [ ] `tests/keycloak-hardening/verify-hardening.sh` — covers all SEC-01 through SEC-07
- [ ] `tests/keycloak-hardening/README.md` — brief usage note (optional, low priority)

---

## Sources

### Primary (HIGH confidence)
- [Keycloak 26.x All Config Options](https://www.keycloak.org/server/all-config) — HTTP, HTTPS, management interface settings
- [Keycloak Server Configuration Guide — Production](https://www.keycloak.org/server/configuration-production) — security hardening recommendations
- [Keycloak Hostname Configuration](https://www.keycloak.org/server/hostname) — KC_HOSTNAME_ADMIN, hostname separation
- [Keycloak Management Interface](https://www.keycloak.org/server/management-interface) — port 9000 vs 8080 distinction
- [Keycloak All Provider Config](https://www.keycloak.org/server/all-provider-config) — SPI login-protocol openid-connect settings
- [Keycloak Admin REST API — Attack Detection](https://www.keycloak.org/docs-api/latest/rest-api/index.html#_attack_detection) — GET /attack-detection/brute-force/users/{userId}
- Phase 1 `keycloak/onboarding-realm.json` — existing configuration baseline

### Secondary (MEDIUM confidence)
- [Keycloak 24.0.0 Release Notes](https://www.keycloak.org/2024/03/keycloak-2400-released) — secure-redirect-uris-enforcer introduction
- [Keycloak Admin REST API — Role Mappings](https://www.keycloak.org/docs-api/latest/rest-api/index.html) — service account role verification endpoints
- [CVE-2020-10770 — Red Hat Bugzilla](https://bugzilla.redhat.com/show_bug.cgi?id=1846270) — request_uri SSRF history, fixed in Keycloak 13
- [Keycloak client policies discussion #9278](https://github.com/keycloak/keycloak/discussions/9278) — secure-redirect-uris-enforcer config options
- [Brute Force Detection docs — wjw465150 gitbook](https://wjw465150.gitbooks.io/keycloak-documentation/content/server_admin/topics/threat/brute-force.html) — realm JSON field names for brute force

### Tertiary (LOW confidence — needs live validation)
- `KC_SPI_LOGIN_PROTOCOL__OPENID_CONNECT__REQUEST_URI_ENABLED=false` — inferred from SPI naming convention; exact flag not found in official 26.x docs
- `clientPolicies.clientProfiles.executors[].configuration["allow-wildcard-in-redirect-uri"]` — field name inferred from community discussions; needs live test

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — no new packages, all changes are configuration
- Architecture: HIGH — realm JSON + startup flag pattern is well-established
- Brute force / password policy fields: HIGH — verified against official Keycloak API docs and Phase 1 implementation
- Admin console restriction: HIGH — Docker port binding mechanism is authoritative
- request_uri disable mechanism: LOW — exact env var name for Keycloak 26.x not found in official docs; needs empirical testing
- clientPolicies JSON schema: MEDIUM — structure confirmed from release notes, exact executor field names need live validation
- Service account role verification API: HIGH — endpoint path confirmed in official REST API docs

**Research date:** 2026-04-01
**Valid until:** 2026-05-01 (stable Keycloak config area; SPI flag question may resolve with live testing)
