# Phase 7: Frontend Foundation - Research

**Researched:** 2026-04-06
**Domain:** React SPA com Vinxi + shadcn/ui + TanStack Router + React Hook Form + Zod + Tailwind CSS v4
**Confidence:** MEDIUM (Vinxi SPA + shadcn/ui juntos tem pouca documentação oficial; TanStack Router HIGH; RHF+Zod HIGH)

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- **Framework:** vinext — https://github.com/cloudflare/vinext (Vinxi-based meta-framework by Cloudflare)
  > **CRITICAL WARNING (veja Pitfall 1):** "vinext" no CONTEXT.md provavelmente refere-se ao Vinxi
  > (nksaraf/vinxi) que JA ESTA instalado no projeto (v0.5.11). O cloudflare/vinext e um projeto
  > completamente diferente — reimplementa Next.js sobre Vite, nao e um SPA framework. Esta pesquisa
  > trata "vinext" como "Vinxi 0.5.x" (o framework ja em uso). [ASSUMED — veja Assumptions Log A1]
- vinext replaces plain Vinxi; use vinext's project scaffold and conventions
- SPA mode (no SSR required for this phase)
- **Design system:** shadcn/ui — https://github.com/shadcn-ui/ui
- Use shadcn/ui components as the atomic building blocks (Button, Input, Label, Card, etc.)
- Install components via `npx shadcn@latest add` — do not copy/paste manually
- Style philosophy: simple and minimalist — use shadcn defaults, avoid custom theme overrides unless necessary
- **Tailwind CSS v4** (required by shadcn/ui v2+ and project stack)
- No custom color tokens unless shadcn defaults are insufficient
- Utility-first; no CSS modules or styled-components
- **TanStack Router v1** — type-safe file-based or code-based routing
- Unknown paths must render a typed 404 component (NotFoundRoute)
- Routes needed in this phase: `/` (home/landing placeholder), `*` (404)
- **React Hook Form v7 + Zod v3** — schema-driven, inline validation
- At least one example form at the molecule/organism level demonstrating inline error display
- Validation errors shown inline beneath fields (not toast/alert)
- Directory: `src/components/` split into `atoms/`, `molecules/`, `organisms/`, `templates/`, `pages/`

### Claude's Discretion
- Exact vinext project init command and config file structure (research vinext docs)
- Whether to use file-based routing (vinext convention) or manual route tree
- shadcn/ui init configuration details (baseColor, cssVariables, etc.)
- TypeScript path aliases setup (`@/components`, `@/lib`, etc.)
- Dockerfile / compose wiring for the frontend service (should mirror existing pattern from Phase 1)

### Deferred Ideas (OUT OF SCOPE)
- Full registration/login forms — Phase 8 e 9
- API integration — Phase 8+
- Authentication state / token management — Phase 9
</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| FRONT-01 | Atomic Design — atoms, molecules, organisms, templates, pages | Estrutura de diretórios em `src/components/`, layering de shadcn/ui primitives |
| FRONT-02 | Vinxi configurado em SPA mode | Vinxi 0.5.x `type: "spa"` com `handler: "./index.html"`, plugins no router |
| FRONT-03 | TanStack Router para rotas type-safe | TanStack Router v1 code-based, `notFoundComponent` no rootRoute para 404 |
| FRONT-04 | React Hook Form + Zod para validação de formulários | `zodResolver` + `useForm` + `formState.errors` inline |
| FRONT-05 | Tailwind CSS para estilização | `@tailwindcss/vite` plugin no app.config.ts + `@import "tailwindcss"` no CSS |
</phase_requirements>

---

## Summary

O projeto ja possui um scaffold Vinxi 0.5.11 funcional no diretório `frontend/` (Phase 1). A Phase 7 extende esse scaffold adicionando: Tailwind CSS v4 + shadcn/ui, TanStack Router v1 para roteamento type-safe, React Hook Form v7 + Zod v3 para formulários, e a estrutura Atomic Design em `src/components/`.

A decisao de usar "vinext" no CONTEXT.md e ambigua — cloudflare/vinext é um framework para rodar aplicacoes Next.js sobre Vite (com SSR, App Router, Pages Router), completamente diferente do Vinxi (nksaraf/vinxi) que ja esta instalado. Para uma SPA sem SSR, Vinxi 0.5.x e a escolha correta e ja esta em uso. Esta pesquisa trata a decisao como "manter Vinxi 0.5.x existente e extende-lo" ate que o usuario confirme o contrário.

O Vinxi suporta `plugins: () => [...]` por router, o que permite injetar `@vitejs/plugin-react` e `@tailwindcss/vite` diretamente no `app.config.ts` existente — sem necessidade de um `vite.config.ts` separado. O shadcn/ui com Tailwind v4 nao usa mais `tailwind.config.js`; toda a configuracao vai em CSS via `@import "tailwindcss"`.

**Primary recommendation:** Manter Vinxi 0.5.x; adicionar `@vitejs/plugin-react` + `@tailwindcss/vite` ao router SPA no `app.config.ts`; inicializar shadcn/ui com `npx shadcn@latest init` apontando para o CSS global; usar TanStack Router v1 code-based com `notFoundComponent` no rootRoute.

---

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| vinxi | 0.5.11 | Framework SPA full-stack | JA INSTALADO; suporta SPA mode com plugins Vite |
| @vitejs/plugin-react | 6.0.1 | Transforma JSX/TSX no build Vite | Necessario para React no Vinxi com Babel/esbuild |
| tailwindcss | 4.2.2 | Styling utility-first | Requerido pelo usuario e por shadcn/ui v2+ |
| @tailwindcss/vite | 4.2.2 | Plugin Vite para Tailwind v4 | Substituicao do postcss no Tailwind v4 |
| shadcn/ui (CLI) | 4.1.2 | Design system — Button, Input, Label, Card | Locked decision do usuario |
| tw-animate-css | 1.4.0 | Animacoes CSS para shadcn/ui | Substitui tailwindcss-animate no shadcn/ui v2+ |
| @tanstack/react-router | 1.168.10 | Roteamento type-safe SPA | Locked decision do usuario |
| @tanstack/router-plugin | 1.167.12 | Plugin Vite para file-based routing (opcional) | Necessario se usar file-based routing |
| react-hook-form | 7.72.1 | Gerenciamento de formularios | Locked decision do usuario |
| zod | 4.3.6 | Schema validation | Locked decision do usuario |
| @hookform/resolvers | 5.2.2 | Adapter zodResolver para RHF | Bridge entre Zod e React Hook Form |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| class-variance-authority | 0.7.1 | Variants de classe CSS | Instalado automaticamente pelo shadcn/ui |
| clsx | 2.1.1 | Condicional class names | Instalado automaticamente pelo shadcn/ui |
| tailwind-merge | 3.5.0 | Merge de classes Tailwind sem conflito | Instalado automaticamente pelo shadcn/ui |
| lucide-react | 1.7.0 | Icones SVG padrao do shadcn/ui | Instalado automaticamente pelo shadcn/ui |
| @types/node | 25.5.2 | Tipos Node para `path.resolve` no config | Necessario para alias `@/*` no tsconfig |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| TanStack Router code-based | TanStack Router file-based | File-based requer `@tanstack/router-plugin` e convencoes de diretorio — mais magica, menos controle. Code-based e mais explicito para um projeto pequeno |
| shadcn/ui | Radix UI diretamente | shadcn/ui ja configura estilos Tailwind; Radix UI puro exige mais CSS manual |
| @tailwindcss/vite | PostCSS plugin | Tailwind v4 descontinuou abordagem postcss em favor do plugin Vite — use @tailwindcss/vite |

**Version verification:** Todas as versoes verificadas via `npm view [package] dist-tags.latest` em 2026-04-06. [VERIFIED: npm registry]

**Installation:**
```bash
# A partir do diretório frontend/
npm install @vitejs/plugin-react tailwindcss @tailwindcss/vite tw-animate-css
npm install @tanstack/react-router
npm install react-hook-form zod @hookform/resolvers
npm install -D @tanstack/router-plugin @types/node
# shadcn/ui instala suas dependencias via CLI
npx shadcn@latest init
# Adicionar componentes conforme necessário
npx shadcn@latest add button input label card
```

---

## Architecture Patterns

### Recommended Project Structure
```
frontend/
├── app.config.ts          # Vinxi SPA config (plugins: react + tailwind)
├── index.html             # SPA entry point — JA EXISTE
├── tsconfig.json          # + paths: { "@/*": ["./src/*"] }
├── Dockerfile             # JA EXISTE (node:22-alpine, npm run dev)
├── src/
│   ├── main.tsx           # React entry — cria root, renderiza <RouterProvider>
│   ├── globals.css        # @import "tailwindcss"; + variaveis shadcn/ui
│   ├── router.ts          # createRouter + declare module type registration
│   ├── components/        # Atomic Design
│   │   ├── ui/            # shadcn/ui components (gerenciados pelo CLI)
│   │   ├── atoms/         # Wrappers sobre shadcn/ui: Button, Input wrappers
│   │   ├── molecules/     # LabeledField = Label + Input + mensagem de erro
│   │   ├── organisms/     # ExampleForm = RHF + Zod + LabeledField(s)
│   │   ├── templates/     # PageLayout = header + main + footer slots
│   │   └── pages/         # HomePage, NotFoundPage
│   ├── lib/
│   │   └── utils.ts       # cn() helper (criado pelo shadcn/ui init)
│   └── hooks/             # hooks customizados (vazio por ora)
└── public/                # assets estaticos
```

### Pattern 1: Vinxi SPA com plugins React + Tailwind v4

**What:** Adicionar `@vitejs/plugin-react` e `@tailwindcss/vite` ao router SPA no `app.config.ts`
**When to use:** Sempre que o Vinxi precisar transpilar JSX/TSX e aplicar Tailwind v4

```typescript
// Source: github.com/nksaraf/vinxi README + vinxi/lib/router-modes.js schema
import { createApp } from "vinxi";
import react from "@vitejs/plugin-react";
import tailwindcss from "@tailwindcss/vite";

export default createApp({
  routers: [
    {
      name: "public",
      type: "static",
      dir: "./public",
    },
    {
      name: "client",
      type: "spa",
      handler: "./index.html",
      vite: {
        server: {
          host: "0.0.0.0",
          port: 5173,
          hmr: { host: "localhost", port: 5173, clientPort: 5173 },
          watch: { usePolling: true, interval: 1000 },
        },
        resolve: {
          alias: {
            "@": new URL("./src", import.meta.url).pathname,
          },
        },
      },
      plugins: () => [react(), tailwindcss()],
    },
  ],
});
```

**NOTA:** O campo `vite` dentro do router SPA aceita configuracao Vite padrao (incluindo `resolve.alias`). O campo `plugins` e uma funcao que retorna array. [VERIFIED: vinxi/lib/router-modes.js schema — campo `plugins` e `vite.server` presentes no spaRouterSchema]

### Pattern 2: TanStack Router v1 Code-Based com notFoundComponent

**What:** Roteamento type-safe manual sem convencoes de arquivo
**When to use:** Projeto pequeno com poucas rotas onde controle explicito e preferivel

```typescript
// Source: tanstack.com/router/latest/docs/routing/code-based-routing [CITED: tanstack.com]
import {
  createRootRoute,
  createRoute,
  createRouter,
  RouterProvider,
  Outlet,
} from "@tanstack/react-router";

// Root route com 404 handler
const rootRoute = createRootRoute({
  component: () => <Outlet />,
  notFoundComponent: NotFoundPage,
});

// Index route
const indexRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/",
  component: HomePage,
});

// Route tree
const routeTree = rootRoute.addChildren([indexRoute]);

// Router instance
const router = createRouter({ routeTree });

// OBRIGATORIO para type safety do TypeScript
declare module "@tanstack/react-router" {
  interface Register {
    router: typeof router;
  }
}

// Entry point
export default function App() {
  return <RouterProvider router={router} />;
}
```

**NOTA sobre NotFoundRoute deprecated:** A classe `NotFoundRoute` esta depreciada. Usar `notFoundComponent` no `rootRoute` e a abordagem recomendada para TanStack Router v1 atual. [VERIFIED: tanstack.com search results, 2025]

### Pattern 3: React Hook Form + Zod com shadcn/ui

**What:** Formulario com validacao inline usando componentes shadcn/ui
**When to use:** Qualquer formulario no projeto — padrao mandatorio

```typescript
// Source: ui.shadcn.com/docs/forms/react-hook-form [CITED: ui.shadcn.com]
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import * as z from "zod";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";

const schema = z.object({
  name: z.string().min(2, "Nome deve ter pelo menos 2 caracteres"),
  email: z.string().email("Email invalido"),
});

type FormData = z.infer<typeof schema>;

export function ExampleForm() {
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<FormData>({
    resolver: zodResolver(schema),
    defaultValues: { name: "", email: "" },
  });

  const onSubmit = (data: FormData) => console.log(data);

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
      <div className="space-y-1">
        <Label htmlFor="name">Nome</Label>
        <Input id="name" {...register("name")} />
        {errors.name && (
          <p className="text-sm text-destructive">{errors.name.message}</p>
        )}
      </div>
      <div className="space-y-1">
        <Label htmlFor="email">Email</Label>
        <Input id="email" type="email" {...register("email")} />
        {errors.email && (
          <p className="text-sm text-destructive">{errors.email.message}</p>
        )}
      </div>
      <Button type="submit">Enviar</Button>
    </form>
  );
}
```

### Pattern 4: Camadas Atomic Design com shadcn/ui

**What:** Como mapear primitivos shadcn/ui para a hierarquia Atomic Design
**When to use:** Estrutura mandatoria do projeto

```
ATOMS      → Wrappers/extensoes minimas sobre shadcn/ui primitives
             Exemplos: atoms/Button.tsx (re-export com defaults do projeto)
             NUNCA: reimplementar algo que shadcn cobre

MOLECULES  → Combinam 2+ atoms com logica de apresentacao propria
             Exemplos: molecules/LabeledField.tsx = Label + Input + <p> de erro
             molecules/FormField.tsx = wrapper RHF para campos com erro inline

ORGANISMS  → Combinam molecules para formar uma secao funcional
             Exemplos: organisms/ExampleForm.tsx = form completo RHF+Zod
             organismos sao stateful (useForm, estado local)

TEMPLATES  → Layouts de pagina sem conteudo real
             Exemplos: templates/PageLayout.tsx = header slot + main slot + footer slot
             templates sao stateless, aceitam children/slots

PAGES      → Instanciam templates com conteudo real, conectam a rotas
             Exemplos: pages/HomePage.tsx, pages/NotFoundPage.tsx
```

**REGRA:** `src/components/ui/` e gerenciado EXCLUSIVAMENTE pelo CLI do shadcn/ui. Nunca editar manualmente.

### Pattern 5: shadcn/ui Init para Vite (nao Next.js)

**What:** Configurar shadcn/ui em projeto Vite/Vinxi sem Next.js
**When to use:** Setup inicial — Wave 0

```bash
# 1. CSS global: frontend/src/globals.css
# @import "tailwindcss";

# 2. shadcn init (interactive — responder as perguntas)
npx shadcn@latest init
# Quando perguntar sobre framework: selecionar "Vite"
# baseColor: neutral (padrao minimalista)
# CSS variables: yes
# CSS file path: src/globals.css
# components alias: @/components
# utils alias: @/lib/utils
```

O `npx shadcn@latest init` gera:
- `components.json` — configuracao do shadcn
- `src/lib/utils.ts` — funcao `cn()` (clsx + tailwind-merge)
- Atualiza o CSS global com variaveis CSS do tema

### Anti-Patterns to Avoid
- **Usar `vite.config.ts` separado ao lado do `app.config.ts`:** O Vinxi gerencia a config Vite internamente via `app.config.ts`. Um arquivo `vite.config.ts` paralelo causa conflitos. Todos os plugins Vite vao dentro do `plugins: () => [...]` do router.
- **Editar `src/components/ui/` manualmente:** Esses arquivos sao propriedade do CLI shadcn. Use `npx shadcn@latest add` para instalar e `npx shadcn@latest diff` para atualizar.
- **Usar `NotFoundRoute` class do TanStack Router:** Depreciada. Usar `notFoundComponent` no `createRootRoute`.
- **`localStorage` para qualquer estado:** Proibido pelo projeto (SEC-10 em Phase 9, mas o padrao deve ser estabelecido desde ja).
- **`tailwind.config.js`:** Nao existe no Tailwind v4. Toda configuracao va em CSS via diretiva `@theme`.
- **Misturar `type: "client"` e `type: "spa"` no Vinxi:** `type: "spa"` e o correto para SPA sem SSR. `type: "client"` e para o bundle do lado cliente em apps SSR.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Sistema de componentes visuais | componentes Button/Input/Card customizados | shadcn/ui via CLI | shadcn cobre acessibilidade (WAI-ARIA), dark mode, focus states |
| Validacao de formulario | logica manual de `useState` + regex | React Hook Form + Zod | RHF evita re-renders desnecessarios; Zod da inferencia TypeScript gratis |
| Merge de classes CSS | concatenacao manual de strings | `cn()` de `@/lib/utils` | tailwind-merge evita conflitos de classes; clsx evita strings undefined |
| Roteamento type-safe | sistema de rotas customizado | TanStack Router | navegacao type-checked em compile time; params tipados; 404 handler |
| Adapter validacao | bridge manual RHF-Zod | `@hookform/resolvers/zod` | resolve validacao assincrona, mensagens de erro do schema, tipos corretos |

**Key insight:** shadcn/ui nao e uma dependencia — e codigo copiado para o projeto via CLI. Isso significa total controle sem ficar preso a breaking changes de versao.

---

## Common Pitfalls

### Pitfall 1: vinext vs Vinxi — Frameworks Completamente Diferentes
**What goes wrong:** O CONTEXT.md menciona "vinext" de github.com/cloudflare/vinext. Mas cloudflare/vinext e um projeto que reimplementa o Next.js sobre Vite (SSR, App Router, Pages Router) — completamente incompativel com uma SPA sem SSR. O projeto JA USA Vinxi (nksaraf/vinxi) 0.5.11.
**Why it happens:** Os nomes sao similares e ambos envolvem Vite.
**How to avoid:** Esta pesquisa trata a intencao como "Vinxi 0.5.x existente". O planejador deve confirmar com o usuario se "vinext" era intencional ou um typo/confusao antes de trocar o framework.
**Warning signs:** Se houver instrucao para `npm install vinext` ou `npx vinext init`, questionar.

### Pitfall 2: `@tailwindcss/vite` fora do `plugins()` do Vinxi
**What goes wrong:** Tailwind v4 com Vinxi exige que `tailwindcss()` seja passado dentro de `plugins: () => [...]` no router SPA do `app.config.ts`. Tentar usar PostCSS separado ou `vite.config.ts` standalone nao funciona com o Vinxi.
**Why it happens:** Documentacao do Tailwind v4 mostra `vite.config.ts` padrao — mas com Vinxi o config Vite e gerenciado internamente.
**How to avoid:** Sempre adicionar plugins Vite via `plugins: () => [react(), tailwindcss()]` no router do `app.config.ts`.
**Warning signs:** Tailwind classes nao aplicadas em dev; build sem estilos.

### Pitfall 3: Alias `@/` nao resolvido pelo TypeScript no Vinxi
**What goes wrong:** O alias `@/*` precisa estar em DOIS lugares: (1) no `vite.resolve.alias` dentro do router do `app.config.ts`, E (2) no `tsconfig.json` (ou `tsconfig.app.json`) em `compilerOptions.paths`. Sem ambos, imports com `@/` ou compilam mas nao funcionam em runtime, ou dao erro no TypeScript.
**Why it happens:** Vite e TypeScript sao sistemas independentes de resolucao de modulos.
**How to avoid:** Configurar ambos: `vite.resolve.alias` no `app.config.ts` E `paths` no `tsconfig.json`.
**Warning signs:** `Module not found: @/components/...` em runtime mesmo com TypeScript sem erro.

### Pitfall 4: `shadcn init` em projeto Vinxi — perguntas interativas
**What goes wrong:** `npx shadcn@latest init` faz perguntas interativas. Em ambiente Docker/CI pode travar. Alem disso, pode detectar Vinxi como framework desconhecido.
**Why it happens:** CLI shadcn detecta frameworks por arquivos (`next.config.js`, `vite.config.ts`, etc.).
**How to avoid:** Executar `npx shadcn@latest init` localmente (fora do Docker). Responder: framework = "Vite", CSS file = `src/globals.css`. Se detectar incorretamente, usar flag `--baseColor neutral`.
**Warning signs:** CLI falha ou gera `components.json` com framework errado.

### Pitfall 5: HMR nao funciona no Docker Windows (JA DOCUMENTADO)
**What goes wrong:** Alteracoes de arquivo nao sao propagadas ao container no Windows.
**Why it happens:** inotify events sao confiaveis no Linux nativo, mas nao no WSL2/Docker Desktop no Windows.
**How to avoid:** MANTER `watch: { usePolling: true, interval: 1000 }` no `app.config.ts` — JA ESTA CONFIGURADO no projeto.
**Warning signs:** Edicoes de arquivo nao refletem no browser sem reload manual.

### Pitfall 6: `globals.css` nao importado no entry point
**What goes wrong:** Tailwind v4 com `@tailwindcss/vite` requer que o CSS seja importado no entry point do modulo JS/TSX. Se nao for importado, nenhum estilo Tailwind e gerado.
**Why it happens:** Diferente do Tailwind v3 com PostCSS (que processava o CSS globalmente), o plugin Vite do Tailwind v4 so processa arquivos que sao importados pelo grafo de modulos.
**How to avoid:** Adicionar `import "@/globals.css"` (ou `import "./globals.css"`) no `src/main.tsx` (entry point).
**Warning signs:** Pagina sem estilos nenhum; inspecao no DevTools mostra HTML sem classes CSS.

---

## Code Examples

### Configuracao completa do app.config.ts (Phase 7)

```typescript
// frontend/app.config.ts
// Source: vinxi README + vinxi/lib/router-modes.js [VERIFIED: local node_modules]
import { createApp } from "vinxi";
import react from "@vitejs/plugin-react";
import tailwindcss from "@tailwindcss/vite";
import { fileURLToPath, URL } from "node:url";

export default createApp({
  routers: [
    {
      name: "public",
      type: "static",
      dir: "./public",
    },
    {
      name: "client",
      type: "spa",
      handler: "./index.html",
      vite: {
        server: {
          host: "0.0.0.0",
          port: 5173,
          hmr: { host: "localhost", port: 5173, clientPort: 5173 },
          watch: { usePolling: true, interval: 1000 },
        },
        resolve: {
          alias: {
            "@": fileURLToPath(new URL("./src", import.meta.url)),
          },
        },
      },
      plugins: () => [react(), tailwindcss()],
    },
  ],
});
```

### globals.css para shadcn/ui + Tailwind v4

```css
/* frontend/src/globals.css */
/* Source: ui.shadcn.com/docs/tailwind-v4 + ui.shadcn.com/docs/installation/vite [CITED] */
@import "tailwindcss";

/* shadcn/ui gera o bloco @theme abaixo durante `npx shadcn@latest init` */
@layer base {
  :root {
    --background: hsl(0 0% 100%);
    --foreground: hsl(0 0% 3.9%);
    --primary: hsl(0 0% 9%);
    --primary-foreground: hsl(0 0% 98%);
    --destructive: hsl(0 84.2% 60.2%);
    --destructive-foreground: hsl(0 0% 98%);
    --muted: hsl(0 0% 96.1%);
    --muted-foreground: hsl(0 0% 45.1%);
    --border: hsl(0 0% 89.8%);
    --input: hsl(0 0% 89.8%);
    --ring: hsl(0 0% 3.9%);
    --radius: 0.5rem;
  }
}
/* NOTA: O init do shadcn substitui este bloco pela saida real gerada pelo CLI */
```

### main.tsx (entry point com Router e CSS)

```tsx
// frontend/src/main.tsx
import { createRoot } from "react-dom/client";
import { RouterProvider } from "@tanstack/react-router";
import { router } from "./router";
import "@/globals.css";

const root = document.getElementById("root")!;
createRoot(root).render(<RouterProvider router={router} />);
```

### src/router.ts (definicao completa do router)

```typescript
// frontend/src/router.ts
// Source: tanstack.com/router/latest/docs/routing/code-based-routing [CITED]
import {
  createRootRoute,
  createRoute,
  createRouter,
  Outlet,
} from "@tanstack/react-router";
import { HomePage } from "@/components/pages/HomePage";
import { NotFoundPage } from "@/components/pages/NotFoundPage";

const rootRoute = createRootRoute({
  component: () => <Outlet />,
  notFoundComponent: NotFoundPage,
});

const indexRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/",
  component: HomePage,
});

const routeTree = rootRoute.addChildren([indexRoute]);

export const router = createRouter({ routeTree });

declare module "@tanstack/react-router" {
  interface Register {
    router: typeof router;
  }
}
```

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| `tailwind.config.js` | Configuracao em CSS via `@theme` + `@import "tailwindcss"` | Tailwind v4 (2025) | Sem arquivo de config JS; tudo em CSS |
| `tailwindcss-animate` | `tw-animate-css` | shadcn/ui deprecou em Marco 2025 | Instalar `tw-animate-css` ao inves de `tailwindcss-animate` |
| PostCSS para Tailwind + Vite | `@tailwindcss/vite` plugin | Tailwind v4 (2025) | Configuracao mais simples, sem postcss.config.js |
| `NotFoundRoute` class | `notFoundComponent` no `createRootRoute` | TanStack Router v1 recente | `NotFoundRoute` depreciado; usar `notFoundComponent` |
| `forwardRef` em shadcn/ui | Componentes React padrao sem `forwardRef` | shadcn/ui 2025 | Componentes mais simples, menos boilerplate |
| HSL puro nas CSS vars | OKLCH no shadcn/ui | shadcn/ui 2025 | Cores perceptualmente uniformes; pode ver valores diferentes do esperado |

**Deprecated/outdated:**
- `tailwindcss-animate`: Depreciado pelo shadcn/ui. Use `tw-animate-css`.
- `tailwind.config.js`: Nao existe no Tailwind v4.
- `NotFoundRoute` class: Depreciada no TanStack Router. Use `notFoundComponent` no rootRoute.

---

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | "vinext" no CONTEXT.md refere-se ao Vinxi (nksaraf/vinxi) 0.5.x ja instalado, e nao ao cloudflare/vinext (que reimplementa Next.js) | User Constraints | Se o usuario realmente queria cloudflare/vinext, o plano inteiro precisaria ser reescrito — vinext tem SSR, App Router, Pages Router e e incompativel com a abordagem SPA adotada |
| A2 | `plugins: () => [react(), tailwindcss()]` dentro do router SPA do `app.config.ts` e o caminho correto para adicionar plugins Vite ao Vinxi | Architecture Patterns | Se o Vinxi nao suportar `@tailwindcss/vite` desta forma, precisar de workaround via PostCSS ou arquivo vite.config.ts separado |
| A3 | `vite.resolve.alias` dentro do campo `vite:` do router SPA e aceito pelo Vinxi para configurar o alias `@/` | Architecture Patterns | Se nao suportado, alias nao funcionara e imports `@/` falharao em runtime |

---

## Open Questions

1. **vinext vs Vinxi — confirmar intencao do usuario**
   - What we know: cloudflare/vinext e Next.js reimplementado (SSR obrigatorio); vinxi e SPA framework ja instalado
   - What's unclear: O usuario quis dizer cloudflare/vinext ou foi um erro/confusao de nome?
   - Recommendation: Planejador deve incluir task de "confirmar com usuario" antes de qualquer mudanca de framework. Enquanto isso, planejar com Vinxi 0.5.x existente.

2. **shadcn/ui init detecta Vinxi como framework correto?**
   - What we know: CLI shadcn detecta frameworks por arquivos (`vite.config.ts`, `next.config.js`, etc.)
   - What's unclear: Vinxi usa `app.config.ts` — o CLI pode nao reconhecer automaticamente
   - Recommendation: Executar `npx shadcn@latest init` localmente e documentar as respostas para reprodutibilidade. Usar `--defaults` se disponivel com as flags corretas.

---

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| Node.js | npm install, shadcn CLI | Sim | v24.14.0 | — |
| npm | install de pacotes | Sim | 11.9.0 | — |
| Docker | `docker compose up` frontend | Sim | 29.3.1 | — |
| Docker Compose | servir frontend em container | Sim | v5.1.1 | — |
| vinxi | JA INSTALADO no projeto | Sim | 0.5.11 | — |
| npx | shadcn CLI init | Sim (via Node 24) | — | — |

**Missing dependencies with no fallback:** Nenhum — ambiente completo disponivel.

---

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Nao ha framework de teste frontend configurado (fase greenfield para testes frontend) |
| Config file | Nenhum — Wave 0 deve adicionar Vitest se necessario |
| Quick run command | N/A — Wave 0 gap |
| Full suite command | N/A — Wave 0 gap |

**Nota:** Os criterios de sucesso da Phase 7 sao majoritariamente verificacao visual/manual ("navegar para URL", "ver erros inline"). Nao ha logica de negocio testavel via unit test nesta fase. O planner deve avaliar se Vitest e necessario aqui ou se testes automatizados sao Wave 0 para Phase 8.

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| FRONT-01 | Diretorio Atomic Design existe com pelo menos 1 componente por nivel | smoke (estrutural) | `test -d src/components/atoms && test -d src/components/molecules ...` | ❌ Wave 0 |
| FRONT-02 | Vinxi SPA serve frontend em `http://localhost:5173` | smoke (curl) | `curl -f http://localhost:5173/ \|\| exit 1` | ❌ manual |
| FRONT-03 | Navegar para rota desconhecida mostra 404 | manual | Inspecao visual no browser | ❌ manual |
| FRONT-04 | Form com Zod mostra erros inline sem submit | manual | Inspecao visual + input invalido | ❌ manual |
| FRONT-05 | Estilos Tailwind aplicados visivelmente | manual | Inspecao visual | ❌ manual |

### Sampling Rate
- **Por task commit:** `curl -f http://localhost:5173/ && docker compose ps frontend`
- **Por wave merge:** docker compose up --wait && verificacao visual das 4 success criteria
- **Phase gate:** Todas as 4 success criteria verificadas manualmente antes de `/gsd-verify-work`

### Wave 0 Gaps
- [ ] Vitest config (opcional para esta fase — testes sao majoritariamente visuais)
- [ ] Smoke test script para verificar estrutura Atomic Design

*(Se nao forem adicionados testes automatizados, todas as success criteria sao verificadas via checklist manual no Phase gate)*

---

## Security Domain

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | nao | — |
| V3 Session Management | nao | — |
| V4 Access Control | nao | — |
| V5 Input Validation | sim (parcial) | Zod v3 — validacao client-side de formularios de exemplo |
| V6 Cryptography | nao | — |

**Nota de segurança para esta fase:** Formularios de exemplo nao enviam dados reais. A validacao client-side (Zod) e UX convenience — nao e controle de segurança (ja documentado no REG-09). SEC-10 (JWT em memoria, nunca localStorage) sera implementada na Phase 9; esta fase nao lida com tokens.

### Known Threat Patterns for React SPA

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| XSS via React JSX | Spoofing/Tampering | React escapa automaticamente; nunca usar `dangerouslySetInnerHTML` |
| Dados sensiveis em localStorage | Information Disclosure | Nao usar localStorage para nada nesta fase |

---

## Sources

### Primary (HIGH confidence)
- `vinxi/lib/router-modes.js` [VERIFIED: local node_modules] — schema do router SPA com campos `plugins`, `vite`, `server`
- `vinxi/lib/router-mode.d.ts` [VERIFIED: local node_modules] — definicoes de tipos do router
- `npm view [package] dist-tags.latest` [VERIFIED: npm registry] — versoes de todos os pacotes
- Node/npm/Docker `--version` [VERIFIED: ambiente local] — disponibilidade de ferramentas

### Secondary (MEDIUM confidence)
- [ui.shadcn.com/docs/installation/vite](https://ui.shadcn.com/docs/installation/vite) [CITED] — setup shadcn/ui com Vite
- [ui.shadcn.com/docs/tailwind-v4](https://ui.shadcn.com/docs/tailwind-v4) [CITED] — mudancas Tailwind v4 no shadcn/ui
- [ui.shadcn.com/docs/forms/react-hook-form](https://ui.shadcn.com/docs/forms/react-hook-form) [CITED] — exemplo RHF + shadcn/ui
- [tanstack.com/router/latest/docs/routing/code-based-routing](https://tanstack.com/router/latest/docs/routing/code-based-routing) [CITED] — routing code-based com notFoundComponent
- [github.com/nksaraf/vinxi README](https://github.com/nksaraf/vinxi) [CITED] — exemplo `plugins: () => [reactRefresh()]` no router

### Tertiary (LOW confidence)
- WebSearch: "TanStack Router v1 notFoundComponent deprecated 2025" — confirmacao da deprecacao de NotFoundRoute
- WebSearch: "shadcn/ui Tailwind v4 Vite init components.json" — detalhes do components.json

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — versoes verificadas via npm registry em 2026-04-06
- Architecture (app.config.ts plugins): MEDIUM — schema do Vinxi verificado em node_modules; exemplo pratico nao testado ainda
- Architecture (TanStack Router): HIGH — documentacao oficial acessada, patterns code-based verificados
- Architecture (RHF + Zod): HIGH — documentacao oficial shadcn/ui + RHF verificada
- Pitfalls: MEDIUM — baseados em analise do codigo do Vinxi + documentacao Tailwind v4; vinext confusion e factual

**Research date:** 2026-04-06
**Valid until:** 2026-05-06 (30 dias — stack relativamente estavel; shadcn/ui e Tailwind v4 mudaram em 2025 mas devem estabilizar)
