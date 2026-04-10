---
phase: 19
plan: 01
name: Frontend Client — Estrutura e Migração
completed_at: "2026-04-09"
strategy_used: copy-first-then-delete
---

# Phase 19 — Plan 01 SUMMARY: Frontend Client Migration

## Objective

Criar o projeto `frontend/client` como um projeto Vinxi independente e funcional, migrando todas as telas e componentes não-admin do monolith, validando que builda e roda na porta 5173.

## What Was Done

### Files Created/Modified

**Scaffolding (8 files):**
- `frontend/client/package.json` — name: "frontend-client", dev script: `vinxi dev --port 5173 --host`
- `frontend/client/tsconfig.json` — sem alterações, `@/*` → `./src/*` funciona localmente
- `frontend/client/components.json` — shadcn/ui config
- `frontend/client/.dockerignore` — Docker exclusions
- `frontend/client/Dockerfile` — `EXPOSE 5173`
- `frontend/client/index.html` — SPA entry point
- `frontend/client/app.config.ts` — 3 routers (public, api-proxy, client), port 5173, HMR config
- `frontend/client/server.ts` — h3 proxy `/api/*` → `http://api:8080`

**Components migrated (20+ files):**
- **atoms/**: ProfileBadge.tsx, ProfileField.tsx, ThemeToggle.tsx
- **molecules/**: LoginForm.tsx, PasswordField.tsx, PasswordStrengthMeter.tsx, PersonTypeRadio.tsx, ProfileCard.tsx, RegistrationForm.tsx, KeycloakStatusBadge.tsx
- **organisms/**: Header.tsx
- **pages/**: LoginPage.tsx, ProfilePage.tsx, ForgotPasswordPage.tsx, ResetPasswordPage.tsx, NotFoundPage.tsx
- **templates/**: AuthLayout.tsx
- **guards/**: AuthGuard.tsx
- **ui/**: 13 shadcn/ui components (alert, badge, button, card, dropdown-menu, form, input, label, radio-group, separator, skeleton, sonner, pagination)

**Libs migrated (7 files):**
- `auth-context.tsx`, `api.ts`, `types.ts`, `validation-schemas.ts`, `password-strength.ts`, `theme-provider.tsx`, `utils.ts`

**Tests migrated (20 files):**
- Todos os testes não-admin do monolith copiados para `frontend/client/src/tests/`

**Infrastructure:**
- `compose.yaml` — serviço `frontend-client` adicionado (porta 5173), monolith mantido temporariamente

### Key Decisions

1. **Copy-first-then-delete** — monolith mantido até Plan 02 validar backoffice
2. **Code duplication** — shadcn/ui components duplicados (13 arquivos) em vez de compartilhados
3. **Same dependency versions** — client usa mesmas versões do monolith
4. **Fixed port 5173** — preserva compatibilidade com config existente

### Verification Results

- [x] `frontend/client/` estrutura completa
- [x] package.json, app.config.ts, Dockerfile, tsconfig.json independentes
- [x] Todas as páginas não-admin migradas
- [x] Zero imports de módulos admin
- [x] 20 testes unitários no client
- [x] Compose service `frontend-client` configurado

## Concerns / Lessons

- 20 testes no client — volume considerável, mas cobrem todos os flows não-admin
- shadcn/ui duplicados (13 componentes) — aceitável pela regra de isolamento total
- Monolith ainda existe — será removido no Plan 02

## Next Steps

→ Phase 19 Plan 02: Criar `frontend/backoffice`, migrar componentes admin, deletar monolith
