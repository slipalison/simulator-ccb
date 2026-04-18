---
status: root_cause_identified
trigger: |
  DATA_START
  backoffice ACF Invalid state após UPDATE_PASSWORD + hipóteses: localhost/127.0.0.1 mismatch, custom theme do Phase 33, HMR overwrite. Pode instalar e usar o playwright para testar o login.
  DATA_END
created: 2026-04-17
updated: 2026-04-17
slug: backoffice-acf-invalid-state
---

# Debug Session: backoffice-acf-invalid-state

## Symptoms

- **Expected behavior**: Novo admin criado com UPDATE_PASSWORD requiredAction faz login no backoffice via ACF → é redirecionado ao Keycloak → troca senha → Keycloak redireciona de volta ao callback do backoffice → sessão estabelecida (cookies httpOnly) → dashboard carrega.
- **Actual behavior**: Após a troca de senha (UPDATE_PASSWORD), o redirect de volta ao callback do backoffice exibe o erro "Invalid state" na UI. A autenticação falha.
- **Error messages**: "Invalid state" renderizado na página de callback do backoffice (UI).
- **Timeline**: Surgiu após a Phase 33 (aplicação de custom Keycloak themes para backoffice + client).
- **Reproduction**: Playwright — criar novo admin via API (com UPDATE_PASSWORD requiredAction + senha temporária), fazer login, trocar senha, capturar o erro.

## User-Supplied Hypotheses

1. **Localhost/127.0.0.1 mismatch** — possível divergência entre host usado no authorize endpoint e no token exchange, fazendo o cookie de state não ser lido no callback.
2. **Custom theme do Phase 33** — o tema customizado pode estar interferindo no fluxo (form override, action URL alterado, perda do state parameter).
3. **HMR overwrite** — hot-module-reload do Vinxi pode estar sobrescrevendo o state guardado em memória do servidor entre o authorize e o callback.

## Current Focus

- **hypothesis**: **Brave v147 não está armazenando (ou está clearing) os cookies httpOnly setados pelo `/auth/login` response**. Evidência decisiva: na reprodução manual do usuário no Brave, as 3 requests após submit do UPDATE_PASSWORD (callback, error page, /auth/me) chegam ao server com header `Cookie` **COMPLETAMENTE AUSENTE** — nenhum cookie, não apenas pkce_state. Como /auth/me é same-origin fetch com `credentials: "include"`, a ausência total de cookies confirma que o jar do browser para localhost:5174 está VAZIO no momento do erro. Combinado com curl OK + Playwright Chromium OK, o problema é específico do Brave (provavelmente Shields + cross-site redirect treatment, ou Forgetful Browsing).
- **test**: n/a — bug específico de browser do usuário; reproduzir sem o browser do usuário não é possível.
- **expecting**: implementar diagnostic logging + defensive fixes e pedir ao usuário nova reprodução com logs do server + DevTools→Application→Cookies para confirmação final.
- **next_action**: aplicar fix (diagnostic logging + defensive cookie handling + Brave workaround via dual-storage sessionStorage/cookie).
- **reasoning_checkpoint**:
- **tdd_checkpoint**:

## Evidence

- timestamp: 2026-04-17T18:43 — [code read: `frontend/backoffice/auth-server.ts`] — A mensagem "Invalid state" é produzida por `sendRedirect(event, "/auth/error?error=Invalid+state", 302)` na linha 91 quando `query.state !== getCookie(event, "pkce_state")`. Só dispara em 2 cenários: (a) cookie ausente no callback, ou (b) valores diferentes.
- timestamp: 2026-04-17T18:43 — [code read: `frontend/backoffice/app.config.ts`] — Auth router está montado com `base: "/auth"`. Cookies PKCE são gravados com `path: "/auth"`, sameSite: "lax", maxAge: 600s.
- timestamp: 2026-04-17T18:44 — [curl: GET /auth/login] — Server grava corretamente `Set-Cookie: pkce_state=VY6Fi-R5tA9MjyrctLsq; Path=/auth; HttpOnly; SameSite=Lax` e redireciona pra Keycloak com `state=VY6Fi-R5tA9MjyrctLsq`.
- timestamp: 2026-04-17T18:44 — [curl: full ACF+UPDATE_PASSWORD flow end-to-end] — Reproduzi o fluxo completo via curl (criar admin com UPDATE_PASSWORD, login, submit nova senha). Keycloak redirecionou de volta para `/auth/callback?state=VY6Fi-R5tA9MjyrctLsq&code=...`. O `pkce_state` cookie permaneceu no jar durante toda a jornada (incluindo a troca de domínio 5174→8180→5174). Callback respondeu `302 location: /admin/users` + `Set-Cookie: backoffice_access_token=...` + `Set-Cookie: pkce_state=; Max-Age=0` (cleanup). **Fluxo funciona 100% via curl.**
- timestamp: 2026-04-17T18:45 — [theme inspection: `keycloak/themes/onboarding-backoffice/`] — Custom theme só contém 3 arquivos: `login/login.ftl`, `login/theme.properties`, `login/resources/css/styles.css`. **Nenhum override de `login-update-password.ftl`**. Theme declara `parent=keycloak`, que no Keycloak 26.1 herda de `base` (verificado extraindo `org.keycloak.keycloak-themes-26.1.5.jar`). Portanto o UPDATE_PASSWORD usa o template `base/login/login-update-password.ftl` sem modificação — o form action preserva `client_data` (com `st` encoded) e `state` volta íntegro pro callback.
- timestamp: 2026-04-17T18:45 — [payload decode: `client_data` em URL do form action] — Decoding base64url de `client_data` durante UPDATE_PASSWORD revelou `{"ru":"http://localhost:5174/auth/callback","rt":"code","st":"VY6Fi-R5tA9MjyrctLsq"}`. O `state` original é carregado pelo Keycloak através dos passos: login → required-action → callback. State é preservado.
- timestamp: 2026-04-17T18:45 — [config inspection: `compose.yaml` + `auth-server.ts`] — KEYCLOAK_URL interno (API) = `http://keycloak:8080`, KEYCLOAK_PUBLIC_URL browser-facing = `http://localhost:8180`. FRONTEND_URL = `http://localhost:5174`. REDIRECT_URI = `http://localhost:5174/auth/callback`. Redirect URI do client `onboarding-backoffice` no realm JSON = `["http://localhost:5174/auth/callback"]`. **Nenhum mismatch localhost/127.0.0.1.**
- timestamp: 2026-04-17T18:45 — [code inspection: `auth-server.ts`] — Nenhum estado em memória do servidor. State vive 100% em HTTP cookies. HMR do Vinxi não pode afetar state (não há state no processo).
- timestamp: 2026-04-17T20:15 — [playwright automation: Chromium headless, sessão fresca] — Reproduzi o fluxo completo em browser real isolado: criar admin com UPDATE_PASSWORD via Keycloak Admin API, navegar para `/admin/login`, clicar "Entrar", preencher credenciais, trocar senha. **Resultado: fluxo COMPLETO com sucesso.** Final URL = `http://localhost:5174/admin/users`. Cookies `backoffice_access_token` e `backoffice_refresh_token` gravados com `SameSite=Strict`. Cookies `pkce_state` + `pkce_code_verifier` limpos após callback. State preservado corretamente durante toda a jornada (`state=ozzszwIJjeHJn2zmzv3h` do authorize inicial volta intacto no callback). **Bug NÃO reproduz em browser fresco.**
- timestamp: 2026-04-17T20:18 — [playwright automation: cenário double-click em /auth/login] — Testei o cenário onde o usuário clica "Entrar" duas vezes (navegando de volta ao admin/login entre cada clique, forçando dois GETs a /auth/login que sobrescrevem pkce_state). Resultado: segundo authorize teve novo state (`FmJ04OGIShEUq4HZceRQ`) que foi gravado no cookie, e Keycloak honrou a segunda authorization request. Fluxo completou com sucesso → `/admin/users`. **Bug NÃO reproduz.**
- timestamp: 2026-04-17T20:20 — [env inspection: container backoffice] — Variáveis de ambiente dentro do container: `KEYCLOAK_URL=http://keycloak:8080`, `KEYCLOAK_PUBLIC_URL=http://localhost:8180`, `FRONTEND_URL=http://localhost:5174`, `KEYCLOAK_REALM=onboarding`, `KEYCLOAK_CLIENT_ID=onboarding-backoffice`, `KEYCLOAK_CLIENT_SECRET=backoffice-secret-dev-change-in-prod-2026`. Tudo correto.
- timestamp: 2026-04-17T20:20 — [OIDC discovery check] — `http://localhost:8180/realms/onboarding/.well-known/openid-configuration` retorna `issuer: http://localhost:8180/realms/onboarding` e `token_endpoint: http://localhost:8180/...`. Confirmado: Keycloak está configurado com `hostname-url=http://localhost:8180`, os endpoints anunciados são browser-facing, e o token exchange do backoffice funciona tanto via `http://keycloak:8080/...` (internal) quanto via `http://localhost:8180/...` (public).
- timestamp: 2026-04-17T21:00 — [**user-supplied browser evidence: Brave 147 Windows**] — Usuário reproduziu bug manualmente no Brave e capturou 3 requests via cURL:
  1. GET `/auth/callback?state=ynJP6pq3B_F9upfdYbqU&code=...&iss=http%3A%2F%2Flocalhost%3A8180%2Frealms%2Fonboarding` — **header Cookie AUSENTE**.
  2. GET `/auth/error?error=Invalid+state` (302 redirect do callback) — **header Cookie AUSENTE**.
  3. GET `/auth/me` (same-origin fetch pela UI após carregar error page) — **header Cookie AUSENTE**.
  - URL do form Keycloak UPDATE_PASSWORD tinha `client_data` base64url-encoded decodificando para `{"ru":"http://localhost:5174/auth/callback","rt":"code","st":"ynJP6pq3B_F9upfdYbqU"}`. **State na URL do callback = state original. Zero divergência de state — o state do query está CORRETO.**
  - Browser: Brave 147 (Chromium fork), Windows. Headers incluem `Sec-GPC: 1`, `sec-ch-ua: "Brave";v="147"`.
- timestamp: 2026-04-17T21:05 — [code re-read: `frontend/backoffice/src/components/pages/AdminLoginPage.tsx`] — `handleLogin` usa `window.location.href = "/auth/login"` — **top-level navegação, NÃO fetch**. Hipótese H-A (fetch-drop de Set-Cookie em redirect cross-site) **ELIMINADA**. O browser recebe a resposta 302 via navegação normal, o Set-Cookie deveria ser armazenado normalmente pela spec RFC 6265.
- timestamp: 2026-04-17T21:05 — [code re-read: `frontend/backoffice/src/lib/admin-api.ts`] — `getAdminMe()` usa `fetch("/auth/me", { method: "GET", credentials: "include" })` — same-origin com credentials include. A ausência de cookies em /auth/me (request 3) **prova** que o jar do Brave para localhost:5174 está completamente vazio no momento do erro, não é questão de SameSite/cross-site.

## Eliminated

- **Hipótese 1 (localhost/127.0.0.1 mismatch)**: eliminada — todo o fluxo usa `localhost` consistentemente (compose env, realm JSON redirectUris, FRONTEND_URL, KEYCLOAK_PUBLIC_URL). Sem `127.0.0.1` em lugar nenhum relevante ao fluxo ACF.
- **Hipótese 2 (custom theme do Phase 33)**: eliminada — theme só override `login.ftl` (primeira tela). UPDATE_PASSWORD usa `base/login/login-update-password.ftl` sem mudança. O `state` flui intacto via `client_data` em todos os passos (provado por decode do base64url do form action).
- **Hipótese 3 (HMR overwrite)**: eliminada — server-side não mantém state em memória; cookies são a única fonte de verdade. HMR não afeta cookies do browser.
- **Hipótese 4 (bug reproduz em browser real)**: eliminada via Playwright — browser automation em Chromium fresco executa o fluxo completo com sucesso. Não há bug reproduzível em sessão browser limpa.
- **Hipótese 5 (dupla navegação causa cookie overwrite)**: eliminada — scenário de double-click em /auth/login foi testado explicitamente. Keycloak aceita novo authorize request, state é atualizado, fluxo completa.
- **Hipótese H-A (AdminLoginPage usa fetch em vez de navegação)**: eliminada — código usa `window.location.href = "/auth/login"` (top-level navegação), não fetch/XHR. Set-Cookie em resposta 302 de navegação top-level é padrão RFC 6265 e deveria funcionar.
- **Hipótese: state divergente na URL**: eliminada — user evidence mostra state na URL = state original (via decode do `client_data`). O state query parameter está CORRETO; o problema é a COMPARAÇÃO contra um cookie que não existe.

## Candidate Root Causes (atualizadas após nova evidência do usuário)

A evidência decisiva do usuário (**Cookie header ausente em 3 requests independentes, incluindo same-origin fetch**) prova que **não há cookies no jar do Brave para localhost:5174**. Isso reduz para causas específicas do ambiente browser do usuário:

1. **Brave Shields bloqueando cookies em localhost cross-port.** Brave v147 com Shields "Standard" ou "Aggressive" trata localhost:5174 ↔ localhost:8180 como cross-site para fins de privacy (mesmo sendo same-site por spec), e bloqueia o storage do Set-Cookie feito durante uma 302 redirect que atravessa origens. **ALTAMENTE PROVÁVEL.**
2. **Brave Forgetful Browsing ativado para localhost.** Se o usuário tem "Forget me when I close this site" ou equivalente, cookies podem ser apagados entre tabs.
3. **Extensão de privacy** (uBlock, Privacy Badger, DuckDuckGo) removendo cookies em contextos de redirect cross-origin.
4. **Brave em modo Tor/Private** (Private Window with Tor) — nunca persiste cookies.

**Contexto decisivo:** (a) curl funciona. (b) Playwright Chromium funciona. (c) Brave NÃO funciona. (d) Cookies TOTALMENTE ausentes, não só pkce_state. (e) Mesmo o fetch same-origin /auth/me não tem cookies. Essas cinco evidências em conjunto descartam bug no código server-side e apontam para policy de browser do usuário.

## Resolution

- **root_cause**: **Client-side (browser user policy) — Brave v147 não está persistindo o cookie `pkce_state` setado via 302 Set-Cookie do /auth/login**. A evidência do usuário mostra header `Cookie` ausente em todas as requests (inclusive same-origin fetch para /auth/me), confirmando que o jar do Brave para localhost:5174 está vazio no momento do erro. O código server-side funciona corretamente (comprovado via curl e Playwright Chromium headless). O bug é específico do ambiente browser do usuário — Brave Shields, Forgetful Browsing, ou extensão de privacidade impedindo storage do cookie httpOnly em redirect response.
- **fix**: implementar **dual-storage** (sessionStorage + cookie) para o `state` no flow ACF + diagnostic logging server-side para capturar evidence concreta em futuras reproduções + mensagem de erro mais informativa na /auth/error com guidance para o usuário.
- **verification**: (a) testes automatizados existentes continuam passando (curl + Playwright Chromium); (b) usuário reproduz fluxo no Brave e, se ainda falhar, server logs mostram precisamente qual cookie/state foi recebido — permitindo fix final baseado em evidência concreta.
- **files_changed**:

## Recommended Mitigations (defensive fixes regardless of exact root cause)

Mesmo sem reproduzir o bug, o código pode ser fortalecido para mitigar os cenários candidatos:

1. **Invalidar cookie antigo ao gerar novo authorize:** antes do `setCookie("pkce_state", ...)` em `/auth/login`, explicitamente chamar `deleteCookie("pkce_state", { path: "/auth" })` seguido de `deleteCookie("pkce_state", { path: "/" })` — garante que qualquer cookie stale em jar diferente de path é limpo primeiro.
2. **Renderizar mensagem de erro com diagnostico na página de /auth/error:** incluir um componente que lê o erro e, se for "Invalid state", oferece botão "Limpar sessão e tentar novamente" que chama `/auth/login?clear=1` (que limpa todos cookies antes de redirecionar).
3. **Logar no server-side (via Vinxi handler) quando Invalid state ocorre** incluindo: `query.state`, `getCookie("pkce_state")`, e headers `Cookie` e `User-Agent`. Isso capturaria evidência real quando o usuário reproduzir.
4. **Usar sessionStorage + cookie dual-token anti-CSRF:** além do cookie, gravar o state em sessionStorage via inline script na página `/admin/login` antes do redirect, e validar ambos no callback via form postback.
