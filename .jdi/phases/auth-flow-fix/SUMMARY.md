# Phase 49 --- auth-flow-fix --- SUMMARY

### T-4 (2026-05-16T00:00:00Z)

**Status:** PASS --- zero code changes. All four checks confirmed correct.

**Commit sha:** none --- verification only

**Files modified:** none --- verification only

---

#### Check 1 --- Issuer match

| Scheme | Config key | appsettings.json value | ValidIssuer resolved |
|---|---|---|---|
| BearerBackoffice | Keycloak:ValidIssuer | http://localhost:8180/realms/backoffice | http://localhost:8180/realms/backoffice |
| BearerClient | Keycloak:ClientRealmUrl .Replace(keycloak:8080 -> localhost:8180) | http://localhost:8180/realms/client (no internal hostname, Replace is no-op) | http://localhost:8180/realms/client |

Both ValidIssuer values resolve to http://localhost:8180/realms/{realm} using the public Keycloak URL, matching JWTs issued by Keycloak. No drift vs compose.yaml wiring (KEYCLOAK_REALM=client line 115 / KEYCLOAK_REALM=backoffice line 140). Authority values confirmed: BackofficeRealmUrl = http://localhost:8180/realms/backoffice, ClientRealmUrl = http://localhost:8180/realms/client. ValidateAudience = false on both schemes intentional (D-05).

#### Check 2 --- CORS allowlist (Program.cs:254)

Origins whitelisted: http://localhost:5173 (client SPA) and http://localhost:5174 (backoffice SPA). No wildcard, no AllowAnyOrigin(), no origin reflection. AllowCredentials() present. SecurityHeaders:AllowedOrigins in appsettings.json mirrors the same two origins. Matches D-15 gate exactly.

#### Check 3 --- Middleware order (Program.cs:284-294)

UseCors -> UseAuthentication -> UseAuthorization confirmed. Session middlewares (UseAdminSession, UseClientSession) run before UseAuthentication --- correct for cookie-to-Bearer conversion.

#### Check 4 --- Test results

| Suite | Passed | Failed | Skipped | Duration |
|---|---|---|---|---|
| Onboarding.API.Tests | 244 | 0 | 4 | ~1m 47s |
| Onboarding.Integration.Tests | 20 | 0 | 0 | ~3m 13s |

Both suites green. The 4 skipped tests are pre-existing (TracePropagationTests x2 + AdminCompanyDetailsTests x2).

---

**Conclusion:** Backend auth wiring is correct and aligned with runtime config. No defect found. No code change required.
