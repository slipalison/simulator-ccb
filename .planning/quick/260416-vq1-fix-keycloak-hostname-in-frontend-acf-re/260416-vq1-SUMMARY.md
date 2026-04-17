# Quick Task 260416-vq1 — Summary

**Task:** fix keycloak hostname in frontend ACF redirect — use localhost instead of docker internal hostname
**Date:** 2026-04-17
**Status:** Complete

## What was done

Introduced `KEYCLOAK_PUBLIC_URL` (default: `http://localhost:8180`) in both frontend auth servers to separate browser-facing redirect URLs from server-side internal Keycloak calls.

### Root cause
`KEYCLOAK_URL=http://keycloak:8080` (Docker-internal hostname) was used for both:
- Server→Keycloak API calls (correct — stays internal)
- Browser-facing redirects via `sendRedirect` (broken — browser can't resolve `keycloak`)

### Changes

| File | Change |
|------|--------|
| `frontend/client/auth-server.ts` | Added `KEYCLOAK_PUBLIC_URL`; used in `buildAuthorizationUrl` and `logoutUrl` |
| `frontend/backoffice/auth-server.ts` | Same fix |
| `compose.yaml` | Added `KEYCLOAK_PUBLIC_URL: http://localhost:8180` to both frontend services |

### Split of responsibilities

| Variable | Default | Used for |
|----------|---------|----------|
| `KEYCLOAK_URL` | `http://keycloak:8080` | `exchangeCodeForTokens`, `refreshAccessToken` (server→Keycloak) |
| `KEYCLOAK_PUBLIC_URL` | `http://localhost:8180` | `buildAuthorizationUrl`, `logoutUrl` (browser redirects) |

Port 8180 matches `ports: "127.0.0.1:8180:8080"` and `KC_HOSTNAME: http://localhost:8180` already set in compose.yaml.
