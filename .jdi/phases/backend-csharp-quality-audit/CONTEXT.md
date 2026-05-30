# Phase 54 — backend-csharp-quality-audit — CONTEXT

## Goal

Auditoria profunda + refactor do backend C# (`Onboarding.API` + `Onboarding.Application` + `Onboarding.Domain` + `Onboarding.Infrastructure`) cobrindo, em uma passada coordenada:

1. **Segurança** — code review (input validation, isolamento multi-tenant D-5, gaps de policy/authz, secrets hygiene, Keycloak hardening drift) **+ pipeline 13-tools rodado localmente** (Semgrep, CodeQL, Trivy, Gitleaks, TruffleHog, Syft, Dockle, Checkov, Kubescape, ZAP, Dependabot/Trivy SCA).
2. **Performance** — EF Core N+1 / tracking desnecessário / `ToList` prematuro, async/await correto (sem `.Result`/`.Wait()`), allocations evitáveis, paginação.
3. **Clean Code / SOLID / DRY / KISS / YAGNI** — com thresholds objetivos (D-52), remoção de código morto, comentários desnecessários, métodos longos, excesso de parâmetros.
4. **Cobertura de testes > 80% retroativa em TODO o src** (D-49) — supera D-2 (que enforça só arquivos novos) dentro do escopo desta phase.
5. **Design patterns** aplicados **só onde removem duplicação/complexidade real** (D-55) — sem abstração especulativa.

Entrega **híbrida** (D-48): reporta todas as violações com severidade + `file:line`, **aplica** as correções mecânicas e behavior-preserving, **adia** as arriscadas (mudança de contrato público, split de rota, troca de algoritmo) como warnings/sub-phases.

**Tamanho do alvo (medido):**

| Camada | Arquivos `.cs` | LoC |
|---|---|---|
| `Onboarding.Domain` | 60 | 2.498 |
| `Onboarding.Application` | 186 | 6.681 |
| `Onboarding.Infrastructure` | 40 | 4.060 |
| `Onboarding.API` | 23 | 5.090 |
| **Total src (excl. Migrations/bin/obj)** | **~309** | **~18.3k** |

Hotspots conhecidos: `FundosController` **1100 LoC** (god class, warning W2 diferido de Phase 48), `CompaniesController` 590, `AdminUserController` 546, `AuthController` 375. 126 arquivos src já tocados desde o boundary `968eefb`.

## Locked decisions (Phase 54)

- **D-48 (DA-1) — Entrega híbrida.** Reportar TODAS as violações (severidade + `file:line`) + aplicar correções mecânicas/behavior-preserving + adiar as arriscadas como warnings ou sub-phases. Não é report-only nem refactor total.

- **D-49 (DA-2) — Cobertura > 80% retroativa em TODO o src, sem exclusões de denominador.** Eleva o gate além do D-2 (que enforça 80% só em arquivos pós-`968eefb`). Aplica às 4 camadas. **Assunção explícita a confirmar:** o denominador é o código C# **autoral** do src; **Migrations geradas pelo EF Core** ficam de fora (código gerado, não autoral — já excluídas da contagem de 18.3k LoC acima). Program.cs/bootstrap/wiring de DI/DTOs/records contam (escolha "tudo no src conta"). D-2 permanece o default global de futuras phases; D-49 é phase-scoped e mais rígido. **Esta é a maior fatia da phase** (ver Notes — risco #1).

- **D-50 (DA-3) — Escopo: backend src inteiro.** As 4 camadas (~309 arquivos / 18.3k LoC), incluindo código legado pré-adoção. Não restrito a hotspots nem só à camada API.

- **D-51 (DA-4) — Segurança: pipeline 13-tools local completo + code review.** Roda SAST/SCA/secrets localmente além da revisão manual. Não depende só de validar o CI verde. Auditoria multi-tenant (D-5) em cada query tocada é obrigatória.

- **D-52 (DA-5) — Thresholds estritos de Clean Code (gate mensurável do reviewer):**
  - Método ≤ **20** LoC
  - Parâmetros ≤ **3** (> 3 → parameter object / command object)
  - Classe ≤ **200** LoC
  - Complexidade ciclomática ≤ **8**
  - Aninhamento ≤ **3**
  - Violações são reportadas sempre; correção segue a fronteira D-54.

- **D-53 (DA-6) — God classes: extração segura, sem split de rota.** Para `FundosController`/`CompaniesController`/`AdminUserController`: extrair métodos privados/helpers e empurrar lógica para os Application handlers (padrão CQRS manual já existente), **mantendo a classe controller e as rotas intactas**. Reduz LoC/complexidade sem risco de quebrar endpoints. **O split completo de `FundosController` (W2) fica ADIADO** para sub-phase dedicada — esta phase só o endereça parcialmente via extração.

- **D-54 (DA-7) — Fronteira safe-fix (mecânico + behavior-preserving).**
  - **Aplica automaticamente:** dead code, `using` não usados, renomes locais, extract method, guard clauses / early return, correções async/await (remover `.Result`/`.Wait()`), `readonly`/`sealed` onde aplicável, extração de pattern que **não** altera contrato público.
  - **Adia (vira warning/sub-phase):** mudança de assinatura pública / contrato de API, split de rota/controller, troca de algoritmo ou estrutura de dados, mudança de comportamento observável.

- **D-55 (DA-8) — Design patterns sob KISS/YAGNI + OSS-only.** Pattern só entra se remover duplicação/complexidade real ou corrigir violação SOLID concreta — nunca especulativo. Respeita o idioma do projeto: **CQRS manual via DI (sem MediatR — licença comercial), DDD aggregates, OSS-only (MIT/Apache)**. Testes novos usam **Shouldly** (não FluentAssertions). Candidatos legítimos: parameter/command object (> 3 params), Strategy/Factory para transições de state-machine duplicadas, Specification para predicados de query repetidos, Result/GlobalExceptionHandler já existente para fluxo de erro. Cada aplicação exige justificativa no SUMMARY.

## Canonical refs

- `.jdi/DECISIONS.md` — **D-2** (cobertura 80% só em new files pós-`968eefb` — D-49 supera dentro desta phase), **D-5** (multi-tenant isolation CRITICAL), D-9/D-22 (state-machine), D-12 (token isolation), D-18 (REL-09), D-48..D-55 (esta phase). Memória de feedback: no-MediatR, OSS-only (Shouldly), NPM-only.
- `.jdi/PROJECT.md` — Definition of Done policy + code-design locked (DDD + CQRS manual).
- `src/Onboarding.API/Controllers/` — god classes alvo da extração D-53 (`FundosController.cs` 1100, `CompaniesController.cs` 590, `AdminUserController.cs` 546, `AuthController.cs` 375).
- `src/Onboarding.Application/` — 186 arquivos (handlers CQRS manuais); maior densidade de lógica testável (alvo principal da cobertura D-49).
- `src/Onboarding.Domain/` — 60 arquivos (aggregates, value objects, invariantes); cobertura de regras de negócio.
- `src/Onboarding.Infrastructure/` — 40 arquivos (EF Core, repos, Keycloak integration); foco perf (N+1) + multi-tenant filters.
- `tests/Onboarding.{Domain,Application,API,Integration}.Tests/` — suites existentes (~141 arquivos / ~25.6k LoC). Baseline ~1204 testes (Phase 53). Cobertura legada atual = **desconhecida**, medir primeiro (ver Notes).
- `coverage-iter6-api.xml` / `coverage-iter6-integration.xml` (untracked em `tests/`) — artefatos coverlet soltos; usar como ponto de partida da medição-baseline ou limpar.
- Pipeline de segurança — workflows CI (`.github/workflows/`) + configs dos 13 tools; rodar equivalente local (D-51).

## Out of scope

- **Frontend** (client SPA 5173 + backoffice 5174) — esta phase é backend C# only. Warnings WFE-* de Phase 53 não tratados aqui.
- **Split completo do `FundosController`** (W2) — adiado para sub-phase dedicada (D-53). Esta phase só faz extração segura sem mexer em rotas.
- **OTel JS / telemetria frontend** — carry-forward de Phase 53, não-backend.
- **Migração de runtime (Vinxi)** — cancelada (D-47).
- **Mudança de contrato público da API / breaking changes** — fora da fronteira safe-fix (D-54); qualquer necessidade vira warning.
- **Novos features / endpoints** — phase é qualidade, não funcionalidade (YAGNI).
- **Reescrita de arquitetura** (trocar CQRS manual por outra coisa, introduzir MediatR) — proibido (D-55, code-design locked).
- **Performance benchmarking formal / profiling sob carga** — esta phase faz revisão estática de perf + fixes óbvios (N+1, async); benchmark suite é phase futura se necessário.
- **Tests para `/scripts/*` shell / infra** — fora de escopo.

## Notes

- **RISCO #1 — viabilidade do 80% retroativo (D-49).** 18.3k LoC src com cobertura legada desconhecida. Subir para >80% line coverage em TODO o src é provavelmente **a fatia dominante da phase** (muitas waves, sobretudo Application 186 arquivos + Domain 60). Sequência obrigatória: (a) **medir baseline** de cobertura por arquivo primeiro (coverlet + ReportGenerator), (b) escrever **characterization tests** ANTES de qualquer refactor de código não coberto — refactor sem rede de teste é proibido. Itens caros e de baixo valor (Program.cs top-level, wiring de DI, DTOs anêmicos) podem precisar de integration tests em vez de unit. O `/jdi-plan` deve dimensionar isso realisticamente e pode recomendar negociar D-49 (ex.: tier por camada) se o esforço estourar — mas a decisão locked atual é 80% literal.

- **Ordem de execução recomendada pro planner** (não é decisão locked, é guidance):
  1. Baseline de cobertura + inventário de violações (auditoria report-only primeiro — produz o mapa).
  2. Characterization/safety-net tests no código a ser refatorado.
  3. Dead code removal + fixes mecânicos (D-54) — baixo risco, alto volume.
  4. Extração segura nos god classes (D-53) + fixes de method-size/param-count (D-52).
  5. Perf fixes (N+1, async, tracking).
  6. Cobertura até > 80% nas lacunas restantes (D-49) — maior wave.
  7. Segurança: pipeline 13-tools local + code review (D-51), multi-tenant audit (D-5).
  8. Design patterns justificados (D-55).
  9. Verificação final (ver DoD).

- **Safety net é pré-condição de refactor.** Para qualquer arquivo onde D-54 aplica extract method/reestruturação mas a cobertura é < 80%, escrever teste de caracterização ANTES. Isso acopla naturalmente D-49 e D-48 — a cobertura é o que torna o refactor seguro.

- **Multi-tenant (D-5) é CRÍTICO.** Toda query tocada durante refactor/perf deve preservar o filtro de tenant. O reviewer de segurança audita cobertura de filtro multi-tenant a cada `/jdi-verify`.

- **Regressão é obrigatória neste projeto** (não opcional — regra dos reviewers): suite completa verde (baseline ~1204) + Playwright regression nos endpoints da API. Nenhum refactor pode mudar comportamento observável.

- **God class — `FundosController` (1100 LoC).** Extração D-53: mover lógica de orquestração para handlers da Application (muitos já existem do padrão CQRS), deixar o controller fino (validação de entrada → dispatch → mapeamento de resposta). Sem tocar rotas/atributos `[Http*]`/policies. O ganho de LoC é colateral; o objetivo é reduzir responsabilidade (SRP).

- **Pipeline 13-tools local (D-51).** Requer toolchain instalado (Semgrep, CodeQL CLI, Trivy, Gitleaks, etc.). Se algum não estiver disponível localmente, o doer/reviewer documenta o gap e valida via CI como fallback parcial — mas o alvo é local completo.

- **Specialist routing:** corpo principal → `jdi-doer-onboarding-keycloak-backend-csharp`. Segurança (D-51, D-5) → `jdi-doer-onboarding-keycloak-security` (cross-cutting). Verify → backend reviewer (build/test/coverage/lint + Playwright regression obrigatório) + security reviewer (13-tools + multi-tenant + secrets).

## Definition of Done (Phase 54 specific — derived from PROJECT.md DoD policy)

### Auditoria & relatório
- [ ] Inventário de violações por dimensão (Security / Performance / SOLID / DRY / KISS / YAGNI / dead-code / Clean-Code-thresholds) com severidade + `file:line`.
- [ ] Baseline de cobertura por arquivo medido e registrado (antes/depois).

### Correções (entrega híbrida D-48 / fronteira D-54)
- [ ] Dead code removido; `using` não usados removidos; comentários desnecessários removidos.
- [ ] Violações de threshold (D-52) corrigidas onde safe (extract method, parameter/command object), ou documentadas como warning quando adiadas.
- [ ] God classes (D-53) reduzidos via extração segura, rotas intactas; split completo registrado como warning/sub-phase.
- [ ] Perf fixes aplicados (N+1, async/await, tracking) onde behavior-preserving.
- [ ] Design patterns aplicados só com justificativa (D-55); zero abstração especulativa.

### Cobertura (D-49)
- [ ] Cobertura de linha > 80% em todo o src autoral (Migrations EF excluídas — assunção a confirmar). Relatório coverlet/ReportGenerator anexado.
- [ ] Characterization tests presentes para todo código refatorado que estava < 80%.

### Segurança (D-51 / D-5)
- [ ] Pipeline 13-tools rodado local (ou gap documentado + CI verde como fallback). Zero finding crítico/high não-triado.
- [ ] Multi-tenant filter (D-5) auditado em todas as queries tocadas.
- [ ] Sem secret leak (Gitleaks/TruffleHog limpos). Sem fallback inseguro introduzido.

### Verificação (regra do projeto — não opcional)
- [ ] Build limpo (zero warning novo de compilador).
- [ ] Suite completa verde (baseline ~1204 testes, 0 fail novos).
- [ ] Playwright regression nos endpoints da API PASS.
- [ ] Zero mudança de comportamento observável da API (contrato preservado).
- [ ] Lint/format limpos.
