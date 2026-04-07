---
phase: 07-frontend-foundation
plan: "03"
subsystem: ui
tags: [react, react-hook-form, zod, tanstack-router, vitest, tdd, validation]

requires:
  - phase: 07-02
    provides: "Componentes Atomic Design + TanStack Router configurado + stubs RED"

provides:
  - "ExampleForm com RHF v7 + Zod v4 + erros inline — zodResolver conectado, errors.name e errors.email passados para LabeledField"
  - "11 testes GREEN: smoke (1), atomic-structure (5), routing (2), form-validation (3)"
  - "router.ts renomeado para router.tsx — JSX requer extensão .tsx para oxc/Vite"
  - "setup.ts com window.scrollTo stub — suprime warning de jsdom para TanStack Router"

affects:
  - "Phase 8 (registration-api frontend) — ExampleForm pode ser substituído por RegisterForm com campos PF/PJ"

tech-stack:
  added: []
  patterns:
    - "zodResolver(@hookform/resolvers/zod) conecta Zod v4 ao RHF v7 — API compatível com resolvers v5"
    - "noValidate no form desativa validação nativa do browser, deixando Zod validar"
    - "TanStack Router assíncrono em jsdom requer testRouter.load() + waitFor nas asserções"
    - "Arquivos com JSX devem ter extensão .tsx — oxc (usado pelo Vite/Vitest) rejeita JSX em .ts"

key-files:
  created:
    - frontend/src/router.tsx
  modified:
    - frontend/src/components/organisms/ExampleForm.tsx
    - frontend/src/tests/smoke.test.tsx
    - frontend/src/tests/atomic-structure.test.ts
    - frontend/src/tests/routing.test.tsx
    - frontend/src/tests/form-validation.test.tsx
    - frontend/src/tests/setup.ts

key-decisions:
  - "router.ts renomeado para router.tsx — oxc não processa JSX em .ts; todos os imports existentes funcionam sem alteração (TypeScript resolve .tsx automaticamente)"
  - "getAllByRole('alert') no lugar de getByRole — submit sem dados dispara erros em name e email simultaneamente"
  - "testRouter.load() antes das asserções de routing — TanStack Router resolve navegação assincronamente"

requirements-completed:
  - FRONT-01
  - FRONT-02
  - FRONT-03
  - FRONT-04
  - FRONT-05

duration: 15min
completed: "2026-04-07"
---

# Phase 07 Plan 03: RHF + Zod wiring + TDD ciclo GREEN Summary

**ExampleForm com React Hook Form v7 + Zod v4 + zodResolver, erros inline por campo, e 11/11 testes convertidos de RED para GREEN completando o ciclo TDD da Phase 7**

## Performance

- **Duration:** ~15 min
- **Started:** 2026-04-07
- **Completed:** 2026-04-07
- **Tasks:** 2
- **Files modified:** 7 (1 criado via rename)

## Accomplishments

- ExampleForm substituído com `useForm<ExampleFormData>({ resolver: zodResolver(exampleSchema) })` — schema Zod v4 com mensagens PT-BR
- `errors.name?.message` e `errors.email?.message` passados para LabeledField que exibe `<p role="alert">` inline
- `noValidate` no form desativa validação nativa do browser
- `onSubmit` no-op (integração de API em Phase 8)
- smoke.test.tsx: `render(<RouterProvider router={router} />)` sem exceção — GREEN
- atomic-structure.test.ts: verificação de filesystem com `existsSync` para 5 diretórios — GREEN
- routing.test.tsx: `testRouter.load()` + `waitFor` para renderização assíncrona do TanStack Router — GREEN
- form-validation.test.tsx: `getAllByRole('alert')`, spy de fetch, verificação de classes Tailwind — GREEN
- setup.ts: `window.scrollTo` stub adicionado para suprimir warning do jsdom
- router.ts renomeado para router.tsx (fix bloqueador — oxc rejeita JSX em .ts)

## Task Commits

1. **Task 1: ExampleForm com RHF + Zod** — `4102e4e` (feat)
2. **Task 2: Converter stubs RED em GREEN** — `0b98948` (feat)

## Files Created/Modified

- `frontend/src/components/organisms/ExampleForm.tsx` — useForm + zodResolver + errors inline passados ao LabeledField
- `frontend/src/router.tsx` — renomeado de .ts para .tsx (JSX <Outlet /> requer .tsx)
- `frontend/src/tests/smoke.test.tsx` — render(RouterProvider) sem exceção
- `frontend/src/tests/atomic-structure.test.ts` — existsSync para 5 componentes Atomic Design
- `frontend/src/tests/routing.test.tsx` — testRouter.load() + waitFor para 404 assíncrono
- `frontend/src/tests/form-validation.test.tsx` — erros inline RHF+Zod, spy fetch, classes Tailwind
- `frontend/src/tests/setup.ts` — window.scrollTo stub para jsdom

## Decisions Made

- `router.ts` renomeado para `router.tsx` — oxc (bundler do Vite) rejeita JSX em arquivos `.ts`; todos os imports existentes funcionam sem alteração porque TypeScript resolve `.tsx` automaticamente pelo alias `@/router`
- `getAllByRole('alert')` no lugar de `getByRole` — quando todos os campos são submetidos vazios, tanto `name` quanto `email` geram alertas simultaneamente
- `testRouter.load()` chamado antes das asserções de routing — TanStack Router resolve a navegação de forma assíncrona e o DOM fica vazio até o `load()` completar

## Deviations from Plan

### Auto-fixed Issues

**1. [Regra 1 - Bug] router.ts renomeado para router.tsx**
- **Encontrado durante:** Task 2 — testes de smoke e routing falhavam com `PARSE_ERROR: Expected > but found /`
- **Problema:** oxc (usado pelo Vite/Vitest internamente) não processa JSX em arquivos `.ts`; `router.ts` usa `<Outlet />` inline
- **Correção:** Arquivo renomeado de `router.ts` para `router.tsx`; nenhum import precisou ser alterado
- **Arquivos modificados:** `frontend/src/router.tsx` (novo), `frontend/src/router.ts` (deletado)
- **Commit:** `0b98948`

**2. [Regra 1 - Bug] getByRole substituído por getAllByRole no form-validation.test.tsx**
- **Encontrado durante:** Task 2 — primeiro teste de form-validation falhava com "Found multiple elements with the role alert"
- **Problema:** Submit sem dados dispara erros em `name` E `email` simultaneamente, gerando 2 elementos `role=alert`
- **Correção:** `getByRole("alert")` substituído por `getAllByRole("alert").length > 0`
- **Arquivos modificados:** `frontend/src/tests/form-validation.test.tsx`
- **Commit:** `0b98948`

**3. [Regra 1 - Bug] testRouter.load() adicionado antes das asserções de routing**
- **Encontrado durante:** Task 2 — testes de routing encontravam DOM vazio `<div />`
- **Problema:** TanStack Router é assíncrono; `render()` retorna antes da navegação ser resolvida
- **Correção:** `await testRouter.load()` adicionado em `renderWithUnknownRoute()` e asserções envolvidas em `waitFor`
- **Arquivos modificados:** `frontend/src/tests/routing.test.tsx`
- **Commit:** `0b98948`

## Known Stubs

Nenhum stub permanece — todos os 11 testes passam. ExampleForm tem validação real conectada via `zodResolver`.

## Threat Surface Scan

Nenhuma superfície nova além do registrado no threat model do plano:
- T-07-03-01: `zodResolver` valida todos os inputs antes de `onSubmit` — dados inválidos nunca chegam ao handler (MITIGADO)
- T-07-03-02: Mensagens de erro são genéricas ("Email inválido", "Nome é obrigatório") — sem information leakage (ACEITO)
- T-07-03-03: Submit repetido sem rate limiting — intencional nesta phase, Phase 8+ adicionará proteção (ACEITO)

## Self-Check: PASSED

Arquivos verificados:
- `frontend/src/components/organisms/ExampleForm.tsx`: FOUND
- `frontend/src/router.tsx`: FOUND
- `frontend/src/tests/smoke.test.tsx`: FOUND
- `frontend/src/tests/atomic-structure.test.ts`: FOUND
- `frontend/src/tests/routing.test.tsx`: FOUND
- `frontend/src/tests/form-validation.test.tsx`: FOUND
- `frontend/src/tests/setup.ts`: FOUND

Commits verificados:
- `4102e4e`: feat(07-03): implementar RHF + Zod no ExampleForm com erros inline
- `0b98948`: feat(07-03): converter stubs RED em GREEN — 11/11 testes passando

Suite de testes: 11/11 passando, 0 falhas.

---
*Phase: 07-frontend-foundation*
*Completed: 2026-04-07*
