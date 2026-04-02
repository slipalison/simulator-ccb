---
phase: 2
slug: keycloak-security-hardening
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-04-01
---

# Phase 2 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Shell scripts (curl + node/python3 JSON parsing) |
| **Config file** | `tests/keycloak-hardening/verify-hardening.sh` |
| **Quick run command** | `docker compose ps keycloak \| grep "(healthy)"` |
| **Full suite command** | `bash tests/keycloak-hardening/verify-hardening.sh` |
| **Estimated runtime** | ~30 seconds (Keycloak already running) |

---

## Sampling Rate

- **After every task commit:** `docker compose ps keycloak | grep "(healthy)"` — Keycloak still healthy
- **After every plan wave:** `bash tests/keycloak-hardening/verify-hardening.sh` — all SEC-0X pass
- **Before `/gsd:verify-work`:** Full suite must be green + `docker compose down -v && docker compose up --wait` clean boot
- **Max feedback latency:** 30 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | Status |
|---------|------|------|-------------|-----------|-------------------|--------|
| redirect-uri | 01 | 1 | SEC-03 | API check | `curl -s .../clients \| grep "localhost:5173/"` (no wildcard) | ⬜ pending |
| client-policies | 01 | 1 | SEC-03 | API check | `curl -s .../client-policies \| grep "secure-redirect-uris"` | ⬜ pending |
| request-uri | 01 | 1 | SEC-04 | API/manual | KC startup log or OIDC discovery endpoint check | ⬜ pending |
| brute-force | 01 | 2 | SEC-01 | API check | GET /admin/realms/onboarding — bruteForceProtected=true, failureFactor=5 | ⬜ pending |
| password-policy | 01 | 2 | SEC-02 | API check | Attempt create user with weak password → expect 400 | ⬜ pending |
| service-account | 01 | 2 | SEC-07 | API check | GET service account roles — only manage-users + view-users | ⬜ pending |
| admin-access | 01 | 2 | SEC-05 | Network | `docker compose ps` shows `127.0.0.1:8180` binding | ⬜ pending |
| ssl-config | 01 | 2 | SEC-06 | API check | GET /admin/realms/onboarding — sslRequired=external | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `tests/keycloak-hardening/verify-hardening.sh` — acceptance test script for all SEC-0X

*Wave 0 creates the test script as part of Plan 01 Task 1.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Admin console unreachable from remote host | SEC-05 | Dev environment uses loopback — cannot test remote access from same machine | From a second machine (or VM), attempt `curl http://<host-ip>:8180/` — should time out or be refused |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 30s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
