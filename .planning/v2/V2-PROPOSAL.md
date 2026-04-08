# Proposta — Milestone v2.0

**Base:** Milestone v1.0 completo (10 fases, 30 planos, 135 testes)
**Objetivo:** Endereçar carried concerns, code review warnings, e elevar maturidade do sistema

---

## 🎯 Temas Principais

### 0. UX Redesign (NOVA — Prioridade Máxima)
**Documento:** `.planning/PHASE-11-UX-REDESIGN.md`

**Problema:** Jornada fragmentada, muitos clicks, UX não intuitiva

**Solução:**
- Formulário único de cadastro com radio button PF/PJ (não 2 telas)
- Password security meter (fraca → muito forte)
- Show/hide password + confirmar senha
- Login-first (página inicial é login, não home genérica)
- Auto-login pós-cadastro (sem tela de login intermediária)
- Forgot password flow via Resend.com (3.000 emails/mês free)
- Página inicial redireciona para profile se logado

**Estimativa:** 6 requisitos (UX-01 a UX-06), ~6 planos

---

### 1. UI Redesign com shadcn/ui (NOVA — Prioridade Máxima)
**Documento:** `.planning/PHASE-12-UI-REDESIGN.md`

**Problema:** Telas extremamente feias, sem tema dark/light

**Solução:**
- Adotar shadcn/ui (componentes profissionais, acessíveis)
- Implementar tema Dark/Light com toggle (persistência localStorage)
- Redesign completo: Login, Registration, Profile, Forgot/Reset
- Design system unificado com tokens de cor, tipografia, espaçamento
- Header com logo, theme toggle, user menu

**Componentes shadcn:** button, input, label, card, form, radio-group, alert, toast, skeleton, badge, dropdown-menu

**Estimativa:** 7 requisitos (UI-01 a UI-07), ~3 planos

---

### 2. Code Review Fixes (Phase 13)
**Motivação:** 6 warnings identificados no review da Phase 10

**Escopo:**
- [ ] WR-01: `loginClient`/`refreshTokenClient` usam `response.ok` (não `status === 200`)
- [ ] WR-02: `getProfileClient` valida runtime shape do response
- [ ] WR-03: `ProfileCard` sem fallback silencioso para `razaoSocial`
- [ ] WR-04: `login()` com `catch` explícito no AuthContext
- [ ] WR-05: `refreshIfNeeded` verifica refresh token expiry
- [ ] WR-06: `ProfilePage` single useEffect (combinar auth guard + fetch)

**Estimativa:** 1 fase pequena

---

### 3. Migração OAuth 2.1: ROPC → Authorization Code + PKCE
- [ ] Remover ROPC do `onboarding-app` client (standardFlowEnabled=true, directAccessGrantsEnabled=false)
- [ ] Atualizar redirect URIs para callback do frontend
- [ ] Testes: reescrever auth tests para PKCE flow
- [ ] Documentar: migration guide de ROPC → PKCE

**Riscos:**
- Perda de controle total da UI de login (Keycloak page)
- Customização do tema Keycloak necessária para manter branding
- Complexidade de teste aumenta (browser redirects)

**Estimativa:** 2-3 fases

---

### 2. Code Review Fixes (Phase 10 Warnings)
**Motivação:** 6 warnings identificados no review da Phase 10

**WR-01: HTTP status check flexível**
```typescript
// De: response.status === 200
// Para: response.ok (cobre todos 2xx)
```
- [ ] `loginClient` usa `response.ok`
- [ ] `refreshTokenClient` usa `response.ok`
- [ ] Testes atualizados

**WR-02: Validação runtime do perfil**
```typescript
const data = await response.json();
if (!data || typeof data.type !== "string") {
  throw new ProfileError("Invalid profile data received");
}
```
- [ ] `getProfileClient` valida campos obrigatórios
- [ ] Teste para resposta malformed

**WR-03: ProfileCard sem fallback silencioso**
```tsx
// De: profile.razaoSocial ?? profile.name
// Para: profile.razaoSocial ?? "—"
```
- [ ] Renderizar placeholder explícito "—" para campos missing
- [ ] Teste para PJ com razaoSocial null

**WR-04: login() com catch explícito**
```typescript
async function login(email, password) {
  setIsLoading(true);
  try {
    const response = await loginClient(email, password);
    tokens = { ... };
    setIsAuthenticated(true);
  } catch (err) {
    setIsLoading(false);
    throw err; // explícito
  }
  setIsLoading(false);
}
```
- [ ] AuthContext login() com catch/re-throw
- [ ] Teste para erro propagado corretamente

**WR-05: Refresh token expiry check**
```typescript
if (tokens.refreshExpiresAt && tokens.refreshExpiresAt <= Date.now()) {
  logout();
  return;
}
```
- [ ] `refreshIfNeeded` verifica refresh token expiry
- [ ] Teste para refresh token expirado → logout

**WR-06: Single useEffect no ProfilePage**
```typescript
useEffect(() => {
  if (!auth.isAuthenticated) {
    navigate({ to: "/login" });
    return;
  }
  fetchProfile();
}, [auth.isAuthenticated, navigate]);
```
- [ ] Combinar auth guard + fetch em um useEffect
- [ ] Testes permanecem passando

**Estimativa:** 1 fase pequena

---

### 3. Production Readiness
**Motivação:** v1 é dev-only — produção requer HTTPS, secrets, backup, monitoring

**3.1 HTTPS/TLS**
- [ ] Self-signed certs para staging (mkcert)
- [ ] Let's Encrypt support para produção
- [ ] Keycloak HTTPS (KC_HTTPS_KEY_STORE_FILE)
- [ ] API HTTPS (ASP.NET Core Kestrell certs)
- [ ] Frontend HTTPS (Vite server ou reverse proxy)
- [ ] compose.prod.yaml com infra HTTPS

**3.2 Secrets Management**
- [ ] Docker secrets para senas de DB
- [ ] Keycloak vault para client secrets
- [ ] CI/CD secrets injection (GitHub Actions, Azure Key Vault)
- [ ] `.env.production` template com placeholders

**3.3 Backup & Recovery**
- [ ] pg_dump agendado para app_db
- [ ] Keycloak realm export agendado
- [ ] Volume backup scripts
- [ ] Restore procedure documentada

**3.4 Monitoring & Alerting**
- [ ] Grafana dashboards (API latency, error rate, Keycloak login failures)
- [ ] Alert rules (error rate > 5%, DB connection failures)
- [ ] Health check endpoint para load balancer
- [ ] Uptime monitoring externo

**Estimativa:** 3-4 fases

---

### 4. Testing Maturity
**Motivação:** Testes em jsdom não capturam problemas reais de browser/network

**4.1 E2E Browser Tests**
- [ ] Playwright setup
- [ ] Test: register PF → login → view profile → logout
- [ ] Test: register PJ → login → view profile → logout
- [ ] Test: duplicate CPF → error message
- [ ] Test: invalid credentials → generic error
- [ ] Test: brute force → account lock
- [ ] CI integration (Playwright em container)

**4.2 Integration Tests Realistas**
- [ ] `RegistrationIntegrationTests` importa realm no Testcontainers Keycloak
- [ ] Teste com Keycloak real (não mock)
- [ ] Teste de compensation (simular falha Keycloak)

**4.3 Performance Testing**
- [ ] k6 load test: 100 req/s em `/api/registration`
- [ ] k6 stress test: idempotency filter sob concorrência
- [ ] k6 soak test: 30 min com carga constante
- [ ] Métricas: p95 latency, error rate, throughput

**Estimativa:** 2 fases

---

### 5. DX & Documentation
**Motivação:** Onboarding de novos devs e operação em produção

**5.1 Runbook de Produção**
- [ ] Deploy procedure (Docker Compose → Kubernetes?)
- [ ] Keycloak admin operations (unlock user, reset password)
- [ ] Database migration procedure
- [ ] Incident response (503, 401 spike, DB down)
- [ ] Contact list / escalation

**5.2 Developer Experience**
- [ ] `make dev` ou `npm run dev:all` para iniciar tudo
- [ ] Seed script: cria usuários demo (PF + PJ)
- [ ] OpenAPI/Swagger para API documentation
- [ ] CHANGELOG.md com versionamento semântico

**5.3 CI/CD Pipeline**
- [ ] GitHub Actions: build + test on PR
- [ ] Docker image build + push to registry
- [ ] Deploy staging on merge to main
- [ ] Security scan (Trivy, Snyk)

**Estimativa:** 2 fases

---

## 📋 Roadmap Proposto v2.0

### Phase 11: Code Review Fixes (1 plano)
- WR-01 a WR-06 todos resolvidos
- Testes atualizados
- **Reqs:** Nenhum novo — melhoria de qualidade

### Phase 12: PKCE Migration (3 planos)
- P12-01: Keycloak config + backend callback endpoint
- P12-02: Frontend redirect flow + PKCE generation
- P12-03: Tests + ROPC removal + migration guide

### Phase 13: E2E Testing (2 planos)
- P13-01: Playwright setup + core flow tests
- P13-02: Integration tests com Keycloak real + perf tests

### Phase 14: Production HTTPS (2 planos)
- P14-01: Certs + Keycloak HTTPS + API HTTPS
- P14-02: compose.prod.yaml + reverse proxy

### Phase 15: Secrets & Backup (2 planos)
- P15-01: Docker secrets + Keycloak vault
- P15-02: Backup scripts + restore procedure

### Phase 16: Monitoring & CI/CD (2 planos)
- P16-01: Grafana dashboards + alert rules
- P16-02: GitHub Actions pipeline + security scan

**Total:** 6 fases, 12 planos
**Estimativa:** 4-6 dias (dependendo de complexidade PKCE)

---

## 🚦 Priorização

| Prioridade | Fase | Motivo |
|-----------|------|--------|
| **P0** | Phase 11: Code Review Fixes | Quick wins — 6 warnings em 1 dia |
| **P1** | Phase 12: PKCE Migration | Security compliance — OAuth 2.1 |
| **P1** | Phase 13: E2E Testing | Confidence boost — catches real bugs |
| **P2** | Phase 14: Production HTTPS | Required for prod — sem HTTPS não deploy |
| **P2** | Phase 15: Secrets & Backup | Required for prod — data protection |
| **P3** | Phase 16: Monitoring & CI/CD | Operational excellence |

---

## 🔓 Quick Wins (Pode fazer agora)

1. **WR-01 a WR-06** — 1 dia, 6 fixes, zero risco
2. **OpenAPI/Swagger** — 2 horas, documentação automática
3. **Seed script** — 2 horas, usuários demo para testes manuais
4. **Grafana dashboards** — 4 horas, visibilidade imediata

---

## ❓ Decisões Pendentes

### 1. Manter UI custom de login ou aceitar Keycloak page?
- **Pró custom:** Branding total, UX controlada
- **Pró Keycloak:** OAuth 2.1 compliant, menos código, MFA pronto
- **Recomendação:** Aceitar Keycloak page com tema custom (balancing)

### 2. Docker Compose ou Kubernetes para produção?
- **Compose:** Simples, bom para single-server
- **K8s:** Escalável, complexo, requer expertise
- **Recomendação:** Compose para MVP → K8s se escalar

### 3. Self-hosted Keycloak ou serviço gerenciado?
- **Self-hosted:** Controle total, custo infra
- **Managed (Keycloak.as-a-Service):** Menos ops, custo mensal
- **Recomendação:** Self-hosted para v2 (já está pronto), avaliar managed para v3

---

## 📊 Comparativo v1.0 vs v2.0 Proposto

| Métrica | v1.0 | v2.0 (proposto) | Delta |
|---------|------|-----------------|-------|
| Fases | 10 | 6 | +6 |
| Planos | 30 | 12 | +12 |
| Testes E2E | 0 (jsdom apenas) | ~20 (Playwright) | +20 |
| HTTPS | ❌ | ✅ | Production ready |
| OAuth Flow | ROPC (deprecated) | PKCE (2.1) | Security upgrade |
| Monitoring | Grafana básico | Dashboards + alerts | Operational |
| CI/CD | Manual | GitHub Actions | Automation |
| Runbook | ❌ | ✅ | Operacional |

---

*Documento proposto em 2026-04-08*
*Autor: AI Assistant com workflow GSD*
*Aguardando revisão e aprovação do usuário*
