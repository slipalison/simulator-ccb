---
phase: 6
slug: authentication-api
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-04-06
---

# Phase 6 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit 2.9.x + Shouldly 4.x |
| **Config file** | `src/Onboarding.Tests/Onboarding.Tests.csproj` |
| **Quick run command** | `dotnet test --filter "Category=Unit" --no-build` |
| **Full suite command** | `dotnet test src/Onboarding.Tests/` |
| **Estimated runtime** | ~15 seconds (unit), ~60 seconds (integration with containers) |

---

## Sampling Rate

- **After every task commit:** Run `dotnet test --filter "Category=Unit" --no-build`
- **After every plan wave:** Run `dotnet test src/Onboarding.Tests/`
- **Before `/gsd-verify-work`:** Full suite must be green
- **Max feedback latency:** 15 seconds (unit), 60 seconds (integration)

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 06-01-01 | 01 | 1 | AUTH-02 | T-6-01 | JWT Bearer validates token from Keycloak OIDC | unit | `dotnet test --filter "FullyQualifiedName~JwtBearerConfigurationTests"` | ❌ W0 | ⬜ pending |
| 06-01-02 | 01 | 1 | AUTH-02 | T-6-02 | `[Authorize]` on route returns 401 without token | unit | `dotnet test --filter "FullyQualifiedName~AuthorizationMiddlewareTests"` | ❌ W0 | ⬜ pending |
| 06-01-03 | 01 | 2 | AUTH-03 | — | GET /api/clients/me returns 200 with valid token | integration | `dotnet test --filter "FullyQualifiedName~ClientsMeEndpointTests"` | ❌ W0 | ⬜ pending |
| 06-02-01 | 02 | 1 | AUTH-04 | T-6-03 | POST /api/auth/login returns access_token + refresh_token | integration | `dotnet test --filter "FullyQualifiedName~LoginEndpointTests"` | ❌ W0 | ⬜ pending |
| 06-02-02 | 02 | 2 | AUTH-04 | T-6-04 | POST /api/auth/refresh returns new access_token | integration | `dotnet test --filter "FullyQualifiedName~RefreshTokenEndpointTests"` | ❌ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `src/Onboarding.Tests/Authentication/JwtBearerConfigurationTests.cs` — stubs para AUTH-02
- [ ] `src/Onboarding.Tests/Authentication/AuthorizationMiddlewareTests.cs` — stubs para AUTH-02
- [ ] `src/Onboarding.Tests/Api/ClientsMeEndpointTests.cs` — stubs para AUTH-03
- [ ] `src/Onboarding.Tests/Api/LoginEndpointTests.cs` — stubs para AUTH-04
- [ ] `src/Onboarding.Tests/Api/RefreshTokenEndpointTests.cs` — stubs para AUTH-04
- [ ] `src/Onboarding.Tests/Helpers/FakeJwtTokenHelper.cs` — helper para emitir tokens falsos via `PostConfigure<JwtBearerOptions>`

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Token silently refreshed próximo à expiração | AUTH-04 | Timing-dependent — requer controle do relógio do token | Emitir token com `exp` = agora + 30s, aguardar, verificar novo token retornado |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 60s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
