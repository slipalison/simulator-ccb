---
phase_slug: backend-csharp-quality-audit
phase_position: 54
iter: 1
total_resets: 0
status: running
max_iter_per_round: 5
max_resets: 3
created_at: 2026-05-30T12:16:59-03:00
last_iter_completed: 2026-05-30
last_task: T-1 (coverage baseline + violation inventory)
---

## Hard constraint (user, /jdi-loop invocation)

**As alterações NÃO podem quebrar a integração com o Front (client SPA 5173 + backoffice SPA 5174) nem com o Keycloak (ACF+PKCE, realm onboarding).**
Reforça D-54: zero mudança de contrato HTTP da API, rotas, shape de payload, cookies de auth, CORS, ou fluxo OIDC. Refactor é behavior-preserving ou não acontece. Regressão Playwright nos endpoints + smoke de auth são gate obrigatório.

## History

