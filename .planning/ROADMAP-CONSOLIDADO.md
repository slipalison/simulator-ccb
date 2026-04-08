# Roadmap Consolidado — Onboarding de Clientes

**Data:** 2026-04-08
**Status:** v1.0 ✅ Completo | v2.0 📋 Planejado

---

## 📊 Resumo Executivo

| Marco | Status | Fases | Planos | Testes | Período |
|-------|--------|-------|--------|--------|---------|
| **v1.0 — Foundation** | ✅ Completo | 10 | 30 | 135 passing | 2026-04-01 → 2026-04-08 |
| **v2.0 — UX/UI + Production** | 📋 Planejado | 7 novas | ~18 | +20 E2E | TBD |

---

## 🏆 MILESTONE v1.0 — COMPLETO

### O que foi entregue

Sistema de onboarding funcional com cadastro PF/PJ, autenticação Keycloak, perfil read-only, observabilidade completa e stack Dockerizada.

### Stack Técnica

| Camada | Tecnologia |
|--------|-----------|
| **Backend** | .NET 10, ASP.NET Core Controllers, EF Core, PostgreSQL |
| **Frontend** | React 19, Vinxi 0.5.x, TypeScript, Tailwind CSS |
| **Auth** | Keycloak 26.1 (hardened), JWT, ROPC Grant |
| **Infra** | Docker Compose (5 serviços), dual PostgreSQL |
| **Observabilidade** | Serilog, OpenTelemetry, Grafana LGTM (Alloy, Loki, Tempo, Mimir) |
| **Testes** | xUnit + Shouldly (backend), Vitest + React Testing Library (frontend) |

### Phases Entregues

| # | Phase | Plans | Testes | Duração | Destaque |
|---|-------|-------|--------|---------|----------|
| 01 | **Infrastructure** | 3/3 | Docker config | 1 dia | 5 serviços, dual PostgreSQL, Keycloak 26.x com healthcheck /dev/tcp |
| 02 | **Keycloak Security Hardening** | 1/1 | 6 checks | 1 dia | Brute force, password policy, clientPolicies, SEC-01 a SEC-07 |
| 03 | **Backend Domain Layer** | 2/2 | 38 | 1 dia | DDD, TDD, value objects (CPF/CNPJ mod-11), CQRS manual sem MediatR |
| 04 | **Observability** | 4/4 | ~14 | 1 dia | Serilog JSON + OTel SDK + Grafana LGTM stack completo |
| 05 | **Registration API** | 4/4 | 20 GREEN | 2 dias | PF/PJ registration, duplicate check, Keycloak Admin API, idempotency filter |
| 06 | **Authentication API** | 3/3 | 42 | 1 dia | JWT via ROPC, token refresh, protected routes, SEC-08 enforced |
| 07 | **Frontend Foundation** | 4/4 | ~10 | 1 dia | Vinxi SPA, Atomic Design, TanStack Router, RHF + Zod |
| 08 | **Registration UI** | 3/3 | ~10 | 1 dia | Forms PF/PJ, Zod validation, API integration, redirect /login |
| 09 | **Login UI** | 3/3 | 24 | 1 dia | ROPC token exchange, AuthContext memory-only (SEC-10), auth guard |
| 10 | **Profile UI** | 3/3 | 48 | 1 dia | GET /api/clients/me, PF/PJ visual distinction, E2E flow tests |

### Métricas v1.0

| Métrica | Valor |
|---------|-------|
| **Dias de desenvolvimento** | 7 |
| **Commits atômicos** | 40+ |
| **Arquivos criados** | ~120+ |
| **Testes passando** | 135 (43 domain + 44 API + 48 frontend) |
| **Testes skipped** | 2 (trace propagation — verificação manual Grafana) |
| **Falhas ativas** | 0 |
| **Auto-fixes de devisões** | ~25 (Rules 1-3) |
| **Regressões** | 0 |

### Requisitos Satisfeitos

- ✅ Cadastro PF com validação CPF (mod-11, all-same-digit rejection)
- ✅ Cadastro PJ com validação CNPJ (mod-11 ASCII-48, alphanumeric-ready)
- ✅ Criação de usuário no Keycloak via Admin API (IKeycloakUserClient)
- ✅ Compensação/rollback se Keycloak falhar após app_db persist (REG-06)
- ✅ Detecção de duplicidade (CPF, CNPJ, Email) — retorna 409
- ✅ Filtro de idempotência (REG-08) — IDistributedCache, TTL 60min, apenas 2xx
- ✅ Login custom ROPC com tokens em memória (SEC-10, zero localStorage)
- ✅ JWT com refresh automático (ValidateAudience=false, MapInboundClaims=false)
- ✅ Profile read-only com distinção visual PF/PJ (badge verde/azul)
- ✅ Auth guard em rotas protegidas ([Authorize] → 401 sem token)
- ✅ Keycloak hardened contra 7 superfícies de ataque (SEC-01 a SEC-07)
- ✅ Observabilidade completa: Serilog + OTel + Grafana LGTM
- ✅ Health checks split: /healthz/live (liveness) + /healthz/ready (readiness)
- ✅ Sensitive data masking em logs (SEC-09: password, token, CPF, CNPJ, email)
- ✅ Password policy: min 8 chars, uppercase, lowercase, digit, special char
- ✅ Brute force protection: 5 falhas → lock 30s com escalating wait

### Lições Aprendidas (Top 10)

1. **Vinxi é imaturo** — API mudou sem documentação (`defineConfig` → `createApp`), port config ignorado, SSR sem index.html crash
2. **Windows Docker HMR** — `usePolling: true` essencial para watcher funcionar
3. **.NET 10 .slnx vs .sln** — Dockerfile precisava do formato clássico
4. **NSubstitute 5.x sem `ThrowsAsync`** — usar `.Returns(Task.FromException<T>())`
5. **xUnit 2.9.x sem `Assert.Fail`** — pattern `true.ShouldBeFalse("msg")`
6. **TanStack Router frágil em jsdom** — navegação assíncrona, `routeTree.gen` não existe
7. **JwtBearer `MapInboundClaims=false`** — sem isso claim "email" vira URI XML
8. **`ValidateAudience=false` para ROPC** — tokens Keycloak têm `aud: ["account"]`
9. **Python3 vs Python no Windows** — auto-detection com fallback necessário
10. **Keycloak 26.x sem curl** — healthcheck via `/dev/tcp` bash socket

### Decisões Arquiteturais

| Decisão | Rationale | Status v2 |
|---------|-----------|-----------|
| **ROPC Grant** | Controle total da UI de login | ⚠️ Revisitar — deprecated OAuth 2.1 |
| **Sem MediatR** | MediatR é comercial agora | ✅ Manter — CQRS manual via DI funciona |
| **app_db PRIMEIRO, Keycloak DEPOIS** | Permite rollback/compensation | ✅ Manter — pattern sólido |
| **Manual DI over Mediator** | Simplicidade, zero dependências extras | ✅ Manter |
| **Testcontainers para integração** | Isolamento, reproducibilidade | ✅ Manter + melhorar (importar realm) |

### Warnings em Aberto (Code Review Phase 10)

| ID | Severidade | Descrição | Impacto |
|----|-----------|-----------|---------|
| WR-01 | Warning | `status === 200` estrito (deveria usar `response.ok`) | Baixo |
| WR-02 | Warning | `getProfileClient` sem validação runtime do response | Médio |
| WR-03 | Warning | `ProfileCard` fallback silencioso `razaoSocial` | Baixo |
| WR-04 | Warning | `login()` sem `catch` explícito | Baixo |
| WR-05 | Warning | `refreshIfNeeded` não verifica refresh token expiry | Médio |
| WR-06 | Warning | Dois `useEffect` no ProfilePage com possível race condition | Baixo |

### Verificação Humana Pendente

- [ ] Visualizar perfil PF no browser com backend ativo
- [ ] Visualizar perfil PJ no browser com backend ativo
- [ ] Testar auth guard acessando `/profile` sem token

---

## 🚀 MILESTONE v2.0 — PLANEJADO

### Visão Geral

Transformar o sistema de "funcional mas cru" em "profissional, seguro e pronto para produção". Duas frentes principais:

1. **Experiência do Usuário** — UX intuitiva + visual profissional (solicitado pelo usuário)
2. **Maturidade Técnica** — Security, testing, CI/CD, production readiness

### Priorização

| Prioridade | Fase | Planos | Estimativa | Motivo |
|-----------|------|--------|------------|--------|
| **P0** | **Phase 11: UX Redesign** | 6 | 2-3 dias | **Solicitado pelo usuário — dor imediata** |
| **P0** | **Phase 12: UI Redesign** | 3 | 2-3 dias | **Solicitado pelo usuário — dor imediata** |
| **P1** | **Phase 13: Code Review Fixes** | 1 | 1 dia | Quick wins — 6 warnings sem risco |
| **P1** | **Phase 14: PKCE Migration** | 3 | 2-3 dias | **Decidido — OAuth 2.1 compliance** |
| **P1** | **Phase 15: E2E Testing** | 2 | 1-2 dias | Confidence boost — browser real |
| **P2** | **Phase 16: Production HTTPS** | 2 | 1-2 dias | Required for prod deploy |
| **P2** | **Phase 17: Secrets & Backup** | 2 | 1-2 dias | Data protection |
| **P3** | **Phase 18: Monitoring & CI/CD** | 2 | 1-2 dias | Operational excellence |

---

### Phase 11: UX Redesign 🔴 P0

**Documento:** `.planning/PHASE-11-UX-REDESIGN.md`
**Motivação:** Jornada fragmentada, muitos clicks, UX não intuitiva

| Plano | Requisito | Problema | Solução |
|-------|-----------|----------|---------|
| **11-01** | UX-01 | 2 telas para cadastro (escolher tipo → form) | **Formulário único** com radio button PF/PJ, campos adaptativos dinâmicos |
| **11-02** | UX-02 | Sem feedback visual de força da senha | **Password Strength Meter** — 5 níveis (muito fraca → muito forte) + checklist de critérios |
| **11-03** | UX-03 | Senha sem show/hide, sem confirmação | **PasswordField** com toggle 👁 + **Confirm Password** com validação em tempo real |
| **11-04** | UX-04 | Home genérica, login escondido | **Login-first** — `/` = LoginPage, usuário logado → redirect `/profile` |
| **11-05** | UX-05 | Sem recuperação de senha | **Forgot Password** via Resend.com (3.000 emails/mês free) — link com token expirável |
| **11-06** | UX-06 | Pós-cadastro obriga logar manualmente | **Auto-login** — após 201, login automático com credenciais informadas → `/profile` |

**Entregáveis:**

| Arquivo | Descrição |
|---------|-----------|
| `RegistrationForm.tsx` | Formulário único substituindo RegistrationTypeSelector + PfRegistrationForm + PjRegistrationForm |
| `PersonTypeRadio.tsx` | Radio button group estilizado PF/PJ |
| `PasswordStrengthMeter.tsx` | Barra de progresso + checklist ✓/✗ |
| `PasswordField.tsx` | Input com toggle show/hide (reutilizável) |
| `ForgotPasswordPage.tsx` | Formulário: "Informe seu email" |
| `ResetPasswordPage.tsx` | Formulário: "Nova senha" + "Confirmar" (acessado via link do email) |
| `POST /api/auth/forgot-password` | Gera token + envia email via Resend.com |
| `POST /api/auth/reset-password` | Valida token + atualiza senha via Keycloak Admin API |

**Fluxo de Navegação:**

```
/ (root)
  ├─ Não logado → LoginPage
  │   ├─ "Criar conta" → /register
  │   ├─ "Esqueci minha senha" → /forgot-password
  │   └─ Login sucesso → /profile
  ├─ Logado → /profile (auto-redirect)
  
/register
  ├─ Radio: PF ◉ / PJ ○
  ├─ Campos mudam dinamicamente
  ├─ Password: [●●●●] 👁 | ████████░░ Forte
  ├─ Confirm:  [●●●●] 👁 | ✓ Senhas coincidem
  └─ Submit → auto-login → /profile

/forgot-password
  └─ Email → "Link enviado" → email com token → /reset-password?token=xxx
  
/reset-password?token=xxx
  └─ Nova senha + Confirmar → "Senha alterada" → /login
```

**Métricas de Sucesso:**

| Métrica | Antes | Depois |
|---------|-------|--------|
| Clicks para cadastro | 4+ | 2 |
| Telas de cadastro | 2 | 1 |
| Feedback de senha | Nenhum | 5 níveis + 5 critérios |
| Tempo login pós-cadastro | 30s+ | 0s (auto) |
| Recuperação de senha | Não existe | Email < 1min |

**Riscos:**

| Risco | Mitigação |
|-------|-----------|
| Radio button confuso | Label claro: "Pessoa Física (CPF)" / "Pessoa Jurídica (CNPJ)" |
| Resend.com free tier insuficiente | 3.000 emails/mês = ~100 cadastros/dia — suficiente para v2 |
| Auto-login falhar silenciosamente | Fallback explícito para `/login` com mensagem de erro |

---

### Phase 12: UI Redesign — shadcn/ui + Temas 🔴 P0

**Documento:** `.planning/PHASE-12-UI-REDESIGN.md`
**Motivação:** Telas extremamente feias, sem tema dark/light

| Plano | Requisito | Descrição |
|-------|-----------|-----------|
| **12-01** | UI-01 + UI-02 | shadcn/ui setup + Theme infrastructure (dark/light toggle com persistência) |
| **12-02** | UI-03 a UI-05 | Redesign completo: LoginPage, RegistrationPage, ProfilePage |
| **12-03** | UI-06 + UI-07 | Header fixo + User menu + Forgot/Reset password pages |

**Componentes shadcn/ui:**

| Componente | Uso | Obrigatório |
|------------|-----|-------------|
| `button` | Login, Register, Logout, Submit | ✅ |
| `input` | Email, password, CPF, CNPJ | ✅ |
| `label` | Labels de formulários | ✅ |
| `card` | Containers de páginas | ✅ |
| `form` | Integration RHF + Zod | ✅ |
| `radio-group` | Seleção PF/PJ | ✅ |
| `alert` | Erros de login/registro | ✅ |
| `toast` | Notificações sucesso/erro | ✅ |
| `skeleton` | Loading states | ✅ |
| `badge` | Indicador PF/PJ no profile | ✅ |
| `dropdown-menu` | User menu no header | ✅ |
| `separator` | Divisores visuais | ✅ |

**Design System:**

| Elemento | Light | Dark |
|----------|-------|------|
| **Background** | `#ffffff` | `#020817` |
| **Card** | `#ffffff` | `#0f172a` |
| **Primary** | `#0f172a` | `#f8fafc` |
| **Border** | `#e2e8f0` | `#1e293b` |
| **Muted FG** | `#64748b` | `#94a3b8` |
| **Destructive** | `#ef4444` | `#f87171` |

**Theme Toggle:**
```
┌──────────────────────────────────────┐
│ Onboarding                 [🌙] [👤]│
│                          Dark       │
└──────────────────────────────────────┘
```

- Persistência: localStorage (next-themes)
- Detecção: `prefers-color-scheme` do sistema na primeira visita
- Transição: `transition-colors` no body (sem flash)

**Entregáveis:**

| Arquivo | Descrição |
|---------|-----------|
| `frontend/components.json` | shadcn/ui config |
| `frontend/src/lib/theme-provider.tsx` | Theme context com next-themes |
| `frontend/src/components/atoms/ThemeToggle.tsx` | Botão sol/lua |
| `frontend/src/components/organisms/Header.tsx` | Logo + theme toggle + user menu |
| `frontend/src/styles/globals.css` | Tailwind + shadcn CSS variables light/dark |
| Todas as pages | Redesign completo com shadcn |

**Arquivos a Remover:**

| Arquivo | Substituído por |
|---------|-----------------|
| `LabeledField.tsx` | shadcn `Form` + `Input` |
| `AppButton.tsx` | shadcn `Button` |
| `PageLayout.tsx` | shadcn `Card` |
| `ExampleForm.tsx` | Scaffold desnecessário |

**Riscos:**

| Risco | Mitigação |
|-------|-----------|
| shadcn incompatível com Vinxi | Testar `npx shadcn@latest init` antes |
| Tailwind v4 conflita | shadcn suporta v4 — verificar no init |
| next-themes não funciona com Vinxi | next-themes é React-only; fallback: provider custom |
| Flash na troca de tema | `suppressHydrationWarning` + CSS `transition-colors` |

---

### Phase 13: Code Review Fixes 🟡 P1

**Motivação:** 6 warnings do code review da Phase 10 — quick wins sem risco

| Plano | Warnings | Descrição | Esforço |
|-------|----------|-----------|---------|
| **13-01** | WR-01 a WR-06 | Todos os warnings | 1 dia |

**Detalhamento:**

| Warning | Arquivo | Problema | Fix |
|---------|---------|----------|-----|
| WR-01 | `api.ts:61-73, 79-93` | `status === 200` estrito | Usar `response.ok` (cobre todos 2xx) |
| WR-02 | `api.ts:244` | Cast sem validação runtime | Verificar `typeof data.type === "string"` |
| WR-03 | `ProfileCard.tsx:42` | Fallback silencioso `razaoSocial` | Renderizar `"—"` explícito |
| WR-04 | `auth-context.tsx:49-65` | `login()` sem catch explícito | Adicionar catch + re-throw |
| WR-05 | `auth-context.tsx:79-98` | Sem check de refresh token expiry | Verificar `refreshExpiresAt <= Date.now()` |
| WR-06 | `ProfilePage.tsx:23-52` | Dois useEffect com race condition | Combinar em um useEffect |

---

### Phase 14: PKCE Migration 🔴 P1

**Motivação:** Decidido migrar para OAuth 2.1 — mais seguro, MFA pronto

| Plano | Descrição | Entregáveis |
|-------|-----------|-------------|
| **14-01** | Keycloak config + Backend callback | PKCE no Keycloak, `POST /api/auth/callback`, code verifier/challenge |
| **14-02** | Frontend redirect flow | Redirect para Keycloak login, handle callback, PKCE generation |
| **14-03** | Tests + ROPC removal | Testes PKCE, remover ROPC endpoints, migration guide |

**Escopo Detalhado:**

| Componente | Mudança |
|------------|---------|
| **Keycloak** | `onboarding-app`: `standardFlowEnabled=true`, `directAccessGrantsEnabled=false` |
| **Keycloak** | PKCE Code Challenge Method: `S256` |
| **Keycloak** | Redirect URIs: `http://localhost:5173/auth/callback` |
| **Backend** | Novo endpoint: `POST /api/auth/callback` (exchange code → tokens) |
| **Backend** | PKCE code verifier generation (crypto.randomBytes → base64url) |
| **Backend** | Remover: `POST /api/auth/login` (ROPC) |
| **Frontend** | Redirect para `http://keycloak:8080/realms/onboarding/protocol/openid-connect/auth` |
| **Frontend** | Params: `client_id`, `redirect_uri`, `response_type=code`, `code_challenge`, `code_challenge_method=S256`, `state` |
| **Frontend** | Handle callback: extrair `code` + `state` → chamar backend `/api/auth/callback` |
| **Frontend** | Remover: LoginForm, loginClient (ROPC) |

**Fluxo PKCE:**

```
1. User clica "Entrar" na LoginPage
2. Frontend gera:
   - codeVerifier: crypto.randomBytes(32).toString('base64url')
   - codeChallenge: SHA256(codeVerifier).toString('base64url')
   - state: crypto.randomUUID()
3. Redirect para Keycloak:
   GET /realms/onboarding/protocol/openid-connect/auth?
     client_id=onboarding-app&
     redirect_uri=http://localhost:5173/auth/callback&
     response_type=code&
     code_challenge=XXXXX&
     code_challenge_method=S256&
     state=YYYYY
4. Usuário loga no Keycloak (Keycloak page)
5. Keycloak redirect para /auth/callback?code=ZZZZZ&state=YYYYY
6. Frontend verifica state → chama backend:
   POST /api/auth/callback { code, codeVerifier, redirectUri }
7. Backend exchange code → tokens (via Keycloak token endpoint)
8. Backend retorna tokens → frontend armazena em memória
9. Redirect para /profile
```

**Riscos:**

| Risco | Mitigação |
|-------|-----------|
| Perda de UI custom de login | Keycloak theme custom (freeMarker templates) |
| Complexidade de teste (redirects) | Playwright E2E tests cobrem fluxo real |
| state mismatch attack | Verificação obrigatória no backend + frontend |
| codeVerifier storage (antes do redirect) | SessionStorage (só para PKCE, não tokens) |

---

### Phase 15: E2E Testing 🟡 P1

**Motivação:** 48 testes em jsdom não capturam problemas reais de browser/network

| Plano | Descrição | Entregáveis |
|-------|-----------|-------------|
| **15-01** | Playwright setup + core flows | `playwright.config.ts`, 5-7 testes de fluxo |
| **15-02** | Integration tests real + perf tests | Keycloak import em Testcontainers, k6 load tests |

**Testes E2E Propostos:**

| # | Fluxo | Esperado |
|---|-------|----------|
| 1 | Register PF → auto-login → view profile → logout | Fluxo completo sem erros |
| 2 | Register PJ → auto-login → view profile → logout | Campos PJ corretos |
| 3 | Login PKCE → Keycloak redirect → callback → profile | Fluxo PKCE completo |
| 4 | Login com credenciais inválidas → erro Keycloak | Keycloak exibe erro genérico |
| 5 | Duplicate CPF → erro inline | "The provided document number is invalid." |
| 6 | Forgot password → email → reset → login | Token válido, senha atualizada |
| 7 | Acessar /profile sem token → redirect /login | Auth guard funciona |
| 8 | Theme toggle → refresh → tema persiste | localStorage funciona |
| 9 | Dark mode: contraste adequado | WCAG AA compliance |

**k6 Load Tests:**

| Teste | Carga | Métrica |
|-------|-------|---------|
| Registration endpoint | 100 req/s, 5 min | p95 latency < 500ms |
| Idempotency filter | 50 req/s com mesma key | 100% cache hit na 2ª chamada |
| PKCE callback endpoint | 100 req/s, 5 min | p95 latency < 300ms |

---

### Phase 16: Production HTTPS 🟢 P2

**Motivação:** v1 é dev-only — produção requer HTTPS

| Plano | Descrição | Entregáveis |
|-------|-----------|-------------|
| **16-01** | Certs + serviços HTTPS | mkcert staging, Keycloak HTTPS, API HTTPS |
| **16-02** | compose.prod.yaml + reverse proxy | Nginx/Caddy como reverse proxy |

**Escopo:**

| Serviço | Dev | Prod |
|---------|-----|------|
| **Keycloak** | `http://localhost:8180` | `https://auth.dominio.com` |
| **API** | `http://localhost:8080` | `https://api.dominio.com` |
| **Frontend** | `http://localhost:5173` | `https://app.dominio.com` |
| **Grafana** | `http://localhost:3000` | `https://grafana.dominio.com` (auth) |

---

### Phase 17: Secrets & Backup 🟢 P2

**Motivação:** Proteção de dados e recuperação de desastres

| Plano | Descrição | Entregáveis |
|-------|-----------|-------------|
| **17-01** | Docker secrets + Keycloak vault | Secrets management sem .env em prod |
| **17-02** | Backup scripts + restore | pg_dump agendado, realm export, restore procedure |

**Backup Schedule:**

| Dado | Frequência | Retenção | Método |
|------|-----------|----------|--------|
| app_db PostgreSQL | Diário 3am | 30 dias | pg_dump + gzip → S3 |
| Keycloak realm | Semanal | 90 dias | Admin API export → JSON |
| Volumes Docker | Semanal | 7 dias | Snapshot |

---

### Phase 18: Monitoring & CI/CD 🔵 P3

**Motivação:** Operational excellence

| Plano | Descrição | Entregáveis |
|-------|-----------|-------------|
| **18-01** | Grafana dashboards + alerts | Dashboards API/Keycloak/DB, alert rules |
| **18-02** | GitHub Actions pipeline | Build + test on PR, deploy staging, security scan |

**Dashboards Propostos:**

| Dashboard | Métricas |
|-----------|----------|
| **API Overview** | Request rate, error rate, p95 latency, throughput |
| **Registration Flow** | Registros/hora, duplicate rate, idempotency hit rate |
| **Authentication** | Login success/fail rate, token refresh rate, brute force attempts |
| **Keycloak** | Health status, active sessions, failed login attempts |
| **Database** | Connection pool, query latency, disk usage |
| **Infrastructure** | Container health, CPU, memory, disk I/O |

**CI/CD Pipeline:**

```yaml
PR:
  - dotnet build + test
  - npm install + build + test
  - Trivy security scan
  - Playwright E2E (se infra disponível)

Merge to main:
  - Build Docker images
  - Push to registry
  - Deploy staging
  - Run smoke tests

Deploy prod:
  - Manual approval
  - Deploy production
  - Health check verification
```

---

## 📊 Comparativo: v1.0 → v2.0

| Dimensão | v1.0 | v2.0 | Delta |
|----------|------|------|-------|
| **Fases** | 10 | 8 novas | +8 |
| **Planos** | 30 | ~21 novos | +21 |
| **Testes** | 135 | 155+ | +20 E2E |
| **Clicks para cadastro** | 4+ | 2 | **-50%** |
| **Telas de cadastro** | 2 | 1 | **-50%** |
| **Password feedback** | Nenhum | 5 níveis + checklist | **+∞** |
| **Temas** | 1 (claro) | 2 (claro + escuro) | **+100%** |
| **OAuth Flow** | ROPC (deprecated) | **PKCE (2.1)** | **Security upgrade** |
| **Esqueci senha** | ❌ | ✅ (Resend.com) | **Novo** |
| **Auto-login pós-cadastro** | ❌ | ✅ | **Novo** |
| **E2E browser tests** | 0 (jsdom) | ~20 (Playwright) | **+20** |
| **HTTPS** | ❌ | ✅ | **Prod ready** |
| **CI/CD** | Manual | GitHub Actions | **Automated** |
| **Backup** | ❌ | ✅ (diário) | **DR ready** |
| **Monitoring** | Grafana básico | 6 dashboards + alerts | **Operational** |

---

## 🎯 Cronograma Sugerido

### Semana 1: UX + UI (P0 — Prioridade Máxima)

| Dia | Fase | Entregáveis |
|-----|------|-------------|
| **Dia 1-2** | Phase 11 (UX) Parte 1 | Formulário único PF/PJ, PasswordStrengthMeter, PasswordField |
| **Dia 3** | Phase 11 (UX) Parte 2 | Login-first, auto-login, forgot password flow |
| **Dia 4-5** | Phase 12 (UI) Parte 1 | shadcn setup, theme infrastructure, LoginPage redesign |
| **Dia 6** | Phase 12 (UI) Parte 2 | RegistrationPage + ProfilePage redesign, Header |

### Semana 2: Fixes + PKCE (P1)

| Dia | Fase | Entregáveis |
|-----|------|-------------|
| **Dia 7** | Phase 13 (Code Fixes) | WR-01 a WR-06 — todos resolvidos |
| **Dia 8-9** | Phase 14 (PKCE) Parte 1 | Keycloak PKCE config, backend callback endpoint, code verifier/challenge |
| **Dia 10** | Phase 14 (PKCE) Parte 2 | Frontend redirect flow, PKCE generation, callback handler |
| **Dia 11** | Phase 14 (PKCE) Parte 3 | Tests, ROPC removal, migration guide |

### Semana 3: Testing + Production (P1-P2)

| Dia | Fase | Entregáveis |
|-----|------|-------------|
| **Dia 12-13** | Phase 15 (E2E) | Playwright setup + 9 core flow tests (incluindo PKCE) |
| **Dia 14-15** | Phase 16 (HTTPS) | Certs + Keycloak HTTPS + API HTTPS + reverse proxy |
| **Dia 16** | Phase 17 (Secrets/Backup) | Docker secrets + backup scripts |
| **Dia 17-18** | Phase 18 (Monitoring) | 6 Grafana dashboards + alert rules + GitHub Actions |

**Total Estimado:** ~18 dias úteis (~3.5 semanas)

---

## ❓ Decisões Pendentes

### Decisões Tomadas ✅

| # | Decisão | Escolha | Motivo |
|---|---------|---------|--------|
| 1 | Forgot Password | **Resend.com** | 3.000 emails/mês free, API moderna, SDK TypeScript |
| 2 | OAuth Flow | **PKCE (OAuth 2.1)** | Mais seguro, compliance, MFA pronto |
| 3 | Infraestrutura Prod | **Docker Compose** | Simples, mesmo stack dev/prod, ~$50/mês |

---

## 📁 Mapa de Documentos

| Arquivo | Conteúdo | Status |
|---------|----------|--------|
| `.planning/ROADMAP.md` | Roadmap v1.0 original | ✅ Histórico |
| `.planning/STATE.md` | Estado atual do projeto | ✅ Atualizado |
| `.planning/PROJECT.md` | Requisitos, decisões, constraints | ✅ Atualizado |
| `.planning/RETROSPECTIVE.md` | Retrospectiva v1.0 (7 dias) | ✅ Criado |
| `.planning/V2-PROPOSAL.md` | Proposta v2.0 original | ✅ Criado |
| `.planning/V2-ROADMAP.md` | Roadmap v2.0 atualizado | ✅ Criado |
| `.planning/PHASE-11-UX-REDESIGN.md` | Escopo detalhado UX | ✅ Criado |
| `.planning/PHASE-12-UI-REDESIGN.md` | Escopo detalhado UI | ✅ Criado |
| **Este arquivo** | Roadmap consolidado v1.0 + v2.0 | ✅ Criado |

---

## 🚦 Próximos Passos

**Todas as decisões foram tomadas!** Pronto para começar a implementação.

### Ordem de Execução Recomendada

```
Semana 1 (P0):
  Phase 11 → UX Redesign (formulário único, password meter, auto-login, forgot password)
  Phase 12 → UI Redesign (shadcn/ui, dark/light theme, todas as telas)

Semana 2 (P1):
  Phase 13 → Code Review Fixes (WR-01 a WR-06)
  Phase 14 → PKCE Migration (OAuth 2.1 compliance)

Semana 3 (P1-P2):
  Phase 15 → E2E Testing (Playwright, browser real)
  Phase 16 → Production HTTPS
  Phase 17 → Secrets & Backup
  Phase 18 → Monitoring & CI/CD
```

**Para começar agora:**

```bash
# Phase 11: UX Redesign (prioridade P0)
/gsd:plan-phase 11
```

---

*Documento consolidado em 2026-04-08*
*Fontes: .planning/RETROSPECTIVE.md, PHASE-11-UX-REDESIGN.md, PHASE-12-UI-REDESIGN.md, V2-ROADMAP.md*
