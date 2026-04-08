# Proposta — Milestone v2.0 (Atualizada)

**Base:** Milestone v1.0 completo (10 fases, 30 planos, 135 testes)
**Novas Fases:** UX Redesign + UI Redesign (solicitadas pelo usuário)
**Objetivo:** Experiência profissional, visual polido, segurança OAuth 2.1

---

## 📋 Roadmap Proposto v2.0

### Phase 11: UX Redesign (6 planos)
**Documento:** `.planning/PHASE-11-UX-REDESIGN.md`
**Objetivo:** Jornada simples, menos clicks, UX intuitiva

| Plano | Requisito | Descrição |
|-------|-----------|-----------|
| 11-01 | UX-01 | Formulário único PF/PJ com radio button (substituir 2 telas) |
| 11-02 | UX-02 | Password Security Meter (5 níveis: muito fraca → muito forte) |
| 11-03 | UX-03 | Show/Hide password + Confirm password field |
| 11-04 | UX-04 | Login-first navigation (página inicial = login) |
| 11-05 | UX-05 | Forgot password flow (Resend.com — 3.000 emails/mês free) |
| 11-06 | UX-06 | Auto-login pós-cadastro (sem tela de login intermediária) |

**Entregáveis:**
- `RegistrationForm.tsx` — Formulário único substituindo 3 componentes
- `PasswordStrengthMeter.tsx` — Barra de força + checklist
- `PasswordField.tsx` — Input com toggle show/hide
- `ForgotPasswordPage.tsx` + `ResetPasswordPage.tsx`
- Backend: endpoints forgot/reset password
- Fluxo: `/` → login → register → auto-login → profile

**Estimativa:** 2-3 dias

---

### Phase 12: UI Redesign — shadcn/ui + Temas (3 planos)
**Documento:** `.planning/PHASE-12-UI-REDESIGN.md`
**Objetivo:** Visual profissional, dark/light mode

| Plano | Requisito | Descrição |
|-------|-----------|-----------|
| 12-01 | UI-01 a UI-02 | shadcn/ui setup + Theme infrastructure (dark/light toggle) |
| 12-02 | UI-03 a UI-05 | Redesign: LoginPage, RegistrationPage, ProfilePage |
| 12-03 | UI-06 a UI-07 | Header + User menu + Forgot/Reset password pages |

**Componentes shadcn:** button, input, label, card, form, radio-group, alert, toast, skeleton, badge, dropdown-menu, separator

**Entregáveis:**
- Todas as telas redesenhadas com shadcn/ui
- Theme toggle (sol/lua) com persistência localStorage
- Header fixo com logo, theme toggle, user menu
- Design system unificado (cores, tipografia, espaçamento)
- Remover componentes antigos (LabeledField, AppButton, PageLayout)

**Estimativa:** 2-3 dias

---

### Phase 13: Code Review Fixes (1 plano)
**Objetivo:** Resolver 6 warnings da Phase 10

| Plano | Warnings | Descrição |
|-------|----------|-----------|
| 13-01 | WR-01 a WR-06 | Todos os warnings do code review |

**Entregáveis:**
- WR-01: `response.ok` em vez de `status === 200`
- WR-02: Validação runtime do perfil
- WR-03: ProfileCard sem fallback silencioso
- WR-04: `login()` com catch explícito
- WR-05: Refresh token expiry check
- WR-06: Single useEffect no ProfilePage

**Estimativa:** 1 dia (quick win)

---

### Phase 14: E2E Testing (2 planos)
**Objetivo:** Testes em browser real (não jsdom)

| Plano | Descrição |
|-------|-----------|
| 14-01 | Playwright setup + core flow tests (register, login, profile) |
| 14-02 | Integration tests com Keycloak real + perf tests (k6) |

**Estimativa:** 1-2 dias

---

### Phase 15: Production HTTPS (2 planos)
**Objetivo:** HTTPS para todos os serviços

| Plano | Descrição |
|-------|-----------|
| 15-01 | Certs (mkcert) + Keycloak HTTPS + API HTTPS |
| 15-02 | Frontend HTTPS + compose.prod.yaml + reverse proxy |

**Estimativa:** 1-2 dias

---

### Phase 16: Secrets & Backup (2 planos)
**Objetivo:** Produção segura

| Plano | Descrição |
|-------|-----------|
| 16-01 | Docker secrets + Keycloak vault |
| 16-02 | Backup scripts + restore procedure |

**Estimativa:** 1-2 dias

---

### Phase 17: Monitoring & CI/CD (2 planos)
**Objetivo:** Operational excellence

| Plano | Descrição |
|-------|-----------|
| 17-01 | Grafana dashboards + alert rules |
| 17-02 | GitHub Actions pipeline + security scan |

**Estimativa:** 1-2 dias

---

## 🚦 Priorização

| Prioridade | Fase | Planos | Estimativa | Motivo |
|-----------|------|--------|------------|--------|
| **P0** | **Phase 11: UX Redesign** | 6 | 2-3 dias | **Solicitado pelo usuário — dor imediata** |
| **P0** | **Phase 12: UI Redesign** | 3 | 2-3 dias | **Solicitado pelo usuário — dor imediata** |
| P1 | Phase 13: Code Review Fixes | 1 | 1 dia | Quick wins — 6 warnings |
| P1 | Phase 14: E2E Testing | 2 | 1-2 dias | Confidence boost |
| P2 | Phase 15: Production HTTPS | 2 | 1-2 dias | Required for prod |
| P2 | Phase 16: Secrets & Backup | 2 | 1-2 dias | Required for prod |
| P3 | Phase 17: Monitoring & CI/CD | 2 | 1-2 dias | Operational excellence |
| P3 | PKCE Migration (futuro) | 3 | 2-3 dias | OAuth 2.1 compliance |

---

## 📊 Comparativo v1.0 vs v2.0 Proposto

| Métrica | v1.0 | v2.0 (proposto) | Delta |
|---------|------|-----------------|-------|
| Fases | 10 | 7 novas | +7 |
| Planos | 30 | ~18 novos | +18 |
| Clicks para cadastro | 4+ | 2 | -50% |
| Telas de cadastro | 2 (tipo → form) | 1 (única) | -50% |
| Password feedback | Nenhum | 5 níveis + checklist | +∞ |
| Tema | Claro apenas | Dark + Light | +100% |
| Visual | Básico/feio | shadcn/ui profissional | Subjetivo ✅ |
| Esqueci senha | Não existe | Resend.com email | Novo |
| Auto-login pós-cadastro | Não | Sim | Novo |
| Testes E2E | 0 (jsdom) | ~20 (Playwright) | +20 |
| HTTPS | ❌ | ✅ | Production ready |
| CI/CD | Manual | GitHub Actions | Automation |

---

## 🔓 Quick Wins (Pode fazer agora)

1. **Phase 13: Code Review Fixes** — 1 dia, 6 fixes, zero risco
2. **Phase 11 UX-02/03** — Password meter + show/hide (isolado, não quebra nada)
3. **OpenAPI/Swagger** — 2 horas, documentação automática
4. **Seed script** — 2 horas, usuários demo para testes manuais

---

## 🎯 Recomendação de Execução

**Semana 1: UX + UI (Prioridade P0)**
```
Dia 1-2: Phase 11 (UX Redesign)
  - Formulário único PF/PJ
  - Password strength meter
  - Show/hide + confirm password
  - Login-first navigation
  - Auto-login pós-cadastro

Dia 3-4: Phase 12 (UI Redesign)
  - shadcn/ui setup + theme infrastructure
  - Redesign LoginPage + RegistrationPage
  - Redesign ProfilePage + Header
  - Dark/Light mode

Dia 5: Phase 13 (Code Review Fixes)
  - WR-01 a WR-06
  - Testes atualizados
```

**Semana 2: Testing + Production**
```
Dia 6-7: Phase 14 (E2E Testing)
  - Playwright setup
  - Core flow tests
  - Integration tests com Keycloak real

Dia 8-9: Phase 15 (HTTPS)
  - Certs + Keycloak HTTPS + API HTTPS
  - compose.prod.yaml

Dia 10: Phase 16/17 (Secrets + Monitoring)
  - Docker secrets
  - Grafana dashboards
```

---

## ❓ Decisões Pendentes

### 1. Forgot Password: Resend.com ou Security Questions?
- **Resend.com (Recomendado):** 3.000 emails/mês free, fluxo padrão indústria, mais seguro
- **Security Questions:** Sem dependência externa, menos seguro
- **Decisão necessária:** Qual abordagem o usuário prefere?

### 2. Manter UI custom de login ou migrar para Keycloak page (PKCE)?
- **UI custom (atual):** Branding total, UX controlada (mas requer ROPC)
- **Keycloak page:** OAuth 2.1 compliant, menos código (mas perde controle da UI)
- **Recomendação:** Manter UI custom para v2, migrar para PKCE em v3

### 3. shadcn/ui: Tailwind v4 compatível?
- shadcn suporta Tailwind v4, mas requer verificação no `init`
- **Mitigação:** Testar `npx shadcn@latest init` antes de começar

---

## 📁 Documentos de Escopo

| Arquivo | Conteúdo |
|---------|----------|
| `.planning/PHASE-11-UX-REDESIGN.md` | Escopo detalhado UX (6 requisitos, mockups, testes) |
| `.planning/PHASE-12-UI-REDESIGN.md` | Escopo detalhado UI (7 requisitos, design system, componentes) |
| `.planning/V2-PROPOSAL.md` | Proposta consolidada v2.0 (priorização, estimativas) |
| `.planning/RETROSPECTIVE.md` | Retrospectiva v1.0 (métricas, lições, destaques) |

---

*Documento atualizado em 2026-04-08 com fases UX/UI solicitadas pelo usuário*
*Aguardando aprovação para início da Phase 11 (UX Redesign)*
