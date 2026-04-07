---
phase: 07-frontend-foundation
plan: "01"
subsystem: ui
tags: [react, vinxi, tailwindcss, shadcn-ui, typescript, vite]

# Dependency graph
requires:
  - phase: 07-00
    provides: "Scaffold Vinxi SPA básico com React 19 e estrutura de diretórios frontend/"

provides:
  - "app.config.ts com plugins @vitejs/plugin-react + @tailwindcss/vite integrados ao router SPA"
  - "Alias @/* resolvido tanto no Vite (runtime) quanto no TypeScript (compilação)"
  - "src/globals.css com @import tailwindcss e variáveis CSS oklch do tema neutral shadcn/ui"
  - "src/lib/utils.ts com cn() usando clsx + tailwind-merge"
  - "components.json configurado para CLI shadcn/ui (rsc=false, Tailwind v4, baseColor=neutral)"
  - "src/main.tsx como entry point com import de globals.css e classes Tailwind ativas"
  - "src/components/ui/ com button, input, label e card gerados pelo CLI shadcn"
  - "Dependências instaladas: react-hook-form, zod, @hookform/resolvers, @tanstack/react-router"

affects:
  - "07-02 (TanStack Router) — RouterProvider entrará em main.tsx"
  - "07-03 (React Hook Form + Zod) — usa componentes ui/ e cn() de utils.ts"

# Tech tracking
tech-stack:
  added:
    - "@vitejs/plugin-react"
    - "tailwindcss (v4)"
    - "@tailwindcss/vite"
    - "tw-animate-css"
    - "clsx"
    - "tailwind-merge"
    - "class-variance-authority"
    - "lucide-react"
    - "@tanstack/react-router"
    - "react-hook-form"
    - "zod"
    - "@hookform/resolvers"
  patterns:
    - "plugins: () => [react(), tailwindcss()] dentro do router SPA no app.config.ts (não em vite.config.ts separado)"
    - "Alias @/* em dois lugares: vite.resolve.alias (runtime) + tsconfig.json paths (compilação)"
    - "shadcn/ui inicializado manualmente (sem CLI interativo) para compatibilidade com Docker/CI"
    - "Tailwind v4 sem tailwind.config.js — configuração apenas via CSS (components.json config='')"

key-files:
  created:
    - "frontend/src/globals.css"
    - "frontend/src/lib/utils.ts"
    - "frontend/src/main.tsx"
    - "frontend/components.json"
    - "frontend/src/components/ui/button.tsx"
    - "frontend/src/components/ui/input.tsx"
    - "frontend/src/components/ui/label.tsx"
    - "frontend/src/components/ui/card.tsx"
  modified:
    - "frontend/app.config.ts"
    - "frontend/tsconfig.json"
    - "frontend/index.html"
    - "frontend/package.json"

key-decisions:
  - "plugins: () => [react(), tailwindcss()] no router SPA do app.config.ts — vite.config.ts paralelo causaria conflito com Vinxi (Pitfall 2 da pesquisa)"
  - "shadcn/ui inicializado manualmente via Write/CLI não-interativo — evita falha em Docker/CI onde stdin não é TTY (Pitfall 4)"
  - "Tailwind v4 usa oklch em vez de hsl nas variáveis CSS do tema neutral — sem tailwind.config.js"
  - "client.tsx mantido (não excluído) — index.html atualizado para apontar para main.tsx"

patterns-established:
  - "Alias @/*: sempre em dois lugares (vite resolve.alias + tsconfig paths) para consistência runtime/compilação"
  - "CSS global com @import tailwindcss obrigatório no grafo de módulos — sem ele Tailwind v4 não gera estilos"
  - "components.json com rsc=false e config='' marca o projeto como SPA + Tailwind v4"

requirements-completed:
  - FRONT-02
  - FRONT-05

# Metrics
duration: 20min
completed: "2026-04-07"
---

# Phase 07 Plan 01: Frontend Foundation — shadcn/ui + Tailwind v4 Summary

**Vinxi SPA migrado para stack completa: @vitejs/plugin-react + Tailwind CSS v4 via @tailwindcss/vite, shadcn/ui inicializado com tema neutral oklch, alias @/* ativo em runtime e TypeScript, componentes ui/button+input+label+card gerados**

## Performance

- **Duration:** ~20 min
- **Started:** 2026-04-07T11:10:00Z
- **Completed:** 2026-04-07T11:31:55Z
- **Tasks:** 2
- **Files modified:** 12

## Accomplishments

- app.config.ts estendido com plugins React + Tailwind v4 no router SPA Vinxi, com alias @/* via fileURLToPath
- shadcn/ui inicializado manualmente: globals.css com variáveis oklch, utils.ts com cn(), components.json configurado para Tailwind v4 SPA
- Componentes ui base adicionados via CLI (button, input, label, card) prontos para as waves seguintes
- Dependências de forms/routing instaladas: react-hook-form, zod, @hookform/resolvers, @tanstack/react-router

## Task Commits

Cada task foi commitada atomicamente:

1. **Task 1: Instalar dependências e atualizar app.config.ts com plugins React + Tailwind v4** - `df48573` (feat)
2. **Task 2: Configurar shadcn/ui, criar globals.css e migrar entry point para main.tsx** - `5c07623` (feat)

## Files Created/Modified

- `frontend/app.config.ts` — plugins React + Tailwind v4 dentro do router SPA, alias @/* com fileURLToPath
- `frontend/tsconfig.json` — paths @/*→./src/*, DOM.Iterable, baseUrl "."
- `frontend/index.html` — script src atualizado para main.tsx
- `frontend/package.json` — dependências adicionadas (@vitejs/plugin-react, tailwindcss, @tailwindcss/vite, etc.)
- `frontend/src/globals.css` — @import tailwindcss + variáveis CSS oklch do tema neutral shadcn/ui
- `frontend/src/lib/utils.ts` — cn() usando clsx + tailwind-merge
- `frontend/components.json` — configuração CLI shadcn/ui (rsc=false, config='', baseColor=neutral)
- `frontend/src/main.tsx` — entry point com import @/globals.css e layout Tailwind
- `frontend/src/components/ui/button.tsx` — gerado por npx shadcn@latest add
- `frontend/src/components/ui/input.tsx` — gerado por npx shadcn@latest add
- `frontend/src/components/ui/label.tsx` — gerado por npx shadcn@latest add
- `frontend/src/components/ui/card.tsx` — gerado por npx shadcn@latest add

## Decisions Made

- `plugins: () => [react(), tailwindcss()]` dentro do router SPA no app.config.ts — vite.config.ts paralelo causa conflito com Vinxi (documentado como Pitfall 2 na pesquisa)
- shadcn/ui inicializado manualmente (sem CLI interativo) para compatibilidade com Docker/CI — `npx shadcn@latest add` com flag `--yes` foi usado apenas para geração de componentes
- Tailwind v4 usa `oklch` em vez de `hsl` nas variáveis CSS — `components.json` com `config: ""` indica ausência de tailwind.config.js
- `src/client.tsx` mantido sem alteração — apenas `index.html` foi atualizado para usar `main.tsx`

## Deviations from Plan

Nenhuma — plano executado exatamente como escrito.

## Issues Encountered

Nenhum.

## User Setup Required

Nenhum — nenhuma configuração externa necessária.

## Next Phase Readiness

- Wave 2 completa: base técnica pronta para waves seguintes
- 07-02 (TanStack Router): pode adicionar RouterProvider em main.tsx — dependências já instaladas
- 07-03 (React Hook Form + Zod): pode usar componentes ui/ e cn() — dependências já instaladas
- `npm run dev` inicia o servidor Vinxi SPA na porta 5173 com Tailwind v4 e shadcn/ui ativos

## Self-Check: PASSED

Todos os arquivos criados verificados. Ambos os commits existem:
- `df48573` (Task 1: plugins React + Tailwind v4 + tsconfig)
- `5c07623` (Task 2: shadcn/ui + globals.css + main.tsx + componentes ui)

---
*Phase: 07-frontend-foundation*
*Completed: 2026-04-07*
