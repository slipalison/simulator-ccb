# CONTEXT — Phase 54 BFF h3 → Hono migration (re-scoped final 2026-05-24)

## Histórico de redirects desta phase

1. **Original (commits `f4ee554` + `ccde3fd` + `85ef11b`):** Migrar `frontend/client` de Vinxi 0.5.11 → cloudflare/vinext. D-38..D-42 capturadas.
2. **Iter 1 do Ralph loop (commit `45811ef`):** Doer descobriu que cloudflare/vinext é reimplementação Next.js sobre Vite, não fork do Vinxi. BLOCKED.
3. **Re-discuss 1 (commit `adbf7c3`):** Phase 54 vira spike comparativo Vinext vs TanStack Start em branch isolado. D-43..D-46 capturadas.
4. **Re-discuss 2 (este CONTEXT, 2026-05-24):** Análise honesta de ganhos vs perdas concluiu Vinext não compensa, TanStack Start tampouco justifica spike isolado. Migração de runtime CANCELADA via D-47. Phase 54 vira BFF-only (Hono via D-44 mantida).

## Goal final

Migrar BFF (`frontend/client/server.ts` + `frontend/client/auth-server.ts`) de **h3** para **Hono 4.12+** preservando:
- Token isolation (D-12) — cookies httpOnly server-side
- Same-origin cookies — SPA + BFF mesma origem
- PKCE state correlation — cookie `pkce_code_verifier` entre `/auth/login` e `/auth/callback`
- ACF+PKCE flow completo (Phase 33 + 49) — sem regressão
- Realm-per-SPA (`KEYCLOAK_REALM` env diferente client vs backoffice eventual)
- API contract decoupling — BFF compõe `/auth/me`
- OWASP-recommended BFF pattern

**Vinxi 0.5.11 mantido como runtime do bundler** (D-47). Sem mudança em `app.config.ts`, sem mudança em `frontend/client/src/router.tsx`, sem mudança nas 17 rotas TanStack, sem mudança em build/HMR/SSR.

## Stack atual relevante

- BFF h3: `frontend/client/server.ts` (proxy `/api/*` para `api:8080`) + `frontend/client/auth-server.ts` (ACF+PKCE: `/auth/login`, `/auth/callback`, `/auth/me`, `/auth/logout`, `/auth/error`)
- h3 primitives usados: `defineEventHandler`, `setCookie`, `getCookie`, `deleteCookie`, `sendRedirect`, `getQuery`, `readBody`, `proxyRequest`
- Docker compose bind mounts: `server.ts` + `auth-server.ts` + `app.config.ts` → hot-reload dev
- Auth: ACF+PKCE com `pkce_code_verifier` cookie + state validation no callback + logout invalidando session em Keycloak (`end_session_endpoint`)
- Realms: client realm (`onboarding`)
- Tests:
  - Vitest unit: `auth-server.test.ts` (37 tests — CLIENT_SECRET fail-fast + cookie attrs + logout + id_token_hint + dev script guard + compose.yaml guard)
  - Playwright e2e: `api-proxy.spec.ts` (single listener + POST 422 not 503 + GET 405 not 503) + `login-flow.spec.ts` ACF+PKCE redirect cookies + registration-flow

## Stack alvo

- BFF Hono 4.12.22: `app.get()`, `app.post()`, `app.all()` substituem `defineEventHandler` h3
- Hono primitives equivalentes: `c.req.query()`, `c.req.json()`, `setCookie(c, ...)`, `getCookie(c, ...)`, `deleteCookie(c, ...)`, `c.redirect(...)`, proxy via fetch nativo ou `@hono/proxy`
- Vinxi 0.5.11 mantido — `app.config.ts` aponta para `server.ts` + `auth-server.ts` como antes
- Vinxi router `type: "http"` aceita Hono apps em vez de h3 (Hono exporta `fetch` Web standard — Vinxi h3 router consome qualquer handler que aceita Request → Response, que é exatamente o que Hono entrega)
- Docker compose hot-reload preservado (bind mounts inalterados; só conteúdo dos files muda)
- Auth flow ACF+PKCE intacto — só implementação do BFF muda, contrato externo igual

## Decisões locked (consolidadas)

### D-38 (kept): escopo só `frontend/client/`
Backoffice em phase futura. Isola blast radius.

### D-41 (kept): NPM exclusivo
Mantido. Cleanup de `pnpm-lock.yaml` continua sendo trabalho válido — pode estar em task dedicada da phase.

### D-44 (kept): BFF migra h3 → Hono
Centro desta phase. Hono substitui h3 em ambos `server.ts` + `auth-server.ts`.

### D-47 (new): Vinxi 0.5.11 mantido como runtime do bundler
Migração de runtime CANCELADA. Re-avaliação só quando substituto razoável amadurecer (TanStack Start v1.0 stable, ou Vinxi 0.6 mantido aparecer, ou requisito de negócio forçar edge global).

### REVOKED
- D-39: superseded por D-44 (BFF muda, mas valor preservado via Hono)
- D-40: superseded por D-47 (sem Vinext)
- D-42: original DoD não se aplica (sem mudança runtime); novo DoD abaixo
- D-43, D-45, D-46: superseded por D-47 (sem spike, sem alvo runtime, sem branch isolado)

## DoD final (Phase 54 BFF-only)

1. **Hono substitui h3 em `server.ts`**: proxy `/api/*` funciona idêntico ao atual.
   - `curl -X POST http://localhost:5173/api/companies/registration` retorna 422 ou 4xx do backend (não 503 fetch failed)
   - `curl -X GET http://localhost:5173/api/employees` retorna 401 (sem auth) ou 200 (com cookie) — comportamento existente

2. **Hono substitui h3 em `auth-server.ts`**: ACF+PKCE flow completo funciona idêntico.
   - `GET /auth/login` → 302 redirect Keycloak `/auth?code_challenge=...&code_challenge_method=S256&state=...` + cookies `pkce_code_verifier` + `pkce_state` setados
   - `GET /auth/callback?code=...&state=...` → token exchange + cookies `id_token` + `refresh_token` + `access_token` setados httpOnly + redirect `/dashboard`
   - `GET /auth/me` → 200 `{ userName, accessGroup, companyId }` ou 401
   - `GET /auth/logout` → 302 Keycloak `end_session_endpoint` com `id_token_hint` + cookies deletados
   - `GET /auth/error` → render página erro

3. **Playwright regression PASS**:
   - `api-proxy.spec.ts` (3 cenários) verde
   - `login-flow.spec.ts` (ACF+PKCE redirect cookies) verde
   - `registration-flow` (se existir) verde

4. **Vitest unit + coverage D-2 ≥ 80%**:
   - `auth-server.test.ts` 37 tests verde — adapter test cases pra Hono mocks em vez de h3
   - Coverage D-2 (new-files since `968eefb`) ≥ 80% lines + 70% branches
   - Suíte total client preserva green (704 pass / 15 pre-boundary skip)

5. **Docker compose hot-reload OK**:
   - `docker compose up -d frontend-client` boots OK
   - Edit em `server.ts` ou `auth-server.ts` → reload do server router via Vinxi HMR (ou container restart documentado se Vinxi não suportar HMR pro h3-port routers)

6. **Bundle size + build time não regridem**:
   - `npm run build` exit 0
   - Bundle main client gz ≤ 250 KB (atual ~200 KB — Hono adiciona ~30 KB esperado, dentro do gate D-3 ≤ 300 KB)
   - Build time não excede 2× baseline

7. **Security não-negociável (D-15 Phase 49)**:
   - PKCE S256 mantido
   - Cookies httpOnly + Secure-em-prod
   - Sem token em storage browser
   - CORS allowlist exato
   - CSRF protection equivalente a state validation no callback + SameSite cookies
   - `bruteForceProtected:true` em realms preservado
   - Logout invalida session Keycloak

8. **Clean-slate journey (opcional)**:
   - `docker compose down -v` + up + jornada de registro PJ + login + 1 navegação autenticada
   - Apenas se reviewer pedir como validação extra; não é blocking gate

## Specialist routing

- **Doer:** `jdi-doer-onboarding-keycloak-frontend-vinext` (cobre BFF rewrite)
- **Reviewers (cross-cutting):** frontend (mandatory Playwright), security (auth flow validation mandatory), backend (no-scope APPROVED defer)

## Riscos

| Risco | Mitigação |
|---|---|
| Hono não integra com Vinxi router `type: "http"` | Hono exporta `fetch` Web standard. Vinxi h3 router internally usa h3 mas aceita handler genérico. Spike 30min antes de Wave 2: confirmar Vinxi aceita Hono via `default export app` ou `export const handler = app.fetch`. Se não, fallback documentado: keep h3 só no entry router, Hono dentro com adapter. |
| ACF+PKCE regressão | Test-first: `auth-server.test.ts` 37 tests devem PASS após swap. Reviewer roda Playwright full regression `login-flow.spec.ts` + manual login no docker compose up |
| Cookie semantics diff h3 vs Hono | Hono `setCookie` usa `hono/cookie` helper — atribui `path`, `httpOnly`, `secure`, `sameSite`, `maxAge` mesma semântica que h3 `setCookie`. Verify via test cases existentes |
| Build error Vinxi + Hono | Hono é pure Web standard ESM, Vite 5 (Vinxi 0.5) compatível. Sem polyfill custom esperado |
| Phase 33 ACF+PKCE recém-estabilizado | DoD não-negociável (item 7). Reviewer recusa fix que afrouxe gate. |
| Hot-reload quebra com Hono | Vinxi hot-reload é arquivo-baseado, não h3-específico. Edit no `auth-server.ts` (h3 ou Hono) trigga reload do `type: "http"` router. Verify durante Wave 2 |

## Estratégia execução (waves esboço pré-plan)

1. **Wave 1 — NPM cleanup (D-41)**: Deletar `pnpm-lock.yaml`. Substituir `pnpm` refs em scripts/Dockerfile/CI/docs/check-dev-env.mjs por `npm`. Atomic commit `chore: migrate to npm-only (D-41)`. Independente de Hono.
2. **Wave 2 — Hono compat spike (30 min)**: Confirma Vinxi aceita Hono apps em `type: "http"` router. Documento em `.jdi/cache/hono-compat-spike-*.md`. Bloqueia Wave 3+ se incompatível.
3. **Wave 3 — `server.ts` h3 → Hono**: Reescreve proxy `/api/*`. Mantém todas as edge cases (single listener guard, error mapping 503 → 4xx). Atomic commit.
4. **Wave 4 — `auth-server.ts` h3 → Hono**: Reescreve ACF+PKCE routes (`/auth/login`, `/auth/callback`, `/auth/me`, `/auth/logout`, `/auth/error`). Cookie helpers `hono/cookie`. Test cases `auth-server.test.ts` adaptados para mocks Hono. Atomic commit.
5. **Wave 5 — Validation**: `npm run typecheck` + `npm run lint` + `npm run test` (vitest) + `npm run test:e2e` (playwright) + manual smoke docker compose up + Playwright full regression suite (mandatory G7/G8). Atomic commit.
6. **Wave 6 — Docs**: Update `CLAUDE.md` se mencionar h3; update `docs/dev-setup.md` se relevante; bump `.jdi/VERSION`. Atomic commit final.

## Out-of-scope

- Backoffice BFF migration (phase futura)
- Backend .NET changes
- Keycloak realm changes
- Runtime bundler migration (D-47 cancelado)
- TanStack Router changes
- Cloudflare Workers deploy
- OTel JS changes
- Hono advanced features (middleware composition complex, validation library) — manter apenas o equivalente direto do que h3 entrega hoje

## Referencias

- ROADMAP.md phase 54
- DECISIONS.md D-38 + D-41 + D-44 + D-47 (kept/new); D-39 + D-40 + D-42 + D-43 + D-45 + D-46 (revoked)
- Phase 54 LOOP.md (iter 1 BLOCKED — registro do redirect inicial)
- Phase 54 DISCOVERY.md (commit `45811ef` — análise técnica original Vinext mismatch)
- Phase 54 REVIEW.md (verdict BLOCKED iter 1)
- https://hono.dev (alvo Wave 3 + 4)
- https://github.com/honojs/hono (repo)
- D-12 (token isolation — preservado via Hono `hono/cookie`)
- D-15 Phase 49 (security gates non-negotiable — preservados)
- D-16 (compose runtime canônico — preservado)
- D-17 (Playwright base URLs — preservado)
- Phase 49 REVIEW.md (auth flow lessons learned — aplica aqui)
