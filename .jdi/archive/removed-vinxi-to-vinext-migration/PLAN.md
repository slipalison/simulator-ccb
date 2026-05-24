# Phase 54: vinxi-to-vinext-migration — Plan  (slug: vinxi-to-vinext-migration)

## Goal

Migrar `frontend/client` de Vinxi 0.5.11 → Vinext (Cloudflare fork, latest semver tag) preservando BFF h3 routers + ACF+PKCE flow. Validar pós-migração via build, vitest, playwright regression existente, **e2e full journey clean-slate** (`docker compose down -v` → up → registration → access group → funcionário → fundo + relationships) executada por specialist com MCP browser.

Backoffice fica para phase 55 (D-38).

## Locked decisions (from CONTEXT.md)

- **D-38:** Incremental — só `frontend/client/` nesta phase.
- **D-39:** BFF preservado permanente (server.ts + auth-server.ts intactos).
- **D-40:** Vinext alvo = latest semver tag, pin exato (não `^`/`~`).
- **D-41:** NPM exclusivo, PNPM proibido em todo o repo.
- **D-42:** DoD cumulativo 4 gates obrigatórios (Playwright e2e + Vitest 80% D-2 + SSR/hydration zero regressão + Docker hot-reload).
- **User add-on (este invoke):** Clean-slate validation via `docker compose down -v` + up + full registration journey cobrindo todos cadastros (access group, funcionário, fundo, consultoria, custodiante, tipo ativo, cedente, relationships).

## Tasks

### Wave 1 — Discovery (sequential blocker)

#### T-1: Vinext discovery + version pin
- **Specialist:** jdi-doer-onboarding-keycloak-frontend-vinext
- **Files modified (read/output only):**
  - `.jdi/phases/53-vinxi-to-vinext-migration/DISCOVERY.md` (new — version pin escolhida, h3 compat status, app.config.ts diff esperado, breaking changes do CHANGELOG, fallback plan se incompat)
- **Acceptance (DoD G0):**
  - Latest semver release tag identificada via `gh release list -R cloudflare/vinext` ou `npm view <pkg> versions --json`.
  - Validar release ≥7 dias sem revert na main (commit log read-only).
  - Documentar compat com h3 (Vinxi usa h3@1.15.x — Vinext mantém ou troca por adapter?).
  - app.config.ts diff esperado: campos novos/removidos/renomeados.
  - Spike ≤1h. Se Vinext for incompat profundo com h3, escalate via SUMMARY.md "BLOCKED — recommended alternatives" antes de prosseguir.
- **Dependencies:** none (sequential blocker — gate pra Wave 2+)
- **Test:** read-only análise + DISCOVERY.md
- **Status:** pending

### Wave 2 — Preparation (parallel-eligible)

#### T-2: NPM cleanup (D-41)
- **Specialist:** jdi-doer-onboarding-keycloak-frontend-vinext
- **Files modified:**
  - `pnpm-lock.yaml` (delete)
  - `frontend/client/package.json` (script `test:e2e` já usa `npx`, verify; nada a mudar)
  - `frontend/backoffice/package.json` (`test:e2e` script `pnpm exec playwright` → `npx playwright`; `test:e2e:ui` idem; `test:e2e:report` idem)
  - `scripts/check-dev-env.mjs` (mensagens `pnpm dev` → `npm run dev`; comentário inicial idem)
  - `frontend/client/Dockerfile` (já usa `npm ci` ou `npm install` — confirma)
  - `frontend/backoffice/Dockerfile` (idem)
  - `.github/workflows/*.yml` (grep `pnpm`, substituir por `npm` — provavelmente `npm ci` em cache step + `npm run lint` / `test`)
  - `package-lock.json` (gerar via `npm install` no client+backoffice+raiz)
  - `docs/dev-setup.md` (se existir) — substituir `pnpm` por `npm`
- **Acceptance (DoD G0):**
  - `pnpm-lock.yaml` deletado.
  - `grep -r 'pnpm' --include='*.{json,yml,yaml,md,mjs,cjs,ts,js}' .` retorna zero matches em arquivos source (CI, scripts, package.json, docs). `node_modules/` exemptado via .gitignore.
  - `package-lock.json` na raiz + cada workspace (npm workspaces nativo gera root lock).
  - `npm install` na raiz funciona, instala ambos workspaces sem erros.
  - Atomic commit `chore: migrate to npm-only (D-41)`.
- **Dependencies:** T-1 done
- **Test:** `npm install` + `npm run lint` em ambos workspaces
- **Status:** pending

#### T-8: Docs draft preparatório
- **Specialist:** jdi-doer-onboarding-keycloak-frontend-vinext
- **Files modified (preparação, commit final em T-7):**
  - `CLAUDE.md` (rascunho — atualizar seção stack: Vinxi → Vinext; comandos npm)
  - `docs/dev-setup.md` (rascunho — comandos `npm run dev`, troubleshoot Vinext-specific)
  - `.jdi/VERSION` (bump)
  - `README.md` (badges/links atualizados se mencionarem Vinxi)
- **Acceptance (DoD G0):**
  - Rascunho preparado em branch — não committado até T-7 (clean-slate journey valida tudo antes de docs go live).
  - Edits NÃO destrutivos — mantém histórico Vinxi como contexto adopted.
- **Dependencies:** T-1 done
- **Test:** lint markdown
- **Status:** pending

### Wave 3 — Vinext install (sequential)

#### T-3: Vinext install + app.config.ts migration (D-40)
- **Specialist:** jdi-doer-onboarding-keycloak-frontend-vinext
- **Files modified:**
  - `frontend/client/package.json` (remove `"vinxi": "^0.5.11"`, add `"vinext": "<exact-version>"` per T-1 pin; remove vinxi-specific peer deps se any)
  - `frontend/client/app.config.ts` (migrate API per T-1 discovery — `createApp` import, routers shape, plugins; preserve TODOS os 4 routers: public static, auth http, api-proxy http, client spa)
  - `frontend/client/package.json` (dev script já tem `--env-file-if-exists=../../.env`; ajustar path de `node_modules/vinxi/bin/cli.mjs` → `node_modules/vinext/bin/cli.mjs` ou equivalente Vinext binary)
  - `frontend/client/predev` script (sem mudanças se check-dev-env.mjs ainda relevante)
  - `frontend/client/server.ts` (NO changes — BFF preservado D-39)
  - `frontend/client/auth-server.ts` (NO changes — BFF preservado D-39)
  - `frontend/client/index.html` (verify Vinext SPA entry attach se mudou)
  - `frontend/client/tsconfig.json` (atualizar `types` se Vinext adicionar `@vinext/types`)
  - `package-lock.json` (gerado via `npm install`)
- **Acceptance (DoD G0):**
  - `npm install` no `frontend/client/` resolve sem peer dep errors nem deprecation warnings críticas.
  - `node_modules/vinext/` existe + binary `cli.mjs` ou equivalente em path canônico.
  - `app.config.ts` compila TypeScript clean (`npm run typecheck` exit 0).
  - 4 routers preservados (public, auth, api-proxy, client SPA) — diff vs Vinxi version visivelmente mínimo (só import statement + nome do API).
  - Atomic commit `feat(client): swap runtime to vinext@<version> (D-40)`.
- **Dependencies:** T-1, T-2 done
- **Test:** `npm run typecheck` + `npm run lint`
- **Status:** pending

### Wave 4 — Build + dev smoke (sequential)

#### T-4: Build production + dev server smoke
- **Specialist:** jdi-doer-onboarding-keycloak-frontend-vinext
- **Files modified:**
  - `frontend/client/.gitignore` (se Vinext gerar `.vinext/` ou similar)
- **Acceptance (DoD G0):**
  - `npm run build` no client SPA: exit 0, 0 errors. Bundle output em `.output/` (ou path Vinext-defined) — verificar.
  - Bundle main client gz ≤ 300 KB (gate D-3 do projeto). Se exceder, code-split antes de prosseguir.
  - `docker compose up -d frontend-client` boots OK; logs do container sem error/throw.
  - `curl -s -o /dev/null -w '%{http_code}' http://localhost:5173/` retorna 200.
  - `curl -s -i http://localhost:5173/auth/login` retorna 302 com PKCE cookies + Location pra Keycloak.
  - `curl -s -i -X POST http://localhost:5173/api/companies/registration` retorna 422 ou 4xx do backend (não 503 fetch failed).
  - Bind mount hot-reload: editar `frontend/client/src/main.tsx` → trigger Vite HMR no browser sem container restart.
  - Atomic commit `feat(client): vinext build + dev validation`.
- **Dependencies:** T-3 done
- **Test:** build + docker compose up + curl smoke
- **Status:** pending

### Wave 5 — Validation gates (parallel-eligible)

#### T-5: Vitest unit + coverage ≥ 80% D-2 (gate D-42 #2)
- **Specialist:** jdi-doer-onboarding-keycloak-frontend-vinext
- **Files modified:**
  - `frontend/client/vitest.config.ts` (se Vinext exigir adapter — ex: novo `@vinext/test` plugin)
- **Acceptance (DoD G0):**
  - `npm run test` no client SPA exit 0, todos os tests pass (atualmente 704 pass / 15 pre-boundary skip).
  - Suite `auth-server.test.ts` mantém 37 tests pass (CLIENT_SECRET fail-fast + cookie attrs + logout + id_token_hint + dev script guard + compose.yaml guard).
  - Coverage D-2 (new-files since 968eefb) ≥ 80% lines + 70% branches.
  - Nenhum teste novo precisa ser escrito; se algum falhar pós-Vinext, doer adiciona regression test mínimo (NOT scope creep — só pra cobrir API change).
- **Dependencies:** T-4 done
- **Test:** `npm run test` + coverage report
- **Status:** pending

#### T-6: Playwright e2e regression existente (gate D-42 #1)
- **Specialist:** jdi-doer-onboarding-keycloak-frontend-vinext
- **Files modified:**
  - `frontend/client/playwright.config.ts` (se Vinext mudar dev server detection)
- **Acceptance (DoD G0):**
  - `npm run test:e2e` no client SPA exit 0.
  - Specs preservadas verde: `api-proxy.spec.ts` (3 cenários: single listener + POST 422 not 503 HTML + GET 405 not 503), `login-flow.spec.ts` ACF+PKCE redirect cookies, registration-flow se existir.
  - Console errors em browser durante regression run = 0 (app-level).
  - Nenhuma 503 / 5xx em response capture.
  - **NÃO inclui** o clean-slate full journey (esse é T-7) — T-6 só valida que regression suite existente passa.
- **Dependencies:** T-4 done
- **Test:** `npm run test:e2e`
- **Status:** pending

### Wave 6 — Clean-slate full journey (sequential, final gate)

#### T-7: Clean-slate full registration journey via MCP browser (D-42 + user mandate)
- **Specialist:** jdi-doer-onboarding-keycloak-frontend-vinext (MCP browser tools)
- **Files modified:**
  - `frontend/client/playwright/specs/clean-slate-journey.spec.ts` (new — codifica a jornada completa pra repeatable regression em futuro)
- **Acceptance (DoD G0) — clean-slate validation:**

  **Pré-condição:**
  1. `docker compose down -v` (autorizado por user — apaga volumes Postgres + Keycloak). Confirma containers + volumes removidos via `docker volume ls`.
  2. `docker compose up -d --build` (rebuild forces Vinext bind mounts up).
  3. Aguarda healthchecks: `api`, `keycloak`, `app_db` healthy via `docker compose ps`.
  4. Keycloak seed: realm `client` + realm `backoffice` aplicados via realm.json import (já automático em compose).
  5. Backend migrations: EF Core auto-apply on first request.

  **Jornada via MCP Playwright (registrado em spec novo):**

  | Step | Ação | Endpoint/Tela | Assertion |
  |---|---|---|---|
  | 1 | Registration PJ-Owner | POST /api/companies/registration via UI form `/register` | 201 Created. Company + Keycloak user criados. |
  | 2 | Login ACF+PKCE | `/auth/login` → Keycloak → callback | Cookies httpOnly set, redirect /dashboard, sem 401. |
  | 3 | Confirma /api/auth/me | GET /auth/me via BFF | Returns userName + accessGroup + companyId. |
  | 4 | Cria AccessGroup custom | POST /api/access-groups (UI ou direct) | 201, novo grupo persisted. |
  | 5 | Cria Funcionário (Employee) | POST /api/employees via UI | 201, employee linked to AccessGroup. |
  | 6 | Cria ConsultoriaFundo | POST /api/fundos/consultorias | 201, listed in GET. |
  | 7 | Cria Custodiante | POST /api/fundos/custodiantes | 201, listed. |
  | 8 | Cria TipoAtivo | POST /api/fundos/tipos-ativo | 201, listed. |
  | 9 | Cria Cedente | POST /api/cedentes | 201, listed. |
  | 10 | Cria Fundo | POST /api/fundos com consultoriaId + custodianteId | 201, fundo persistido com FK refs válidas. |
  | 11 | Associa Fundo↔Cedente (N-N) | POST /api/fundos/{id}/cedentes | 201, association criada. |
  | 12 | Associa Fundo↔TipoAtivo | POST /api/fundos/{id}/tipos-ativos | 201. |
  | 13 | Associa Cedente↔TipoAtivo | POST /api/cedentes/{id}/tipos-ativos | 201. |
  | 14 | State transition Fundo | PATCH /api/fundos/{id}/status (rascunho→ativo) | 200, status atualizado. |
  | 15 | GET list Fundos | GET /api/fundos?page=1 | 200, paginação válida, total ≥1. |
  | 16 | Logout | `/auth/logout` BFF | 302 Keycloak end-session + cookies deletados + id_token_hint forwarded. |
  | 17 | Console errors check | DevTools console durante jornada | 0 erros app-level (warnings React permitidos se pre-existing). |
  | 18 | Network 5xx check | HAR capture | 0 5xx responses. |

  **Spec adicional:**
  - SSR/hydration zero regression (D-42 #3): inspeção MCP DevTools console no first paint — sem `Warning: Text content did not match`, sem `Warning: Hydration failed`.
  - Docker hot-reload (D-42 #4): durante journey, alterar `frontend/client/src/main.tsx` (whitespace change) → browser auto-refresh sem container restart.
  - HAR + screenshots em `.jdi/cache/phase-54-clean-slate-journey-*`.
  - Atomic commit `test(vinxi-to-vinext-migration): clean-slate full e2e journey (D-42)`.

- **Dependencies:** T-5 + T-6 done (gates de cobertura passam antes da journey final)
- **Test:** Playwright spec + MCP browser tools + docker compose down -v + up
- **Status:** pending

#### T-8 (cont.): Docs commit final + bump VERSION
- (Continuação do draft em Wave 2)
- **Files modified (commit final):**
  - `CLAUDE.md` (Vinxi → Vinext stack reference; `npm run` em todos exemplos)
  - `docs/dev-setup.md` (se existir, idem)
  - `.jdi/VERSION` (bump conforme padrão)
  - `README.md` se necessário
- **Acceptance (DoD G0):**
  - Apenas após T-7 PASS clean-slate journey.
  - Atomic commit `docs(vinxi-to-vinext-migration): finalize docs + bump VERSION`.
- **Dependencies:** T-7 done
- **Test:** lint markdown
- **Status:** pending

## Execution stats

- **Total tasks:** 8
- **Waves:** 6
- **Sequential blockers:** T-1 (discovery), T-3 (install), T-4 (build), T-7 (journey)
- **Parallel-eligible:** T-2 || T-8-draft (Wave 2); T-5 || T-6 (Wave 5)
- **Specialist:** `jdi-doer-onboarding-keycloak-frontend-vinext` exclusivo (sem backend C#, sem security ad-hoc — security reviewer ainda roda no /jdi-verify por mandate cross-cutting)
- **Estimated iters Ralph loop:** 1-2 se Vinext drop-in; 3-4 se app.config.ts API diverge + adapter h3 necessário.

## Cross-references

- CONTEXT.md (D-38..D-42)
- DECISIONS.md D-38..D-42
- ROADMAP.md Phase 54
- Phase 50 REVIEW (G11 Vinext debt gates herdadas como guardrail)
- Phase 53 REVIEW (W1-W4 telemetry + WFE-1-5 carry-forward — não-blocker pra esta phase)
