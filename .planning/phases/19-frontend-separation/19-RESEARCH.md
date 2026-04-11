## Standard Stack

Cada projeto separado herda exatamente o mesmo stack base — sem variacoes:

| Component | Package | Version |
|-----------|---------|---------|
| Meta-framework | Vinxi | 0.5.x |
| UI library | React | 19.x |
| Routing | TanStack Router | 1.x |
| Styling | Tailwind CSS | 4.x (@tailwindcss/vite) |
| UI components | shadcn/ui | default style, neutral baseColor |
| Forms | React Hook Form | 7.x + @hookform/resolvers |
| Validation | Zod | 4.x |
| Testing | Vitest + Testing Library | 4.x + 16.x |
| Type checking | TypeScript | 5.7.x |
| Icons | lucide-react | 1.x |
| Notifications | sonner | 2.x |
| Theme | next-themes | 0.4.x |
| Dev plugins | @vitejs/plugin-react, vite-tsconfig-paths | latest |

**Regra:** Ambos os projetos mantem as mesmas versoes de dependencias. Nunca use versoes diferentes entre client e backoffice — isso cria divergencias de comportamento impossiveis de debugar.

## Architecture Patterns

### Directory Layout

```
frontend/
├── client/                 # frontend-client project
│   ├── src/
│   │   ├── components/     # atoms, molecules, organisms, pages, templates, ui/
│   │   ├── lib/            # auth-context, api, utils, types, validation, theme-provider
│   │   ├── tests/          # client-specific test setup
│   │   ├── globals.css
│   │   ├── main.tsx
│   │   └── router.tsx
│   ├── tests/              # integration/e2e tests
│   ├── public/
│   ├── app.config.ts
│   ├── components.json
│   ├── Dockerfile
│   ├── index.html
│   ├── package.json
│   ├── tsconfig.json
│   ├── vitest.config.ts
│   └── .dockerignore
│
├── backoffice/             # frontend-backoffice project
│   ├── src/
│   │   ├── components/     # atoms, molecules, organisms, pages, templates, ui/
│   │   ├── lib/            # admin-auth-context, admin-api, utils, types, theme-provider
│   │   ├── tests/          # backoffice-specific test setup
│   │   ├── globals.css
│   │   ├── main.tsx
│   │   └── router.tsx
│   ├── tests/
│   ├── public/
│   ├── app.config.ts
│   ├── components.json
│   ├── Dockerfile
│   ├── index.html
│   ├── package.json
│   ├── tsconfig.json
│   ├── vitest.config.ts
│   └── .dockerignore
│
└── (monolith removed after migration)
```

### Docker Compose Port Assignment

| Service | Container Port | Host Port | Purpose |
|---------|---------------|-----------|---------|
| frontend-client | 5173 | 5173 | End-user SPA |
| frontend-backoffice | 5174 | 5174 | Admin SPA |

**Regra:** Client always gets 5173 (porta original). Backoffice recebe 5174. Nao use portas aleatorias — portas fixas simplificam .env, CI, e documentacao.

### API Proxy Strategy

Cada projeto tem seu proprio `server.ts` com proxy h3:
- **client/server.ts:** proxy `/api/*` -> `http://api:8080` (mesmo servidor.ts atual, sem alteracoes)
- **backoffice/server.ts:** proxy `/api/*` -> `http://api:8080` (identico ao client)

Ambos apontam para o mesmo backend. Nao ha separacao de rotas de API no nivel do proxy — a separacao e feita no nivel de autenticacao (contexts diferentes, endpoints diferentes).

### app.config.ts Strategy

Cada projeto usa configuracao independente com porta dedicada:

**client/app.config.ts:**
```ts
import { createApp } from "vinxi";
import react from "@vitejs/plugin-react";
import tailwindcss from "@tailwindcss/vite";
import tsconfigPaths from "vite-tsconfig-paths";

export default createApp({
  routers: [
    { name: "public", type: "static", dir: "./public" },
    { name: "api-proxy", type: "http", handler: "./server.ts", target: "server", base: "/api" },
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
      },
      plugins: () => [tsconfigPaths(), react(), tailwindcss()],
    },
  ],
});
```

**backoffice/app.config.ts:**
```ts
import { createApp } from "vinxi";
import react from "@vitejs/plugin-react";
import tailwindcss from "@tailwindcss/vite";
import tsconfigPaths from "vite-tsconfig-paths";

export default createApp({
  routers: [
    { name: "public", type: "static", dir: "./public" },
    { name: "api-proxy", type: "http", handler: "./server.ts", target: "server", base: "/api" },
    {
      name: "client",
      type: "spa",
      handler: "./index.html",
      vite: {
        server: {
          host: "0.0.0.0",
          port: 5174,
          hmr: { host: "localhost", port: 5174, clientPort: 5174 },
          watch: { usePolling: true, interval: 1000 },
        },
      },
      plugins: () => [tsconfigPaths(), react(), tailwindcss()],
    },
  ],
});
```

**Diferenca unica:** a porta (5173 vs 5174) e o HMR config. Todo o resto e identico.

## Shared vs Duplicated Code

### O que DUPLICAR (copiar para ambos os projetos)

| Item | Origem | Razao |
|------|--------|-------|
| `components/ui/*` (13 arquivos shadcn) | Todos os 13 componentes | Componentes de UI sao stateless e identicos. Duplicar e mais seguro que criar package compartilhado. |
| `lib/utils.ts` (funcao `cn`) | clsx + tailwind-merge | Utilitario puro, sem dependencias externas. |
| `globals.css` | Tailwind entry + CSS variables | Cada projeto precisa do seu proprio entry point CSS. |
| `components.json` | shadcn config | Necessario para `npx shadcn@latest add` funcionar em cada projeto. |
| `ThemeToggle`, `ThemeProvider` | Theme components | Stateless, funcionam identico em ambos. |
| `vitest.config.ts` | Test config | Cada projeto roda testes isoladamente. |
| `.dockerignore` | Docker exclusions | Padrao identico para ambos. |
| `Dockerfile` | Build config | Mesma estrutura, mesma imagem base. |
| `tsconfig.json` | TypeScript config | Paths `@/*` apontam para `./src/*` local. |

### O que MANTER SEPARADO (especifico de cada projeto)

| Item | Client | Backoffice | Razao |
|------|--------|------------|-------|
| `router.tsx` | Rotas publicas (/, /login, /register, /profile, /forgot, /reset) | Rotas admin (/admin/login, /admin/users, /admin/users/$id) | Router e o ponto de separacao principal. |
| `main.tsx` | AuthProvider + RouterProvider | AdminAuthProvider + RouterProvider | Contextos de auth diferentes. |
| `lib/auth-context.tsx` | Sim | Nao | Client usa autenticação de usuario final. |
| `lib/admin-auth-context.tsx` | Nao | Sim | Backoffice usa autenticação administrativa. |
| `lib/api.ts` | Client API clients (login, register, profile, password) | Nao | Endpoints diferentes. |
| `lib/admin-api.ts` | Nao | Admin API clients (login, list users, detail) | Endpoints diferentes. |
| `lib/types.ts` | ClientProfileDto | UserDetailDto, UserSummaryDto | DTOs diferentes. |
| `lib/validation-schemas.ts` | Login, registration, password schemas | Admin login schema (se necessario) | Schemas diferentes. |
| Pages | LoginPage, ProfilePage, etc | AdminLoginPage, AdminUsersPage, etc | Telas diferentes. |
| Molecules/Organisms | LoginForm, RegistrationForm, etc | AdminLoginForm, AdminUsersTable, etc | Componentes diferentes. |
| Templates | AuthLayout | AdminLayout | Layouts diferentes. |

### Por que duplicacao ao inves de package compartilhado?

1. **Zero acoplamento de build** — cada projeto compila independentemente. Mudar shadcn no client nao quebra o backoffice.
2. **Zero complexidade de monorepo** — sem workspaces, sem path aliases cruzados, sem symlinks.
3. **Deploy independente** — cada Dockerfile builda seu proprio projeto sem depender de artefatos externos.
4. **Custo de duplicacao e baixo** — 13 componentes ui + 2 utilitarios = ~15 arquivos. A cada alteracao no shadcn, atualizar nos dois projetos e trivial (diff + copy-paste).
5. **Evolucao independente** — no futuro, o backoffice pode trocar para uma biblioteca de tabelas diferente sem afetar o client.

## Don't Hand-Roll

Nunca construa do zero o que ja existe como biblioteca ou boilerplate consolidado:

| O que | Como |
|-------|------|
| shadcn/ui components | Use `npx shadcn@latest add <component>` em cada projeto. Nao reescreva button.tsx, input.tsx, etc. |
| Tailwind config | Use `@tailwindcss/vite` plugin. Nao crie tailwind.config.js manual. |
| Vinxi setup | Use `createApp()` com routers config. Nao tente montar Vite + HMR na mao. |
| Vitest setup | Use `defineConfig` com `@vitejs/plugin-react` + `jsdom`. Nao configure test runner custom. |
| TypeScript config | Use tsconfig.json com `moduleResolution: "bundler"` e paths `@/*`. |
| React Router (TanStack) | Use `createRootRoute`, `createRoute`, `createRouter`. Nao monte routing manual com window.location. |
| Form handling | Use React Hook Form + Zod + @hookform/resolvers. Nao valide form na mao. |

## Common Pitfalls

### Port conflicts in Docker Compose
- **Sintoma:** `EADDRINUSE` ao subir ambos os frontends.
- **Causa:** Ambos tentando usar porta 5173 no host.
- **Prevencao:** Client = 5173, Backoffice = 5174. Defina portas fixas no `app.config.ts` E no `compose.yaml`.

### Stale imports after migration
- **Sintoma:** `Cannot find module '@/lib/admin-auth-context'` no client, ou `Cannot find module '@/lib/auth-context'` no backoffice.
- **Causa:** Router.tsx ou componentes ainda importam modulos do projeto irmo.
- **Prevencao:** Ao copiar arquivos, revise TODOS os imports. Use `grep` para buscar referencias cruzadas antes de remover o monolith.

### Test isolation failures
- **Sintoma:** Testes do client falham porque procuram AdminAuthProvider, ou vice-versa.
- **Causa:** `src/tests/setup.ts` ou `vitest.config.ts` referenciam modulos que nao existem mais no projeto.
- **Prevencao:** Cada projeto deve ter seu proprio `vitest.config.ts` apontando para seu `src/`. Remova imports de contextos nao-existentes do setup.

### HMR conflicts between containers
- **Sintoma:** Hot reload nao funciona ou reconecta em loop.
- **Causa:** HMR config com porta errada ou conflitos de WebSocket entre containers.
- **Prevencao:** Cada `app.config.ts` deve ter `hmr: { host: "localhost", port: PORT, clientPort: PORT }` com a porta correta do projeto.

### Environment variable duplication/mismatch
- **Sintoma:** Um frontend funciona, o outro nao encontra `VITE_API_URL`.
- **Causa:** Variaveis definidas apenas para um servico no compose.yaml.
- **Prevencao:** Ambos os servicos devem ter o mesmo bloco de environment variables (exceto se houver variaveis especificas de contexto).

### shadcn/ui components that import from old paths
- **Sintoma:** `Cannot find module '@/components/ui/button'` apos migracao.
- **Causa:** `components.json` ainda aponta para paths do monolith ou componentes foram movidos para pasta diferente.
- **Prevencao:** Regenerate `components.json` em cada projeto com `npx shadcn@latest init` antes de adicionar componentes. Verifique que `aliases.components` aponta para `@/components`.

### Index.html references
- **Sintoma:** Vinxi nao encontra entry point.
- **Causa:** `index.html` com `<script type="module" src="/src/main.tsx">` apontando para caminho inexistente.
- **Prevencao:** Cada projeto deve ter seu proprio `index.html` com o path correto.

## Migration Strategy

Use a estrategia **copy-first-then-delete** — nunca delete antes de validar.

### Passo 1: Criar estruturas vazias
```
mkdir -p frontend/client frontend/backoffice
```
Copie `package.json`, `tsconfig.json`, `components.json`, `.dockerignore`, `Dockerfile`, `index.html` como base para ambos.

### Passo 2: Copiar codigo compartilhado (duplicar)
Para **cada projeto**, copie:
- `components/ui/*` (todos os 13)
- `lib/utils.ts`
- `globals.css`
- `src/tests/setup.ts`
- `vitest.config.ts`

### Passo 3: Copiar codigo especifico do client
Para `frontend/client`:
- `lib/auth-context.tsx`, `lib/api.ts`, `lib/types.ts`, `lib/validation-schemas.ts`, `lib/theme-provider.tsx`, `lib/password-strength.ts`, `lib/error-handler.ts`, `lib/http-interceptor.ts`
- `components/atoms/*` (exceto os que sao admin-only)
- `components/molecules/` (LoginForm, RegistrationForm, PasswordField, PasswordStrengthMeter, PersonTypeRadio, ProfileCard)
- `components/organisms/Header.tsx`
- `components/pages/` (LoginPage, RegistrationPage se existir, ProfilePage, ForgotPasswordPage, ResetPasswordPage, NotFoundPage)
- `components/templates/AuthLayout.tsx`
- `router.tsx` (com apenas rotas publicas)
- `main.tsx` (com AuthProvider apenas)

### Passo 4: Copiar codigo especifico do backoffice
Para `frontend/backoffice`:
- `lib/admin-auth-context.tsx`, `lib/admin-api.ts`
- `components/molecules/` (AdminLoginForm, AdminPagination, AdminSearchBar, AdminStatusFilter, AdminUsersTable, KeycloakStatusBadge, UserDetailCard)
- `components/pages/` (AdminLoginPage, AdminAccessDeniedPage, AdminUsersPage, AdminUserDetailPage, NotFoundPage)
- `components/templates/AdminLayout.tsx`
- `components/atoms/ProfileBadge.tsx` e `ProfileField.tsx` (usados no detail page)
- `router.tsx` (com apenas rotas admin)
- `main.tsx` (com AdminAuthProvider apenas)

### Passo 5: Ajustar configuracoes
- `app.config.ts` de cada projeto com porta correta (5173 client, 5174 backoffice)
- `server.ts` de cada projeto (identicos, apontando para `http://api:8080`)
- `package.json` — atualizar `"name"` para `"frontend-client"` e `"frontend-backoffice"`
- `package.json` — atualizar scripts de dev para usar portas corretas

### Passo 6: Validar independentemente
```bash
cd frontend/client && npm install && npm run dev     # verifica que sobe em 5173
cd frontend/backoffice && npm install && npm run dev  # verifica que sobe em 5174
```

### Passo 7: Validar no Docker Compose
Atualize `compose.yaml` com dois servicos. Rode `docker compose up frontend-client frontend-backoffice`. Verifique ambos acessiveis.

### Passo 8: Deletar monolith
Apos confirmar que ambos funcionam:
```bash
rm -rf frontend/src frontend/app.config.ts frontend/server.ts frontend/index.html frontend/package.json frontend/tsconfig.json frontend/vitest.config.ts frontend/components.json frontend/Dockerfile frontend/.dockerignore
```

## Code Examples

### Target: frontend/client/app.config.ts
```ts
import { createApp } from "vinxi";
import react from "@vitejs/plugin-react";
import tailwindcss from "@tailwindcss/vite";
import tsconfigPaths from "vite-tsconfig-paths";

export default createApp({
  routers: [
    { name: "public", type: "static", dir: "./public" },
    { name: "api-proxy", type: "http", handler: "./server.ts", target: "server", base: "/api" },
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
      },
      plugins: () => [tsconfigPaths(), react(), tailwindcss()],
    },
  ],
});
```

### Target: frontend/backoffice/app.config.ts
```ts
import { createApp } from "vinxi";
import react from "@vitejs/plugin-react";
import tailwindcss from "@tailwindcss/vite";
import tsconfigPaths from "vite-tsconfig-paths";

export default createApp({
  routers: [
    { name: "public", type: "static", dir: "./public" },
    { name: "api-proxy", type: "http", handler: "./server.ts", target: "server", base: "/api" },
    {
      name: "client",
      type: "spa",
      handler: "./index.html",
      vite: {
        server: {
          host: "0.0.0.0",
          port: 5174,
          hmr: { host: "localhost", port: 5174, clientPort: 5174 },
          watch: { usePolling: true, interval: 1000 },
        },
      },
      plugins: () => [tsconfigPaths(), react(), tailwindcss()],
    },
  ],
});
```

### Target: compose.yaml (frontend services only)
```yaml
  frontend-client:
    build:
      context: ./frontend/client
      dockerfile: Dockerfile
    environment:
      VITE_API_URL: http://localhost:8080
      VITE_KEYCLOAK_URL: http://localhost:8180
      VITE_KEYCLOAK_REALM: onboarding
      VITE_KEYCLOAK_CLIENT_ID: onboarding-app
    ports:
      - "127.0.0.1:5173:5173"

  frontend-backoffice:
    build:
      context: ./frontend/backoffice
      dockerfile: Dockerfile
    environment:
      VITE_API_URL: http://localhost:8080
      VITE_KEYCLOAK_URL: http://localhost:8180
    ports:
      - "127.0.0.1:5174:5174"
```

### Target: frontend/client/Dockerfile
```dockerfile
FROM node:22-alpine AS dev
WORKDIR /app
COPY package.json package-lock.json* ./
RUN npm install
COPY . .
EXPOSE 5173
CMD ["npm", "run", "dev"]
```

### Target: frontend/backoffice/Dockerfile
```dockerfile
FROM node:22-alpine AS dev
WORKDIR /app
COPY package.json package-lock.json* ./
RUN npm install
COPY . .
EXPOSE 5174
CMD ["npm", "run", "dev"]
```

### Target: frontend/client/package.json (scripts)
```json
{
  "name": "frontend-client",
  "scripts": {
    "dev": "vinxi dev --port 5173 --host",
    "build": "vinxi build",
    "start": "vinxi start",
    "test": "vitest run",
    "test:watch": "vitest"
  }
}
```

### Target: frontend/backoffice/package.json (scripts)
```json
{
  "name": "frontend-backoffice",
  "scripts": {
    "dev": "vinxi dev --port 5174 --host",
    "build": "vinxi build",
    "start": "vinxi start",
    "test": "vitest run",
    "test:watch": "vitest"
  }
}
```

## Docker Compose Strategy

### Servicos de Frontend

Ambos os servicos seguem o mesmo padrao, diferindo apenas em:
1. **Build context:** `./frontend/client` vs `./frontend/backoffice`
2. **Port mapping:** `5173:5173` vs `5174:5174`
3. **Environment variables:** Client precisa de Keycloak vars; Backoffice nao (usa cookies httpOnly para auth admin via backend)

### Bind mounts para desenvolvimento

Para HMR funcionar com bind mounts em Docker Compose, adicione volumes:

```yaml
  frontend-client:
    build:
      context: ./frontend/client
      dockerfile: Dockerfile
    volumes:
      - ./frontend/client/src:/app/src
      - ./frontend/client/public:/app/public
      - ./frontend/client/server.ts:/app/server.ts
      - ./frontend/client/app.config.ts:/app/app.config.ts
    environment:
      VITE_API_URL: http://localhost:8080
    ports:
      - "127.0.0.1:5173:5173"
    depends_on:
      api:
        condition: service_healthy

  frontend-backoffice:
    build:
      context: ./frontend/backoffice
      dockerfile: Dockerfile
    volumes:
      - ./frontend/backoffice/src:/app/src
      - ./frontend/backoffice/public:/app/public
      - ./frontend/backoffice/server.ts:/app/server.ts
      - ./frontend/backoffice/app.config.ts:/app/app.config.ts
    environment:
      VITE_API_URL: http://localhost:8080
    ports:
      - "127.0.0.1:5174:5174"
    depends_on:
      api:
        condition: service_healthy
```

### Dependencias

Ambos dependem do `api` com `condition: service_healthy`. Nao dependem diretamente do Keycloak (o backend gerencia isso). O proxy h3 encaminha requests para o API, que por sua vez fala com o Keycloak.

### Remocao do servico original

Apos a migracao, remova o servico `frontend:` original do `compose.yaml`. Mantenha apenas `frontend-client:` e `frontend-backoffice:`.

### Frontend Base URL na API

A variavel `Frontend__BaseUrl` no servico `api` deve ser atualizada se o backend precisar redirecionar para frontends especificos. Para o fluxo de forgot/reset password, o backend provavelmente precisa saber a URL do client (`http://localhost:5173`). O backoffice nao recebe redirecionamentos do backend, entao nao precisa de entrada no `Frontend__BaseUrl`.
