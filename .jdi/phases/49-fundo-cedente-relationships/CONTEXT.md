# Phase 50 — fundo-cedente-relationships — CONTEXT

## Goal

Modelar e expor as três associações N-N do módulo Fundos como aggregates de relacionamento simétricos com payload completo (limites + janela de datas + status):

- **Fundo ↔ Cedente** (com `LimiteExposicaoPercentual`/`LimiteExposicaoValor`)
- **Cedente ↔ TipoAtivo**
- **Fundo ↔ TipoAtivo**

Cumprir REL-09 (uma única associação ATIVA por par Fundo-Cedente) com defesa em profundidade. Manter consistência com o pattern de state-machine + AdminAuditLog estabelecido em Phase 48 (D-9). Preservar isolamento multi-tenant (D-5) via `Fundo.ClienteId` / `Cedente.ClienteId` (já HasQueryFilter nos aggregates pais).

## Locked decisions (Phase 50)

- **D-18 (DA-1):** REL-09 enforced em duas camadas — partial unique index Postgres `(FundoId, CedenteId) WHERE Status='ATIVO'` + invariante de domínio no aggregate de associação. DB é o gate autoritativo contra race condition; domínio lança exception tipada para erro de negócio em fluxo feliz. Trade-off aceito: erro retornado é `DuplicateActiveAssociationException` no fluxo normal, `DbUpdateException` traduzido para 409 no race.

- **D-19 (DA-2):** Status da associação é coluna explícita `Status` com enum `ATIVO`/`INATIVO`/`HISTORICO`. NÃO é derivado da janela de datas. Janela de datas registra vigência declarada; Status registra estado lógico atual. REL-09 partial index usa `WHERE Status='ATIVO'` (sem cálculo temporal no índice). Reviewer pode flagar drift entre Status e janela se semanticamente inconsistente, mas não bloqueia.

- **D-20 (DA-3):** Janela de datas é half-open `[data_inicio, data_fim)` com `data_fim` nullable (NULL = vigência infinita). `data_inicio` obrigatório. Status=`ATIVO` semanticamente compatível com `data_fim IS NULL` ou `data_fim > NOW()`, mas não é enforced pelo DB — apenas pelo domínio em operações de status transition (validação no aggregate ao mover para ATIVO).

- **D-21 (DA-4):** Todas três associações (Fundo↔Cedente, Cedente↔TipoAtivo, Fundo↔TipoAtivo) compartilham o mesmo shape: payload com limites + janela de datas + Status enum. Três aggregates de associação simétricos, três controllers, três sets de migrations. Custo de código maior compensado por uniformidade arquitetural e auditabilidade idêntica. Reviewer recusa shortcuts que reduzam um dos aggregates a "tag simples".

- **D-22 (DA-5):** Mudança de Status via state-machine action no padrão Phase 48 D-9: `POST /api/fundos/{fundoId}/cedentes/{cedenteId}/status` com body `{ NewStatus }`. ActorSub/ActorEmail capturados do JWT. AdminAuditLog registra a transição automaticamente. Transições válidas validadas em invariante de domínio (ex: HISTORICO é terminal). Mesmo pattern aplicado a `POST /api/fundos/{fundoId}/tipos-ativos/{tipoAtivoId}/status` e `POST /api/cedentes/{cedenteId}/tipos-ativos/{tipoAtivoId}/status`.

## Canonical refs

- `.jdi/DECISIONS.md` D-5 (multi-tenant isolation), D-9 (Phase 48 state-machine pattern), D-10 (Cedente uniqueness company-scoped).
- `.jdi/phases/48-api-permissions/CONTEXT.md` — precedent para state-machine action em `POST /api/fundos/{id}/status`.
- `src/Onboarding.Domain/Aggregates/Fundo/` — aggregate pai.
- `src/Onboarding.Domain/Aggregates/Cedente/` — aggregate pai.
- `src/Onboarding.Domain/Aggregates/TipoAtivo/` — aggregate global (não company-scoped per D-5).
- `src/Onboarding.Infrastructure/Persistence/Migrations/` — Postgres migrations base.

## Out of scope

- Regra de obrigatoriedade `LimiteExposicaoPercentual` vs `LimiteExposicaoValor` (XOR vs ambos opcionais vs ambos obrigatórios) — **defer to /jdi-plan**: planner propõe regra padrão com FluentValidation, reviewer aprova ou pede ajuste. Não bloqueia o plan.
- Frontend UI das relações (forms, listagens, filtros) — coberto em Phase 51 (`frontend-client-fundos`) e Phase 52 (`frontend-backoffice-fundos`).
- Imports em massa / bulk operations — out of scope; CRUD individual apenas.
- Webhooks ou notificações em mudança de Status — backlog futuro.
- ETL de dados históricos de Cedente↔TipoAtivo regulatório CVM — fora do escopo desta phase.

## Notes

- Estrutura sugerida para o planner: três aggregates novos em `src/Onboarding.Domain/Aggregates/` (`FundoCedente`, `CedenteTipoAtivo`, `FundoTipoAtivo`), três pares Command/Handler para Create/UpdateLimite/StatusTransition, três controllers, migrations Postgres incluindo as 3 partial unique indexes (apenas FundoCedente tem REL-09 strict; verificar se Cedente↔TipoAtivo e Fundo↔TipoAtivo precisam de constraint análoga ou não — decisão do planner).
- TipoAtivo é catálogo global (D-5); associações Cedente↔TipoAtivo e Fundo↔TipoAtivo NÃO precisam de re-check de tenant porque o aggregate company-scoped já é o Cedente/Fundo.
- Domain exceptions tipadas seguindo padrão existente (`Onboarding.Domain.Exceptions.DuplicateActiveAssociationException`, `InvalidStatusTransitionException`).
- AdminAuditLog: reusar o mecanismo já existente do Phase 48 (não criar entry-point novo).
- Integration test cobre cross-tenant guard nas 3 associações análogo ao guard de FundosController Phase 48 iter 2 (commit `eb5bc24`).
