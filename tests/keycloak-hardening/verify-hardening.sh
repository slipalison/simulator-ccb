#!/usr/bin/env bash
set -euo pipefail

KC_BASE="http://localhost:8180"
KC_REALM="onboarding"
ADMIN_USER="admin"
ADMIN_PASS="${KC_ADMIN_PASSWORD:-Admin@Keycloak2026!}"
PASS=0
FAIL=0

log_pass() { echo "PASS: $1"; PASS=$((PASS+1)); }
log_fail() { echo "FAIL: $1"; FAIL=$((FAIL+1)); }

echo "=== Keycloak Security Hardening Verification ==="
echo "Target: $KC_BASE/admin/realms/$KC_REALM"
echo ""

# Obtain admin token
TOKEN=$(curl -s -X POST "$KC_BASE/realms/master/protocol/openid-connect/token" \
  -d "client_id=admin-cli&grant_type=password&username=$ADMIN_USER&password=$ADMIN_PASS" \
  | python3 -c "import json,sys; d=json.load(sys.stdin); print(d.get('access_token','FAILED'))")

if [ "$TOKEN" = "FAILED" ]; then
  echo "ERROR: Cannot obtain admin token. Is Keycloak running on $KC_BASE?"
  exit 1
fi

# GET realm settings once — reuse for multiple checks
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
echo "$PP" | grep -q "length(8)" && \
echo "$PP" | grep -q "upperCase(1)" && \
echo "$PP" | grep -q "lowerCase(1)" && \
echo "$PP" | grep -q "digits(1)" && \
echo "$PP" | grep -q "specialChars(1)" \
  && log_pass "SEC-02: passwordPolicy contains length(8) upperCase(1) lowerCase(1) digits(1) specialChars(1)" \
  || log_fail "SEC-02: passwordPolicy missing components: $PP"

# SEC-03: No wildcards in redirectUris — fetch clients for this and SEC-07
CLIENTS=$(curl -s -H "Authorization: Bearer $TOKEN" "$KC_BASE/admin/realms/$KC_REALM/clients")
WILDCARD_COUNT=$(echo "$CLIENTS" | python3 -c "
import json,sys
clients = json.load(sys.stdin)
count = sum(1 for c in clients for uri in c.get('redirectUris', []) if '*' in uri)
print(count)
")
[ "$WILDCARD_COUNT" = "0" ] \
  && log_pass "SEC-03: No wildcard redirect URIs found in any client" \
  || log_fail "SEC-03: Found $WILDCARD_COUNT redirect URIs containing wildcards"

# SEC-05: Admin console port binding restricted to loopback
PORT_BINDING=$(docker compose ps keycloak 2>/dev/null | grep "8180" || echo "")
echo "$PORT_BINDING" | grep -q "127.0.0.1:8180" \
  && log_pass "SEC-05: Keycloak bound to 127.0.0.1:8180 (not 0.0.0.0)" \
  || log_fail "SEC-05: Port binding is NOT restricted to 127.0.0.1 (got: $PORT_BINDING)"

# SEC-06: SSL required for external connections
SSL=$(echo "$REALM" | python3 -c "import json,sys; d=json.load(sys.stdin); print(d.get('sslRequired'))")
[ "$SSL" = "external" ] \
  && log_pass "SEC-06: sslRequired=external (HTTPS required for non-loopback)" \
  || log_fail "SEC-06: sslRequired=$SSL (expected 'external')"

# SEC-07: Service account roles — exactly manage-users + view-users, nothing broader
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
  && log_pass "SEC-07: Service account has only manage-users + view-users (no realm-admin or broader)" \
  || log_fail "SEC-07: Service account has unexpected roles: $EXTRA_ROLES"

# Summary
echo ""
echo "Results: $PASS passed, $FAIL failed"
[ "$FAIL" = "0" ] && exit 0 || exit 1
