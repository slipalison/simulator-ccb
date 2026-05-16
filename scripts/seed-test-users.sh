#!/usr/bin/env bash
# =============================================================================
# scripts/seed-test-users.sh
# =============================================================================
# Idempotent fixture: seeds two e2e test users via Keycloak Admin REST API
# using the onboarding-api-admin service account (client_credentials grant).
#
# Preconditions:
#   - docker compose up -d (Keycloak must be healthy on ${KEYCLOAK_URL})
#   - KC_ADMIN_CLIENT_SECRET env var set to the onboarding-api-admin secret
#     (matches value in .env / compose.yaml; default: dev-admin-secret)
#
# Invocation:
#   KC_ADMIN_CLIENT_SECRET=dev-admin-secret bash scripts/seed-test-users.sh
#   # or, if .env is sourced:
#   bash scripts/seed-test-users.sh
#
# What this script does:
#   1. Acquires a per-realm service-account token for each realm using
#      onboarding-api-admin client_credentials.
#   2. client realm  → upsert e2e-client@example.com (password E2EClient@123!,
#      emailVerified, no requiredActions, group admin-empresa).
#   3. backoffice realm → upsert e2e-admin@example.com (password E2EAdmin@123!,
#      emailVerified, no requiredActions, realm role admin).
#   4. Fully idempotent: re-run after docker compose down -v && docker compose up -d
#      produces identical state with no 409 errors and no orphan users.
#
# Note: group name in client realm is admin-empresa (not pj-admin — verified
# in keycloak/client-realm.json; pj-admin does not exist in this project).
# =============================================================================

set -euo pipefail

KEYCLOAK_URL="${KEYCLOAK_URL:-http://localhost:8180}"
KC_ADMIN_CLIENT_ID="${KC_ADMIN_CLIENT_ID:-onboarding-api-admin}"
KC_ADMIN_CLIENT_SECRET="${KC_ADMIN_CLIENT_SECRET:-dev-admin-secret}"

CLIENT_REALM="client"
BACKOFFICE_REALM="backoffice"

E2E_CLIENT_EMAIL="e2e-client@example.com"
E2E_CLIENT_PASSWORD="E2EClient@123!"
E2E_CLIENT_GROUP="admin-empresa"

E2E_ADMIN_EMAIL="e2e-admin@example.com"
E2E_ADMIN_PASSWORD="E2EAdmin@123!"
E2E_ADMIN_ROLE="admin"

# ── Helpers ──────────────────────────────────────────────────────────────────

# get_token <realm>
# Acquires a client_credentials token for onboarding-api-admin in the given realm.
get_token() {
  local realm="$1"
  local response
  response=$(curl -s -X POST \
    "${KEYCLOAK_URL}/realms/${realm}/protocol/openid-connect/token" \
    -H "Content-Type: application/x-www-form-urlencoded" \
    -d "grant_type=client_credentials" \
    -d "client_id=${KC_ADMIN_CLIENT_ID}" \
    -d "client_secret=${KC_ADMIN_CLIENT_SECRET}")
  echo "${response}" | jq -r '.access_token'
}

# get_user_id <realm> <token> <email>
# Returns the Keycloak UUID for the user with the given email, or empty string.
get_user_id() {
  local realm="$1"
  local token="$2"
  local email="$3"
  curl -s \
    "${KEYCLOAK_URL}/admin/realms/${realm}/users?email=$(python3 -c "import urllib.parse,sys; print(urllib.parse.quote(sys.argv[1]))" "${email}")&exact=true" \
    -H "Authorization: Bearer ${token}" \
    | jq -r '.[0].id // empty'
}

# url_encode <string>
# Simple percent-encoding using python3 (available on all target environments).
url_encode() {
  python3 -c "import urllib.parse,sys; print(urllib.parse.quote(sys.argv[1]))" "$1"
}

# upsert_user <realm> <token> <email> <password>
# Creates or locates a user. Returns the user UUID.
upsert_user() {
  local realm="$1"
  local token="$2"
  local email="$3"
  local password="$4"

  local existing_id
  existing_id=$(curl -s \
    "${KEYCLOAK_URL}/admin/realms/${realm}/users?email=$(url_encode "${email}")&exact=true" \
    -H "Authorization: Bearer ${token}" \
    | jq -r '.[0].id // empty')

  if [ -z "${existing_id}" ]; then
    echo "  [${realm}] Creating user ${email}..." >&2
    local location
    location=$(curl -s -o /dev/null -w '%{redirect_url}' -D - -X POST \
      "${KEYCLOAK_URL}/admin/realms/${realm}/users" \
      -H "Authorization: Bearer ${token}" \
      -H "Content-Type: application/json" \
      -d "{
        \"username\": \"${email}\",
        \"email\": \"${email}\",
        \"emailVerified\": true,
        \"enabled\": true,
        \"requiredActions\": []
      }" | grep -i '^Location:' | tr -d '\r' | awk '{print $2}')

    if [ -z "${location}" ]; then
      # Fallback: re-query after creation
      existing_id=$(curl -s \
        "${KEYCLOAK_URL}/admin/realms/${realm}/users?email=$(url_encode "${email}")&exact=true" \
        -H "Authorization: Bearer ${token}" \
        | jq -r '.[0].id // empty')
    else
      existing_id="${location##*/}"
    fi
  else
    echo "  [${realm}] User ${email} already exists (id=${existing_id}), updating..." >&2
    curl -s -o /dev/null -X PUT \
      "${KEYCLOAK_URL}/admin/realms/${realm}/users/${existing_id}" \
      -H "Authorization: Bearer ${token}" \
      -H "Content-Type: application/json" \
      -d "{
        \"emailVerified\": true,
        \"enabled\": true,
        \"requiredActions\": []
      }"
  fi

  echo "${existing_id}"
}

# reset_password <realm> <token> <user_id> <password>
reset_password() {
  local realm="$1"
  local token="$2"
  local user_id="$3"
  local password="$4"

  curl -s -o /dev/null -X PUT \
    "${KEYCLOAK_URL}/admin/realms/${realm}/users/${user_id}/reset-password" \
    -H "Authorization: Bearer ${token}" \
    -H "Content-Type: application/json" \
    -d "{\"type\": \"password\", \"value\": \"${password}\", \"temporary\": false}"
  echo "  [${realm}] Password reset for user ${user_id}" >&2
}

# ensure_group_membership <realm> <token> <user_id> <group_name>
ensure_group_membership() {
  local realm="$1"
  local token="$2"
  local user_id="$3"
  local group_name="$4"

  # Find the group ID by name
  local group_id
  group_id=$(curl -s \
    "${KEYCLOAK_URL}/admin/realms/${realm}/groups?search=$(url_encode "${group_name}")&exact=true" \
    -H "Authorization: Bearer ${token}" \
    | jq -r '.[] | select(.name == "'"${group_name}"'") | .id' | head -1)

  if [ -z "${group_id}" ]; then
    echo "  [${realm}] WARNING: group '${group_name}' not found — skipping group assignment" >&2
    return 0
  fi

  # Check if user is already in the group
  local already_member
  already_member=$(curl -s \
    "${KEYCLOAK_URL}/admin/realms/${realm}/users/${user_id}/groups" \
    -H "Authorization: Bearer ${token}" \
    | jq -r '.[] | select(.id == "'"${group_id}"'") | .id' | head -1)

  if [ -z "${already_member}" ]; then
    curl -s -o /dev/null -X PUT \
      "${KEYCLOAK_URL}/admin/realms/${realm}/users/${user_id}/groups/${group_id}" \
      -H "Authorization: Bearer ${token}"
    echo "  [${realm}] Added user ${user_id} to group '${group_name}'" >&2
  else
    echo "  [${realm}] User ${user_id} already in group '${group_name}'" >&2
  fi
}

# ensure_realm_role <realm> <token> <user_id> <role_name>
ensure_realm_role() {
  local realm="$1"
  local token="$2"
  local user_id="$3"
  local role_name="$4"

  # Fetch role representation
  local role_json
  role_json=$(curl -s \
    "${KEYCLOAK_URL}/admin/realms/${realm}/roles/${role_name}" \
    -H "Authorization: Bearer ${token}")

  if echo "${role_json}" | jq -e '.error' > /dev/null 2>&1; then
    echo "  [${realm}] WARNING: realm role '${role_name}' not found — skipping role assignment" >&2
    return 0
  fi

  # Check current role mappings
  local already_has_role
  already_has_role=$(curl -s \
    "${KEYCLOAK_URL}/admin/realms/${realm}/users/${user_id}/role-mappings/realm" \
    -H "Authorization: Bearer ${token}" \
    | jq -r '.[] | select(.name == "'"${role_name}"'") | .name' | head -1)

  if [ -z "${already_has_role}" ]; then
    curl -s -o /dev/null -X POST \
      "${KEYCLOAK_URL}/admin/realms/${realm}/users/${user_id}/role-mappings/realm" \
      -H "Authorization: Bearer ${token}" \
      -H "Content-Type: application/json" \
      -d "[${role_json}]"
    echo "  [${realm}] Assigned realm role '${role_name}' to user ${user_id}" >&2
  else
    echo "  [${realm}] User ${user_id} already has realm role '${role_name}'" >&2
  fi
}

# ── Main ─────────────────────────────────────────────────────────────────────

echo "==> Seeding e2e test users in Keycloak at ${KEYCLOAK_URL}"

# ── Client realm: e2e-client@example.com ──────────────────────────────────

echo ""
echo "--- Realm: ${CLIENT_REALM} ---"
CLIENT_TOKEN=$(get_token "${CLIENT_REALM}")
if [ "${CLIENT_TOKEN}" = "null" ] || [ -z "${CLIENT_TOKEN}" ]; then
  echo "ERROR: Failed to acquire token for realm '${CLIENT_REALM}'. Check KC_ADMIN_CLIENT_SECRET and that Keycloak is running." >&2
  exit 1
fi

CLIENT_USER_ID=$(upsert_user "${CLIENT_REALM}" "${CLIENT_TOKEN}" "${E2E_CLIENT_EMAIL}" "${E2E_CLIENT_PASSWORD}")
if [ -z "${CLIENT_USER_ID}" ]; then
  echo "ERROR: Could not create or locate user '${E2E_CLIENT_EMAIL}' in realm '${CLIENT_REALM}'" >&2
  exit 1
fi

reset_password "${CLIENT_REALM}" "${CLIENT_TOKEN}" "${CLIENT_USER_ID}" "${E2E_CLIENT_PASSWORD}"
ensure_group_membership "${CLIENT_REALM}" "${CLIENT_TOKEN}" "${CLIENT_USER_ID}" "${E2E_CLIENT_GROUP}"

echo "  [${CLIENT_REALM}] ${E2E_CLIENT_EMAIL} ready (id=${CLIENT_USER_ID})"

# ── Backoffice realm: e2e-admin@example.com ───────────────────────────────

echo ""
echo "--- Realm: ${BACKOFFICE_REALM} ---"
BACKOFFICE_TOKEN=$(get_token "${BACKOFFICE_REALM}")
if [ "${BACKOFFICE_TOKEN}" = "null" ] || [ -z "${BACKOFFICE_TOKEN}" ]; then
  echo "ERROR: Failed to acquire token for realm '${BACKOFFICE_REALM}'. Check KC_ADMIN_CLIENT_SECRET and that Keycloak is running." >&2
  exit 1
fi

ADMIN_USER_ID=$(upsert_user "${BACKOFFICE_REALM}" "${BACKOFFICE_TOKEN}" "${E2E_ADMIN_EMAIL}" "${E2E_ADMIN_PASSWORD}")
if [ -z "${ADMIN_USER_ID}" ]; then
  echo "ERROR: Could not create or locate user '${E2E_ADMIN_EMAIL}' in realm '${BACKOFFICE_REALM}'" >&2
  exit 1
fi

reset_password "${BACKOFFICE_REALM}" "${BACKOFFICE_TOKEN}" "${ADMIN_USER_ID}" "${E2E_ADMIN_PASSWORD}"
ensure_realm_role "${BACKOFFICE_REALM}" "${BACKOFFICE_TOKEN}" "${ADMIN_USER_ID}" "${E2E_ADMIN_ROLE}"

echo "  [${BACKOFFICE_REALM}] ${E2E_ADMIN_EMAIL} ready (id=${ADMIN_USER_ID})"

echo ""
echo "==> Done. Test users seeded successfully."
echo "    client realm  : ${E2E_CLIENT_EMAIL} / ${E2E_CLIENT_PASSWORD} (group: ${E2E_CLIENT_GROUP})"
echo "    backoffice    : ${E2E_ADMIN_EMAIL} / ${E2E_ADMIN_PASSWORD} (role: ${E2E_ADMIN_ROLE})"
