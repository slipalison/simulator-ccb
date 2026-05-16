# Phase 49 — auth-flow-fix — CONTEXT

## Goal

Diagnosticar e corrigir 2 bugs do fluxo ACF+PKCE que afetam ambos SPAs (`frontend/client` :5173 e `frontend/backoffice` :5174), preservando a postura de seguranca locked desde a Phase 33 (ACF+PKCE com S256, tokens em cookies HttpOnly, sem ROPC, sem localStorage). Entregar fix end-to-end + suite de regressao Playwright executando contra o stack `docker compose` real, evidenciando login + logout + refresh e o caso "post-login error flash" em ambos SPAs. Seguranca eh nao-negociavel: nenhum atalho que afrouxe SameSite, CORS, CSRF, PKCE ou expiry de cookies entra no escopo.

## Bugs

### Bug 1: Login/logout retorna para a pagina Keycloak ACF auth

**Evidencia (verbatim do usuario):**
```
http://localhost:8180/realms/onboarding/protocol/openid-connect/auth?client_id=onboarding-backoffice&redirect_uri=http%3A%2F%2Flocalhost%3A5174%2Fauth%2Fcallback&response_type=code&scope=openid+profile+email&code_challenge=...&code_challenge_method=S256&state=...
```

**Symptom:** Usuario clica em "Entrar", chega na tela de login Keycloak, mas o fluxo nao completa — fica em loop voltando para a mesma URL de autorizacao, ou recebe erro de "realm not found". Mesma sintoma em logout.

**Root-cause hypothesis primaria (alta confianca):**
- URL no relato usa `/realms/onboarding/...`, mas **nao existe realm `onboarding`** no projeto. Os realms importados sao `client` e `backoffice` (ver `keycloak/client-realm.json:2` e `keycloak/backoffice-realm.json:2`).
- Em ambos `frontend/backoffice/auth-server.ts:24` e `frontend/client/auth-server.ts:23`, o fallback default eh:
  ```ts
  const KEYCLOAK_REALM = process.env.KEYCLOAK_REALM || "onboarding";
  ```
- O valor `"onboarding"` eh holdover de antes da Phase 34 (que separou os realms). Quando os SPAs sao executados **fora do `docker compose`** (ex: `pnpm dev` na maquina host com Keycloak no container), `process.env.KEYCLOAK_REALM` nao existe e o fallback eh usado → SPA constroi `https://localhost:8180/realms/onboarding/...` que **nao tem cliente registrado** → Keycloak ou retorna 404 ou renderiza pagina de auth vazia que entra em loop.
- Confirmacao: `compose.yaml:115,140` define `KEYCLOAK_REALM=client` e `KEYCLOAK_REALM=backoffice` respectivamente. `.env` NAO exporta `KEYCLOAK_REALM`, entao qualquer execucao fora do compose-orquestrado vai bater no default errado.

**Root-cause hypothesis secundaria (media confianca):**
- `backoffice-realm.json:50-53` lista `redirectUris` apenas para `http://localhost:5174/auth/callback` e `?retry=1`. Nao ha `postLogoutRedirectUris` configurado explicitamente — a URL de logout no `auth-server.ts:248` redireciona para `${FRONTEND_URL}/auth/login` que pode nao estar em `validPostLogoutRedirectUris`. Keycloak 26 valida `post_logout_redirect_uri` contra a lista; se faltar, retorna erro e mantem o usuario na pagina de auth.
- `client-realm.json:58-62` tem `redirectUris: ["http://localhost:5173/auth/callback"]` mas idem — sem `postLogoutRedirectUris` declarado.

**Files relevantes:**
- `frontend/backoffice/auth-server.ts:21-30` (config defaults)
- `frontend/client/auth-server.ts:21-29` (config defaults)
- `keycloak/backoffice-realm.json:50-56`
- `keycloak/client-realm.json:58-63`
- `compose.yaml:111-148` (env var wiring)
- `.env` (missing `KEYCLOAK_REALM` exports)

### Bug 2: Tela de erro pos-login que desaparece no reload

**Symptom:** Apos login bem-sucedido (cookies setados, redirect chega no SPA), aparece tela de erro/redirect para login por alguns ms. Refresh resolve.

**Root-cause hypothesis primaria (alta confianca — client SPA):**
- `frontend/client/src/components/guards/AuthGuard.tsx:17-22` faz `useEffect` que dispara `navigate({to: "/login"})` quando `auth.isAuthenticated === false`. Mas **nao checa `auth.isLoading`**. Durante o primeiro render apos o callback redirect, `AuthProvider` estado inicial eh `{ isAuthenticated: false, isLoading: true }`. AuthGuard imediatamente fire navigate, antes que `tryRestore()` em `auth-context.tsx:60-101` complete e atualize o estado.
- O `tryRestore` faz fetch `/auth/me` que retorna 200 com cookie valido, mas o navigate ja foi disparado — usuario ve flash de "Verificando autenticacao..." + redirect para `/login`, depois (no reload) o estado completa antes do guard fire.

**Root-cause hypothesis primaria (alta confianca — backoffice SPA):**
- `frontend/backoffice/auth-server.ts:228` redireciona para `/admin/users` apos token exchange.
- `frontend/backoffice/src/router.tsx:77-82` mapeia `/admin/users` para `RedirectCompanies` component que faz `navigate({to:"/admin/companies"})`. Cadeia de redirects no mount.
- `frontend/backoffice/src/components/templates/AdminLayout.tsx:89-93` tem `useEffect` que faz `window.location.href = "/admin/login"` se `!isLoading && !isAuthenticated`. Mas L101 retorna `null` enquanto loading. Race: durante o `tryRestore` em `admin-auth-context.tsx:33-50`, se o cookie nao chegou no primeiro fetch (ver hipotese 2 abaixo), `getAdminMe()` joga → estado `isAuthenticated=false, isLoading=false` → effect dispara `window.location.href` para login.

**Root-cause hypothesis secundaria (media confianca — cookie commit race):**
- Cookies setados com `sameSite: "strict"` em ambos `auth-server.ts:166-176` (backoffice) e `auth-server.ts:110-122` (client). Em redirects 30x originados de cross-site (Keycloak `:8180` → SPA `:5174`), Chrome pode nao enviar o cookie Strict no primeiro request mesmo apos same-origin redirect interno. `sameSite: "lax"` seria mais robusto para a primeira requisicao apos auth, mantendo CSRF protection razoavel.
- Validar empiricamente via Playwright + DevTools — pode ser combinacao de Strict + 302 chain causando cookie nao-enviado no primeiro `/auth/me`.

**Root-cause hypothesis terciaria (baixa confianca, mas observavel):**
- `AuthCallbackPage` (`frontend/client/src/components/pages/AuthCallbackPage.tsx`) tem polling de `/auth/me` com retry 5x/500ms — **mas eh dead code**: o Vinxi auth router (`base: "/auth"`) intercepta `GET /auth/callback` antes do SPA, redirecionando para `/profile`. Logo o polling client-side nunca executa. Se a intencao original era poll para superar a race, o codigo nao esta wired.

**Files relevantes:**
- `frontend/client/src/lib/auth-context.tsx:49-101` (provider + tryRestore)
- `frontend/client/src/components/guards/AuthGuard.tsx:13-35` (missing isLoading check)
- `frontend/client/src/components/pages/AuthCallbackPage.tsx` (dead code — verificar)
- `frontend/backoffice/src/lib/admin-auth-context.tsx:33-50` (tryRestore)
- `frontend/backoffice/src/components/templates/AdminLayout.tsx:86-117` (redirect logic)
- `frontend/backoffice/auth-server.ts:163-177` (cookie config)
- `frontend/client/auth-server.ts:109-123` (cookie config)

## Scope (in)

- **Keycloak realm configs:** revisar/expandir `redirectUris`, `webOrigins`, `postLogoutRedirectUris` (ou `validPostLogoutRedirectUris` no formato Keycloak 26) em `keycloak/backoffice-realm.json` + `keycloak/client-realm.json`. Garantir match exato com URLs construidas por `auth-server.ts`.
- **Frontend auth pipeline (ambos SPAs):**
  - Corrigir defaults de `KEYCLOAK_REALM` para refletir Phase 34 (sem fallback `"onboarding"` — fail-fast se env nao setado, ou default por SPA `"backoffice"`/`"client"`).
  - `AuthGuard` (client) checar `isLoading` antes de navegar.
  - `AdminLayout` (backoffice) garantir que redirect-on-mount nao dispara durante loading inicial.
  - Avaliar `sameSite` strict vs lax para `*_access_token` cookies (security review obrigatorio).
  - Remover/wire AuthCallbackPage (decidir: deletar como dead code ou rotear corretamente).
- **Backend auth (`src/Onboarding.API`):**
  - `/api/auth/*` e `/api/admin/me/*` — confirmar que `ValidIssuer` (Program.cs:125-126, 183) bate com KC public URL `http://localhost:8180/realms/{realm}` (atualmente OK em `appsettings.json`, validar via teste).
  - CORS allowlist `Program.cs:254` ja cobre `:5173` + `:5174` com `AllowCredentials` — manter.
- **Test fixtures:**
  - Script `scripts/seed-test-users.sh` (ou bootstrap JSON dentro de `keycloak/*-realm.json`) — usuarios de teste com password fixo + grupo determinado. Nao criar manualmente em instancia rodando.
- **Playwright regression suite:**
  - Suite cobrindo: client login happy path → /profile rendered, client logout → /auth/login com /auth/me 401, backoffice login → /admin/companies rendered, backoffice logout, simulacao do race (verificar nenhum flash de erro entre callback e tela final), reload-resilience.

## Scope (out)

- Migrar Keycloak para outra versao
- Substituir ACF+PKCE por implicit/ROPC (locked em D-feedback `feedback_backoffice_auth_ropc`)
- Remocao do cliente ROPC legado `onboarding-app` em `client-realm.json:22-46` (cleanup futuro, nao bloqueia)
- Features de produto fora do auth (Fundos UI, etc — Phases 50-54)
- Testes manuais — somente Playwright + curl reproducible em CI

## Locked decisions (Phase 49)

Decisoes incorporadas ao `.jdi/DECISIONS.md` como `D-11..D-15` (2026-05-16):

- **D-11 (DA-1):** ACF+PKCE eh o auth flow para ambos SPAs. Sem fallback para implicit/ROPC, sem refatoracao para outro fluxo. Locked desde Phase 33 + MEMORY `feedback_backoffice_auth_ropc`. Toda mudanca preserva S256 PKCE e secret confidencial do cliente.
- **D-12 (DA-2):** Tokens (access + refresh) ficam **exclusivamente** em cookies HttpOnly setados pelo handler Vinxi (`auth-server.ts`). Proibido escrever em `localStorage`/`sessionStorage`/IndexedDB. Cookie `path:"/"`, `httpOnly:true`, `secure` em prod. SameSite vai ser revisitado (lax vs strict) com seguranca como gate.
- **D-13 (DA-3):** `docker compose down -v` autorizado para reproducao local. Toda mudanca de realm precisa estar persistida em `keycloak/{client,backoffice,master}-realm.json` antes do commit — proibido alterar realm via Admin UI sem refletir no JSON (CI quebra reprodutibilidade).
- **D-14 (DA-4):** Usuarios de teste para diagnostico criados via fixture: ou bloco `users` adicionado nos realm JSONs ja existentes, ou script `scripts/seed-test-users.sh` idempotente que usa o token de service account do `onboarding-api-admin`. Proibido criar usuarios manualmente em Keycloak rodando.
- **D-15 (DA-5):** Gates de seguranca nao-negociaveis: PKCE S256 mantido, cookies HttpOnly + Secure-em-prod, CORS allowlist exato (sem `*`), CSRF protection equivalente a SameSite + state validation, sem desabilitar `bruteForceProtected`. Reviewer recusa qualquer fix que afrouxe esses guard rails para "fazer funcionar".

## Open questions para /jdi-plan

- **Q1:** Os realm JSONs precisam de mudanca? Provavelmente sim para adicionar `validPostLogoutRedirectUris`. Se sim, security reviewer dispara hardening drift check (testes em `tests/keycloak-hardening/` podem precisar atualizacao).
- **Q2:** Defaults `KEYCLOAK_REALM` em `auth-server.ts`: fail-fast (throw se env undefined) ou default por SPA (backoffice→`"backoffice"`, client→`"client"`)? Fail-fast eh mais seguro porem quebra `pnpm dev` sem `.env.local`. Recomendacao: fail-fast + atualizar `.env.example` + documentar.
- **Q3:** Strategy para race condition Bug 2: (a) AuthGuard checa `isLoading`, (b) callback handler valida cookie antes do redirect final, (c) primeira fetch `/auth/me` no SPA com retry/backoff curto (200ms x 3), ou (d) combinar. Custo/risco vs cobertura por opcao.
- **Q4:** Cookies `sameSite: "strict"` vs `"lax"` para `*_access_token` — strict evita cookie-leakage em cross-site GET, lax permite cookie em top-level navigation pos-redirect. Trade-off de seguranca documentado em D-15.
- **Q5:** `AuthCallbackPage` (client SPA): delete (dead code) ou wire (rota separada `/auth/spa-callback`)? Decidir antes do plan.
- **Q6:** Diagrama de fluxo auth (sequence diagram em `docs/`) — vale a pena para futuro debugging? Sim/nao, escopo do plan.

## Acceptance criteria

1. **Ambos SPAs:** login completa em <2s, lid na rota final (client → `/profile` ou `/employees` per access group; backoffice → `/admin/companies`). Sem loop de volta para Keycloak, sem flash de tela de erro.
2. **Logout:** invalida sessao em Keycloak (front-channel logout), limpa cookies, redireciona para tela de login do SPA. `/auth/me` retorna 401 imediatamente apos logout.
3. **Playwright suite:** minimo 8 cenarios cobrindo: client login happy, client logout, backoffice login happy, backoffice logout, post-login race (sem flash), refresh resilience (recarregar pagina autenticada), expired-token-refresh, login com cookie bloqueado (graceful error). Suite roda contra `docker compose up -d` limpo e passa 100%.
4. **Security:**
   - ACF+PKCE S256 verificado (Playwright intercepta authorize URL e valida `code_challenge_method=S256`).
   - Tokens nunca aparecem em `localStorage`/`sessionStorage` (Playwright `evaluate` checa storage vazio).
   - CORS allowlist exato (sem `*`), preflight respeitado.
   - Logout invalida session em Keycloak (front-channel ou end_session_endpoint).
5. **Sem regressao:** Endpoints existentes (`/api/admin/companies`, `/api/fundos`, etc) continuam respondendo 401 sem token e 200 com token valido. Testes Application+Integration existentes verde.
6. **Reprodutibilidade:** `docker compose down -v && docker compose up -d` reproduz estado funcional sem touch manual no Keycloak Admin UI. Realm JSON contem tudo necessario.

## References

- MEMORY: [feedback_backoffice_auth_ropc](C:\Users\aliso\.claude\projects\D--REPO-keycloak-tests\memory\feedback_backoffice_auth_ropc.md) — Phase 33 ACF+PKCE migration
- `.planning/phases/33-pkce-keycloak-custom-themes/` — design original do ACF+PKCE com temas Keycloak
- `.planning/phases/34-isolar-backoffice-e-client-em-realms-separados/` — quando os realms `onboarding` foram separados em `backoffice` + `client`
- Bug 1 evidence URL: `http://localhost:8180/realms/onboarding/protocol/openid-connect/auth?client_id=onboarding-backoffice&redirect_uri=http%3A%2F%2Flocalhost%3A5174%2Fauth%2Fcallback&response_type=code&scope=openid+profile+email&code_challenge=...&code_challenge_method=S256&state=...`
- `compose.yaml:34-63` (Keycloak service + import realm volume)
- `compose.yaml:100-148` (frontend env wiring with `KEYCLOAK_REALM`)
- Backend auth wiring: `src/Onboarding.API/Program.cs:109-185` (BearerBackoffice + BearerClient), `:248-259` (CORS), `:284-294` (middleware pipeline)
