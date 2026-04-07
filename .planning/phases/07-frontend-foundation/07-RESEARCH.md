# Phase 7: Frontend Foundation - Research

**Researched:** 2026-04-06
**Domain:** cloudflare/vinext (SSR, Next.js-style) + shadcn/ui + Tailwind v4 + TanStack Router + React Hook Form + Zod
**Confidence:** MEDIUM — vinext é experimental (lançado fev/2026, "not battle-tested"), TanStack Router HIGH, RHF+Zod HIGH

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- **Framework:** cloudflare/vinext — https://github.com/cloudflare/vinext
  - CONFIRMADO pelo usuário (2026-04-06): substituir scaffold Vinxi existente por vinext
  - CONFIRMADO pelo usuário (2026-04-06): usar vinext com SSR — aceito explicitamente
  - vinext reimplementa as convenções Next.js sobre Vite; usar seu scaffold e suas convenções de arquivo
  - NÃO fazer fallback para Vinxi puro — o usuário confirmou vinext+SSR
  - O scaffold Vinxi da Fase 1 será substituído/migrado
- **Design system:** shadcn/ui — https://github.com/shadcn-ui/ui
  - Instalar via `npx shadcn@latest add` — não copiar/colar manualmente
  - Estilo simples e minimalista — usar defaults do shadcn, evitar overrides de tema desnecessários
- **Tailwind CSS v4** (obrigatório pelo shadcn/ui v2+)
  - Sem tokens de cor customizados
  - Utility-first; sem CSS modules ou styled-components
- **TanStack Router v1** — roteamento type-safe
  - Caminhos desconhecidos devem renderizar componente 404 tipado via notFoundComponent
  - Rotas desta fase: `/` (home/landing placeholder), `*` (404)
- **React Hook Form v7 + Zod v3** — validação inline
  - Erros exibidos inline sob os campos (não toast/alert)
  - Um formulário de exemplo no nível molecule/organism
- **Atomic Design:** `src/components/` → `atoms/`, `molecules/`, `organisms/`, `templates/`, `pages/`

### Claude's Discretion
- Comando exato de init do vinext e estrutura do config file
- File-based routing (convenção vinext/Next.js) vs manual route tree para TanStack Router
- Detalhes de configuração do shadcn/ui init (baseColor, cssVariables, etc.)
- Setup de path aliases TypeScript (`@/components`, `@/lib`, etc.)
- Dockerfile / compose wiring para o serviço frontend (deve espelhar padrão da Fase 1)

### Deferred Ideas (OUT OF SCOPE)
- Formulários completos de cadastro/login — Fases 8 e 9
- Integração com API — Fase 8+
- Gerenciamento de estado de autenticação / tokens — Fase 9
</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Descrição | Suporte da Pesquisa |
|----|-----------|---------------------|
| FRONT-01 | Atomic Design — atoms, molecules, organisms, templates, pages | Estrutura de diretórios `src/components/` documentada; padrão independente de framework |
| FRONT-02 | Vinxi configurado em SPA mode → **ATUALIZADO para vinext SSR** | vinext usa pages/ ou app/ router no estilo Next.js; sem SPA mode — SSR por padrão |
| FRONT-03 | TanStack Router para rotas type-safe | TanStack Router v1.168.x com code-based routing; notFoundComponent para 404 |
| FRONT-04 | React Hook Form + Zod para validação de formulários | RHF v7.72.x + Zod v4.3.x; zodResolver; padrão "use client" em contexto SSR |
| FRONT-05 | Tailwind CSS para estilização | Tailwind v4.2.x + @tailwindcss/vite; shadcn/ui v4.1.x como sistema de design |
</phase_requirements>

---

## Summary

cloudflare/vinext (lançado fevereiro 2026) reimplementa a superfície de API do Next.js sobre o Vite. É **SSR por padrão**, usa convenções de arquivo Next.js (`pages/` ou `app/` directory), e seu deployment primário é Cloudflare Workers — com suporte emergente para Node.js standalone (`node dist/standalone/server.js`). O projeto está explicitamente marcado como **experimental**: "under heavy development", "not battle-tested", "use at your own risk."

**Conflito arquitetural crítico identificado:** vinext possui seu próprio sistema de roteamento baseado em arquivos (Next.js-style). TanStack Router é um roteador client-side independente que gerencia sua própria árvore de rotas. Usados juntos, o sistema de rotas do vinext tentaria capturar as requisições antes do TanStack Router. Para implementar TanStack Router em vinext, é necessário criar uma única página vinext que delega todo o rendering ao RouterProvider do TanStack Router (padrão SPA-within-SSR shell).

Para um ambiente Docker local (não Cloudflare Workers), o vinext suporta saída standalone: configurar `output: "standalone"` no `next.config.ts` e iniciar com `node dist/standalone/server.js` na porta 3000 (variável `PORT`).

**Recomendação primária:** Usar vinext com Pages Router (`pages/`) — mais simples que App Router para esta fase. Uma página catch-all (`pages/[...slug].tsx` ou `pages/404.tsx`) renderiza o shell da aplicação. TanStack Router gerencia o roteamento client-side completo dentro desse shell. O RHF + Zod requerem componentes "use client" em contexto SSR.

## Standard Stack

### Core
| Biblioteca | Versão | Propósito | Por que padrão |
|-----------|--------|-----------|----------------|
| vinext | 0.0.39 | Framework SSR Next.js-style sobre Vite | Escolha do usuário — confirmada |
| react | 19.2.4 (existente) | UI library | Já instalado no projeto |
| react-dom | 19.2.4 (existente) | DOM renderer | Já instalado |
| @vitejs/plugin-react | 6.0.1 | Plugin React para Vite | Requerido pelo vinext |
| tailwindcss | 4.2.2 | Utility-first CSS | Exigido pelo shadcn/ui v2+ |
| @tailwindcss/vite | 4.2.2 | Plugin Vite para Tailwind v4 | Substitui PostCSS em Tailwind v4 |
| shadcn (CLI) | 4.1.2 | Design system + CLI para adicionar componentes | Escolha do usuário |
| @tanstack/react-router | 1.168.10 | Roteamento client-side type-safe | Escolha do usuário |
| react-hook-form | 7.72.1 | Gerenciamento de estado de formulários | Escolha do usuário; zero re-renders |
| zod | 4.3.6 | Schema validation | Escolha do usuário; integra com RHF via @hookform/resolvers |
| @hookform/resolvers | latest | Bridge entre RHF e Zod | Obrigatório para zodResolver |

### Supporting (Testes)
| Biblioteca | Versão | Propósito | Quando Usar |
|-----------|--------|-----------|-------------|
| vitest | 4.1.2 | Test runner nativo para Vite | Todos os testes unitários/componentes |
| @testing-library/react | 16.3.2 | Teste de componentes React | Renderização e interação com componentes |
| @testing-library/jest-dom | 6.9.1 | Matchers DOM para Vitest | Assertions como `.toBeInTheDocument()` |
| jsdom | latest | DOM simulado para Node.js | Ambiente de teste para componentes |

### Alternatives Considered
| Em vez de | Poderia usar | Tradeoff |
|-----------|--------------|---------|
| vinext (SSR) | Vinxi SPA puro | Vinxi SPA já está instalado e funciona — mas usuário confirmou vinext+SSR |
| TanStack Router code-based | File-based routing do vinext (Next.js-style) | Os dois sistemas de roteamento conflitam — deve-se escolher um; TanStack Router é choice do usuário |
| shadcn/ui | Material UI, Ant Design | shadcn/ui é "copy-owned" — componentes ficam no repo, sem black-box; melhor para customização |
| Zod v4 | Zod v3 | CONTEXT.md menciona "Zod v3" mas npm registry retorna v4.3.6; veja Open Questions |

**Instalação:**
```bash
# Remover vinxi e instalar vinext + deps adicionais
npm uninstall vinxi
npm install vinext @vitejs/plugin-react
npm install @tanstack/react-router react-hook-form zod @hookform/resolvers
npm install tailwindcss @tailwindcss/vite

# shadcn/ui init (interativo)
npx shadcn@latest init -t vite

# Adicionar componentes shadcn individualmente
npx shadcn@latest add button input label card

# Teste
npm install -D vitest @testing-library/react @testing-library/jest-dom jsdom
```

**Verificação de versões:** [VERIFIED: npm registry 2026-04-06]
- vinext: 0.0.39
- @tanstack/react-router: 1.168.10
- react-hook-form: 7.72.1
- zod: 4.3.6
- vitest: 4.1.2
- tailwindcss: 4.2.2

## Architecture Patterns

### Recommended Project Structure

```
frontend/
├── pages/                   # vinext routing (Next.js-style)
│   ├── index.tsx            # Rota raiz "/" → renderiza <AppShell /> com RouterProvider
│   └── _app.tsx             # Wrapper global do vinext (opcional)
├── src/
│   ├── components/          # Atomic Design
│   │   ├── atoms/           # Primitivos: Button, Input wrappers sobre shadcn
│   │   ├── molecules/       # Composições: LabeledField (Label + Input + erro)
│   │   ├── organisms/       # Funcionalidades: ExampleForm (RHF + Zod)
│   │   ├── templates/       # Layouts: PageLayout (header + main + footer slots)
│   │   └── pages/           # Telas concretas: HomePage, NotFoundPage
│   ├── routes/              # Definições TanStack Router (code-based)
│   │   ├── __root.tsx       # createRootRoute com notFoundComponent
│   │   ├── index.tsx        # Rota "/"
│   │   └── router.ts        # createRouter, exporta Router type
│   ├── lib/
│   │   └── utils.ts         # cn() helper (shadcn/ui)
│   └── globals.css          # @import "tailwindcss"; + CSS vars shadcn
├── public/                  # Assets estáticos
├── vite.config.ts           # @vitejs/plugin-react + @tailwindcss/vite + alias @/*
├── next.config.ts           # vinext config (output: "standalone" para Docker)
├── tsconfig.json            # paths: { "@/*": ["./src/*"] }
├── package.json
└── Dockerfile
```

### Pattern 1: vinext com Pages Router (SSR Shell + TanStack Router client-side)

**O quê:** vinext fornece o servidor SSR. A única "página vinext" (`pages/index.tsx`) renderiza o HTML shell e inicializa o TanStack Router para gerenciar toda a navegação client-side.

**Quando usar:** Quando vinext é obrigatório mas o roteamento client-side precisa ser type-safe via TanStack Router.

```typescript
// pages/index.tsx — único ponto de entrada vinext
// Source: [ASSUMED baseado em padrão Next.js + TanStack Router]
'use client'  // Componentes interativos requerem this em RSC/SSR context

import { RouterProvider } from '@tanstack/react-router'
import { router } from '../src/routes/router'

export default function RootPage() {
  return <RouterProvider router={router} />
}
```

```typescript
// src/routes/router.ts
// Source: [VERIFIED: tanstack.com/router/v1/docs]
import { createRouter } from '@tanstack/react-router'
import { routeTree } from './__root'

export const router = createRouter({ routeTree })

// Declaração TypeScript obrigatória
declare module '@tanstack/react-router' {
  interface Register {
    router: typeof router
  }
}
```

```typescript
// src/routes/__root.tsx
// Source: [CITED: tanstack.com/router/v1/docs — not-found-errors guide]
import { createRootRoute, Outlet } from '@tanstack/react-router'
import { NotFoundPage } from '../components/pages/NotFoundPage'

export const rootRoute = createRootRoute({
  component: () => <Outlet />,
  notFoundComponent: () => <NotFoundPage />,
})
```

### Pattern 2: Configuração vinext para Docker (standalone output)

**O quê:** `next.config.ts` com `output: "standalone"` gera bundle autocontido em `dist/standalone/server.js`.

```typescript
// next.config.ts
// Source: [CITED: vinext README — standalone output]
import type { NextConfig } from 'vinext'

const config: NextConfig = {
  output: 'standalone',
}

export default config
```

```dockerfile
# frontend/Dockerfile — prod stage para vinext SSR
# Source: [ASSUMED — baseado em padrão standalone Node.js]
FROM node:22-alpine AS builder
WORKDIR /app
COPY package.json package-lock.json* ./
RUN npm ci
COPY . .
RUN npm run build

FROM node:22-alpine AS runner
WORKDIR /app
COPY --from=builder /app/dist/standalone ./
EXPOSE 3000
ENV PORT=3000
ENV HOST=0.0.0.0
CMD ["node", "server.js"]
```

**ATENÇÃO:** O Dockerfile acima é para produção. Em dev, manter o padrão atual (node:22-alpine + `npm run dev`). O Docker Compose atual usa porta 5173 — para vinext dev server, a porta padrão é 3001 (vinext usa 3001 para não conflitar com Next.js em 3000).

### Pattern 3: shadcn/ui init com Tailwind v4 + vite.config.ts

```typescript
// vite.config.ts
// Source: [CITED: ui.shadcn.com/docs/installation/vite]
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'
import path from 'path'

export default defineConfig({
  plugins: [react(), tailwindcss()],
  resolve: {
    alias: { '@': path.resolve(__dirname, './src') },
  },
  server: {
    host: '0.0.0.0',
    port: 5173,
    watch: {
      usePolling: true,   // Obrigatório no Windows Docker
      interval: 1000,
    },
  },
})
```

```css
/* src/globals.css — Tailwind v4 (sem postcss.config.js) */
/* Source: [CITED: ui.shadcn.com/docs/tailwind-v4] */
@import "tailwindcss";

/* shadcn/ui theme vars geradas pelo `npx shadcn init` */
:root {
  --background: hsl(0 0% 100%);
  /* ... demais vars geradas pelo CLI */
}

@theme inline {
  --color-background: var(--background);
  /* ... */
}
```

### Pattern 4: React Hook Form + Zod com "use client"

```typescript
// src/components/organisms/ExampleForm.tsx
// Source: [ASSUMED baseado em padrão RHF+Zod documentado]
'use client'  // Obrigatório em contexto SSR — RHF usa Context API

import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'

const schema = z.object({
  name: z.string().min(2, 'Nome deve ter ao menos 2 caracteres'),
  email: z.string().email('Email inválido'),
})

type FormData = z.infer<typeof schema>

export function ExampleForm() {
  const { register, handleSubmit, formState: { errors } } = useForm<FormData>({
    resolver: zodResolver(schema),
  })

  return (
    <form onSubmit={handleSubmit((data) => console.log(data))}>
      <div>
        <input {...register('name')} placeholder="Nome" />
        {errors.name && <p className="text-red-500 text-sm">{errors.name.message}</p>}
      </div>
      <div>
        <input {...register('email')} placeholder="Email" />
        {errors.email && <p className="text-red-500 text-sm">{errors.email.message}</p>}
      </div>
      <button type="submit">Enviar</button>
    </form>
  )
}
```

### Anti-Patterns to Avoid

- **Usar `NotFoundRoute` (deprecado):** TanStack Router v1 deprecou `NotFoundRoute` em favor de `notFoundComponent` no `createRootRoute`. Não usar `NotFoundRoute`.
- **Tentar usar file-based routing do vinext E TanStack Router simultaneamente:** São sistemas de roteamento conflitantes. Escolher um. Nesta fase: TanStack Router gerencia tudo client-side dentro de uma página vinext.
- **localStorage para tokens:** Proibido explicitamente no CLAUDE.md (SEC-10) — nunca armazenar JWT.
- **Usar keycloak-js:** Listado como proibido em CLAUDE.md — não instalar.
- **Usar MediatR / FluentAssertions:** Proibidos no CLAUDE.md (backend, mas registrar para evitar confusão).

## Don't Hand-Roll

| Problema | Não construir | Usar em vez disso | Por quê |
|---------|---------------|-------------------|---------|
| Validação de formulário | Lógica de validação custom | React Hook Form + Zod | Gerencia estado, re-renders, erros async, arrays de campos |
| Componentes UI (botões, inputs, cards) | Componentes styled custom | shadcn/ui | Acessibilidade, Radix primitives, Tailwind-first |
| Utility CSS class merging | Função `cx()` custom | `cn()` do shadcn (`clsx` + `tailwind-merge`) | Resolve conflitos de classes Tailwind corretamente |
| Type-safe navigation | Router custom | TanStack Router | Inferência TypeScript de params/search params por toda a app |
| 404 handling | Conditional rendering manual | `notFoundComponent` no `createRootRoute` | Integra com type system do router |

## Common Pitfalls

### Pitfall 1: vinext é experimental e focado em Cloudflare Workers
**O que dá errado:** vinext foi lançado em fevereiro 2026 e está explicitamente marcado como "experimental — under heavy development, use at your own risk." O target primário é Cloudflare Workers; o servidor Node.js standalone (`vinext start`) é descrito como "less complete."
**Por que acontece:** O projeto foi construído em uma semana com IA (Claude Code) e ainda não passou por tráfego de produção real.
**Como evitar:** Usar `vinext dev` para desenvolvimento local. Para Docker, manter o script `dev` com `npm run dev` apontando para `vinext dev`. Não tentar deploy para produção real ainda. Se vinext causar problemas irresolvíveis, o fallback é revert para Vinxi SPA puro.
**Sinais de alerta:** Erros de build inexplicáveis, comportamento SSR inconsistente, incompatibilidades com módulos Node.js nativos no modo dev RSC.

### Pitfall 2: Porta do dev server — vinext usa 3001, não 5173
**O que dá errado:** `vinext dev` por padrão usa porta 3001 (para não conflitar com Next.js em 3000). O Docker Compose atual expõe `5173:5173`. A porta precisa ser configurada explicitamente.
**Por que acontece:** vinext imita o comportamento do Next.js dev server.
**Como evitar:** Configurar `PORT=5173` como variável de ambiente no Dockerfile/compose.yaml, ou passar `vinext dev --port 5173`. Verificar que o `vite.config.ts` também define `server.port: 5173`.
**Sinais de alerta:** `docker compose up` sobe o frontend mas `curl http://localhost:5173/` retorna connection refused.

### Pitfall 3: "use client" é obrigatório para componentes com RHF
**O que dá errado:** React Hook Form usa React Context internamente. Em SSR com React Server Components (RSC), componentes que usam hooks não funcionam no servidor sem `'use client'`.
**Por que acontece:** RSC/SSR executa no servidor onde hooks interativos não existem.
**Como evitar:** Adicionar `'use client'` no topo de qualquer componente que use `useForm`, `useRouter`, estado React, ou event handlers.
**Sinais de alerta:** Erro "You're importing a component that needs useState. It only works in a Client Component but none of its parents are marked with 'use client'."

### Pitfall 4: Tailwind v4 remove PostCSS — não criar postcss.config.js
**O que dá errado:** Tailwind v4 usa o plugin Vite (`@tailwindcss/vite`) em vez de PostCSS. Criar um `postcss.config.js` pode causar conflito ou processamento duplicado.
**Por que acontece:** Tailwind v4 mudou a arquitetura de compilação completamente.
**Como evitar:** Usar apenas `@tailwindcss/vite` no `vite.config.ts`. Não criar `postcss.config.js`. O `globals.css` começa com `@import "tailwindcss";` (não `@tailwind base; @tailwind components;`).
**Sinais de alerta:** Classes Tailwind não sendo aplicadas mesmo após importar o CSS.

### Pitfall 5: Conflito de roteamento vinext vs TanStack Router
**O que dá errado:** vinext intercepta todas as rotas no servidor (`pages/` directory). TanStack Router gerencia rotas no cliente. Se mal configurados, rotas client-side podem não hidratar corretamente após SSR.
**Por que acontece:** São dois sistemas de roteamento com responsabilidades sobrepostas.
**Como evitar:** Usar uma única "página catch-all" no vinext (ex: `pages/index.tsx` ou `pages/[[...slug]].tsx`) que sempre renderiza o `RouterProvider` do TanStack Router. O vinext serve o HTML shell; TanStack Router gerencia tudo depois da hidratação. Configurar o vinext para retornar sempre o mesmo HTML independente do path.
**Sinais de alerta:** Hard refresh em `/sobre` retorna 404 do servidor em vez de renderizar a NotFoundPage do TanStack Router.

### Pitfall 6: HMR no Windows Docker requer usePolling
**O que dá errado:** Alterações em arquivos não atualizam o browser automaticamente no Docker Desktop para Windows.
**Por que acontece:** inotify events não são confiáveis em bind mounts no Windows Docker.
**Como evitar:** Manter `watch: { usePolling: true, interval: 1000 }` no `vite.config.ts` (já presente no scaffold atual).
**Sinais de alerta:** Editar um arquivo e a página não recarregar após alguns segundos.

### Pitfall 7: vinext precisa de next.config.ts (não app.config.ts)
**O que dá errado:** O scaffold atual usa `app.config.ts` (Vinxi). vinext usa `next.config.ts` (padrão Next.js) ou `vite.config.ts`. Manter `app.config.ts` pode causar conflito ou ser ignorado.
**Por que acontece:** Mudança de framework — vinext não usa a API `createApp()` do Vinxi.
**Como evitar:** Remover `app.config.ts`. Criar `next.config.ts` para configurações vinext (ex: `output: 'standalone'`). Configurações Vite vão em `vite.config.ts`.
**Sinais de alerta:** `npm run dev` com vinext não reconhece configurações de HMR do `app.config.ts`.

## Code Examples

### Exemplo: createRootRoute com notFoundComponent
```typescript
// src/routes/__root.tsx
// Source: [CITED: tanstack.com/router/v1/docs — NotFoundRoute Class deprecation note]
import { createRootRoute, Outlet } from '@tanstack/react-router'

function NotFoundPage() {
  return (
    <div>
      <h1>404 — Página não encontrada</h1>
      <a href="/">Voltar ao início</a>
    </div>
  )
}

export const rootRoute = createRootRoute({
  component: () => <Outlet />,
  notFoundComponent: NotFoundPage,
})
```

### Exemplo: Router completo (code-based)
```typescript
// src/routes/router.ts
// Source: [CITED: tanstack.com/router/v1/docs — creating-a-router]
import { createRouter, createRoute } from '@tanstack/react-router'
import { rootRoute } from './__root'
import { HomePage } from '../components/pages/HomePage'

const indexRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/',
  component: HomePage,
})

const routeTree = rootRoute.addChildren([indexRoute])

export const router = createRouter({ routeTree })

declare module '@tanstack/react-router' {
  interface Register {
    router: typeof router
  }
}
```

### Exemplo: Atom — Button wrapper sobre shadcn
```typescript
// src/components/atoms/AppButton.tsx
// Source: [ASSUMED baseado em padrão shadcn/ui]
import { Button, type ButtonProps } from '@/components/ui/button'

interface AppButtonProps extends ButtonProps {
  label: string
}

export function AppButton({ label, ...props }: AppButtonProps) {
  return <Button {...props}>{label}</Button>
}
```

### Exemplo: Molecule — LabeledField
```typescript
// src/components/molecules/LabeledField.tsx
// Source: [ASSUMED]
import { Label } from '@/components/ui/label'
import { Input } from '@/components/ui/input'

interface LabeledFieldProps {
  id: string
  label: string
  error?: string
  registration: object  // retorno de register() do RHF
}

export function LabeledField({ id, label, error, registration }: LabeledFieldProps) {
  return (
    <div className="flex flex-col gap-1">
      <Label htmlFor={id}>{label}</Label>
      <Input id={id} {...registration} aria-invalid={!!error} />
      {error && <p className="text-sm text-destructive">{error}</p>}
    </div>
  )
}
```

## State of the Art

| Abordagem Antiga | Abordagem Atual | Quando Mudou | Impacto |
|-----------------|-----------------|--------------|---------|
| Vinxi SPA (nksaraf/vinxi) | cloudflare/vinext SSR | 2026-04-06 (decisão do usuário) | Mudança de SPA para SSR; precisa de "use client" em componentes interativos |
| `NotFoundRoute` (TanStack Router) | `notFoundComponent` em `createRootRoute` | TanStack Router v1 (2024) | API depreciada; usar notFoundComponent |
| Tailwind v3 com PostCSS | Tailwind v4 com @tailwindcss/vite | Fev/2025 | Sem postcss.config.js; globals.css usa `@import "tailwindcss"` |
| shadcn/ui default style | shadcn/ui new-york style (padrão) | 2025 | Novos projetos usam `new-york` como padrão |
| Zod v3 | Zod v4 (última versão no npm) | 2025 | CONTEXT.md menciona v3 mas npm serve v4.3.6 — verificar compatibilidade |

**Deprecado/obsoleto:**
- `app.config.ts` com `createApp()` do Vinxi: substituído por `vite.config.ts` + `next.config.ts` no vinext
- `NotFoundRoute` class do TanStack Router: depreciada em favor de `notFoundComponent`
- `postcss.config.js` para Tailwind: removido em Tailwind v4

## Assumptions Log

| # | Afirmação | Seção | Risco se errado |
|---|-----------|-------|-----------------|
| A1 | vinext dev server pode ser configurado para rodar na porta 5173 via variável PORT ou flag CLI | Pitfall 2 | Se porta não for configurável, compose.yaml precisa mapear para porta 3001 |
| A2 | Uma página catch-all `pages/[[...slug]].tsx` no vinext servirá como shell SSR, permitindo TanStack Router gerenciar routing client-side | Pattern 1 / Pitfall 5 | Se vinext interceptar rotas de forma que inviabilize TanStack Router, será preciso escolher apenas um dos dois roteadores |
| A3 | `'use client'` em componentes vinext funciona conforme documentação Next.js para desabilitar RSC | Pattern 1 / Pitfall 3 | vinext é experimental — pode ter comportamento diferente do Next.js para "use client" |
| A4 | vinext instala como `npm install vinext` e os scripts substituem `next` por `vinext` | Standard Stack | Se vinext requer uma estrutura de projeto Next.js pré-existente e não funciona em projeto novo, será necessário `npm create next-app` primeiro |
| A5 | Zod v4.3.x é compatível com `@hookform/resolvers` (zodResolver) | Standard Stack | Se zodResolver ainda requer Zod v3, precisar pinnar `zod@3` |

**Se esta tabela estiver vazia:** Todas as afirmações desta pesquisa foram verificadas ou citadas.

## Open Questions

1. **Conflito de arquitetura: TanStack Router dentro de vinext é viável?**
   - O que sabemos: vinext tem roteamento file-based (Next.js-style); TanStack Router tem seu próprio sistema client-side
   - O que não está claro: Se TanStack Router funcionará corretamente após hidratação SSR do vinext (especialmente após hard refresh em rotas não-raiz)
   - Recomendação: Plano 07-01 deve incluir task de verificação com hard refresh em `/rota-inexistente` e confirmar que NotFoundPage renderiza corretamente

2. **Zod v3 vs v4 — CONTEXT.md menciona v3, npm serve v4**
   - O que sabemos: CONTEXT.md especifica "Zod v3"; npm registry retorna v4.3.6 como latest
   - O que não está claro: Se a aplicação usa APIs que mudaram entre v3 e v4 (Zod v4 tem breaking changes na API de `.nullable()`, `.optional()` etc.)
   - Recomendação: Verificar se `@hookform/resolvers` é compatível com Zod v4. Se sim, usar v4. Se não, pinnar `zod@^3`.

3. **vinext standalone para Docker local — está realmente funcional?**
   - O que sabemos: Documentação menciona `output: "standalone"` e `node dist/standalone/server.js`; porém "Node.js production server less complete than Workers"
   - O que não está claro: Se o standalone server funciona de forma estável para dev/test local
   - Recomendação: Para esta fase (Phase 7), usar `vinext dev` no Docker (modo desenvolvimento), não standalone. Standalone seria para fases futuras de produção.

## Environment Availability

| Dependência | Requerida por | Disponível | Versão | Fallback |
|------------|--------------|-----------|--------|----------|
| Node.js | vinext, build | ✓ | v24.14.0 | — |
| npm | package install | ✓ | 11.9.0 | — |
| Docker / Docker Compose | frontend service | [não verificado] | — | Ambiente local sem Docker |
| vinext (0.0.39) | Framework SSR | ✓ (npm) | 0.0.39 | Vinxi SPA puro (já instalado) |
| @tanstack/react-router | Roteamento | ✓ (npm) | 1.168.10 | — |

**Dependências ausentes sem fallback:** Nenhuma identificada — todos os pacotes disponíveis via npm.

**Nota sobre scaffold existente:** O projeto já tem `frontend/` com Vinxi 0.5.11. A migração para vinext requer:
1. `npm uninstall vinxi`
2. `npm install vinext @vitejs/plugin-react`
3. Remover `app.config.ts` (Vinxi-specific)
4. Criar `vite.config.ts` (vinext usa config Vite padrão)
5. Criar `pages/` directory (ou `app/`)

## Validation Architecture

### Test Framework
| Propriedade | Valor |
|------------|-------|
| Framework | vitest 4.1.2 |
| Config file | `vite.config.ts` (seção `test:`) — ou `vitest.config.ts` separado |
| Comando rápido | `npx vitest run --reporter=verbose` |
| Suite completa | `npx vitest run` |

### Phase Requirements → Test Map
| Req ID | Comportamento | Tipo de Teste | Comando Automatizado | Arquivo Existe? |
|--------|--------------|---------------|---------------------|-----------------|
| FRONT-01 | Estrutura Atomic Design tem ao menos 1 componente por nível | unit | `npx vitest run tests/atomic-design.test.tsx` | ❌ Wave 0 |
| FRONT-02 | App carrega sem erros em `/` (vinext SSR) | smoke | `npx vitest run tests/app-shell.test.tsx` | ❌ Wave 0 |
| FRONT-03 | Caminho desconhecido renderiza NotFoundPage tipada | unit | `npx vitest run tests/routing.test.tsx` | ❌ Wave 0 |
| FRONT-04 | ExampleForm mostra erros inline antes de submit quando campo inválido | unit | `npx vitest run tests/example-form.test.tsx` | ❌ Wave 0 |
| FRONT-05 | Classes Tailwind aplicadas em componentes shadcn | unit (implícito) | Coberto por atomic-design.test.tsx | ❌ Wave 0 |

### Sampling Rate
- **Por commit de task:** `npx vitest run --reporter=verbose`
- **Por merge de wave:** Suite completa: `npx vitest run`
- **Phase gate:** Suite completa verde antes de `/gsd-verify-work`

### Wave 0 Gaps
- [ ] `frontend/tests/atomic-design.test.tsx` — cobre FRONT-01, FRONT-05
- [ ] `frontend/tests/app-shell.test.tsx` — cobre FRONT-02
- [ ] `frontend/tests/routing.test.tsx` — cobre FRONT-03
- [ ] `frontend/tests/example-form.test.tsx` — cobre FRONT-04
- [ ] `frontend/tests/setup.ts` — configura `@testing-library/jest-dom`
- [ ] Instalação: `npm install -D vitest @testing-library/react @testing-library/jest-dom jsdom`

## Security Domain

### Applicable ASVS Categories

| Categoria ASVS | Aplica | Controle Padrão |
|----------------|--------|-----------------|
| V2 Authentication | Não (fase 7 = scaffold, sem auth) | — |
| V3 Session Management | Não | — |
| V4 Access Control | Não | — |
| V5 Input Validation | Sim (formulários) | Zod + RHF — validação client-side como UX; server-side é a verdadeira segurança |
| V6 Cryptography | Não | — |

### Known Threat Patterns para este Stack

| Padrão | STRIDE | Mitigação Padrão |
|--------|--------|-----------------|
| XSS via valores de formulário | Tampering | React escapa JSX por padrão; não usar `dangerouslySetInnerHTML` |
| JWT em localStorage | Information Disclosure | Proibido pelo CLAUDE.md (SEC-10) — apenas memória React |
| Dados sensíveis em URL params | Information Disclosure | TanStack Router type-safe; não expor tokens em search params |

## Project Constraints (from CLAUDE.md)

Diretivas obrigatórias que o planejador DEVE verificar:

| Diretiva | Aplicação nesta Fase |
|----------|---------------------|
| Tech Stack: React/Vinxi → agora vinext por decisão do usuário | Substituir Vinxi por vinext |
| API Style: Controllers ASP.NET (sem Minimal API) | Backend — não aplicável a esta fase |
| Proibido: keycloak-js | Não instalar — não é necessário nesta fase |
| Proibido: localStorage para tokens | SEC-10 — não armazenar JWT (fase 9, mas criar habito desde agora) |
| Proibido: Next.js | vinext reimplementa Next.js conventions mas não É Next.js — aceitável |
| Proibido: Redux/Zustand | React Context é suficiente para esta fase |
| Proibido: Axios | fetch/ky preferido — não é necessário nesta fase |
| License Rule: OSS apenas (MIT/Apache 2.0) | vinext: MIT ✓; TanStack Router: MIT ✓; shadcn/ui: MIT ✓; RHF: MIT ✓; Zod: MIT ✓ |
| Serilog + OpenTelemetry obrigatórios | Backend — não aplicável ao frontend nesta fase |
| Vitest para testes | Correto — usar vitest (não Jest) |

## Sources

### Primary (HIGH confidence)
- [VERIFIED: npm registry 2026-04-06] — versões de todos os pacotes confirmadas
- [github.com/cloudflare/vinext README] — init commands, standalone output, env vars PORT/HOST
- [ui.shadcn.com/docs/installation/vite] — setup Tailwind v4, vite.config.ts, path aliases
- [ui.shadcn.com/docs/tailwind-v4] — mudanças no CSS (sem PostCSS, globals.css)
- [tanstack.com/router/v1/docs] — notFoundComponent, createRootRoute, createRouter

### Secondary (MEDIUM confidence)
- [blog.cloudflare.com/vinext/] — contexto arquitetural, status experimental (fev/2026)
- [vinext.io] — quickstart commands, router auto-detection
- [github.com/replicate/getting-started-vinext] — estrutura de projeto app/ directory

### Tertiary (LOW confidence)
- WebSearch results sobre vinext Docker (sem documentação oficial específica para Docker local)
- Padrão de integração vinext + TanStack Router (inferido, não documentado oficialmente)

## Metadata

**Confidence breakdown:**
- Standard Stack: HIGH — versões verificadas no npm registry
- Architecture: MEDIUM — vinext é experimental; integração com TanStack Router é inferida (A2)
- Pitfalls: MEDIUM-HIGH — maioria baseada em comportamentos documentados de Next.js/Vite que vinext replica

**Research date:** 2026-04-06
**Valid until:** 2026-05-06 (vinext evolui rapidamente — re-verificar se houver problemas)

---

## RESEARCH COMPLETE
