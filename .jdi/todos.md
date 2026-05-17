# Backlog — scope creep capturado durante /jdi-discuss

Items fora do escopo de phases atuais. Cada item pode virar phase futura via `/jdi-add-phase`.

## De Phase 48 (/jdi-discuss 48, 2026-05-11)

- **Motivo/EvidenciaUrl em status transition** — `POST /api/fundos/{id}/status` poderia receber `{ NewStatus, Motivo, EvidenciaUrl? }` pra compliance CVM. Decisao D-9 manteve body minimo no MVP. Revisitar apos feedback usuario PJ.
- **Admin status force override** — AdminFundosController poderia ter `POST /admin/fundos/{id}/force-status` ignorando state machine. Decisao D-8 manteve admin read-only. Requer D-decision separada com auditoria reforcada se reaberto.
- **Idempotency-Key header** — retry-safe POSTs (Fundo/Cedente register). Hoje POST com mesmo CNPJ retorna 409. Pra clientes que retentam apos timeout, suportar header `Idempotency-Key` retornaria resposta original em vez de 409. Backlog.
- **Audit drill-down em AdminFundosController** — `GET /admin/fundos/{id}/audit-history` agregando AdminAuditLog entries filtrados por entity_id + tipo. Decisao D-8 manteve admin apenas List. Backlog.
- **Detail-by-id em AdminFundosController** — `GET /admin/fundos/{id}` mostrando entity completa cross-company. Decisao D-8 manteve apenas List. Backlog.
