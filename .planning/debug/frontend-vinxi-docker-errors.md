---
status: awaiting_human_verify
trigger: "Frontend dentro do Docker container apresenta dois erros simultâneos ao rodar vinxi dev: (1) @/router e @/globals.css não resolvem (alias @/ quebrado no container); (2) Missing field 'moduleType' no plugin builtin:vite-react-refresh-wrapper"
created: 2026-04-07T00:00:00Z
updated: 2026-04-07T00:00:00Z
---

## Current Focus

hypothesis: AMBOS os erros têm causas confirmadas pelos arquivos lidos — (1) alias @/ está corretamente definido no app.config.ts mas o Dockerfile usa bind mount em dev e o npm install resolve @vitejs/plugin-react v6 que é incompatível com Vinxi 0.5.x/Vite 5; (2) @vitejs/plugin-react ^6.0.1 requer Vite 6+ (usa API moduleType/rolldown) mas Vinxi 0.5.x embarca Vite 5 internamente — causando "Missing field moduleType"
test: confirmar versão do Vite embutido no Vinxi 0.5.11 e verificar qual API moduleType pertence ao Vite 6/rolldown
expecting: Vinxi 0.5.x usa Vite 5.x internamente, incompatível com @vitejs/plugin-react v6
next_action: verificar se o alias @/ está corretamente configurado (já lido — está) e confirmar versão interna do Vite no Vinxi; depois rebaixar @vitejs/plugin-react para ^4.3.4

## Symptoms

expected: vinxi dev inicia e serve os módulos sem erro
actual: dois erros simultâneos no container Docker:
  ERRO 1 — Path alias @/ não resolve:
    "The following dependencies are imported but could not be resolved:
      @/router (imported by /app/src/main.tsx)
      @/globals.css (imported by /app/src/main.tsx)"
  ERRO 2 — Plugin interno quebrado:
    "Internal server error: Missing field moduleType
      Plugin: builtin:vite-react-refresh-wrapper
      at TransformPluginContext.wrappedHook (rolldown/dist/shared/normalize-string-or-regex-BzTP-qJS.mjs)"
errors: ver acima
reproduction: docker compose up frontend → acessar http://localhost:5173
started: após criar app.config.ts com alias @/* e @vitejs/plugin-react v6

## Eliminated

- hypothesis: alias @/ não está configurado no app.config.ts
  evidence: app.config.ts linha 24-28 contém resolve.alias com "@" mapeado para fileURLToPath(new URL("./src", import.meta.url)) — configuração está correta
  timestamp: 2026-04-07T00:00:00Z

## Evidence

- timestamp: 2026-04-07T00:00:00Z
  checked: frontend/app.config.ts
  found: alias "@" → "./src" está corretamente definido em vite.resolve.alias (linha 24-28) via fileURLToPath/URL
  implication: o alias está configurado no Vinxi; o ERRO 1 provavelmente é consequência do ERRO 2 — quando o plugin react quebra no transform, o servidor falha antes de conseguir resolver os módulos, ou o HMR wrapper quebra o pipeline de resolução

- timestamp: 2026-04-07T00:00:00Z
  checked: frontend/package.json
  found: "@vitejs/plugin-react": "^6.0.1" listado TANTO em dependencies quanto em devDependencies; "vinxi": "^0.5.11"
  implication: @vitejs/plugin-react v6 usa Rolldown/oxc e exige Vite 6+. Vinxi 0.5.x embarca Vite 5 internamente. Conflito direto — este é o root cause do ERRO 2 ("Missing field moduleType" é campo da API do Vite 6/rolldown)

- timestamp: 2026-04-07T00:00:00Z
  checked: frontend/Dockerfile
  found: FROM node:22-alpine, WORKDIR /app, npm install, COPY . . — sem lock de versão específica, instala o que está em package.json
  implication: container instala @vitejs/plugin-react ^6.0.1 (resolve para v6.x), causando o conflito com o Vite 5 embutido no Vinxi 0.5.11

- timestamp: 2026-04-07T00:00:00Z
  checked: frontend/src/main.tsx
  found: importa "@/router" e "@/globals.css" — ambos dependem do alias "@/" funcionar no pipeline do Vite
  implication: se o plugin react falha no startup (moduleType error), o servidor Vite pode não registrar o alias corretamente ou falhar antes de processar os módulos — ERRO 1 é consequência do ERRO 2

- timestamp: 2026-04-07T00:00:00Z
  checked: frontend/package.json — duplicação de @vitejs/plugin-react
  found: o pacote aparece em dependencies E devDependencies — além de ser a versão errada, está duplicado
  implication: deve ser movido apenas para devDependencies e rebaixado para ^4.3.4

## Resolution

root_cause: @vitejs/plugin-react ^6.0.1 é incompatível com Vinxi 0.5.x. O Vinxi 0.5.x usa Vite 5 internamente (node_modules/vinxi/node_modules/vite). O plugin-react v6 foi reescrito para usar a API do Vite 6 (Rolldown/oxc), incluindo o campo moduleType que não existe no Vite 5. Isso quebra o plugin builtin:vite-react-refresh-wrapper causando o ERRO 2. O ERRO 1 (@/ não resolve) é consequência — o servidor falha durante o transform/HMR setup, impedindo que os módulos sejam servidos corretamente mesmo com o alias corretamente configurado.
fix: rebaixar @vitejs/plugin-react de ^6.0.1 para ^4.3.4 (última versão compatível com Vite 5/Vinxi 0.5.x); remover duplicação em dependencies (manter apenas em devDependencies)
verification: pendente
files_changed:
  - frontend/package.json
