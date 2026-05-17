# Phase 50 — fundo-cedente-relationships — SUMMARY

## Status
Executed via /jdi-loop iter 1 — all 7 tasks completed.

## Tasks completed

| Task | Commit | Notes |
|---|---|---|
| T-1 | `9da648e` | Domain aggregates + VOs + exceptions + repo interfaces + domain tests |
| T-2 | `f9c4f4f` | EF configs + 3 repo impls + partial unique indexes + migration |
| T-3 | `99ba5ec` | FundoCedente application layer (commands + query + DTO + handler tests) |
| T-4 | `e44ac89` | CedenteTipoAtivo + FundoTipoAtivo application layer (symmetric) |
| T-5 | `a90baab` | API controllers + DI registrations (3 controllers, 3 test files) |
| T-6 | `777b331` | AdminFundosController relationship extensions (D-8, IgnoreQueryFilters) |
| T-7 | (pending) | 21 integration scenarios + DI validator fix |

## Task-level design decisions

### T-1 — LimiteExposicao rule
Adopted "at least one of Percentual or Valor required" per PLAN. LimiteExposicao.Create(null, null) throws ArgumentException. Both are optional individually but the pair is not. Reviewer may adjust.

### T-2 — Partial unique indexes
Three partial unique indexes (symmetric D-21):
- (FundoId, CedenteId) WHERE Status='ATIVO' — REL-09 (D-18)
- (CedenteId, TipoAtivoId) WHERE Status='ATIVO' — symmetry
- (FundoId, TipoAtivoId) WHERE Status='ATIVO' — symmetry
HasQueryFilter NOT applied — tenant scoping via parent aggregate.

### T-3 — REL-09 in-memory guard
ActivateGuard(existsActiveForPair) called before save. DuplicateActiveAssociationException → 409.
DB partial index is authoritative race gate (GlobalExceptionHandler maps DbUpdateException → 409).

### T-5 — DI validator registrations
Validators were missing from DI. Fixed pre-staged DependencyInjection.cs committed with T-7.

### T-7 — Race condition test
FundoCedente_ConcurrentCreate_OnlyOneSucceeds: two concurrent POSTs for same (FundoId, CedenteId).
Expected: exactly one 201 + one 409. Dedicated _cedenteConcurrentId (CPF 40484604805) seeded in
InitializeAsync to isolate from other tests. DB partial index rejects second insert → GlobalExceptionHandler → 409.

### T-7 (2026-05-17)
