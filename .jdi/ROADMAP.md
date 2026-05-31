# Onboarding PF/PJ — Roadmap (adopted)

## Status
adopted: true
current_phase: none
total_phases: 55

## Context
Projeto adotado em 2026-05-11. Vinha sendo desenvolvido com GSD (`.planning/`, milestones v1-v8). Phases 1-47 estao completas e documentadas em `.planning/phases/`. JDI continua daqui em diante, comecando em **Phase 48** que estava em flight (plans criados em commit `968eefb`, execucao nao iniciada).

Numeracao preservada pra alinhar com `.planning/` historico. Phases pre-48 nao sao re-executadas — sao contexto.

## Phases historicas (GSD `.planning/`, ja completas)
- v1.0 Foundation (1-10): docker infra, Keycloak hardening, DDD domain, observability, registration/auth API, frontend foundation, registration/login/profile UI
- v2.0 UX/UI + Production (11-15)
- v3.0 Admin Backoffice + Frontend Separation (16-20)
- v4.0 CI/CD + Cybersecurity (21-28)
- v5.0 Auth Code Flow + Admins + Auditoria (29-34)
- v6.0 Gestao Completa de Administradores (35-36)
- v7.0 PJ-Only Onboarding + Funcionarios (37-44)
- v8.0 Gestao de Fundos: Phase 45 Domain (done), Phase 46 Infrastructure (done), Phase 47 Application (done — 3 plans com SUMMARY, sem VERIFICATION formal — JDI fara verify retroativo se necessario)

## Phases ativas JDI

### Phase 48: API + Permissions for Fundos
- **Slug:** 48-api-permissions
- **Status:** done (shipped 2026-05-16, verdict APPROVED_WITH_WARNINGS, 4 iter)
- **Goal:** FundosController expoe CRUD pra ConsultoriaFundo/Custodiante/TipoAtivo/Fundo/Cedente + AdminFundosController read-only cross-company + policies funds:read/write/delete/manage + extensao access groups (admin-empresa=funds:manage, viewer=funds:read).
- **Plans existentes (GSD):**
  - 48-01: FundosController parte 1 (ConsultoriaFundo/Custodiante/TipoAtivo) + permission policies + GlobalExceptionHandler enhancement
  - 48-02: FundosController parte 2 (Fundo/Cedente) + AdminFundosController + access group extension
- **Deferred warnings (futuras phases):** W2 FundosController split (1061 LoC god class); W4 OTel JS telemetry stack ambos SPAs

### Phase 49: Auth Flow Fix (login/logout + post-login error screen)
- **Slug:** auth-flow-fix
- **Status:** done (shipped 2026-05-17, verdict APPROVED_WITH_WARNINGS, 5 iter)
- **Goal:** Diagnosticar e corrigir 2 bugs do fluxo ACF+PKCE (client SPA 5173 + backoffice SPA 5174):
  1. Login/logout caindo de volta na pagina Keycloak `/realms/onboarding/protocol/openid-connect/auth` (autenticacao nao completa)
  2. Tela de erro pos-login que desaparece no reload (race condition cookie/session/hidration)
- **Scope:** Keycloak realm configs (`keycloak/*.json`, redirect URIs, web origins, valid post-logout URIs), backend auth middleware (`Onboarding.API` — token exchange, cookie set, CORS, logout endpoint), frontend (`frontend/{client,backoffice}` — auth callback handler, refresh logic, error boundary, race condition no callback)
- **Allowed:** `docker compose down -v` (wipe stack), criar usuarios de teste via Keycloak admin, Playwright pra evidencia + regressao
- **Security:** Mandatory — sem fallback inseguro, sem token em localStorage, ACF+PKCE intacto, CSRF protection
- **Deliverables:** Bugs fixed + Playwright regression suite cobrindo login+logout+refresh em ambos SPAs + REVIEW.md APPROVED

### Phase 50: FundoCedente & Relationship CRUD
- **Slug:** 49-fundo-cedente-relationships
- **Status:** done (shipped 2026-05-17, verdict APPROVED_WITH_WARNINGS, 1 iter)
- **Goal:** Endpoints N-N Fundo↔Cedente (com payload — LimiteExposicaoPercentual/Valor + janela de datas), Cedente↔TipoAtivo, Fundo↔TipoAtivo. Enforcement REL-09 (uma unica associacao ATIVA por par Fundo-Cedente).

### Phase 51: Frontend Client — Fundos UI
- **Slug:** 50-frontend-client-fundos
- **Status:** done (shipped 2026-05-17, verdict APPROVED_WITH_WARNINGS, 12 iter / 3 rounds / 3 resets)
- **Goal:** SPA cliente PJ ganha secao Fundos (paginacao, search, badges de status, forms Zod espelhando regras backend). Dropdown de status filtra transicoes validas baseado no estado atual (RASCUNHO→ATIVO, ATIVO↔SUSPENSO, etc).
- **Carry-forward warnings (Phase 52/53):** W-G4.4 (Meter placement migrate to Telemetry/ when Phase 53 creates), W-G2 (Authorize justification comment), W-seed (seed script email match), W-metric-privacy (full sub logged), W-audit-format (JsonStringEnumConverter int→string), W-perf/W-bundle (765 KB raw → code-split before Phase 52), W-otel (Phase 53), W-react-setstate (Transitioner pre-existing).

### Phase 52: Frontend Backoffice — Fundos UI (read-only)
- **Slug:** 51-frontend-backoffice-fundos
- **Status:** done (shipped 2026-05-23, verdict APPROVED clean, 3 iter / 2 rounds / 1 reset)
- **Goal:** Backoffice admin lista/visualiza Fundo/ConsultoriaFundo/Custodiante/Cedente cross-company em read-only. Mostra nome da empresa alongside fund data. Sem create/update/delete (FRO-04).
- **DoD G0 cumprido:** Testcontainers 13/13 backend (4 endpoints GET /{id}) + 5/5 (audit filter) + MCP runtime detail pages direct fetch + AuditEventRow entity caption + perFile coverage thresholds D-2 (94.46/90.30/93.10/95.77) + bundle 205 KB gz + CI security pipeline ativa.
- **Carry-forward NÃO-phase-52 (pre-existing):** W-telemetry (OTel JS Phase 53 mandate), W-gitignore (cleanup futuro).

### Phase 53: Integration Tests — v8.0 Fundos end-to-end
- **Slug:** 52-integration-tests-fundos
- **Status:** done (shipped 2026-05-24, verdict APPROVED_WITH_WARNINGS, 6 iter / 2 rounds / 1 reset)
- **Goal:** Testcontainers PostgreSQL real cobrindo CRUD round-trip dos 5 entity types + 3 relationship types, isolamento multi-tenant, transicoes de state machine, REL-09, deteccao de duplicatas (409).
- **DoD G0 cumprido:** 1204 testes pass / 0 fail / 4 pre-existing skip; Integration.Tests 187 -> 195 com 8 novos GET-list happy-path; coverage D-2 dos 4 arquivos antes-bloqueados agora 100% (GetFundoCedentesQueryHandler, GetFundoTiposAtivosQueryHandler, GetCedenteTiposAtivosQueryHandler, AdminFundosController); Playwright regression on backend (health, auth-blocked, Jaeger UI) PASS; security audit zero src/ changes em diff iter 6.
- **Carry-forward NÃO-phase-53 (pre-existing):** W1-W4 backend telemetry (PII scrubber naming, TenantBaggageMiddleware/TelemetryCommandHandlerDecorator unwired, run-uat.mjs legacy route), WFE-1-5 frontend (init-after-render backoffice telemetry, double-import Vite warning, pt-BR strings em main.tsx + D-2 JSX, VITE_OTEL_ENABLED missing em compose.yaml), SEC-W1-W7b cross-cutting (ROPC legado, password policy length, db.statement scrubber gap).

<!-- Phase 54 (vinxi-to-vinext-migration) REMOVED 2026-05-24 via /jdi-remove-phase. Artifacts archived in .jdi/archive/removed-vinxi-to-vinext-migration/. Reason: runtime migration cancelled (D-47); user opted to drop entire phase. -->

### Phase 54: Backend C# Quality & Refactor Audit
- **Slug:** backend-csharp-quality-audit
- **Status:** done
- **Goal:** Auditoria profunda do backend C#: segurança, performance, Clean Code / SOLID / DRY / KISS / YAGNI, cobertura de testes >80%, remoção de código morto, correção de violações de Clean Code (tamanho de método, comentários desnecessários, número de parâmetros, etc.) e aplicação de design patterns onde aplicável e necessário.
- **Scope:** Código C# do backend — `Onboarding.API` + camadas Domain / Application / Infrastructure. Auditoria cross-cutting: roteia para `backend-csharp` + `security` specialists.
- **Aberto pro /jdi-discuss:**
  - **Tensão de cobertura:** usuário pediu >80% em código existente; D-2 hoje enforça 80% só em arquivos novos pós-boundary `968eefb`. Decidir se a phase eleva o gate retroativo para o backend legado ou mantém D-2.
  - **Refactor vs. auditoria:** definir se a phase só reporta violações (SOLID/DRY/method-size/param-count/dead-code) ou também aplica as correções; e quais design patterns são candidatos.

### Phase 55: Controller Dependency Reduction
- **Slug:** controller-di-reduction
- **Status:** done
- **Goal:** Eliminar a explosão de parâmetros de construtor nos controllers — `FundosController` **37** deps injetadas, `AdminUserController` **23**, `CompaniesController` **17** (god class / violação SRP-SOLID). Passou no Phase 54 porque o gate D-52 (`params ≤ 3`) só mediu parâmetros de **método**, nunca **injeção de construtor**. Reduzir cada controller a um número saudável de deps **SEM** mudar rotas / contrato HTTP / fluxo de auth (constraint herdado de D-54).
- **Scope:** `Onboarding.API/Controllers/*` + infra de dispatch (Application/Infrastructure) se necessário. **Subsume o `W-FUNDOS-SPLIT` diferido** do Phase 54.
- **Aberto pro /jdi-discuss:**
  - **Abordagem:** (a) **dispatcher manual** — 1 `ICommandDispatcher`/`IQueryDispatcher` resolve handler+validator via `IServiceProvider`, colapsa 37→~2 deps, **sem split de rota, sem MediatR** (CQRS manual, OSS-only); (b) **split** do FundosController em 5 controllers por sub-domínio (muda route discovery → exige Playwright regression); (c) ambos. Decidir.
  - **Gate de ctor-params:** definir o threshold que o reviewer enforça (ex.: ≤ 5 deps por controller) — fecha o gap do Phase 54.
  - **Sem MediatR** (D-3 OSS-only): qualquer dispatcher é implementação manual.

