# Phase 4: Observability — Discussion Log

**Date:** 2026-04-02
**Phase:** 04-observability

Audit trail do discuss-phase. Para referência humana — não consumido por agentes downstream.

---

## Area 1: Export Target em Dev

**Q: Para o ambiente de desenvolvimento local, onde o OTLP deve exportar?**
- Options: Stdout JSON apenas | Adicionar Collector + Grafana stack agora
- **Selected: Adicionar Collector + Grafana stack agora**

**Q: Qual OpenTelemetry Collector usar no Docker Compose?**
- Options: otel/opentelemetry-collector-contrib | grafana/alloy
- **Selected: grafana/alloy**

**Q: A API deve exportar OTLP direto para o Alloy, ou via variável de ambiente configurável?**
- Options: Variável de ambiente configurável | Hard-coded para Alloy em dev
- **Selected: Variável de ambiente configurável (OTEL_EXPORTER_OTLP_ENDPOINT)**

---

## Area 2: Log Masking (SEC-09)

**Q: Como mascarar dados sensíveis nos logs?**
- Options: Destructuring policy no Serilog | Custom enricher + filter | Masking no sink
- **Selected: Destructuring policy no Serilog**

**Q: Quais campos devem ser mascarados?**
- Options: Authorization header, client_secret, CPF e CNPJ, Email
- **Selected: Todos (Authorization header, client_secret, CPF, CNPJ, Email)**

---

## Area 3: Health Check Design (OBS-05)

**Q: Como estruturar os endpoints de health check?**
- Options: /healthz único com sub-checks | Split /healthz/live + /healthz/ready
- **Selected: Split /healthz/live + /healthz/ready**

**Q: O que o /healthz/ready deve checar?**
- Options: PostgreSQL, Keycloak, Disco/memória, Só PostgreSQL
- **Selected: PostgreSQL (app_db), Keycloak, Disco/memória**

---

## Area 4: Correlation ID (OBS-04)

**Q: Como propagar o Correlation ID nas chamadas ao Keycloak Admin API?**
- Options: W3C traceparent via OpenTelemetry | Header X-Correlation-ID customizado
- **Selected: W3C traceparent via OpenTelemetry**

---

*Log gerado em: 2026-04-02*
