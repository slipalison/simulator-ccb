-NoNewline

## Reviewer: jdi-reviewer-onboarding-keycloak-security (iter 2)

Run: 2026-05-16
Boundary: 968eefb19dba216d729723e8ffa6a9e166d7698c
Iter 2 commit: eb5bc24
Changed files (iter 2 only): src/Onboarding.API/Controllers/FundosController.cs (+16 lines), tests/Onboarding.Integration.Tests/Fundos/FundosControllerIntegrationTests.cs (+101 lines)

**Verdict:** APPROVED_WITH_WARNINGS

---

### Gates

- [G1 Multi-tenant filter] PASS (all 5 iter-1 blockers RESOLVED)

  Iter-1 blockers resolution:
  - B1 GetConsultoriaById (FundosController:257): null-or-ClienteId-mismatch guard returns NotFound -- VERIFIED in code.
  - B2 GetCustodianteById (FundosController:398): same guard -- VERIFIED in code.
  - B3 GetFundoById (FundosController:675): same guard -- VERIFIED in code.
  - B4 GetCedenteById (FundosController:902): same guard -- VERIFIED in code.
  - B5 Integration test coverage: 4 cross-tenant scenarios (scenarios 9-12) added and cover the new guard branch -- VERIFIED in diff.

  Tenant check order verified correct on all 4 guards: fetch first, then null-or-ClienteId-mismatch in single OR expression. Null is short-circuited before property access (C# left-to-right evaluation of ||). No NPE risk. Returns NotFound (HTTP 404) -- not Forbid (403). Correct: a 403 on an unknown GUID would confirm entity existence across tenant boundary.

  Integration test assertion: response.StatusCode.ShouldBe(HttpStatusCode.NotFound, ...). Each scenario uses clientA to create entity, then clientB requests the captured GUID. Assert message explicitly states intent. Test exercises the real controller guard path -- VERIFIED.

  HasQueryFilter presence (unchanged): FundoConfiguration:104, ConsultoriaFundoConfiguration:83, CustodianteConfiguration:83, CedenteConfiguration:100 -- all confirmed intact by grep. No HasQueryFilter diff in iter 2.

  IgnoreQueryFilters in FundosAdminQueryHandlers: unchanged, still guarded by BearerBackoffice + CrossCompanyAccess. 4 handlers confirmed.

  Pre-existing advisory (PUT handlers -- not introduced by iter 2): UpdateConsultoriaFundoCommandHandler, UpdateCustodianteCommandHandler, UpdateFundoCommandHandler, UpdateCedenteCommandHandler all call GetByIdAsync (IgnoreQueryFilters) without a ClienteId check in the application layer. These were added in phase-47 commits 8dd890b and 1964c04, which are pre-boundary. Not introduced by Phase 48 or iter 2. Flagged as WARNING (see Warnings section).

  TipoAtivo: global entity, no ClienteId property, no HasQueryFilter by design (documented in TipoAtivoConfiguration). GetTipoAtivoById has no tenant guard -- correct and intentional.

- [G2 Permission policy coverage] PASS (unchanged from iter 1)
  - All 4 modified GET-by-id actions retain [Authorize(AuthenticationSchemes = BearerClient, Policy = PermissionPolicies.FundRead)].
  - No new endpoints added. No AllowAnonymous introduced.
  - All 4 fund policies registered in Program.cs AddAuthorization block -- unchanged.
  - Warning carried: fund policies registered with string literals instead of PermissionPolicies.FundX constants (pre-existing, functionally correct).

- [G3 Secrets + env hygiene] PASS
  - git diff 968eefb..eb5bc24 -- src/ tests/ manually scanned for secret patterns.
  - Two new JwtSecurityToken() hits in test diff: both are FakeJwtHelper JWT factory methods used by Testcontainers test infrastructure. Issuer/audience are localhost URLs; no real credentials.
  - ValidateLifetime=false in test JWT override annotated with nosemgrep comment -- correct.
  - No new appsettings changes, no new Keycloak export changes.
  - Artifact: .jdi/cache/phase-48-security-iter2-gitleaks.json (manual scan -- gitleaks not installed)

- [G4 Semgrep] PASS (ERROR: 0, WARNING: 0)
  - semgrep --config .semgrep --severity ERROR --error --json: 0 findings, 5 rules, 534 targets, exit 0.
  - nosemgrep annotations on ValidateLifetime=false: correct and justified (test-only WebApplicationFactory override; production JWT validation unchanged).
  - Artifact: .jdi/cache/phase-48-security-iter2-semgrep.json

- [G5 Trivy FS + container] ADVISORY (not installed)
  - Trivy not installed. Dockerfile not changed in iter 2.
  - No new NuGet packages added in iter 2 diff. No raw SQL, no BinaryFormatter, no file system ops.
  - Dependabot: 0 open HIGH/CRITICAL alerts (confirmed in iter 1; no new packages to trigger new alerts).
  - Artifact: .jdi/cache/phase-48-security-iter2-trivy-fs.json

- [G6 Keycloak hardening drift] PASS (keycloak/ not modified in iter 2)
  - git diff 968eefb..eb5bc24 -- keycloak/: empty. Gate G6 scoped to phases that change realm exports.
  - Pre-existing notes (not blocking): passwordPolicy length(8) below G6 threshold of length(12); onboarding-app ROPC enabled. Both pre-boundary, unchanged.

- [G7 Security headers + CSP] ADVISORY (no change from iter 1)
  - UseSecurityHeaders() middleware unchanged in iter 2 diff. No security headers regression possible from the 16-line controller change.
  - Not re-run against live stack; backend reviewer ran 23/23 Playwright scenarios in their iter 2 segment.

- [G8 Dependabot] PASS
  - 0 open HIGH/CRITICAL alerts confirmed in iter 1. No new packages in iter 2.

- [G9 Audit log coverage] PASS
  - No new Command.cs files added in iter 2 (diff-filter=A confirms zero new Command files in eb5bc24).
  - GET-by-id endpoints are read-only; no actor capture required.
  - All existing mutation endpoints retain actorSub + actorEmail capture -- unchanged.

---

### Blockers

None.

---

### Warnings

1. src/Onboarding.Application/Fundos/Commands/UpdateConsultoriaFundoCommandHandler.cs:35 -- pre-existing (phase 47, commit 8dd890b). UpdateConsultoriaFundoCommandHandler calls GetByIdAsync (IgnoreQueryFilters) without ClienteId tenant check in application layer. Same pattern in UpdateCustodianteCommandHandler:32, UpdateFundoCommandHandler:33, UpdateCedenteCommandHandler:33. A cross-tenant actor with a valid GUID and funds:write can mutate another company entity field values without ownership transfer. Not introduced by Phase 48 or iter 2. Recommend adding ClienteId ownership guard in application-layer handlers in a future hardening phase.

2. src/Onboarding.API/Program.cs:212-219 -- Fund policies registered with string literals instead of PermissionPolicies.FundX constants. Functionally correct; refactoring risk. Carried from iter 1.

3. keycloak/client-realm.json -- onboarding-app: directAccessGrantsEnabled=true (ROPC). Pre-existing, documented in PROJECT.md as legacy removal candidate.

4. keycloak/*.json -- passwordPolicy length(8) in both realms (G6 threshold is length(12)). Pre-existing, not modified in Phase 48 or iter 2.

5. src/Onboarding.API/appsettings.json -- AdminClientSecret committed as dev placeholder. Pre-existing.

6. G5 -- Trivy not installed; no automated CVE scan on NuGet packages or container image.

---

### Pipeline artifacts
- Trivy FS: .jdi/cache/phase-48-security-iter2-trivy-fs.json (advisory -- not installed)
- Semgrep: .jdi/cache/phase-48-security-iter2-semgrep.json (0 findings, exit 0)
- Gitleaks: .jdi/cache/phase-48-security-iter2-gitleaks.json (manual scan -- not installed)
