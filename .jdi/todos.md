# Backlog — scope creep capturado durante /jdi-discuss

Items fora do escopo de phases atuais. Cada item pode virar phase futura via `/jdi-add-phase`.

## De Phase 48 (/jdi-discuss 48, 2026-05-11)

- **Motivo/EvidenciaUrl em status transition** — `POST /api/fundos/{id}/status` poderia receber `{ NewStatus, Motivo, EvidenciaUrl? }` pra compliance CVM. Decisao D-9 manteve body minimo no MVP. Revisitar apos feedback usuario PJ.
- **Admin status force override** — AdminFundosController poderia ter `POST /admin/fundos/{id}/force-status` ignorando state machine. Decisao D-8 manteve admin read-only. Requer D-decision separada com auditoria reforcada se reaberto.
- **Idempotency-Key header** — retry-safe POSTs (Fundo/Cedente register). Hoje POST com mesmo CNPJ retorna 409. Pra clientes que retentam apos timeout, suportar header `Idempotency-Key` retornaria resposta original em vez de 409. Backlog.
- **Audit drill-down em AdminFundosController** — `GET /admin/fundos/{id}/audit-history` agregando AdminAuditLog entries filtrados por entity_id + tipo. Decisao D-8 manteve admin apenas List. Backlog.
- **Detail-by-id em AdminFundosController** — `GET /admin/fundos/{id}` mostrando entity completa cross-company. Decisao D-8 manteve apenas List. Backlog.

## De Phase 54 (/jdi-discuss backend-csharp-quality-audit, 2026-05-30)

- **Split completo do `FundosController`** (god class 1100 LoC, warning W2 de Phase 48) — D-53 só faz extração segura sem mexer em rotas. Quebrar em múltiplos controllers / feature-folders (com regressão Playwright pesada) fica como sub-phase dedicada.
- **Re-negociar D-49 (cobertura 80% retroativa) se o esforço estourar** — 18.3k LoC src com cobertura legada desconhecida pode dominar a phase. Se inviável, candidato a tier por camada (Domain+Application estritos, Infra+API via integration) ou voltar ao D-2. Decisão durante `/jdi-plan` se a baseline mostrar esforço proibitivo.
- **Migrations EF no denominador de cobertura** — D-49 assume Migrations excluídas (geradas, não autorais). Confirmar com usuário; se incluídas, exige integration tests específicos ou exclusão por atributo `[ExcludeFromCodeCoverage]`.
- **Frontend quality audit (espelho de Phase 54 no client/backoffice SPAs)** — esta phase é backend-only. Auditoria equivalente de qualidade nos 2 SPAs React fica para phase futura.
- ~~Re-negociar D-49 / Migrations no denominador~~ **RESOLVIDO 2026-05-30 (D-56):** D-49 mantido literal sem tier; `[ExcludeFromCodeCoverage]` removido dos repos EF + InMemory; Migrations EF = única exclusão. Verificação integration/Playwright via main thread.
- **Repos EF: `[ExcludeFromCodeCoverage]` removido + InMemory tests (D-56)** — trade-off aceito: InMemory é menos fiel que Testcontainers. Se algum repo tiver lógica SQL-específica não exercível em InMemory, manter Integration.Tests como cobertura primária e documentar no WARNINGS.
