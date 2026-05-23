# Phase 53: integration-tests-fundos — Plan  (slug: integration-tests-fundos)

## Goal

Dupla missão: (1) Integration tests end-to-end Testcontainers PG cobrindo 5 entity types + 3 N-N associations + state-machine + REL-09 + multi-tenant cross-probe — formaliza suite e fecha gaps; (2) OTel JS telemetry ambos SPAs + first-party collector com PII scrub + W3C propagation browser→backend→DB.

## Locked decisions (from CONTEXT.md)

- **D-33:** Escopo full (integration + OTel JS + collector).
- **D-34:** Test depth FULL — CRUD + state-machine + errors + REL-09 + multi-tenant cross-probe.
- **D-35:** OTel JS via `@opentelemetry/sdk-trace-web` + auto-instrumentations + W3C propagator; `propagateTraceHeaderCorsUrls` exclude Keycloak.
- **D-36:** First-party collector docker-compose + PII scrub processor.
- **D-37:** `IClassFixture<PostgreSqlFixture>` + transaction rollback; refactor `PostgreSqlIntegrationTestBase`.

## Tasks

### Wave 1 (parallel-eligible)

#### T-1: Refactor PostgreSqlIntegrationTestBase + fixture extraction
- **Specialist:** jdi-doer-onboarding-keycloak-backend-csharp
- **Files modified:**
  - `tests/Onboarding.Integration.Tests/Fixtures/PostgreSqlFixture.cs` (new — IClassFixture base extracting Testcontainer setup)
  - `tests/Onboarding.Integration.Tests/Fixtures/PostgreSqlIntegrationTestBase.cs` (new — base class with WebApplicationFactory + transaction begin/rollback wrapper)
  - `tests/Onboarding.Integration.Tests/Fundos/FundosControllerIntegrationTests.cs` (refactor to inherit base)
  - `tests/Onboarding.Integration.Tests/Fundos/RelationshipAggregatesIntegrationTests.cs` (refactor)
  - `tests/Onboarding.Integration.Tests/Admin/AdminFundosByIdIntegrationTests.cs` (refactor)
  - `tests/Onboarding.Integration.Tests/Admin/AuditLogEntityFilterIntegrationTests.cs` (refactor)
  - `tests/Onboarding.Integration.Tests/Registration/RegistrationIntegrationTests.cs` (refactor if pattern applies; else skip)
- **Acceptance (DoD G0):**
  - All 4 existing integration test files refactored to inherit `PostgreSqlIntegrationTestBase`.
  - Transaction rollback wrapper isolates tests per D-37 — verify via test order independence (xunit run with `Parallel = false` first, then `Parallel = true`).
  - Existing 61 integration tests STILL pass (no behavioral change).
  - Code reduction ~30% per class (extracted fixture + setup).
- **Dependencies:** none
- **Test:** xUnit (Integration) — re-run existing 61 tests
- **Status:** pending

#### T-5: OTel collector container + config + PII scrub
- **Specialist:** jdi-doer-onboarding-keycloak-security (cross-cutting infra)
- **Files modified:**
  - `docker-compose.yml` (add service `otel-collector` image `otel/opentelemetry-collector-contrib:latest`, ports 4317 gRPC + 4318 HTTP, depends_on PostgreSQL? no — collector independent)
  - `docker-compose.yml` (optional service `jaeger` image `jaegertracing/all-in-one:latest` for dev UI at 16686)
  - `infra/otel-collector-config.yaml` (new — receivers OTLP gRPC+HTTP, processors attributes (drop email/cpf/cnpj/sub/refresh_token/access_token/authorization/set-cookie) + redact (regex PII residual) + memory_limiter, exporters debug + otlphttp/jaeger optional)
  - `src/Onboarding.API/Program.cs` (OTel OTLP exporter endpoint env var `OTEL_EXPORTER_OTLP_ENDPOINT` defaults `http://otel-collector:4318`)
  - `docker-compose.yml` (api service env `OTEL_EXPORTER_OTLP_ENDPOINT=http://otel-collector:4318`)
  - `docs/dev-setup.md` (section: "OTel telemetry — collector + Jaeger UI", explica `docker compose up -d otel-collector jaeger`, abrir http://localhost:16686 para Jaeger UI dev)
- **Acceptance (DoD G0):**
  - `docker compose up -d otel-collector` boots collector; logs show `Everything is ready. Begin running and processing data.`.
  - PII scrub test: send test span via curl OTLP HTTP com atributo `email=alison@x.com` → collector log mostra atributo dropped/redacted.
  - Backend Program.cs exporter aponta collector OTLP endpoint.
  - Jaeger UI (se habilitado) acessível em http://localhost:16686.
- **Dependencies:** none
- **Test:** docker-compose smoke + curl OTLP test span
- **Status:** pending

#### T-6: Frontend client OTel JS instrumentation
- **Specialist:** jdi-doer-onboarding-keycloak-frontend-vinext
- **Files modified:**
  - `frontend/client/package.json` (add `@opentelemetry/api`, `@opentelemetry/sdk-trace-web`, `@opentelemetry/auto-instrumentations-web`, `@opentelemetry/context-zone`, `@opentelemetry/propagator-b3`, `@opentelemetry/exporter-trace-otlp-http`, `@opentelemetry/resources`, `@opentelemetry/semantic-conventions`)
  - `frontend/client/src/lib/telemetry.ts` (new — initialize WebTracerProvider, ZoneContextManager, W3CTraceContextPropagator, OTLP HTTP exporter pointing to `http://localhost:4318/v1/traces`, auto-instrumentations-web with `propagateTraceHeaderCorsUrls` allowlist excluding `http://localhost:8180/*` (Keycloak), resource attributes including anonymous `session.id`)
  - `frontend/client/src/main.tsx` (import + call `initTelemetry()` BEFORE `ReactDOM.render`)
  - `frontend/client/vitest.config.ts` (mock @opentelemetry/* in tests if needed)
  - `frontend/client/src/tests/lib/telemetry.test.ts` (vitest assertion init runs without throwing; mock fetch + assert span emit shape)
- **Acceptance (DoD G0):**
  - `pnpm --filter frontend-client build` — main bundle ≤300 KB gz. If exceed, code-split telemetry init.
  - `docker compose up -d` + MCP browser navigate `/` → Network MCP filter `traceparent` header IS present on `/api/*` requests, NOT on `/realms/*` Keycloak requests.
  - Collector receives spans from client SPA (verify in collector log).
  - Console MCP: zero `[OTel]` error logs.
- **Dependencies:** none (parallel with backend)
- **Test:** Vitest + Playwright MCP (network filter)
- **Status:** pending

#### T-7: Frontend backoffice OTel JS instrumentation
- **Specialist:** jdi-doer-onboarding-keycloak-frontend-vinext
- **Files modified:**
  - `frontend/backoffice/package.json` (add OTel JS deps — INDEPENDENT install per D-4)
  - `frontend/backoffice/src/lib/admin-telemetry.ts` (new — symmetric to client telemetry.ts, but NO shared lib path)
  - `frontend/backoffice/src/main.tsx` (init telemetry pre-render)
  - `frontend/backoffice/vitest.config.ts` (mock OTel in tests if needed)
  - `frontend/backoffice/src/tests/lib/admin-telemetry.test.ts`
- **Acceptance (DoD G0):**
  - Bundle ≤300 KB gz post-OTel.
  - MCP `/admin/login` → Network MCP filter `traceparent` on `/api/admin/*` requests, NOT on Keycloak realm requests.
  - Collector receives spans from backoffice SPA.
  - Zero token leak (regression D-12) — verify `browser_evaluate` for storage.
- **Dependencies:** none (parallel — symmetric with T-6)
- **Test:** Vitest + Playwright MCP
- **Status:** pending

### Wave 2 (parallel-eligible — depends on T-1)

#### T-2: Integration tests — Fundo + ConsultoriaFundo + Custodiante CRUD + state-machine + audit
- **Specialist:** jdi-doer-onboarding-keycloak-backend-csharp
- **Files modified:**
  - `tests/Onboarding.Integration.Tests/Fundos/FundoCrudIntegrationTests.cs` (new OR extend existing — full CRUD + state-machine RASCUNHO→ATIVO→SUSPENSO→HISTORICO + 409 dup + 422 validation + 404 cross-tenant + audit row assert)
  - `tests/Onboarding.Integration.Tests/Fundos/ConsultoriaFundoIntegrationTests.cs` (new — full CRUD + 409 CNPJ dup company-scoped per D-10 + multi-tenant cross-probe)
  - `tests/Onboarding.Integration.Tests/Fundos/CustodianteIntegrationTests.cs` (new — analog)
- **Acceptance (DoD G0):**
  - Por entidade: POST 201 + GET single 200 + GET list 200 paginated + PUT 200 + DELETE 204 (where applicable).
  - Fundo state-machine: cada transição válida → audit row inserido com entityType="Fundo" + correct ActorSub.
  - Multi-tenant: PJ-B request Fundo de PJ-A → 404.
  - Validation: missing required field → 422 with ProblemDetails errors[].
  - Coverage ≥80% on new test files + exercised handler/repo new code lines.
- **Dependencies:** T-1
- **Test:** xUnit Integration (Testcontainers)
- **Status:** pending

#### T-3: Integration tests — Cedente PF+PJ + TipoAtivo CRUD + uniqueness
- **Specialist:** jdi-doer-onboarding-keycloak-backend-csharp
- **Files modified:**
  - `tests/Onboarding.Integration.Tests/Fundos/CedenteCrudIntegrationTests.cs` (new — CedentePf + CedentePj CRUD + 409 CPF/CNPJ dup company-scoped D-10 + 404 cross-tenant)
  - `tests/Onboarding.Integration.Tests/Fundos/TipoAtivoCrudIntegrationTests.cs` (new — CRUD global catalog D-5; no cross-tenant test since global; 409 codigo dup)
- **Acceptance (DoD G0):**
  - CedentePf POST → 201 with CPF. Outra empresa pode ter mesmo CPF (D-10) — test confirms.
  - CedentePj POST → 201 with CNPJ. Same company duplicate CNPJ → 409.
  - TipoAtivo: global catalog, no tenant filter. Duplicate codigo → 409.
- **Dependencies:** T-1
- **Test:** xUnit Integration
- **Status:** pending

#### T-4: Integration tests — 3 N-N associations + REL-09
- **Specialist:** jdi-doer-onboarding-keycloak-backend-csharp
- **Files modified:**
  - `tests/Onboarding.Integration.Tests/Fundos/FundoCedenteAssociationIntegrationTests.cs` (new — CRUD + state-machine + REL-09 enforcement)
  - `tests/Onboarding.Integration.Tests/Fundos/CedenteTipoAtivoAssociationIntegrationTests.cs` (new)
  - `tests/Onboarding.Integration.Tests/Fundos/FundoTipoAtivoAssociationIntegrationTests.cs` (new)
  - Pode refactor RelationshipAggregatesIntegrationTests.cs existente para distribuir cobertura entre 3 arquivos.
- **Acceptance (DoD G0):**
  - REL-09: criar FundoCedente ATIVO, tentar criar OUTRO ATIVO mesmo par → 409 (DuplicateActiveAssociationException OR DB partial unique index race).
  - Status transition flows nas 3 associações (ATIVO↔INATIVO + INATIVO→HISTORICO terminal).
  - Audit row gravado per D-22 com entityType correct ("FundoCedente"/"CedenteTipoAtivo"/"FundoTipoAtivo").
  - Multi-tenant: PJ-B → association de PJ-A → 404.
  - Janela de datas D-20 + LimiteExposicaoPercentual/Valor D-18 invariants tested.
- **Dependencies:** T-1
- **Test:** xUnit Integration
- **Status:** pending

### Wave 3 (sequential — depends on T-5, T-6, T-7)

#### T-8: End-to-end OTel verification + Jaeger UI evidence
- **Specialist:** jdi-doer-onboarding-keycloak-frontend-vinext (Playwright MCP + collector log assertion)
- **Files modified:**
  - `frontend/client/tests/e2e/otel-trace.spec.ts` (new — login flow + dispatch API call + assert traceparent header on `/api/*`, NOT on `/realms/*`)
  - `frontend/backoffice/tests/e2e/otel-trace.spec.ts` (new — analog)
  - `docs/dev-setup.md` (extend OTel section with end-to-end trace inspection workflow via Jaeger UI)
  - `infra/otel-collector-config.yaml` (refine PII scrub regex if test reveals gaps)
- **Acceptance (DoD G0):**
  - End-to-end browser→backend→DB trace visible in Jaeger UI (screenshot mandatory).
  - PII scrub working: span with `email`/`cpf`/`cnpj` attribute → collector log shows DROPPED/REDACTED.
  - `traceparent` header propagates browser → backend (Network MCP filter).
  - Keycloak requests do NOT carry trace headers (cross-origin allowlist exclude).
  - Bundle final: client ≤300 KB gz + backoffice ≤300 KB gz post OTel inclusion.
  - All integration tests (Wave 2) still pass (no regression from collector container startup).
- **Dependencies:** T-5, T-6, T-7
- **Test:** Playwright MCP + collector log inspection + Jaeger UI screenshot
- **Status:** pending

## Execution

- Total tasks: 8
- Waves: 3
- Estimated parallel speedup: ~2.67x

## Files modified (summary)

- Tests: `tests/Onboarding.Integration.Tests/{Fixtures,Fundos}/**` — new base class + 6 new test files + refactor of 4 existing.
- Backend: `src/Onboarding.API/Program.cs` — OTel endpoint env wire.
- Infra: `docker-compose.yml`, `infra/otel-collector-config.yaml`.
- Frontend: `frontend/{client,backoffice}/{package.json,src/lib/*telemetry.ts,src/main.tsx,vitest.config.ts,tests/e2e/otel-trace.spec.ts}` — OTel JS instrumentation symmetric (independent installs).
- Docs: `docs/dev-setup.md` (collector + Jaeger UI sections).

## Test requirements

- Backend integration: xUnit + Testcontainers + Shouldly + NSubstitute. `dotnet test` with Docker available locally OR CI.
- Frontend unit: Vitest mocking OTel SDK where applicable.
- Frontend e2e: Playwright MCP + network filter assertions on `traceparent` header.
- Collector: docker-compose smoke + PII scrub log inspection.
- Coverage ≥80% on new test files (D-2 boundary).
- Bundle gate: client + backoffice main ≤300 KB gz post-OTel.

## DoD enforcement note (per PROJECT.md policy)

Reviewer DEVE seguir checklist DoD em CONTEXT.md sections "Integration tests", "OTel JS telemetry", "Collector", "Evidência obrigatória". Verdict APPROVED exige TODOS confirmados live (não unit-only). Warnings tipo "Docker not available", "MCP not run", "Jaeger not booted" SÃO BLOCKERS disguised — não warnings.
