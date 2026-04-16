# Phase 33 Context: PKCE + Custom Keycloak Themes

**Gathered:** 2026-04-16
**Status:** Ready for planning
**Source:** /gsd:plan-phase 33

<domain>
## Phase Boundary

Phase 33 é a maior mudança de arquitetura do milestone v5.0. Envolve:

1. **Client app migra de ROPC para ACF+PKCE** (como backoffice fez na Phase 31)
2. **Custom Keycloak Themes** para ambos apps (client + backoffice)
3. **Novo client Keycloak** para o client app (confidential, ACF+PKCE)

**O que já existe:**
- Backoffice: ACF+PKCE completo (Phase 31) — auth-server.ts, auth-code-flow.ts, /auth/* routes
- Client: ROPC completo — auth-context.tsx com loginClient(), api.ts com loginClient()
- Keycloak: client `onboarding-app` (public, directAccessGrantsEnabled=true, standardFlowEnabled=false)

**O que precisa ser criado:**
1. Novo client Keycloak `onboarding-client-acf` (confidential, standardFlowEnabled=true, PKCE S256)
2. Client app: Vinxi auth-server.ts (como backoffice), auth-code-flow.ts, /auth/* routes
3. Client app: AuthLoginPage (redirect-only), AuthCallbackPage, AuthErrorPage
4. Client app: auth-context.tsx reescrito com redirects síncronos
5. Client app: remover loginClient, logoutClient de api.ts
6. Custom Keycloak Theme `onboarding-client` (login, reset-password, update-password, otp)
7. Custom Keycloak Theme `onboarding-backoffice` (login, reset-password, update-password, otp)
8. realm.json atualizado com themes e novo client

**Decisão importante:** O client `onboarding-app` existente (ROPC) será mantido ou substituído?
- Se substituído: mudar clientId, redirect URIs, etc.
- Se mantido: criar novo client `onboarding-client-acf`

**Recomendação:** Criar novo client `onboarding-client-acf` e manter `onboarding-app` como fallback durante transição. Após verificação, remover `onboarding-app`.

</domain>

<decisions>
## Implementation Decisions

### Client ACF Migration (mesmo padrão do backoffice Phase 31)
- Vinxi server-side auth routes em `frontend/client/auth-server.ts`
- PKCE utilities em `frontend/client/src/lib/auth-code-flow.ts`
- app.config.ts com router `/auth`
- AuthLoginPage redirect-only → /auth/login
- auth-context.tsx: login()/logout() como redirects síncronos
- api.ts: remover loginClient, logoutClient

### Custom Keycloak Themes
- Herdar do theme `base` do Keycloak
- Templates FreeMarker (.ftl) para login, reset-password, update-password, otp
- CSS customizado para cada app
- Theme properties configurando herança
- Deploy via volume mount no compose.yaml

### Keycloak Client Config
- `onboarding-client-acf`: confidential, standardFlowEnabled=true, PKCE S256
- redirectUris: `["http://localhost:5173/auth/callback"]`
- webOrigins: `["http://localhost:5173"]`

</decisions>

<canonical_refs>
## Canonical References

**Client app (atual ROPC — será migrado):**
- `frontend/client/src/lib/auth-context.tsx` — auth context ROPC atual
- `frontend/client/src/lib/api.ts` — loginClient, logoutClient
- `frontend/client/src/components/pages/LoginPage.tsx` — formulário ROPC
- `frontend/client/app.config.ts` — Vinxi config atual

**Backoffice (referência para ACF):**
- `frontend/backoffice/auth-server.ts` — padrão de auth-server
- `frontend/backoffice/src/lib/auth-code-flow.ts` — PKCE utilities
- `frontend/backoffice/src/lib/admin-auth-context.tsx` — auth context ACF

**Keycloak:**
- `keycloak/onboarding-realm.json` — realm config atual
- `keycloak/themes/` — diretório para themes (criar)

</canonical_refs>

<deferred>
## Deferred Ideas

- 2FA TOTP setup UI — Keycloak handles natively, no app code needed
- WebAuthn support — future phase
- Social login (Google, GitHub) — out of scope
- Mobile PWA — out of scope
</deferred>

---

*Phase: 33-pkce-custom-keycloak-themes*
*Context gathered: 2026-04-16 via /gsd:plan-phase 33*
