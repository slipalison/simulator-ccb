# Phase 31 Context: Backoffice Auth Code Flow UI

**Gathered:** 2026-04-16
**Status:** Ready for planning
**Source:** /gsd:plan-phase 31

<domain>
## Phase Boundary

Phase 31 é frontend-only (backoffice). Entrega a migração do backoffice de ROPC para Auth Code Flow + PKCE no lado do UI.

**O que já existe (Phase 29 foi revertida — ROPC atual):**
- `AdminLoginPage.tsx` — formulário ROPC com email+senha, chama `login(email, password)`
- `admin-auth-context.tsx` — context com `loginAdmin`, `logoutAdmin`, `getAdminMe`
- `admin-api.ts` — `loginAdmin()` (POST /api/admin/auth/login), `logoutAdmin()` (POST /api/admin/auth/logout), `AdminLoginError`
- `AdminLoginForm.tsx` — componente de formulário ROPC
- `AdminSessionMiddleware.cs` — cookie path `/`, nome `backoffice_refresh_token` (já atualizado na Phase 29 antes do revert)

**O que NÃO existe mais (revertido da Phase 29):**
- `auth-code-flow.ts` — deletado
- `auth-server.ts` — deletado
- `AuthLoginPage.tsx`, `AuthCallbackPage.tsx`, `AuthErrorPage.tsx` — deletados
- Client Keycloak `onboarding-backoffice` — precisa verificar se ainda existe no realm

**O que precisa ser criado/recriado:**
1. Vinxi server-side auth routes (`/auth/login`, `/auth/callback`, `/auth/logout`, `/auth/me`, `/auth/refresh`)
2. PKCE utilities (`auth-code-flow.ts`)
3. Frontend pages: AuthLoginPage (redirect-only), AuthCallbackPage, AuthErrorPage
4. Atualizar `admin-auth-context.tsx` — `login()` e `logout()` viram redirects síncronos
5. Atualizar `admin-api.ts` — remover `loginAdmin`, `logoutAdmin`, `AdminLoginError`
6. Atualizar `admin-http-interceptor.ts` — 401 → `/auth/login`
7. Router com ProtectedRoute guard
8. Keycloak client `onboarding-backoffice` (se não existir mais)

**Decisão importante da Phase 29 (antes do revert):**
- Vinxi h3 auth-server handles all PKCE/token logic server-side — client JS never sees tokens
- backoffice_refresh_token cookie path `/` para ser legível por Vinxi e .NET
- AdminSessionMiddleware cookie name: `backoffice_refresh_token`
- AdminLayout logout() delegates to context (window.location.href = /auth/logout)

</domain>

<decisions>
## Implementation Decisions

### Auth Code Flow Architecture (reutilizar decisões da Phase 29)

**Vinxi server-side (auth-server.ts):**
- `/auth/login` → gera PKCE code_verifier + code_challenge, redirect para Keycloak authorization URL
- `/auth/callback` → recebe `code` + `state`, troca por tokens via `POST /token` com client_secret + code_verifier, seta cookies httpOnly
- `/auth/logout` → limpa cookies, redirect para Keycloak OIDC logout endpoint
- `/auth/me` → decodifica access token do cookie, retorna admin info
- `/auth/refresh` → usa refresh token cookie para obter novo access token

**Cookies:**
- `backoffice_access_token` — httpOnly, Secure (false em dev), SameSite=Strict, path `/`
- `backoffice_refresh_token` — httpOnly, Secure (false em dev), SameSite=Strict, path `/`

**Frontend:**
- `login()` → `window.location.href = '/auth/login'` (síncrono, sem form)
- `logout()` → `window.location.href = '/auth/logout'` (síncrono)
- `AdminLoginPage` → redirect-only component (sem formulário)
- `AuthCallbackPage` → loading state enquanto processa code exchange
- `AuthErrorPage` → exibe erros de auth

### Keycloak Client

Verificar se `onboarding-backoffice` ainda existe em `keycloak/onboarding-realm.json`. Se não existir, recriar:
- clientId: `onboarding-backoffice`
- confidential client (client_secret)
- standardFlowEnabled: true
- directAccessGrantsEnabled: false
- redirectUris: `["http://localhost:5174/auth/callback"]`
- webOrigins: `["http://localhost:5174"]`

### ROPC Removal

- Remover `loginAdmin()`, `logoutAdmin()`, `AdminLoginError` de `admin-api.ts`
- Remover `AdminLoginForm` import de `AdminLoginPage.tsx`
- Atualizar todos os testes que referenciam funções removidas

</decisions>

<canonical_refs>
## Canonical References

**Arquivos atuais (ROPC — serão modificados):**
- `frontend/backoffice/src/lib/admin-auth-context.tsx` — context atual com login/logout ROPC
- `frontend/backoffice/src/lib/admin-api.ts` — loginAdmin, logoutAdmin, AdminLoginError
- `frontend/backoffice/src/components/pages/AdminLoginPage.tsx` — formulário ROPC
- `frontend/backoffice/src/components/molecules/AdminLoginForm.tsx` — componente de formulário
- `frontend/backoffice/src/router.tsx` — rotas atuais
- `frontend/backoffice/src/components/templates/AdminLayout.tsx` — logout handler
- `frontend/backoffice/app.config.ts` — Vinxi config
- `keycloak/onboarding-realm.json` — realm config

**Arquivos a criar:**
- `frontend/backoffice/auth-server.ts` — Vinxi h3 server-side auth routes
- `frontend/backoffice/src/lib/auth-code-flow.ts` — PKCE utilities
- `frontend/backoffice/src/components/pages/AuthLoginPage.tsx` — redirect-only
- `frontend/backoffice/src/components/pages/AuthCallbackPage.tsx` — code exchange
- `frontend/backoffice/src/components/pages/AuthErrorPage.tsx` — error display

**Referência:**
- `.planning/phases/29-keycloak-auth-code-flow/29-01-SUMMARY.md` — o que foi feito na Phase 29 (antes do revert)
- `.planning/ROADMAP.md` — Phase 31 success criteria

</canonical_refs>

<deferred>
## Deferred Ideas

- AdminLoginPage com formulário ROPC como fallback — manter como opção secundária (não nesta fase)
- Token introspection para lightweight tokens — warning já existe, não bloqueante
- Custom Keycloak themes para login page — Phase 33
</deferred>

---

*Phase: 31-backoffice-auth-code-flow-ui*
*Context gathered: 2026-04-16 via /gsd:plan-phase 31*
