---
status: awaiting_human_verify
trigger: "Frontend no browser retorna a página HTML mas todos os recursos Vite/Vinxi falham com 503 Service Unavailable. Página fica em branco."
created: 2026-04-07T00:00:00Z
updated: 2026-04-07T00:00:00Z
---

## Current Focus

hypothesis: O frontend depende de `api: condition: service_healthy`, o que faz o container do frontend esperar a API estar saudável antes de iniciar. A API aguarda Keycloak (que aguarda keycloak_db). A cadeia toda pode levar vários minutos. Durante esse tempo a porta 5173 está mapeada mas o processo Vite ainda não iniciou — então o container responde 503 (serviço indisponível). Hipótese secundária: o Dockerfile usa `npm run dev` (vinxi dev) mas o processo Vite dentro do container falha ao servir módulos @fs por problema de permissões ou caminho.
test: Verificar se o 503 ocorre antes do Vite subir (timing/depends_on) ou depois (misconfiguration do servidor)
expecting: depends_on api:healthy é desnecessário para o frontend e causa a janela de 503
next_action: Confirmar causa raiz e aplicar fix no compose.yaml

## Symptoms

expected: http://localhost:5173 carrega a SPA React normalmente
actual: index.html carrega mas todos os módulos JS falham com 503:
  - GET http://127.0.0.1:5173/@fs/app/node_modules/vinxi/runtime/client.js → 503
  - GET http://127.0.0.1:5173/@vite/client → 503
  - GET http://127.0.0.1:5173/@react-refresh → 503
  - GET http://127.0.0.1:5173/src/main.tsx → 503
  - favicon.ico → 404
errors: net::ERR_ABORTED 503 (Server Unavailable) para todos os módulos JS
reproduction: docker compose up → abrir http://localhost:5173
started: após fix do healthcheck da API (Keycloak__AuthServerUrl adicionado ao compose.yaml)

## Eliminated

(nenhuma hipótese eliminada ainda)

## Evidence

- timestamp: 2026-04-07T00:00:00Z
  checked: frontend/Dockerfile
  found: Imagem usa `FROM node:22-alpine AS dev`, CMD é `npm run dev` (vinxi dev). Nenhum estágio de build/produção. O servidor de dev é o que roda no container.
  implication: Não é um problema de build vs dev — o servidor correto está sendo iniciado.

- timestamp: 2026-04-07T00:00:00Z
  checked: frontend/package.json scripts
  found: `"dev": "vinxi dev --port 5173 --host"` — servidor de desenvolvimento Vite com HMR.
  implication: Quando o Vite está rodando, @vite/client e @react-refresh devem ser servidos normalmente. Se estão retornando 503, o Vite ainda não subiu ou travou.

- timestamp: 2026-04-07T00:00:00Z
  checked: compose.yaml — serviço frontend, depends_on
  found: |
    frontend:
      depends_on:
        api:
          condition: service_healthy
  implication: O container do frontend AGUARDA a API estar healthy antes de iniciar. A API por sua vez aguarda Keycloak (service_healthy) que aguarda keycloak_db (service_healthy). Cadeia total: keycloak_db healthy → Keycloak healthy (60s start_period + 10x15s retries = até 210s) → API healthy (30s start_period + 5x10s = 80s). O frontend pode demorar 3-5 minutos para iniciar. Durante esse período a porta 5173 está exposta mas o processo Node ainda não iniciou.

- timestamp: 2026-04-07T00:00:00Z
  checked: compose.yaml — porta do frontend
  found: `ports: - "127.0.0.1:5173:5173"` mapeada independentemente do depends_on.
  implication: O Docker mapeia a porta no host assim que o container é criado (não quando o processo dentro inicia). Mas como depends_on com condition impede o container de iniciar, a porta 5173 provavelmente não responde durante a espera — o 503 provavelmente vem do curl ou browser ao tentar conectar antes do container iniciar.

- timestamp: 2026-04-07T00:00:00Z
  checked: app.config.ts — configuração do servidor Vite
  found: server.host = "0.0.0.0", port 5173, HMR configurado. Configuração correta para Docker.
  implication: Quando o Vite finalmente sobe, a configuração está correta. O problema é temporal — o usuário abre o browser antes do frontend container iniciar.

- timestamp: 2026-04-07T00:00:00Z
  checked: Relação entre frontend e API
  found: O frontend é um SPA puro — não faz SSR, não depende da API para servir arquivos estáticos. A dependência `api: service_healthy` é desnecessária para o processo de dev do Vite.
  implication: O frontend pode (e deve) subir independentemente. A API só é necessária para chamadas AJAX do browser, que são feitas pelo usuário após o carregamento — não pelo servidor Vite.

## Resolution

root_cause: O serviço `frontend` no compose.yaml tem `depends_on: api: condition: service_healthy`, criando uma cadeia de dependências desnecessária (frontend → api → keycloak → keycloak_db). O container do frontend só inicia após toda essa cadeia estar saudável, o que pode levar 3-5 minutos. O browser abre http://localhost:5173 antes do Vite ter iniciado, recebendo 503. O frontend é um SPA dev server (Vite) que não tem dependência real de runtime na API — ele apenas serve arquivos estáticos JS/HTML.
fix: Remover o `depends_on` do serviço frontend no compose.yaml. O Vite dev server não precisa que a API esteja healthy para servir os módulos JS. O browser fará as chamadas à API só depois do SPA carregar — e se a API ainda não estiver pronta, o frontend pode exibir um estado de loading/erro adequado.
verification: pendente
files_changed: [compose.yaml]
