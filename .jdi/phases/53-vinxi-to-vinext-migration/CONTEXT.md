# CONTEXT — Phase 54 Migração Vinxi → Vinext (Cloudflare fork)

## Goal
Migrar o runtime dos 2 SPAs (`frontend/client`, `frontend/backoffice`) de Vinxi 0.5.11 para Vinext (https://github.com/cloudflare/vinext) preservando a arquitetura BFF atual (h3 routers `server.ts` + `auth-server.ts`). Sem regressões funcionais, sem perda de SSR/hydration, sem mudanças no flow de autenticação ACF+PKCE.

Pure runtime swap — não toca código de auth, backend C#, Keycloak realms ou contrato de API.

---

## Stack atual

- React 19 + Vinxi 0.5.11 + TanStack Router + Tailwind 4 + react-hook-form + zod + radix-ui + sonner
- 2 SPAs independentes (D-4): `frontend/client` (porta 5173) + `frontend/backoffice` (porta 5174)
- BFF h3 routers em ambos: `server.ts` (proxy `/api/*`) + `auth-server.ts` (ACF+PKCE flow, cookie management)
- Testes: Vitest unit (37 client + 35 backoffice em `auth-server.test.ts`) + Playwright e2e (api-proxy, login flow, regression)
- Docker compose: bind mounts pra hot-reload dev (`server.ts`, `auth-server.ts`, `app.config.ts`)
- Node 22-alpine (container), Node 24.x (host)

## Stack alvo

- Vinext (latest semver tag de https://github.com/cloudflare/vinext) substitui `vinxi` em ambos `package.json`
- `app.config.ts` migrado pra Vinext API (provavelmente compatível drop-in dado fork)
- h3 routers intactos (Vinext mantém h3 ou troca por adapter equivalente — TBD na descoberta)
- React 19, TanStack Router, Tailwind 4 inalterados

---

## Decisões locked

### D-38 (2026-05-24): Estratégia incremental — client primeiro

Migrar `frontend/client` primeiro (validar Vinext em SPA de menor escopo de tela). Backoffice migra em phase 55 separada após client convergir clean.

**Razão:** Isola blast radius. Se Vinext quebrar SSR/hydration, só client é afetado. Backoffice mantém serviço pra admin enquanto debug.

**Aplicação:** Phase 54 só toca `frontend/client/`. `frontend/backoffice/` permanece em Vinxi 0.5.11 até phase 55.

### D-39 (2026-05-24): BFF preservado permanente — não há phase 55 de BFF removal

`server.ts` (proxy `/api/*`) + `auth-server.ts` (ACF+PKCE) permanecem em ambos SPAs indefinidamente.

**Razão:** BFF entrega valor real — não é debt:

- Token isolation (D-12): `id_token` + `refresh_token` nunca tocam JS browser; só BFF os possui. XSS comprometendo SPA não vaza tokens long-lived
- Same-origin cookies: SPA + BFF na mesma origem evita CORS dance pra httpOnly cookies
- Realm-per-SPA: BFF tem `KEYCLOAK_REALM` env diferente por SPA (client vs backoffice). Backend .NET é único — não consegue servir 2 realms sem complicar config
- API contract decoupling: SPA pede `/auth/me`, BFF compõe response. Backend muda formato Keycloak — só BFF re-mapeia
- PKCE state correlation: cookie `pkce_code_verifier` entre `/auth/login` ↔ `/auth/callback`. BFF é o lugar natural
- OWASP recommendation: BFF pattern é OWASP-recommended pra SPAs com auth flow
- Hook point pra audit/rate-limit/tenant-resolution middleware

**Aplicação:** Phase 50 REVIEW:935 sugeriu "Vinext removes BFF entirely" — descartar essa sugestão. G11 gate "zero Vinxi imports" pós-migração não é justificativa pra remover BFF (BFF não tem `from 'vinxi'` imports relevantes).

### D-40 (2026-05-24): Vinext alvo = latest semver tag (release)

Pinning via release tag mais recente do fork Cloudflare. Não usar `main` (HEAD do branch) — pin instável.

**Razão:** Estabilidade. Release tags assumem CI passou + sign-off do mantenedor.

**Aplicação:** Doer consulta `gh release list` / `npm view @cloudflare/vinext versions` no momento do plan, escolhe latest. Pin em `package.json` como `"vinext": "<version-exata>"` (não `^` ou `~`) pra evitar drift.

### D-41 (2026-05-24): NPM exclusivo — PNPM proibido

Todo o projeto usa NPM. PNPM é proibido em scripts, docs, Dockerfile, CI/CD.

**Razão:** Unificação do ecosystem. Histórico atual mistura `pnpm` em alguns scripts e `npm` em outros — fonte de confusão.

**Aplicação:**
- Scripts em `package.json`: `npm run dev`, `npx playwright test`, etc.
- Lock file: `package-lock.json` único; remover `pnpm-lock.yaml` (já em git status?)
- Dockerfile: `npm ci` ou `npm install` (já está)
- `scripts/check-dev-env.mjs` mensagens: substituir `pnpm dev` por `npm run dev`
- Workspaces: raiz `package.json` usa `"workspaces": [...]` (NPM nativo) — manter
- Documentação `.jdi/` em pt-BR pode mencionar `npm`; código/PRs em inglês idem
- CI workflows `.github/workflows/*`: confirmar `npm install` / `npm ci` em todos os jobs

Migration debt: phase 54 também converte qualquer script residual `pnpm` → `npm`.

### D-42 (2026-05-24): Definition of Done — 4 gates obrigatórios

DoD cumulativo (todos PASS):

1. **Playwright e2e full PASS ambos SPAs** — regression suite completa de `frontend/client/playwright/specs/*.spec.ts` verde. Inclui api-proxy, login flow, registration, ACF+PKCE callback. Backoffice playwright spec pra phase 55, não phase 54.
2. **Vitest unit + coverage ≥ 80% D-2 novos files** — projeto pattern D-2 (only-new-files since boundary 968eefb). 72 testes existentes auth-server.test.ts permanecem ≥ 80% coverage.
3. **SSR/hydration zero regressão** — manual + DevTools console: nenhum hydration mismatch warning, nenhum flash de unstyled content, nenhum React `useId` warning.
4. **Docker compose dev hot-reload OK** — `docker compose up -d frontend-client` continua funcionando com bind mounts. Edição em `src/**` reflete em browser dev refresh. Edição em `auth-server.ts` reflete via HMR ou requer container restart documentado.

Quaisquer 4 destes FAIL → BLOCKED. Reviewer não cria APPROVED_WITH_WARNINGS pra problemas nestes 4 — todos devem fechar.

---

## Specialist routing

- **Wave 1 (frontend Vinext migration):** `jdi-doer-onboarding-keycloak-frontend-vinext` — é o specialist nomeado pra esta phase (D-25 + project routing). Owns `frontend/client/**/*.{ts,tsx,jsx,js,css,scss,html,mjs,cjs}`.
- **Wave 2 (security audit):** `jdi-doer-onboarding-keycloak-security` se Vinext introduzir mudanças em `Dockerfile`, deps com CVE, ou .github/workflows. Trigger automático via glob da security routing.
- **Sem backend C# specialist** — phase não toca .NET / EF / Keycloak realms.

---

## Migration debt items (capturadas de phases anteriores)

- **G11 gate "zero from 'vinxi' imports"** (phase 50 REVIEW: 282, 509, 622, 690, 771, 920) — vira PASS automático pós-migração, mas pode ser removido como gate (não-aplicável). Reviewer decide se mantém como guardrail anti-regressão.
- **W-react-setstate** (phase 50 REVIEW:1171) — React setState-in-render no Transitioner (Vinxi/TanStack Router interop). Validar se Vinext resolve ou se persiste. Não-blocker.
- **BFF debt** (phase 50 REVIEW:935) — sugeria Vinext "removes BFF entirely". Descartado por D-39.
- **Vinxi 0.5.11 deps lock** — `pnpm-lock.yaml` referencia `vinxi@0.5.11`. Pós-migração remove via D-41 (npm exclusivo).

---

## Out-of-scope

- Backoffice migration (phase 55 separada por D-38)
- BFF removal (descartado por D-39)
- Backend .NET changes
- Keycloak realm config (D-7 PHASE-33 acabou de migrar pra ACF+PKCE, intocável agora)
- Cloudflare Workers deploy (Vinext suporta, mas deploy = phase futura)
- OTel JS instrumentation (phase 53 já cobre, debt items separados)

---

## Riscos

| Risco | Mitigação |
|---|---|
| Vinext API incompat com h3 routers atuais | Doer roda spike (1 hora) em phase de descoberta — se Vinext quebra h3, escalate pra discussão sobre alternativas (Hono, Vinxi 0.6) antes de plan completo |
| SSR/hydration mismatch após swap | Playwright e2e + manual DevTools console gate cobre. Doer pode forçar CSR-only se mismatch persiste (registro como debt; backoffice phase 55 herda decisão) |
| Vinext usa Cloudflare-specific APIs (Workers) que não rodam em Node | Pin a release tag que suporta Node runtime explicitamente. Doer valida `node app.config.ts` import limpo antes de commit inicial |
| Bind mounts compose quebram | Re-validar compose.yaml mount paths pós-migração. Atualizar Dockerfile se Vinext muda entry point |
| Lock file conflict NPM vs PNPM coexistência | D-41 obriga remoção `pnpm-lock.yaml`; commit dedicado pra cleanup antes de Vinext install |
| Latest Vinext semver tag não estável | Plan fase de descoberta — doer roda `npm view` + GitHub releases, valida há ≥7 dias sem revert na main pra confiar |

---

## Estratégia de execução (esboço pré-plan)

1. **Wave 0 (descoberta, ≤1h):** Identificar latest Vinext semver, ler README/CHANGELOG, validar Node runtime suporte, confirmar h3 compatibility ou alternativa documentada.
2. **Wave 1 (NPM cleanup):** Remover `pnpm-lock.yaml`, atualizar scripts residuais `pnpm` → `npm` (`scripts/check-dev-env.mjs`, qualquer docs/*.md), `npm install` no `frontend/client/`. Atomic commit `chore(client): migrate to npm-only (D-41)`.
3. **Wave 2 (Vinext install client):** Substituir `"vinxi": "^0.5.11"` por `"vinext": "<exact-version>"` em `frontend/client/package.json`. Atualizar `app.config.ts` se Vinext API diverge. `npm install`. Atomic commit `feat(client): swap runtime to vinext@<version> (D-40)`.
4. **Wave 3 (validation):** `npm run dev` host + container both — confirma 302 redirect ACF, `/api/*` proxy 422/200, console limpo. Playwright e2e full PASS. Coverage ≥ 80%. Atomic commit `docs(vinxi-to-vinext-migration): validation evidence (iter N)`.
5. **Wave 4 (cleanup):** Remover dead code, gate G11 atualizado (`zero from 'vinext'` se reviewer manter como anti-regressão pra phase 55), update CLAUDE.md / dev-setup docs.

Loop Ralph esperado: 1-2 iter se Vinext for drop-in; 3-4 iter se h3/SSR exigir adapter.

---

## Referencias

- ROADMAP.md phase 54
- DECISIONS.md D-38 a D-42 (esta phase)
- Phase 50 REVIEW.md (debt items Vinext)
- Phase 53 REVIEW.md (gates atuais pra herdar)
- https://github.com/cloudflare/vinext (alvo)
- D-4 (2 SPAs independentes, no shared code)
- D-12 (id_token never in JS)
- D-16 (compose runtime canônico)
- D-25 (specialist routing)
