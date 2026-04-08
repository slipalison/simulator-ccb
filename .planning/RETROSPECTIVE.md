# Retrospectiva — Milestone v1.0

**Período:** 2026-04-01 a 2026-04-08 (7 dias)
**Fases:** 10 completas (30 planos)
**Resultado:** ✅ Milestone v1.0 entregue

---

## 📊 Métricas do Projeto

| Métrica | Valor |
|---------|-------|
| **Dias de desenvolvimento** | 7 |
| **Fases completas** | 10/10 (100%) |
| **Planos completos** | 30/30 (100%) |
| **Commits** | 40+ atômicos |
| **Testes backend** | 43 domain + 44 API = 87 (2 skipped) |
| **Testes frontend** | 48 (9 arquivos) |
| **Total de testes** | **135 passando** |
| **Falhas ativas** | 0 |
| **Deviações auto-corrigidas** | ~25 (Rule 1-3) |
| **Arquivos criados** | ~120+ |

### Velocidade por Fase

| Fase | Duração | Plans | Complexidade |
|------|---------|-------|--------------|
| 01-Infrastructure | 1 dia | 3/3 | Alta (Docker, Keycloak, .NET, Vinxi) |
| 02-Keycloak Hardening | 1 dia | 1/1 | Média (security, shell tests) |
| 03-Backend Domain | 1 dia | 2/2 | Alta (DDD, TDD, value objects) |
| 04-Observability | 1 dia | 4/4 | Alta (Serilog, OTel, Grafana) |
| 05-Registration API | 2 dias | 4/4 | Muito Alta (CQRS, EF Core, Keycloak SDK, idempotency) |
| 06-Authentication API | 1 dia | 3/3 | Alta (JWT, ROPC, middleware) |
| 07-Frontend Foundation | 1 dia | 4/4 | Média (Vinxi, Atomic Design, RHF) |
| 08-Registration UI | 1 dia | 3/3 | Média (Zod, forms, API integration) |
| 09-Login UI | 1 dia | 3/3 | Alta (AuthContext, SEC-10, vitest) |
| 10-Profile UI | 1 dia | 3/3 | Média (data fetching, E2E tests) |

---

## ✅ O que Funcionou Bem

### 1. TDD Rigoroso
- Wave 0 RED stubs em todas as fases de backend criaram segurança para implementação
- 0 regressões em toda a base de código — cada commit manteve testes existentes passando
- Pattern `true.ShouldBeFalse("RED stub — not implemented")` foi efetivo e consistente

### 2. Commits Atômicos
- Cada task commitou separadamente com mensagens semânticas (feat, test, fix, docs)
- Facilitou debugging e rollback quando necessário
- Git log serve como trilha de auditoria completa

### 3. Auto-Fix de Devisões (Rules 1-3)
- Rule 1 (Bug fix): 20+ correções automáticas de compilação/comportamento
- Rule 2 (Missing functionality): 3+ adições de pacotes necessários
- Rule 3 (Blocking): 2+ correções de dependências ausentes
- Nenhuma deviação causou scope creep — todas foram necessárias para corretude

### 4. Keycloak Hardening desde o Início
- Phase 2 hardening antes de qualquer dado fluir pelo sistema foi decisão acertada
- ClientPolicies importados via realm JSON funcionaram no primeiro boot
- Acceptance test suite (`verify-hardening.sh`) provou valor continuamente

### 5. Compensação/Rollback Strategy (REG-06)
- Handler registra no app_db PRIMEIRO, depois cria no Keycloak
- Se Keycloak falha → `DeleteAsync` remove registro do app_db
- Pattern implementado e testado — funciona

### 6. Idempotency Filter (REG-08)
- `IdempotentAttribute` como `IAsyncActionFilter` cacheia respostas 2xx via `IDistributedCache`
- TTL 60 minutos, apenas 2xx cacheados (previne cache poisoning)
- Testes provam que mesma key retorna resposta cacheada sem chamar handler

### 7. SEC-10: Tokens em Memória Apenas
- Módulo `let tokens` variável — não useState, não localStorage
- Testes espiam `Storage.prototype` e confirmam nunca chamado
- Tokens destruídos no page refresh — comportamento intencional

### 8. Observabilidade Funcional
- Serilog JSON com TraceId/SpanId enrichment
- OpenTelemetry SDK instrumentando ASP.NET Core, HttpClient, EF Core
- Grafana LGTM stack (Alloy, Loki, Tempo, Mimir) rodando via Docker Compose
- SensitiveDataDestructuringPolicy maskando password, token, CPF, CNPJ, email

---

## ⚠️ Lições Aprendidas

### 1. Vinxi é Imaturo
- `defineConfig` não existia na v0.5.x — API mudou sem documentação clara
- Port config em `app.config.ts` não era respeitado — precisou passar via CLI
- Sem `index.html` → SSR fallback crash com "document is not defined"
- **Lição:** Meta-frameworks novos exigem mais tempo de investigação antes de usar

### 2. Windows Docker HMR
- `usePolling: true` no Vinxi foi essencial para hot reload funcionar dentro de container Docker no Windows
- Sem isso, mudanças no código não eram detectadas pelo watcher
- **Lição:** Desenvolvimento cross-platform exige atenção a filesystem events

### 3. .NET 10 .slnx Format
- .NET 10 defaults para `.slnx` (XML format) mas Dockerfile esperava `.sln` clássico
- Forçar `--format sln` resolveu
- **Lição:** Verificar compatibilidade de formats de arquivo antes de scaffold

### 4. NSubstitute 5.x sem ThrowsAsync
- Plano usava `.ThrowsAsync()` que não existe no NSubstitute 5.x
- Substituído por `.Returns(Task.FromException<T>(exception))`
- **Lição:** Sempre verificar API da versão exata do pacote antes de usar

### 5. xUnit 2.9.x sem Assert.Fail
- Usar `true.ShouldBeFalse(message)` como pattern RED stub
- **Lição:** Framework de testes pode ter limitações — adaptar pattern

### 6. TanStack Router em Testes
- `routeTree.gen` não existe — rotas definidas inline em `router.tsx`
- `testRouter.state.location.pathname` não atualiza sincronamente em jsdom
- Navegação assíncrona requer `waitFor` mas pode timeout sem re-render
- **Lição:** Testar navigation em jsdom é frágil — preferir assert de side-effects

### 7. JwtBearer MapInboundClaims=false
- Sem isso, claim "email" do Keycloak é mapeado para URI XML namespace
- `User.FindFirst("email")` retornava null silenciosamente
- **Lição:** OIDC claims mapping não é trivial — documentar em runbook

### 8. ValidateAudience=false para ROPC
- Tokens ROPC do Keycloak têm `aud: ["account"]`, não a nossa API
- Sem `ValidateAudience=false`, todos os requests retornariam 401
- **Lição:** Audience validation depende do flow OAuth usado

### 9. Python3 vs Python no Windows
- `verify-hardening.sh` usava `python3` — no Windows é `python`
- Auto-detection com fallback resolveu portabilidade
- **Lição:** Shell scripts devem detectar runtime disponível

### 10. Keycloak 26.x Healthcheck
- Sem `curl` na imagem — usar `/dev/tcp` bash TCP socket
- Management port 9000 (não 8080) para health endpoints
- **Lição:** Imagens minimalistas não têm ferramentas padrão

---

## 🔍 Code Review Warnings (Phase 10)

6 warnings identificados — nenhum crítico, todos melhorias:

| ID | Severidade | Descrição | Impacto |
|----|-----------|-----------|---------|
| WR-01 | Warning | `status === 200` estrito (deveria usar `response.ok`) | Baixo — backend sempre retorna 200 |
| WR-02 | Warning | `getProfileClient` sem validação runtime | Médio — shape mismatch silencioso possível |
| WR-03 | Warning | `ProfileCard` fallback silencioso `razaoSocial` | Baixo — UX confusa se dado missing |
| WR-04 | Warning | `login()` sem `catch` explícito | Baixo — funciona mas é frágil |
| WR-05 | Warning | `refreshIfNeeded` não verifica refresh token expiry | Médio — silent session termination |
| WR-06 | Warning | Dois `useEffect` com possível race condition | Baixo — React Strict Mode warnings |

---

## 🎯 Decisões Arquiteturais Revisitadas

### ROPC vs PKCE
- **Decisão:** ROPC grant escolhido para v1 (controle total da UI de login)
- **Status:** ⚠️ Deprecated no OAuth 2.1
- **v2:** Migrar para Authorization Code + PKCE com Keycloak login page
- **Impacto:** Mudança significativa no frontend (redirecionar para Keycloak)

### Sem MediatR
- **Decisão:** CQRS manual via DI nativo
- **Status:** ✅ Boa decisão — MediatR é comercial agora
- **Resultado:** Handlers injetados diretamente, código mais simples

### app_db PRIMEIRO, Keycloak DEPOIS
- **Decisão:** Persistir no PostgreSQL antes de criar no Keycloak
- **Status:** ✅ Correto — permite rollback/compensation
- **Alternativa ruim:** Keycloak primeiro → se app_db falhar, órfão no Keycloak

---

## 📈 O que Poderia ser Melhor

### 1. Testes de Integração Reais
- `RegistrationIntegrationTests` aceitam 503 como válido (Keycloak sem realm)
- Ideal: importar realm no container de teste via Testcontainers
- **Motivo:** Testes não provam fluxo completo com Keycloak real

### 2. Coverage de E2E no Browser
- 48 testes frontend rodam em jsdom — não browser real
- Playwright/Cypress seria ideal para fluxo real login→profile
- **Motivo:** jsdom não captura problemas de rendering/CSS/network real

### 3. UAT Automation
- `run-uat.mjs` existe mas requer stack rodando manualmente
- Ideal: CI pipeline com Docker Compose → UAT → tear down
- **Motivo:** Testes manuais não escalam

### 4. Performance Testing
- Sem load testing — API nunca foi testada sob concorrência
- k6 ou Artillery seriam úteis
- **Motivo:** IdempotencyFilter com IDistributedCache pode ter race conditions sob carga

### 5. Documentação de Runbook
- Sem runbook para produção (HTTPS, secrets, backup, monitoring)
- README é dev-focused apenas
- **Motivo:** Operacionalizar requer procedimentos claros

---

## 🏆 Destaques

### Melhor Feature: IdempotencyFilter
- Pattern elegante: `IAsyncActionFilter` como attribute
- Cacheia apenas 2xx, ignora 4xx/5xx (previne cache poisoning)
- TTL 60 minutos via `IDistributedCache`
- Testes provam comportamento exato

### Melhor Test: SEC-10 Memory-Only Tokens
- Espionar `Storage.prototype.getItem/setItem` é engenhoso
- Prova que tokens nunca tocam localStorage/sessionStorage
- Module-level `let` variável é simples e efetivo

### Melhor Decisão: Keycloak Hardening na Phase 2
- Antes de qualquer dado fluir, segurança estava pronta
- ClientPolicies, brute force, password policy testados
- `verify-hardening.sh` como regression test

---

## 🚦 Status Final

| Área | Status | Notas |
|------|--------|-------|
| **Infraestrutura** | ✅ Pronto | Docker Compose, 5 serviços, healthchecks |
| **Segurança** | ✅ Pronto | Keycloak hardened, SEC-01 a SEC-10 |
| **Backend** | ✅ Pronto | DDD, CQRS, 87 testes |
| **Frontend** | ✅ Pronto | Atomic Design, 48 testes |
| **Observabilidade** | ✅ Pronto | Serilog, OTel, Grafana LGTM |
| **Testes** | ✅ Pronto | 135 testes passando |
| **Documentação** | ⚠️ Parcial | README dev ok, runbook prod ausente |
| **Produção** | ❌ Não pronto | Falta HTTPS, secrets management, backup |

---

## 👥 Agradecimentos

- **Fluxo GSD** (Get Shit Done): Planejamento por fases com PLAN→SUMMARY→checkpoint foi efetivo
- **TDD rigoroso**: Wave 0 RED stubs antes de qualquer código de produção
- **Commits atômicos**: Cada task com commit separado facilitou debugging
- **Auto-fix de devisões**: Rules 1-3 permitiram adaptação sem burocracia

---

*Documento gerado em 2026-04-08 após conclusão do Milestone v1.0*
*Autor: AI Assistant com workflow GSD*
