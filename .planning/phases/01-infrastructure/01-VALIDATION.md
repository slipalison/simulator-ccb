---
phase: 1
slug: infrastructure
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-04-01
---

# Phase 1 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Docker Compose healthchecks + shell scripts |
| **Config file** | `docker-compose.yml` |
| **Quick run command** | `docker compose ps` |
| **Full suite command** | `docker compose up --wait && docker compose ps` |
| **Estimated runtime** | ~60-90 seconds (Keycloak startup) |

---

## Sampling Rate

- **After every task commit:** `docker compose ps` — all services Running
- **After every plan wave:** `docker compose up --wait` — all healthchecks pass
- **Before `/gsd:verify-work`:** Full stack boots from clean state (`docker compose down -v && docker compose up --wait`)
- **Max feedback latency:** 90 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | Status |
|---------|------|------|-------------|-----------|-------------------|--------|
| app_db | 01 | 1 | INFRA-02,03 | Healthcheck | `docker compose exec app_db pg_isready -U postgres` | Pending |
| keycloak_db | 01 | 1 | INFRA-03 | Healthcheck | `docker compose exec keycloak_db pg_isready -U postgres` | Pending |
| keycloak | 01 | 2 | INFRA-01,05 | Healthcheck | bash /dev/tcp check port 9000 | Pending |
| api | 01 | 3 | INFRA-01,04 | HTTP | `curl -f http://localhost:8080/healthz` | Pending |
| frontend | 01 | 3 | INFRA-01 | HTTP | `curl -f http://localhost:5173/` | Pending |
| realm | 01 | 2 | INFRA-05 | API check | Keycloak realm "onboarding" exists | Pending |
