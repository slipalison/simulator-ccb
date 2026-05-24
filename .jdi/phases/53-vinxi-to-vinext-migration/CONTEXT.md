# CONTEXT — Phase 54 Migração de runtime (spike comparativo Vinext vs TanStack Start)

## Goal

**Phase 54 foi redefinida após iter 1 do Ralph loop (commit `45811ef`)** revelar que `cloudflare/vinext` não é fork do Vinxi — é reimplementação da API do Next.js sobre Vite. A premissa original era inválida.

Phase 54 agora é um **spike comparativo data-driven**: migrar **1 rota (`/dashboard`)** em ambos os alvos candidatos em um branch isolado (`spike/migration-poc-vinext-vs-start`), medir métricas reais neste codebase (build time, bundle size, effort em horas, integração com BFF Hono), e tomar a decisão de alvo final em uma phase 54.5 ou 55 dedicada.

Após o spike, decisão de alvo final é tomada com base em:
- Tempo de build empírico (vs benchmark de terceiros)
- Bundle size empírico (vs benchmark de terceiros)
- Effort em horas (rewrite de routes vs zero rewrite)
- Integração com BFF Hono (decidido em D-44)
- DX (HMR, type errors, debugger)

**O que esta phase NÃO faz:**
- Não migra master para nenhum runtime
- Não compromete nenhuma decisão de alvo final
- Não modifica `frontend/client/` em master (só em branch de spike)

**O que esta phase entrega:**
- Branch `spike/migration-poc-vinext-vs-start` com 2 commits (1 por alvo)
- Documento `SPIKE-COMPARISON.md` com métricas medidas neste codebase
- DECISIONS.md D-43..D-46 capturadas
- ROADMAP.md ajustado: phase 54 = spike; phase 55 = migração efetiva pós-decisão

---

## Stack atual

- React 19 + Vinxi 0.5.11 + TanStack Router + TanStack Query + Tailwind 4
- 2 SPAs independentes (D-4): `frontend/client` (5173) + `frontend/backoffice` (5174)
- BFF h3 routers em ambos: `server.ts` (proxy `/api/*`) + `auth-server.ts` (ACF+PKCE flow)
- Testes: Vitest unit + Playwright e2e
- Docker compose: bind mounts hot-reload dev
- Node 22-alpine (container), Node 24.x (host)
- 17 rotas em `frontend/client/src/router.tsx` (createRoute pattern; 7 lazy via `lazyRouteComponent`; 3 com `validateSearch` Zod)
- Bundle main client: 678 KB raw / ~200 KB gzip (medido em commit `c4e2623`)

## Alvos candidatos do spike

### A. Vinext (`vinext@0.0.52`)
- Cloudflare fork — reimplementação Next.js sobre Vite
- 8.1k stars, 939 commits, push há 2 dias (ativo)
- 94% Next.js 16 API surface
- RSC nativo via `@vitejs/plugin-rsc`
- Self-marked experimental, "use at your own risk"
- Benchmark declarado: 4× faster build, 50% smaller bundle (vs Next.js+Turbopack)
- Peer deps: Vite 7/8, React 19.2.6, react-server-dom-webpack 19.2.6
- **Custo neste projeto:** rewrite 17 rotas TanStack → file-based `app/`, 21 arquivos dependentes, 3 Zod searchSchemas

### B. TanStack Start (`@tanstack/react-start@1.168.11`)
- TanStack ecosystem — SSR sobre Vite
- v1.x stable (declared RC, feature-complete approaching 1.0)
- Mantém TanStack Router (`@tanstack/react-router@1.170.8` incluído)
- SSR streaming, server functions, middleware
- RSC support em desenvolvimento (não nativo ainda)
- Peer deps: Vite >=7, React >=18 || >=19
- **Custo neste projeto:** zero rewrite de rotas (estrutura TanStack preservada), só BFF + config SSR

### C. Vinxi 0.6+ (incremental — investigado durante spike)
- Investigar se existe 0.6.x estável ou fork mantido
- Se sim: alvo de menor risco (drop-in upgrade)
- Se não: fica fora do spike

---

## Decisões locked (D-43..D-46)

### D-43 (2026-05-24): Phase 54 redefinida — spike comparativo data-driven

Phase 54 deixa de ser "migrar para Vinext" e vira "**spike comparativo empírico em branch isolado**" entre Vinext e TanStack Start (Vinxi 0.6+ se viável). Decisão de alvo final fica em phase 55 dedicada baseada em métricas medidas neste codebase.

**Razão:** Iter 1 do Ralph loop revelou que assumimos Vinext = fork do Vinxi. É reimplementação Next.js. Benchmarks de terceiros (4× build, -50% bundle) são interessantes mas não medidos neste codebase. Migrar 17 rotas + 21 arquivos por benchmark genérico é risk inaceitável sem dados deste projeto.

**Aplicação:** Branch `spike/migration-poc-vinext-vs-start`. Migra `/dashboard` em cada alvo. Mede build time, bundle size, effort, DX. Documenta em `SPIKE-COMPARISON.md`. Master fica intocado durante spike.

---

### D-44 (2026-05-24): BFF migra de h3 → Hono (substitui D-39)

`server.ts` (proxy `/api/*`) + `auth-server.ts` (ACF+PKCE) migram de h3 para **Hono 4.12+**. D-39 ("BFF preservada em h3 permanente") fica **REVOKED** — preservação da BFF em h3 não é mais princípio.

**Razão:** Independentemente do alvo runtime escolhido (Vinext, TanStack Start, Vinxi 0.6), nenhum integra h3 da mesma forma que Vinxi. Hono é o equivalente moderno: estável (4.12.22), Cloudflare Workers + Node + Bun ready, syntax similar a h3, multi-platform, sem vendor-lock.

**Aplicação:**
- BFF rewrite: `defineEventHandler` → `app.get()/post()`, `getCookie/setCookie/deleteCookie/getQuery` mantidos com Hono equivalentes.
- Token isolation D-12 preservado (cookies httpOnly server-side).
- PKCE state correlation preservada.
- Same-origin preservado (Hono serve BFF na mesma origem do SPA).
- Realm-per-SPA preservado (`KEYCLOAK_REALM` env por SPA).
- API contract decoupling preservado (BFF compõe `/auth/me`).

D-44 aplica em ambos SPAs (client + backoffice) eventualmente, mas spike valida só client.

---

### D-45 (2026-05-24): Alvo final = decisão pós-spike data-driven

Não há alvo runtime locked em Phase 54. Spike produz `SPIKE-COMPARISON.md` com métricas medidas; decisão de alvo final é tomada em phase 55 dedicada, com base nessas métricas.

**Razão:** Vinext promete 4× build / -50% bundle mas exige rewrite de 17 rotas. TanStack Start preserva código mas RSC ainda em desenvolvimento. Decisão correta depende de:
- Performance real neste codebase (não benchmark de 3rd-party)
- ROI: ganho em performance vs custo de rewrite
- Estratégia de longo prazo (ecosystem Next.js vs TanStack)

**Aplicação:**
- Spike compara Vinext + TanStack Start (Vinxi 0.6 investigado se factível).
- `SPIKE-COMPARISON.md` é critério objetivo de decisão.
- Phase 55 abre com alvo locked.

---

### D-46 (2026-05-24): Branch `spike/migration-poc-vinext-vs-start` isolado, master intocado

Spike roda exclusivamente em branch `spike/migration-poc-vinext-vs-start`. Master não recebe mudança de runtime durante phase 54.

**Razão:** Limita blast radius. Permite abandonar qualquer alvo sem custo de revert em master. Decisão de alvo final puxa de branch ou refaz limpo em phase 55.

**Aplicação:**
- 2 commits no branch spike (1 por alvo): `spike(vinext): migrate /dashboard route` + `spike(start): migrate /dashboard route`.
- Métricas capturadas em `.jdi/cache/spike-54-*` (HAR, build logs, bundle reports) e summarized em `SPIKE-COMPARISON.md`.
- Branch spike rebase com master conforme master avança.

---

## Decisões mantidas

### D-38 (2026-05-24): Spike + migração eventual = só `frontend/client/`

Backoffice fica fora desta phase. Spike + decisão + migração eventual cobrem só client SPA. Backoffice migra em phase 56+ separada após client converger.

**Razão:** Isola blast radius. Reduz escopo de variáveis no spike.

---

### D-41 (2026-05-24): NPM exclusivo — PNPM proibido

NPM em todo o projeto. Scripts, Dockerfile, CI, docs npm-only. Lock file: `package-lock.json` único; `pnpm-lock.yaml` removido.

**Razão:** Unificação. Histórico misto cria confusão.

**Aplicação:** Mantém-se. NPM cleanup é tarefa independente do alvo escolhido — pode ser parte do spike ou phase própria.

---

### D-42 (2026-05-24): DoD adaptado para spike

DoD original (Playwright full + Vitest 80% + SSR/hydration + hot-reload) **não se aplica ao spike** porque master fica intocado. Spike tem DoD próprio:

1. **Branch `spike/migration-poc-vinext-vs-start` existe** com 2+ commits documentados.
2. **`/dashboard` route funciona em ambos alvos** (manual smoke via `curl` + abrir browser).
3. **`SPIKE-COMPARISON.md` documenta**:
   - Build time medido (3 runs cada, mediana)
   - Bundle size medido (raw + gzip)
   - Effort em horas (timestamped commits)
   - BFF Hono migration: feito uma vez antes do spike (D-44), reused em ambos POCs
   - DX subjective notes (HMR speed, type errors clarity, debugger)
4. **Decisão de alvo final** registrada como D-XX (novo) com justificativa.

DoD de migração real (full Playwright, full Vitest, etc.) volta na phase 55 dedicada.

---

## Decisões REVOKED

### D-39 (2026-05-24): REVOKED — superseded por D-44

BFF preservada permanente em h3 não é mais princípio. D-44 substitui.

### D-40 (2026-05-24): REVOKED — superseded por D-45

Vinext como alvo locked com pin de versão não é mais lockado. D-45 substitui (alvo final pós-spike).

---

## Specialist routing

- **Spike execution:** `jdi-doer-onboarding-keycloak-frontend-vinext` (renomeio do specialist semantic: cobre frontend migration em geral, não só Vinext)
- **BFF Hono migration:** mesmo specialist frontend
- **Security validation (cross-cutting):** `jdi-reviewer-onboarding-keycloak-security` no /jdi-verify regardless
- **Backend:** nenhum — phase não toca .NET

---

## Riscos

| Risco | Mitigação |
|---|---|
| Spike vira projeto eterno (scope creep) | Time-box estrito: 2 dias por alvo. Quem estourar = abandona, registra "BLOCKED" em SPIKE-COMPARISON.md |
| Spike inconclusivo (números próximos) | Critério tie-breaker locked antes do spike: se Vinext < 10% mais rápido E < 10% bundle menor → escolhe TanStack Start (menor risk) |
| BFF Hono migration introduz bug em master | BFF Hono também roda em branch spike. Só vai para master quando phase 55 mergear |
| Vinext breaks em minor version durante spike | Pin exato `0.0.52`. Aceita re-pin se quebrar |
| TanStack Start RC → 1.0 quebra durante spike | Pin exato `1.168.11`. Aceita re-pin |
| Effort de rewrite Vinext > 2 dias por route | Confirma hipótese — registra "BLOCKED por custo" em SPIKE-COMPARISON.md, escolhe alternativa |

---

## Estratégia de execução (waves esboço pré-plan)

1. **Wave 0 (cleanup + branching):** Cria branch `spike/migration-poc-vinext-vs-start`. NPM cleanup (D-41) executa em master independente do spike. Master segue limpo.
2. **Wave 1 (BFF Hono migration):** Migra `server.ts` + `auth-server.ts` h3 → Hono em branch spike. Valida via Playwright reduzido (login flow + api-proxy). Atomic commit.
3. **Wave 2 (Vinext POC):** Migra rota `/dashboard` + dependências mínimas para Vinext em branch spike. Mede build/bundle/effort. Atomic commit.
4. **Wave 3 (TanStack Start POC):** Reseta branch spike pré-Vinext (ou cria sub-branch). Migra `/dashboard` para TanStack Start. Mede mesmas métricas. Atomic commit.
5. **Wave 4 (Vinxi 0.6 check, opcional):** Investiga existência. Se existe e é trivial, mede também.
6. **Wave 5 (SPIKE-COMPARISON.md):** Sintetiza métricas. Recomendação data-driven para phase 55.
7. **Ship:** ROADMAP.md atualiza — phase 54 done, phase 55 = "Migração runtime efetiva (alvo X)" criada com alvo locked.

---

## Métricas a capturar no spike (template SPIKE-COMPARISON.md)

| Métrica | Vinext | TanStack Start | Vinxi atual (baseline) |
|---|---|---|---|
| Build time produção (mediana 3 runs) | ? | ? | medido |
| Bundle main raw | ? | ? | 678 KB |
| Bundle main gzip | ? | ? | ~200 KB |
| Effort horas (commits timestamped) | ? | ? | 0 |
| HMR speed dev (ms até refresh) | ? | ? | medido |
| Type errors quality | ? | ? | medido |
| BFF integration (linhas mudadas) | ? | ? | 0 |
| Tests broken (vitest count) | ? | ? | 0 |
| Tests broken (playwright count) | ? | ? | 0 |

---

## Out-of-scope

- Backoffice migration (phase 56+)
- Backend .NET changes (sem mudanças nesta phase)
- Keycloak realm config
- Cloudflare Workers deploy real (spike só local + docker)
- OTel JS re-instrumentation (phase 53 cobriu)

---

## Referencias

- ROADMAP.md phase 54
- DECISIONS.md D-38 + D-41 (mantidas), D-39 + D-40 (revoked), D-43..D-46 (novas)
- Phase 54 LOOP.md (iter 1 BLOCKED — registro do redirect)
- Phase 54 DISCOVERY.md (commit `45811ef` — descoberta original do mismatch Vinext)
- Phase 54 REVIEW.md (verdict BLOCKED com B-1/B-2/B-3)
- https://github.com/cloudflare/vinext (alvo A do spike)
- https://tanstack.com/start/latest (alvo B do spike)
- https://hono.dev (substituto h3 D-44)
- D-4 (2 SPAs independentes — preservado)
- D-12 (id_token never in JS — preservado via Hono BFF)
- D-16 (compose runtime canônico — preservado)
- D-25 (specialist routing — preservado)
