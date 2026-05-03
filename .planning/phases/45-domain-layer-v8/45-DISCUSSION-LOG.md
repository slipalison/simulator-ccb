# Phase 45: Domain Layer v8.0 - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-05-03
**Phase:** 45-domain-layer-v8
**Areas discussed:** Cedente polymorphic, FundoStatus state machine, FundoCedente join entity

---

## Cedente Polymorphic

| Option | Description | Selected |
|--------|-------------|----------|
| Single aggregate (PF+PJ) | Cedente como entity único com CPF/CNPJ condicionais | ✓ |
| Two separate entities | CedentePf e CedentePj como aggregates separados | |

**User's choice:** Single aggregate para PF e PJ
**Notes:** Usuário confirmou que Cedente pode ser um aggregate só

### CedenteDocumento VO

| Option | Description | Selected |
|--------|-------------|----------|
| Discriminated union VO | CedenteDocumento com .Pf(Cpf) e .Pj(Cnpj) — type-safe, zero null risk | ✓ |
| TipoPessoa enum + factory methods | CreatePf() e CreatePj() com validação condicional | |
| Claude decide | Seguir pattern mais simples (opção 2) | |

**User's choice:** Discriminated union VO
**Notes:** Preferência por pattern functional, type-safe, sem null risk

---

## FundoStatus State Machine

| Option | Description | Selected |
|--------|-------------|----------|
| Enum + TransitionTo() no Fundo | Todas transições centralizadas no aggregate. YAGNI para State Pattern | ✓ |
| State Pattern com classes | 5+ classes para estados, transições delegadas | |

**User's choice:** Enum + TransitionTo() (deferred to SOLID+DDD analysis)
**Notes:** Claude decidiu baseado em SOLID (SRP: transições visíveis num lugar) e DDD (aggregate enforce próprio invariante). YAGNI — 5 estados não justificam State Pattern.

---

## FundoCedente Join Entity

| Option | Description | Selected |
|--------|-------------|----------|
| Entity dentro aggregate Fundo | Fundo gerencia coleção de FundoCedentes. REL-09 enforce inline | ✓ |
| Aggregate root separado | FundoCedente com repository próprio. Invariante via domain service | |

**User's choice:** Entity dentro aggregate Fundo (deferred to SOLID+DDD analysis)
**Notes:** Claude decidiu baseado em DDD (invariante sobre coleção = mesmo aggregate) e consistência transactional. Pattern consistente com Employee/CompanyId (Guid FK sem navigation).

---

## Claude's Discretion

- Estrutura de pastas dos novos aggregates
- Nomes exatos de propriedades e métodos
- Details de FundoCedente payload
- Ordem e nomes dos enums
- Implementação interna de CedenteDocumento
- Nomes de novos ActionType enum values

## Deferred Ideas

None — discussão ficou dentro do escopo da fase.