# Phase 4: Observability - Context

**Gathered:** 2026-04-02
**Status:** Ready for planning

<domain>
## Phase Boundary

Instrumentar o sistema com logs estruturados, traces distribuídos e métricas — tudo correlacionado via OpenTelemetry e Serilog. Adicionar a stack de observabilidade local (Grafana Alloy + Loki + Tempo + Mimir + Grafana) ao Docker Compose para que os dados sejam visualizáveis imediatamente. A fase entrega a infra de observabilidade completa e pronta para produção — apenas as variáveis de endpoint mudam entre ambientes.

Fora do escopo desta fase: dashboards Grafana customizados, alertas, integração com ferramentas APM externas.

</domain>

<decisions>
## Implementation Decisions

### Logging — Serilog
- **D-01:** Usar Serilog configurado via `UseSerilog()` no `Program.cs`, substituindo o logging padrão do .NET.
- **D-02:** Sink obrigatório: **Console** com formatador JSON (`Serilog.Formatting.Compact.CompactJsonFormatter` ou `RenderedCompactJsonFormatter`).
- **D-03:** TraceId e SpanId são enriquecidos automaticamente via integração Serilog ↔ OpenTelemetry (`Serilog.Enrichers.Span` ou equivalente). Nenhum campo manual.
- **D-04:** Nível mínimo: `Information` em produção, `Debug` em desenvolvimento — configurável via `appsettings.{env}.json`.
- **D-05:** Request logging via middleware `app.UseSerilogRequestLogging()` — log por request com propriedades: método, path, status code, duração.

### Export Target — Grafana Stack no Docker Compose
- **D-06:** Adicionar ao `docker-compose.yml` os serviços: **Grafana Alloy** (collector), **Loki** (logs), **Tempo** (traces), **Mimir** (métricas), **Grafana** (UI).
- **D-07:** A API exporta OTLP via variável de ambiente `OTEL_EXPORTER_OTLP_ENDPOINT`. Em `docker-compose.yml`, essa variável aponta para o Alloy (`http://alloy:4317`). Em produção, basta trocar a variável.
- **D-08:** Grafana Alloy configurado para receber OTLP (gRPC/HTTP), rotear logs → Loki, traces → Tempo, métricas → Mimir.
- **D-09:** Grafana exposto na porta 3000, pré-configurado com datasources Loki, Tempo e Mimir via provisioning em arquivo.

### OpenTelemetry — Traces e Métricas
- **D-10:** SDK configurado no `Program.cs` via `AddOpenTelemetry()` com instrumentações:
  - `AddAspNetCoreInstrumentation()` — traces de requests HTTP inbound
  - `AddHttpClientInstrumentation()` — traces de chamadas outbound (Keycloak Admin API)
  - `AddEntityFrameworkCoreInstrumentation()` — traces de queries ao PostgreSQL
- **D-11:** Métricas: `AddRuntimeInstrumentation()` + `AddAspNetCoreInstrumentation()` exportadas via OTLP.
- **D-12:** Exporters: `AddOtlpExporter()` para traces e métricas. Endpoint lido de `OTEL_EXPORTER_OTLP_ENDPOINT`.
- **D-13:** Service name configurável via `OTEL_SERVICE_NAME` (default: `onboarding-api`).

### Correlation ID — W3C traceparent (OBS-04)
- **D-14:** Não criar header customizado. O `HttpClient` instrumentado pelo OpenTelemetry propaga automaticamente o header W3C `traceparent` em todas as chamadas outbound, incluindo o Keycloak Admin API.
- **D-15:** O `SpanId` do trace ativo já é o correlation ID. Aparece em todos os log entries via enriquecimento automático (D-03).
- **D-16:** Nenhum middleware adicional necessário — propagação é transparente via OTEL context.

### Log Masking — Destructuring Policy (SEC-09)
- **D-17:** Implementar `IDestructuringPolicy` customizado no Serilog que intercepta objetos de request/response e substitui campos sensíveis por `[REDACTED]`.
- **D-18:** Campos a mascarar (case-insensitive): `password`, `token`, `secret`, `client_secret`, todos os valores do header `Authorization`.
- **D-19:** CPF mascarado como `***.***.***-**` (mantém formato, oculta valor). CNPJ mascarado como `**.***.***/****.***-**`.
- **D-20:** Email mascarado parcialmente: `a***@domain.com` (preserva domínio para debugging, oculta identidade).
- **D-21:** A policy é registrada globalmente em `Log.Logger` — aplica a todos os sinks automaticamente, sem código repetido.

### Health Checks — Split live/ready (OBS-05)
- **D-22:** Dois endpoints distintos:
  - `GET /healthz/live` — Liveness: API está rodando? Retorna 200 imediatamente sem checar dependências.
  - `GET /healthz/ready` — Readiness: dependências estão acessíveis? Retorna 200 se todos os checks passam, 503 se algum falha.
- **D-23:** `/healthz/ready` inclui checks: **PostgreSQL** (EF Core ping), **Keycloak** (HTTP GET no endpoint `/health/ready` do Keycloak), **Disco** (espaço livre mínimo), **Memória** (uso máximo).
- **D-24:** Usar `Microsoft.Extensions.Diagnostics.HealthChecks` + pacotes auxiliares (`AspNetCore.HealthChecks.Npgsql`, `AspNetCore.HealthChecks.Uris`).
- **D-25:** Resposta JSON detalhada (não só HTTP status) com nome de cada check, status e duração.
- **D-26:** Health check do Docker Compose para a API usa `/healthz/live` (rápido, sem I/O).

### Claude's Discretion
- Configuração exata de retenção e limites de memória no Loki/Mimir/Tempo (defaults das imagens são aceitáveis em dev)
- Exata estrutura do arquivo de configuração do Grafana Alloy (`.alloy` ou `config.river`)
- Escolha entre OTLP gRPC (porta 4317) ou HTTP (porta 4318) para o Alloy — gRPC é padrão
- Dashboards Grafana pré-provisionados (se possível, adicionar um básico; se complexo, deixar para fase dedicada)

</decisions>

<specifics>
## Specific Ideas

- "Deixar tudo pronto para plugar em uma stack Grafana (Grafana, Loki, Tempo e Mimir)" — o ambiente de desenvolvimento já deve ter a stack completa rodando via Docker Compose, não apenas configurada.
- "Respeitar as boas práticas de log, trace e métricas" — logs estruturados, traces com spans corretos, métricas com nomes seguindo convenção OpenTelemetry (snake_case, prefixo `onboarding_`).
- Grafana Alloy é preferido ao `otel/opentelemetry-collector-contrib` como collector — mais integrado ao ecossistema Grafana.

</specifics>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Tech stack e packages aprovados
- `CLAUDE.md` — Lista de pacotes aprovados (Serilog, OpenTelemetry SDK, versões, licença OSS obrigatória)
- `.planning/REQUIREMENTS.md` §Observabilidade — OBS-01 a OBS-05, SEC-09 (requisitos formais desta fase)
- `.planning/ROADMAP.md` §Phase 4 — Goal e success criteria oficiais

### Código existente (ponto de integração)
- `src/Onboarding.API/Program.cs` — Entry point onde toda a configuração de Serilog e OpenTelemetry será adicionada
- `src/Onboarding.API/Onboarding.API.csproj` — Projeto que receberá os PackageReferences de observabilidade
- `docker-compose.yml` — Arquivo existente que receberá os novos serviços Grafana stack

### Sem specs externas adicionais
Não há ADRs ou documentos de spec adicionais além dos listados acima. Todas as decisões de implementação estão capturadas nas seções `<decisions>` acima.

</canonical_refs>

<code_context>
## Existing Code Insights

### Estado atual da observabilidade
- `Program.cs` está vazio (apenas `AddControllers` e `MapControllers`) — nenhum Serilog ou OpenTelemetry configurado
- `Onboarding.API.csproj` tem apenas `Microsoft.AspNetCore.OpenApi` — nenhum pacote de observabilidade instalado
- Ponto de integração principal: `Program.cs` é onde toda a configuração será adicionada

### Padrões estabelecidos
- Extensões de DI registradas em métodos `AddX()` por projeto (ex: `AddApplication()` em `Onboarding.Application/DependencyInjection.cs`)
- Seguir o mesmo padrão: criar `AddObservability()` em `Onboarding.API` ou diretamente em `Program.cs`

### Integration Points
- `HttpClient` para chamadas ao Keycloak Admin API (Phase 5) — OpenTelemetry `AddHttpClientInstrumentation()` precisa estar ativo antes
- EF Core (Phase 5) — `AddEntityFrameworkCoreInstrumentation()` precisará do DbContext registrado
- `docker-compose.yml` existente — novos serviços Grafana stack serão adicionados

</code_context>

<deferred>
## Deferred Ideas

- Dashboards Grafana customizados (boards específicos para onboarding) — fase de infra dedicada ou backlog
- Alertas (Grafana alerting ou Alertmanager) — out of scope para v1
- APM externo (Datadog, New Relic) — out of scope; Grafana self-hosted é suficiente
- Log sampling em produção — otimização futura quando houver volume real

</deferred>

---

*Phase: 04-observability*
*Context gathered: 2026-04-02*
