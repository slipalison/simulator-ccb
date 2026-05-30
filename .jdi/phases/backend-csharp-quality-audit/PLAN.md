# Phase 54: backend-csharp-quality-audit — Plan  (slug: backend-csharp-quality-audit)

## Goal

Auditoria profunda + refactor híbrido do backend C# (4 camadas, ~309 arquivos / 18.3k LoC): segurança (13-tools local + review + multi-tenant D-5), performance (EF N+1/async/tracking), Clean Code/SOLID/DRY/KISS/YAGNI com thresholds estritos (D-52), remoção de código morto, **cobertura > 80% retroativa em todo o src** (D-49), design patterns só onde justificado (D-55). Entrega híbrida: aplica o seguro, adia o arriscado.

## Locked decisions (from CONTEXT.md)

- **D-48:** Entrega híbrida — reporta tudo + aplica mecânico/behavior-preserving + adia arriscado.
- **D-49:** Cobertura > 80% retroativa em TODO o src, sem exclusões (Migrations EF fora — assunção). Supera D-2 nesta phase.
- **D-50:** Escopo = backend src inteiro (4 camadas).
- **D-51:** Segurança = pipeline 13-tools local completo + code review + multi-tenant D-5.
- **D-52:** Thresholds estritos — método ≤ 20 LoC, params ≤ 3, classe ≤ 200 LoC, ciclomática ≤ 8, nesting ≤ 3.
- **D-53:** God classes via extração segura, sem split de rota. Split completo do `FundosController` (W2) adiado.
- **D-54:** Safe-fix = mecânico + behavior-preserving; contrato público/rota/algoritmo → adia.
- **D-55:** Patterns sob KISS/YAGNI + OSS-only (CQRS manual, sem MediatR; Shouldly).

## Partition strategy (parallelism safety)

Waves de refactor (W2) e cobertura (W4) são particionadas por **projeto/camada** para garantir conjuntos de arquivos disjuntos → paralelismo seguro:

- **T2 é restrito a `src/Onboarding.API/**`** — extração para métodos privados/helpers **dentro da camada API**. Se a lógica de um controller pertence a um Application handler inexistente, T2 **documenta como deferido** em vez de criar mudança cross-layer no meio da wave (evita conflito com T3).
- Testes safety-net de cada task de refactor vão para test projects disjuntos (T2→API.Tests, T3→Domain.Tests+Application.Tests, T4→Integration.Tests).
- Cobertura (W4) roda depois do refactor assentar; particionada por test project disjunto.

## Tasks

### Wave 1 — Foundation (solo, bloqueia tudo)

#### T-1: Coverage baseline + violation inventory (report-only)
- **Specialist:** jdi-doer-onboarding-keycloak-backend-csharp
- **Files modified:**
  - `.jdi/phases/backend-csharp-quality-audit/AUDIT.md` (new — inventário de violações por dimensão: Security / Performance / SOLID / DRY / KISS / YAGNI / dead-code / thresholds D-52, com severidade + `file:line`)
  - `.jdi/phases/backend-csharp-quality-audit/COVERAGE-BASELINE.md` (new — cobertura por arquivo/camada medida via coverlet + ReportGenerator)
  - **Zero mudança em `src/` ou `tests/`** (report-only).
- **Acceptance:**
  - Baseline de cobertura por arquivo medido: `dotnet test <cada csproj> /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura` agregado por ReportGenerator. Tabela camada × %line × %branch + lista de arquivos < 80%.
  - Inventário lista cada violação com dimensão + severidade + `file:line` + classificação safe-fix vs deferido (D-54).
  - Hotspots dimensionados: `FundosController` (1100), `CompaniesController` (590), `AdminUserController` (546) + top arquivos por LoC/complexidade.
  - Estimativa de esforço de cobertura por camada (input para o gate de renegociação D-49 — ver Risks).
- **Dependencies:** none
- **Test:** coverlet run completa + sanity (suite baseline ~1204 ainda verde antes de qualquer mudança)
- **Status:** pending

### Wave 2 — Safe refactor & fixes (parallel, particionado por camada)

#### T-2: API layer — god-class safe extraction + threshold fixes
- **Specialist:** jdi-doer-onboarding-keycloak-backend-csharp
- **Files modified:**
  - `src/Onboarding.API/Controllers/FundosController.cs` (extração D-53 — thin controller, lógica → métodos privados/helpers da camada API; rotas/atributos intactos)
  - `src/Onboarding.API/Controllers/CompaniesController.cs`, `AdminUserController.cs`, `AuthController.cs` (mesmo padrão)
  - `src/Onboarding.API/**/*.cs` (dead code, usings, comentários desnecessários, guard clauses, params→object onde > 3; **só camada API**)
  - `tests/Onboarding.API.Tests/**` (characterization tests ANTES de cada extração — Shouldly)
- **Acceptance:**
  - Controllers alvo abaixo dos thresholds D-52 (método ≤ 20, classe ≤ 200) OU violação remanescente documentada como deferida (W2/Fundos split em `WARNINGS`).
  - **Zero mudança de rota / assinatura pública / contrato HTTP** (D-54). Atributos `[Http*]`/`[Authorize]`/policies intactos.
  - Characterization tests cobrindo o comportamento dos controllers refatorados, escritos antes do refactor.
  - Mudanças confinadas a `src/Onboarding.API/**` (sem tocar Application — itens cross-layer ficam deferidos).
  - Suite API.Tests verde; sem regressão.
- **Dependencies:** T-1 (inventário guia o que extrair)
- **Test:** xUnit API.Tests + (regressão Playwright endpoints fica em T-8 / verify)
- **Status:** pending

#### T-3: Application + Domain — Clean Code, DRY, justified patterns
- **Specialist:** jdi-doer-onboarding-keycloak-backend-csharp
- **Files modified:**
  - `src/Onboarding.Application/**/*.cs` (method size, params→command object, dead code, DRY entre handlers, Specification para predicados repetidos se justificado — D-55)
  - `src/Onboarding.Domain/**/*.cs` (invariantes, value objects; dead code, thresholds; Strategy/Factory para transições de state-machine duplicadas só se removerem duplicação real)
  - `tests/Onboarding.Application.Tests/**`, `tests/Onboarding.Domain.Tests/**` (safety-net ANTES de refactor — Shouldly)
- **Acceptance:**
  - Violações D-52 corrigidas (safe) ou deferidas com justificativa.
  - Patterns aplicados SÓ com justificativa registrada no SUMMARY (D-55); zero abstração especulativa (KISS/YAGNI). Sem MediatR, sem lib não-OSS.
  - CQRS manual + DDD aggregates preservados (code-design locked).
  - Mudanças confinadas a `src/Onboarding.Application/**` + `src/Onboarding.Domain/**`.
  - Suite Domain.Tests + Application.Tests verde.
- **Dependencies:** T-1
- **Test:** xUnit Domain.Tests + Application.Tests
- **Status:** pending

#### T-4: Infrastructure — performance + Clean Code
- **Specialist:** jdi-doer-onboarding-keycloak-backend-csharp
- **Files modified:**
  - `src/Onboarding.Infrastructure/**/*.cs` (EF Core: eliminar N+1 via `Include`/projeção, `AsNoTracking` em reads, remover `ToList` prematuro, async/await correto sem `.Result`/`.Wait()`; dead code; thresholds D-52)
  - `tests/Onboarding.Integration.Tests/**` (safety-net para queries/repos tocados — Testcontainers PG)
- **Acceptance:**
  - N+1 e tracking desnecessário identificados em T-1 corrigidos; comportamento de query inalterado (mesmos resultados).
  - **Multi-tenant filter (D-5) preservado em toda query tocada** — auditado em T-5.
  - async/await sem bloqueio síncrono; sem mudança de contrato.
  - Mudanças confinadas a `src/Onboarding.Infrastructure/**`.
  - Integration.Tests verde.
- **Dependencies:** T-1
- **Test:** xUnit Integration.Tests (Testcontainers)
- **Status:** pending

### Wave 3 — Security (solo, sobre código já refatorado)

#### T-5: Security audit — 13-tool pipeline local + review + multi-tenant
- **Specialist:** jdi-doer-onboarding-keycloak-security
- **Files modified:**
  - `.jdi/phases/backend-csharp-quality-audit/SECURITY.md` (new — findings dos 13 tools + code review, severidade, triagem)
  - `src/Onboarding.**/*.cs` (fixes critical/high DENTRO da fronteira safe-fix D-54; resto vira warning)
  - (gaps de tool não-instalado documentados; CI verde como fallback parcial)
- **Acceptance:**
  - Pipeline 13-tools rodado local (Semgrep, CodeQL, Trivy, Gitleaks, TruffleHog, Syft, Dockle, Checkov, Kubescape, ZAP, Dependabot/Trivy SCA) — ou gap documentado + CI verde.
  - **Multi-tenant filter (D-5) auditado em TODAS as queries tocadas pelas W2** — zero gap de isolamento; cross-probe continua 404.
  - Zero finding critical/high não-triado. Sem secret leak (Gitleaks/TruffleHog limpos). Sem fallback inseguro introduzido.
  - Keycloak hardening sem drift.
- **Dependencies:** T-2, T-3, T-4 (audita código pós-refactor)
- **Test:** pipeline tools + multi-tenant cross-probe integration
- **Status:** pending

### Wave 4 — Coverage gap-fill → > 80% (parallel, test projects disjuntos)

#### T-6: Coverage Domain + Application → > 80%
- **Specialist:** jdi-doer-onboarding-keycloak-backend-csharp
- **Files modified:**
  - `tests/Onboarding.Domain.Tests/**` (preencher gaps até > 80% line por arquivo — Shouldly)
  - `tests/Onboarding.Application.Tests/**` (idem; handlers CQRS, branches de validação/erro)
- **Acceptance:**
  - Cobertura de linha > 80% por arquivo autoral em Domain + Application (incl. fixes de T-3 e T-5).
  - Testes determinísticos (sem flaky), Shouldly, sem libs não-OSS.
- **Dependencies:** T-3, T-5
- **Test:** coverlet por projeto + threshold check
- **Status:** pending

#### T-7: Coverage Infrastructure + API → > 80%
- **Specialist:** jdi-doer-onboarding-keycloak-backend-csharp
- **Files modified:**
  - `tests/Onboarding.API.Tests/**` (controllers, filtros, model binding, branches de erro — incl. extrações de T-2)
  - `tests/Onboarding.Integration.Tests/**` (Infrastructure/repos/EF via Testcontainers — incl. fixes de T-4 e T-5)
- **Acceptance:**
  - Cobertura de linha > 80% por arquivo autoral em Infrastructure + API (Program.cs/DI/DTOs contam — D-49; Migrations EF fora por assunção).
  - Multi-tenant cross-probe coberto. Determinístico.
- **Dependencies:** T-2, T-4, T-5
- **Test:** coverlet por projeto + threshold check
- **Status:** pending

### Wave 5 — Consolidation & self-check

#### T-8: Final coverage report + deferred-warnings doc + green-suite self-check
- **Specialist:** jdi-doer-onboarding-keycloak-backend-csharp
- **Files modified:**
  - `.jdi/phases/backend-csharp-quality-audit/COVERAGE-FINAL.md` (new — relatório consolidado antes/depois, por camada/arquivo)
  - `.jdi/phases/backend-csharp-quality-audit/WARNINGS.md` (new — itens deferidos: split completo `FundosController` W2, mudanças de contrato adiadas D-54, findings de segurança não-críticos, gaps de cobertura aceitos se D-49 renegociado)
  - (nenhuma mudança de `src/`; só consolidação + self-check)
- **Acceptance:**
  - Suite completa verde (baseline ~1204 + novos, 0 fail). Build limpo (zero warning novo de compilador). Lint/format limpos.
  - Cobertura > 80% confirmada em todo o src autoral (relatório anexado) OU desvio documentado + renegociação D-49 registrada.
  - Zero mudança de comportamento observável da API (preparado para regressão Playwright do /jdi-verify).
  - WARNINGS.md completo (todo item deferido rastreado, pronto para virar todos.md/sub-phase).
- **Dependencies:** T-6, T-7
- **Test:** full suite + build + lint; entrega para /jdi-verify (Playwright regression + gates do reviewer)
- **Status:** pending

## Execution

- **Total tasks:** 8
- **Waves:** 5
- **Critical path:** T-1 → (T-2 ‖ T-3 ‖ T-4) → T-5 → (T-6 ‖ T-7) → T-8
- **Parallel-eligible:** W2 {T-2, T-3, T-4} (camadas disjuntas), W4 {T-6, T-7} (test projects disjuntos)
- **Specialists:** backend-csharp (T-1,2,3,4,6,7,8) + security (T-5)

## Risks & sequencing notes

- **Risco #1 — D-49 (cobertura 80% retroativa, ~18.3k LoC, baseline desconhecida).** Provavelmente domina a phase (W4 + safety-nets de W2). T-1 mede o esforço real por camada. **Gate de renegociação:** se T-1 mostrar esforço proibitivo (ex.: < 30% baseline em Infra/API), parar e renegociar D-49 (tier por camada: Domain+Application estritos, Infra+API via integration) antes de W4. Registrado em `.jdi/todos.md`.
- **Safety-net é pré-condição de refactor (D-48/D-54).** Cada task de W2 escreve characterization tests ANTES de refatorar arquivo < 80%. Por isso W2 já consome parte da cobertura; W4 fecha o resto.
- **Cross-layer na extração de god-class (D-53).** T-2 fica API-only para preservar paralelismo de W2; lógica que pertence a handler inexistente é deferida (WARNINGS), não criada mid-wave.
- **Multi-tenant (D-5) crítico.** T-4 toca queries; T-5 audita isolamento em todas elas. Regressão Playwright + cross-probe no /jdi-verify.
- **Segurança depende de código estável** → W3 após W2. Cobertura (W4) após segurança para cobrir também os fixes de T-5.
- **Migrations EF** assumidas fora do denominador (D-49). Confirmar com usuário; se incluídas, `[ExcludeFromCodeCoverage]` ou integration coverage.
