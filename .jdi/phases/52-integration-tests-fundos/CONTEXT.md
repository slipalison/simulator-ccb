# Phase 53 — integration-tests-fundos — CONTEXT

## Goal

Phase 53 dupla missão:

1. **Integration tests end-to-end** Testcontainers PostgreSQL real cobrindo CRUD round-trip dos 5 entity types (Fundo, ConsultoriaFundo, Custodiante, Cedente, TipoAtivo) + 3 N-N association types (FundoCedente, CedenteTipoAtivo, FundoTipoAtivo) + state-machine transitions (Fundo + 3 association status flows) + REL-09 (uma única associação ATIVA por par Fundo-Cedente) + detecção de duplicatas (409) + multi-tenant cross-probe (D-5). Formaliza suite que Phase 51+52 começou (4 integration test files existentes — FundosControllerIntegrationTests, RelationshipAggregatesIntegrationTests, AdminFundosByIdIntegrationTests, AuditLogEntityFilterIntegrationTests).

2. **OTel JS telemetry instrumentation** ambos SPAs (frontend/client + frontend/backoffice) + first-party collector container + PII scrub processor + W3C trace context propagation end-to-end (browser → backend → DB span via OTel). Resolve W-telemetry carry-forward de Phase 51 (G2 BLOCKED pre-existing, agora atendido).

## Locked decisions (Phase 53)

- **D-33 (DA-1):** Escopo full — integration tests Fundos end-to-end + OTel JS instrumentation ambos SPAs + collector container. Sem split em sub-phase. Planner organiza waves: backend integration coverage + collector setup + frontend OTel instrumentation.

- **D-34 (DA-2):** **Test depth full** por entidade — CRUD round-trip (POST 201, GET 200 single+list, PUT 200, DELETE 204), erro paths (409 dup, 422 validation, 404 cross-tenant), state-machine transitions completas (RASCUNHO→ATIVO→SUSPENSO→HISTORICO etc nos 4 fluxos: Fundo + 3 association status), REL-09 enforcement (DB partial index + domain invariant), multi-tenant cross-probe (PJ-B request sub não vê PJ-A data → 404).

- **D-35 (DA-3):** **OTel JS manual setup** via `@opentelemetry/sdk-trace-web` + `@opentelemetry/auto-instrumentations-web` (fetch/document-load/user-interaction instrumentations) + `@opentelemetry/propagator-b3` + `W3CTraceContextPropagator`. OTLP HTTP exporter pra collector. `propagateTraceHeaderCorsUrls` allowlist exclui Keycloak (`http://localhost:8180/*`) — apenas backend API endpoints recebem trace headers. Anonymous session id no resource attributes. Bundle gate mantém ≤300 KB gz pós-OTel inclusion.

- **D-36 (DA-4):** **First-party OTel collector** via container em `docker-compose.yml`. Image `otel/opentelemetry-collector-contrib:latest`. Receivers: OTLP gRPC (4317) + HTTP (4318). Processors: `attributes` (drop sensitive: email, cpf, cnpj, sub, refresh_token, access_token, authorization, set-cookie via regex/key match) + `redact` (regex de campos PII residuais) + `memory_limiter`. Exporters: `debug` (dev console) + `otlphttp/jaeger` opcional (Jaeger UI service localhost). Backend + ambos SPAs apontam OTLP endpoint `http://otel-collector:4318/v1/traces`. Configuração em `infra/otel-collector-config.yaml`.

- **D-37 (DA-5):** **Integration test fixture**: xUnit `IClassFixture<PostgreSqlFixture>` — um Testcontainer PG por classe. Cada teste roda em transação que faz rollback no Dispose (`AppDbContext.Database.BeginTransaction()` no setup, `transaction.Rollback() + Dispose()` no teardown). Isola dados sem reiniciar container — ~30s por classe vs ~3min container restart por teste. Padrão herdado das classes Phase 51+52 (FundosControllerIntegrationTests etc) — formalizar como base class `PostgreSqlIntegrationTestBase`.

## Canonical refs

- `.jdi/DECISIONS.md` D-5 (multi-tenant CRITICAL), D-9/D-22 (state-machine pattern), D-18 (REL-09 partial index + domain invariant), D-21 (associations symmetric shape), D-33..D-37 (esta phase).
- `.jdi/PROJECT.md` Definition of Done section — phase 53 cumpre policy (every test = integrated runtime evidence).
- `tests/Onboarding.Integration.Tests/` — 4 arquivos Fundos existentes; phase 53 extende cobertura + base class refactor.
- `tests/Onboarding.Integration.Tests/Fundos/FundosControllerIntegrationTests.cs` — gold pattern (Testcontainers + WebApplicationFactory).
- `tests/Onboarding.Integration.Tests/Fundos/RelationshipAggregatesIntegrationTests.cs` — pattern N-N relationships + REL-09.
- `src/Onboarding.API/Program.cs` — backend OTel já wired (OTLP exporter); collector endpoint env var.
- `src/Onboarding.API/Observability/` — backend Telemetry/Metrics namespace (existente).
- `frontend/client/src/main.tsx` + `frontend/backoffice/src/main.tsx` — entry points pra wire OTel SDK init pré-React mount.
- `infra/` — docker-compose.yml + otel-collector-config.yaml + jaeger.yml (opcional).
- `keycloak/*-realm.json` — verificar CORS allowlist permite OTLP collector caso navegador envia spans direto (geralmente envio é via fetch proxied — mas confirmar).

## Out of scope

- **Vinxi → Vinext migration** — Phase 54 dedicada.
- **CI pipeline OTel collector** — Phase 53 cobre dev/local stack. Produção/CI fica futuro (configuração environment-specific).
- **Sentry integration** — D-3 OSS-only proíbe.
- **Performance benchmarks** — integration tests cobrem correctness, não perf. Benchmark suite phase futura se necessário.
- **Frontend behavior changes** — Phase 53 ADICIONA instrumentação; não muda comportamento UI/API.
- **Mobile telemetry** — fora de escopo (sem app mobile no projeto).
- **Audit log telemetry separation** — AdminAuditLog continua sendo o canal de business audit; OTel tracks request flow distinct.
- **Tests for /scripts/* shell** — out of scope.
- **W-react-setstate** Transitioner pre-existing (Phase 51 carry) — não tratado nesta phase.
- **W-gitignore lib/ negation cleanup** — operacional, future cleanup phase.

## Notes

- **Multi-tenant cross-probe pattern (D-34):** padrão herdado de FundosControllerIntegrationTests + RelationshipAggregatesIntegrationTests Phase 50: cria 2 Companies (PJ-A + PJ-B) + entities em cada, então PJ-B request para entidade de PJ-A retorna 404 (não 403 — não revela existência cross-tenant). Aplica a 5 entidades + 3 associations + audit-log.

- **State-machine integration test coverage:** cobertura completa exige exercer cada transição válida + cada inválida via real endpoint (POST /status). Cada handler de transição (TransitionFundoStatusCommandHandler etc) já loga AdminAuditLog com entityType+entityId (Phase 52 T-1). Test exerce endpoint + assert audit row aparece + assert state válido pós-transição + assert InvalidStatusTransitionException → 400 em transição inválida.

- **REL-09 integration coverage (D-18 defesa em profundidade):** DB partial unique index `(FundoId, CedenteId) WHERE Status='ATIVO'` + domain `DuplicateActiveAssociationException`. Test: criar FundoCedente ATIVO, tentar criar OUTRO ATIVO mesmo par → 409 (via domain exception OR via race-condition DB constraint via concurrent insert).

- **PostgreSqlIntegrationTestBase refactor:** extrair fixture comum + transaction wrapper + WebApplicationFactory setup das 4 classes existentes. Cada classe atual herda da base. Reduz duplicação ~30% por classe.

- **OTel JS bundle impact:** auto-instrumentations-web pode pesar ~50 KB gz. Bundle gate atual: client 210 KB + backoffice 205 KB. Pós-OTel: ainda ≤300 KB gz target. Se exceder, code-split OTel init em chunk separado (lazy load post-mount).

- **Collector PII scrub critical (D-36):** OTel browser spans podem capturar URL com query params (search=email@x.com), referer header, navigation. Processor `attributes` deve dropar/redactar atributos `http.url` query + `http.request.header.*` sensitive. Test integração: span emitted → collector pipeline → assert sanitizado.

- **W3C propagation chain:** browser fetch dispara span → trace-id + span-id em `traceparent` header → backend ASP.NET middleware extrai → backend span filho → DB query span. End-to-end trace visível em Jaeger UI (opcional dev local).

- **Specialist routing:** integration tests → backend C# specialist. OTel JS SPAs → frontend Vinext specialist. Collector container + docker-compose → security specialist (cross-cutting infra) ou frontend (collector é dev tooling). Coordenar.

## Definition of Done (Phase 53 specific — derived from PROJECT.md DoD policy)

### Integration tests

**Por entidade (5 + 3 associations + audit + state-machine):**
- [ ] CRUD round-trip: POST 201 + GET single 200 + GET list 200 + PUT 200 + DELETE 204 — todos via real HTTP contra Testcontainers PG.
- [ ] Error paths: 409 duplicate, 422 validation, 404 cross-tenant, 404 not-found.
- [ ] State-machine transitions (apenas 4 entidades com FSM: Fundo, FundoCedente, FundoTipoAtivo, CedenteTipoAtivo): cada transição válida assertada + cada inválida retorna 400 com InvalidStatusTransitionException.
- [ ] REL-09 (FundoCedente apenas): segunda associação ATIVA mesmo par → 409 (defesa em profundidade DB + domain).
- [ ] AdminAuditLog: pós state-machine transition, row inserido com entityType+entityId+actorSub+actorEmail correto.
- [ ] Multi-tenant cross-probe: PJ-B request entity de PJ-A → 404 (não 403, não data leak).

### OTel JS telemetry

- [ ] `@opentelemetry/sdk-trace-web` + `auto-instrumentations-web` instalados ambos SPAs (independent install per D-4).
- [ ] OTel init em `main.tsx` antes `ReactDOM.render` em ambos SPAs.
- [ ] `propagateTraceHeaderCorsUrls` allowlist EXCLUDES `localhost:8180/*` (Keycloak).
- [ ] Bundle main ≤300 KB gz ambos SPAs pós-OTel.
- [ ] No `console.*` em produção bundle (anti-pattern).

### Collector

- [ ] `docker-compose.yml` adiciona service `otel-collector` (image otel/opentelemetry-collector-contrib).
- [ ] `infra/otel-collector-config.yaml` com receivers OTLP HTTP+gRPC + processors attributes+redact+memory_limiter + exporter debug+jaeger (opcional).
- [ ] PII scrub: span emitted com email/cpf/cnpj/sub/token NO atributo → collector loga sanitizado.
- [ ] W3C propagation: browser fetch → backend → DB span end-to-end via traceparent header.

### Evidência obrigatória em REVIEW.md

- xUnit integration suite output (X/X passed, duration) — backend + frontend coverage gates ≥80% nas zonas D-2 novas.
- docker-compose up: collector + Jaeger UI screenshot mostrando spans end-to-end.
- Collector log paste mostrando PII sanitizado.
- Bundle Vinxi reports both SPAs pós-OTel.
- Network MCP filter from browser MCP showing `traceparent` header em request `/api/*` (não em `/realms/*` Keycloak).

### Verdict thresholds

- APPROVED se: todos integration tests pass + collector PII scrub verified + bundle ≤300 KB gz ambos SPAs + no token storage regression.
- APPROVED_WITH_WARNINGS aceitável para: bundle ligeiramente acima target com lazy-split docs (não acima 350 KB gz) OU collector exporter Jaeger NÃO wired (debug-only OK dev) OU minor PII scrub gap docs.
- BLOCKED se: state-machine transition test fail OR REL-09 violation OR multi-tenant cross-probe leak OR token em browser storage OR OTel não inicializa OR collector não recebe spans.
