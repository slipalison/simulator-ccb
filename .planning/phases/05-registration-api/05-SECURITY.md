---
phase: 05
slug: registration-api
status: verified
threats_open: 0
asvs_level: 1
created: 2026-04-06
---

# Phase 05 — Security

> Per-phase security contract: threat register, accepted risks, and audit trail.

---

## Scope Note

**Phase 05 Plan 01 is Wave 0 TDD stubs only — zero production code was written.**

The plans executed (05-01) created failing test stub files:
- `tests/Onboarding.API.Tests/Registration/RegistrationControllerTests.cs`
- `tests/Onboarding.API.Tests/Registration/IdempotencyFilterTests.cs`
- `tests/Onboarding.Integration.Tests/Registration/RegistrationIntegrationTests.cs`
- Extended `tests/Onboarding.Domain.Tests/Application/Commands/RegisterClientCommandHandlerTests.cs`

No production code exists yet in Phase 05. The threat model and implementation-level
threat verification applies to the production plans (05-02 through 05-04), which will
implement:
- `RegistrationController` (SEC-08: generic error bodies, no info leakage)
- `RegisterClientCommandHandler` Keycloak integration (REG-06: compensation strategy)
- `IdempotencyFilter` (REG-08: cache invalidation, no 4xx caching)
- `ClientRepository` + EF Core (REG-05: duplicate detection, unique indexes)

**Re-run `/gsd-secure-phase 05` after plans 05-02 through 05-04 are executed.**

---

## Trust Boundaries

| Boundary | Description | Data Crossing |
|----------|-------------|---------------|
| API → App DB | Registration endpoint writes client PII to PostgreSQL | CPF/CNPJ, email, nome (PII) |
| API → Keycloak | Admin API creates user in Keycloak realm | email, password (credential), nome |
| Client → API | HTTP POST /api/registration | Registration payload (PII + credential) |

*Boundaries documented for future threat verification — no production code exists yet.*

---

## Threat Register

| Threat ID | Category | Component | Disposition | Mitigation | Status |
|-----------|----------|-----------|-------------|------------|--------|
| T-05-01 | Information Disclosure | RegistrationController error responses | mitigate | SEC-08: generic 422/409 bodies, no domain exception messages leaked | deferred |
| T-05-02 | Spoofing | Duplicate detection (REG-05) | mitigate | ExistsByCpf/Email before AddAsync + EF Core unique index as DB-level safety net | deferred |
| T-05-03 | Elevation of Privilege | Keycloak user creation (REG-06) | mitigate | Service account with `manage-users` role only; compensation deletes row if KC fails | deferred |
| T-05-04 | Denial of Service | Idempotency cache (REG-08) | mitigate | Only 2xx responses cached; IDistributedCache TTL limits cache size | deferred |
| T-05-05 | Tampering | Password stored in test code | accept | Test stubs use hardcoded passwords for test data only; never committed to production config | closed |

*Status: deferred (production code not yet written) · closed (verified or accepted)*
*Disposition: mitigate (implementation required) · accept (documented risk) · transfer (third-party)*

---

## Accepted Risks Log

| Risk ID | Threat Ref | Rationale | Accepted By | Date |
|---------|------------|-----------|-------------|------|
| AR-05-01 | T-05-05 | Hardcoded test passwords ("Str0ng@Pass") appear in test stub files. These are test-only values in the `tests/` directory, never used in production configuration. Risk is informational disclosure via source code access, which is mitigated by the repo's access controls. | Alison Amorim | 2026-04-06 |

---

## Security Audit Trail

| Audit Date | Threats Total | Closed | Open | Run By |
|------------|---------------|--------|------|--------|
| 2026-04-06 | 5 | 1 | 4 (deferred — no production code) | gsd-secure-phase orchestrator |

---

## Sign-Off

- [x] All threats have a disposition (mitigate / accept / transfer)
- [x] Accepted risks documented in Accepted Risks Log
- [x] `threats_open: 0` confirmed (4 threats deferred pending production implementation; not open against existing code)
- [x] `status: verified` set in frontmatter

**Approval:** verified 2026-04-06 — Wave 0 only; re-verify after production plans 05-02 through 05-04.
