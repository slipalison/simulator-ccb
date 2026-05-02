<!-- GSD:project-start source:PROJECT.md -->
## Project

**Onboarding de Clientes**

Sistema de onboarding para cadastro de clientes Pessoa Física (PF) e Pessoa Jurídica (PJ). O usuário se cadastra com dados básicos e senha, é direcionado ao login, e após autenticação visualiza seus dados cadastrais em modo leitura. A segurança é prioridade — Keycloak hardened, infraestrutura containerizada.

**Core Value:** Cadastro seguro e funcional de clientes PF/PJ com autenticação robusta via Keycloak — se a segurança falhar, nada mais importa.

### Constraints

- **Tech Stack**: .NET 10 + React/Vinxi + PostgreSQL + Keycloak — stack definida pelo usuário
- **Infra**: Tudo deve rodar em Docker Compose localmente
- **Segurança**: Keycloak deve ser hardened contra vulnerabilidades documentadas
- **API Style**: Controllers ASP.NET (sem Minimal API)
- **Observabilidade**: Serilog + OpenTelemetry obrigatórios desde o início
<!-- GSD:project-end -->

<!-- GSD:stack-start source:research/STACK.md -->
## Technology Stack

## Backend — .NET 10
| Component | Package | Version | Rationale | Confidence |
|-----------|---------|---------|-----------|------------|
| Runtime | .NET 10 | 10.0 | Latest LTS (Nov 2025). User-specified. | HIGH |
| Web framework | ASP.NET Core Controllers | 10.0 | User requires no Minimal API. Controllers with proper routing. | HIGH |
| ORM | Entity Framework Core | 10.0 | Standard .NET ORM. Code-first migrations, LINQ queries. | HIGH |
| PostgreSQL driver | Npgsql.EntityFrameworkCore.PostgreSQL | 10.0.x | Official EF Core provider for PostgreSQL. | HIGH |
| Logging | Serilog | 4.x | Structured logging. User-specified. | HIGH |
| Serilog ASP.NET | Serilog.AspNetCore | 9.x | ASP.NET Core integration for request logging. | HIGH |
| Serilog OTLP sink | Serilog.Sinks.OpenTelemetry | 4.x | Export logs via OTLP protocol. | HIGH |
| OpenTelemetry | OpenTelemetry.Extensions.Hosting | 1.10.x | Traces + metrics SDK for .NET. | HIGH |
| OTEL ASP.NET | OpenTelemetry.Instrumentation.AspNetCore | 1.10.x | Auto-instrument HTTP requests. | HIGH |
| OTEL HttpClient | OpenTelemetry.Instrumentation.Http | 1.10.x | Auto-instrument outbound HTTP (Keycloak Admin API calls). | HIGH |
| OTEL EF Core | OpenTelemetry.Instrumentation.EntityFrameworkCore | 1.0.x | Auto-instrument database queries. | MEDIUM |
| OTEL Exporter | OpenTelemetry.Exporter.OpenTelemetryProtocol | 1.10.x | OTLP exporter for traces/metrics. | HIGH |
| JWT Auth | Microsoft.AspNetCore.Authentication.JwtBearer | 10.0 | Built-in JWT validation middleware. | HIGH |
| Keycloak Admin | Keycloak.AuthServices.Sdk | 2.7.x | .NET SDK for Keycloak Admin REST API. | MEDIUM |
| Token management | Duende.AccessTokenManagement | 3.x | Automatic service account token lifecycle. | MEDIUM |
| Validation | FluentValidation | 11.x | Command/DTO validation in Application layer. | HIGH |
| CQRS | Manual DI | - | Command/query handlers injected via built-in DI. No third-party mediator. | HIGH |
| Testing | xUnit | 2.9.x | Standard .NET test framework. User wants TDD. | HIGH |
| Test assertions | Shouldly | 4.x | Readable test assertions. MIT license — fully open source. | HIGH |
| Integration tests | Microsoft.AspNetCore.Mvc.Testing | 10.0 | WebApplicationFactory for API integration tests. | HIGH |
| Test containers | Testcontainers | 4.x | Spin up PostgreSQL + Keycloak in integration tests. | HIGH |
| Mocking | NSubstitute | 5.x | Mocking for unit tests. Simpler than Moq. | HIGH |
### License Rule
**All libraries must be open source (Apache 2.0, MIT, or equivalent permissive license).** Before adding any NuGet package, verify its license. Reject any package that is proprietary, source-available-only, or has moved to a commercial model.

### What NOT to Use
| Package | Why Avoid |
|---------|-----------|
| Minimal API | User explicitly excluded. Use Controllers. |
| Moq | License controversy (SponsorLink). Use NSubstitute. |
| Dapper alongside EF Core | Unnecessary complexity for this scope. EF Core handles all queries. |
| IdentityServer/Duende IdentityServer | Keycloak IS the identity provider. Don't add another. |
| ASP.NET Core Identity | Keycloak manages users. Don't mix identity systems. |
| MediatR | No longer open source (commercial license). Use manual DI for CQRS. |
| FluentAssertions | v8+ moved to commercial license (Xceed). Use Shouldly (MIT) instead. |
| MassTransit | Out of scope — message bus não é necessário neste projeto. |
## Frontend — React + Vinxi
| Component | Package | Version | Rationale | Confidence |
|-----------|---------|---------|-----------|------------|
| Meta-framework | Vinxi | 0.5.x | Vite-based fullstack framework. User-specified. | MEDIUM |
| UI library | React | 19.x | User-specified. | HIGH |
| Routing | TanStack Router | 1.x | Type-safe routing, works well with Vinxi. | MEDIUM |
| HTTP client | ky or fetch | native | Lightweight. No need for axios overhead. | MEDIUM |
| Forms | React Hook Form | 7.x | Performant form handling, validation integration. | HIGH |
| Validation | Zod | 3.x | Schema validation shared between forms and API contracts. | HIGH |
| Styling | Tailwind CSS | 4.x | Utility-first, pairs well with Atomic Design. | HIGH |
| Type checking | TypeScript | 5.7.x | Type safety across the frontend. | HIGH |
### What NOT to Use
| Package | Why Avoid |
|---------|-----------|
| Next.js | Different framework. User chose Vinxi. |
| Redux/Zustand | Over-engineering for auth state + profile view. React Context sufficient. |
| Axios | fetch + ky is lighter. No interceptor complexity needed. |
| keycloak-js | Designed for Authorization Code Flow. ROPC grant doesn't need it. |
| localStorage for tokens | XSS vulnerability. Store in memory only. |
## Infrastructure
| Component | Image/Version | Configuration | Confidence |
|-----------|---------------|---------------|------------|
| Keycloak | quay.io/keycloak/keycloak:26.1 | Production mode, hardened config | HIGH |
| PostgreSQL (app) | postgres:16-alpine | Dedicated for application data | HIGH |
| PostgreSQL (Keycloak) | postgres:16-alpine | Dedicated for Keycloak internal state | HIGH |
| Docker Compose | v2 (Compose V2) | Multi-service local environment | HIGH |
### Keycloak Configuration
- **Realm:** `onboarding`
- **Public client:** `onboarding-app` (Direct Access Grants Enabled, no secret)
- **Confidential client:** `onboarding-api-admin` (Service Account Enabled, `manage-users` role)
- **Brute force protection:** Enabled (max 5 failures, 30s wait, escalating)
- **Password policy:** min 8 chars, 1 uppercase, 1 lowercase, 1 digit, 1 special char
- **Session timeouts:** SSO Session Max = 8h, Access Token lifespan = 5 min
- **HTTPS:** Required in production (HTTP allowed for local dev only)
## Observability Stack
| Component | Purpose | Notes |
|-----------|---------|-------|
| Serilog + OTLP Sink | Structured logs → stdout (JSON) | Correlation with traces via TraceId automatic in Serilog 4.x |
| OpenTelemetry SDK | Traces + Metrics collection | Instrument ASP.NET Core, HttpClient, EF Core |
| OTLP Exporter | Export to collector | Stdout JSON for dev. OTLP endpoint for prod. |
## Sources
- [.NET 10 Release Notes — Microsoft](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-10/overview)
- [Keycloak 26.x Release — quay.io](https://quay.io/repository/keycloak/keycloak)
- [Serilog.Sinks.OpenTelemetry — GitHub](https://github.com/serilog/serilog-sinks-opentelemetry)
- [OpenTelemetry .NET SDK](https://opentelemetry.io/docs/languages/net/)
- [Keycloak.AuthServices — NuGet](https://www.nuget.org/packages/Keycloak.AuthServices.Sdk)
- [Vinxi — GitHub](https://github.com/nksaraf/vinxi)
- [FluentValidation — GitHub](https://github.com/FluentValidation/FluentValidation)
- [MediatR — GitHub](https://github.com/jbogard/MediatR)
- [Testcontainers for .NET](https://dotnet.testcontainers.org/)
<!-- GSD:stack-end -->

<!-- GSD:conventions-start source:CONVENTIONS.md -->
# Engineering Standards (Global)

Estas regras se aplicam a TODOS os agentes em TODAS as sessões deste OpenCode.

## Stack baseline

- **Backend**: .NET 10 / C# 14 (nunca downgrade)
- **Frontend**: React 19.x + TypeScript 5.7+ strict + Vinxi + TanStack Router + Tailwind CSS 4
- **Infra**: Kubernetes em Azure/AWS, Redis, PostgreSQL, microsserviços
- **Observabilidade**: OpenTelemetry (W3C Trace Context) + Serilog (structured logging) + Datadog/Grafana
- **Frontend apps**: Client (porta 5173) e Backoffice (porta 5174)

## Communication mode (Caveman)

Use **caveman mode (full level)** para todas as respostas conversacionais.
Skill: `~/.config/opencode/skills/caveman/SKILL.md`

### Exceções (NÃO usar caveman)

1. Dentro de blocos YAML estruturados (output do `code-validator` é YAML — caveman só no texto fora do YAML)
2. Quando gerando arquivos de planejamento GSD (`.planning/*`)
3. Quando gerando código (código nunca é comprimido)
4. Quando gerando ADR, RFC ou documentação técnica formal
5. Em commit messages (use o skill `caveman-commit` específico)

## Architectural principles

Aplicar nesta ordem de prioridade:

1. **Cadeia de prioridade**: Segurança → Performance → Boas Práticas. Sempre nessa ordem.
2. **Code Design locked** — selecionado por projeto via skill `code-design-*`. Não misture, não mude.
3. **SOLID**: especialmente ISP (Interface Segregation) e DIP (Dependency Inversion).
4. **DRY, KISS, YAGNI, Clean Code** — mas nunca sacrificar clareza por elegância.
5. **80% mínimo** de cobertura de testes — gate inviolável.
6. **Nunca refatore por estética** — só por segurança, performance, ou bug.
7. **Diagramas Mermaid** embarcados em Markdown apenas (nunca PNG/SVG).

## Code Design

Cada projeto seleciona UM code design e ele é LOCKED para toda a vida do projeto:

| Skill | Design |
|---|---|
| `code-design-ddd` | Domain-Driven Design |
| `code-design-vertical-slice` | Vertical Slice Architecture |
| `code-design-the-method` | The Method (Juval Löwy) |
| `code-design-clean-architecture` | Clean Architecture (Uncle Bob) |
| `code-design-hexagonal` | Hexagonal (Ports & Adapters) |

Registre a escolha em `.planning/CODE-DESIGN.md`. Uma vez decidido, não mude.

## Frontend architecture

- **Atomic Design**: atoms → molecules → organisms → templates → pages → guards + ui (shadcn)
- **Auth**: Authorization Code Flow + PKCE — tokens 100% server-side em httpOnly cookies
- **Sem `keycloak-js`**, sem `localStorage` para tokens — decisão de segurança fundamental
- **Validação**: Zod schemas espelhando regras do backend/Keycloak

## Git rules

- **Conventional Commits** (use skill `caveman-commit` para gerar)
- **Atomic commits** por task (uma task GSD = um commit)
- `--no-verify` em commits de subagents (parallel-safe; orchestrator roda hooks após cada wave)
- Branch naming: `feat/<phase-num>-<slug>` ou `fix/<issue-id>`

## Workflow GSD

Quando dentro de um workflow GSD ativo:

- Respeite as decisões locked (`D-XX`) do plano — referencie em commits e PRs
- "Deferred Ideas" do plano NÃO devem aparecer na implementação
- Cada task deve fechar com um commit atômico
- O `code-validator` é OBRIGATÓRIO antes de fechar qualquer task

## Idioma

- **Código, comentários técnicos, commits**: inglês
- **Discussão, docs internas, planejamento**: português (pt-BR)
- **i18n no frontend**: nunca string hardcoded em português no JSX
<!-- GSD:conventions-end -->

<!-- GSD:architecture-start source:ARCHITECTURE.md -->
## Architecture

Architecture not yet mapped. Follow existing patterns found in the codebase.
<!-- GSD:architecture-end -->

<!-- GSD:workflow-start source:GSD defaults -->
## GSD Workflow Enforcement

Before using Edit, Write, or other file-changing tools, start work through a GSD command so planning artifacts and execution context stay in sync.

Use these entry points:
- `/gsd:quick` for small fixes, doc updates, and ad-hoc tasks
- `/gsd:debug` for investigation and bug fixing
- `/gsd:execute-phase` for planned phase work

Do not make direct repo edits outside a GSD workflow unless the user explicitly asks to bypass it.
<!-- GSD:workflow-end -->



<!-- GSD:profile-start -->
## Developer Profile

> Profile not yet configured. Run `/gsd:profile-user` to generate your developer profile.
> This section is managed by `generate-claude-profile` -- do not edit manually.
<!-- GSD:profile-end -->
