# Phase 49 - api-proxy 503 - Investigation

## TL;DR

O 503 `TypeError: fetch failed` no `POST /api/companies/registration` (e em qualquer outro path `/api/*`) **nao e um bug no codigo do proxy nem na stack Vinxi/h3/undici**. E um **problema de roteamento de porta no host**: uma instancia de `vinxi dev` esta rodando direto no Windows host (PID 50572, Node v24.14.0, iniciada por `pnpm dev` ha ~7h) **em paralelo** com o container `frontend-client` (PID 18, Node v22.22.3). O processo do host ouve em `0.0.0.0:5173` + `[::]:5173`, o do container expoe `127.0.0.1:5173` via mapping. Quando o browser/curl resolve `localhost` -> `::1` (preferencia IPv6 default no Windows), o trafego cai no processo do host, que **nao tem rota para a bridge `onboarding-net` do Docker** e portanto nao consegue resolver `api` (ENOTFOUND) nem alcancar `172.18.0.10:8080` (ConnectTimeout). Resultado: o handler do `server.ts` faz `fetch("http://api:8080/...")`, recebe `TypeError: fetch failed`, e o h3 transforma em `503`. **A correcao e parar o processo host e disciplinar o workflow para nunca mais rodar `pnpm dev` fora do `docker compose`.**

## Bug reproduction

### Steps

1. Containers do projeto rodando via `docker compose up` (estado atual confirmado em `docker compose ps`).
2. Em paralelo, existe ha ~7h um `pnpm dev` rodando no Windows host (em D:\REPO\keycloak-tests\frontend\client) - **estado nao-orquestrado e ate este momento invisivel para quem so olha o compose**.
3. Browser/curl em `http://localhost:5173/api/companies/registration`.

### Observed output

```
$ curl -sS -o /tmp/r.out -w 'status=%{http_code}\n' \
    -X POST -H 'Content-Type: application/json' -d '{}' \
    http://localhost:5173/api/companies/registration
status=503

<!DOCTYPE html>
<html><body>
  <h2>503 Server Unavailable</h2>
  <pre>TypeError: fetch failed</pre>
</body></html>
```

E quando forcamos `localhost` -> `127.0.0.1` IPv4 (que rota para o port mapping do Docker):

```
$ curl -sS -4 -o /tmp/r4.out -w 'status=%{http_code}\n' \
    -X POST -H 'Content-Type: application/json' -d '{}' \
    http://127.0.0.1:5173/api/companies/registration
status=422
{"title":"One or more validation errors occurred.","errors":{...}}
```

422 = backend rejeitando payload vazio (esperado). Provando que **quando o request chega no container, tudo funciona**.

### Smoking gun: dois listeners em :5173

```
$ netstat -ano | findstr ":5173 "
  TCP    0.0.0.0:5173    0.0.0.0:0    LISTENING    50572   <-- host vinxi (Node 24)
  TCP    127.0.0.1:5173  0.0.0.0:0    LISTENING    24376   <-- com.docker.backend (port map)
  TCP    [::]:5173       [::]:0       LISTENING    50572   <-- host vinxi tambem em IPv6
```

```
$ Get-Process -Id 50572 | fl Path, CommandLine
Path        : C:\Program Files\nodejs\node.exe
CommandLine : node "D:\REPO\keycloak-tests\frontend\client\node_modules\.bin\..\vinxi\bin\cli.mjs" dev --port 5173 --host
StartTime   : 16/05/2026 09:23:17     <-- iniciado ha 7+ horas pelo `pnpm dev`
```

### Diagnostic data (read-only diag endpoint temporario em `server.ts`, ja revertido)

Endpoint `__diag` adicionou print de DNS lookup, `undici.getGlobalDispatcher()`, `http.request` raw, `globalThis.fetch`, env, etc. Resultados:

**Via `curl http://localhost:5173/api/__diag` (resolve a `::1` -> host vinxi):**
```json
{
  "node_version": "v24.14.0",       <-- HOST, nao container (container e 22.22.3)
  "pid": 50572,                       <-- PID do host, nao do container (container e 18)
  "uptime_s": 25196.4,                <-- 6.9 horas (container restartou ha minutos)
  "dns_lookup_api_all": { "error": "getaddrinfo ENOTFOUND api", "code": "ENOTFOUND" },
  "raw_http_ip": { "error": "timeout" },                                       // raw http -> 172.18.0.10:8080 = timeout
  "raw_http_dns": { "error": "getaddrinfo ENOTFOUND api" },
  "global_fetch_dns": { "error": "fetch failed", "cause": "ENOTFOUND" },
  "global_fetch_ip": { "error": "fetch failed", "cause": "ConnectTimeoutError ... 172.18.0.10:8080, timeout: 10000ms" },
  "undici_client_dns": { "error": "ENOTFOUND" },
  "undici_client_ip": { "error": "Connect Timeout Error" },
  "env": { "HTTP_PROXY": null, "HTTPS_PROXY": null, "NO_PROXY": null, ... },   // sem proxy env
  "dns_default_result_order": "verbatim",
  "global_dispatcher": { "ctor": "Agent", ... },                               // dispatcher default, sem patch
  "fetch_native": "function fetch(input, init = undefined) { ... }"            // fetch nativo Node, nao monkey-patched
}
```

**Via `curl -4 http://127.0.0.1:5173/api/__diag` (rota IPv4 -> Docker port map -> container vinxi):**
```json
{
  "node_version": "v22.22.3",         <-- CONTAINER
  "pid": 18,                          <-- container vinxi
  "uptime_s": 134.9,                  <-- recem restartado
  "dns_lookup_api_all": [ { "address": "172.18.0.10", "family": 4 } ],         // resolve OK
  "raw_http_ip": { "status": 200, "body": "Healthy" },                          // tudo funciona
  "raw_http_dns": { "status": 200, "body": "Healthy" },
  "global_fetch_dns": { "status": 200, "body": "Healthy" },
  "global_fetch_ip": { "status": 200, "body": "Healthy" },
  "undici_client_dns": { "status": 200 },
  "undici_client_ip": { "status": 200 },
  ...
}
```

Resultado identico mostra: **o codigo de `server.ts` esta correto. O ambiente de execucao e que esta errado.**

## Root cause

**Categoria: nao-listada (proxima de C — runtime errado — e G — env mismatch).** Nenhuma das hipoteses A-G do briefing se aplica:

| Hipotese | Veredito |
|---|---|
| (A) `setGlobalDispatcher` com pool quebrado | Falso. Diag mostra `Agent` default sem options. Grep em `vinxi`, `h3`, `listhen`, `nitropack` por `setGlobalDispatcher` retorna **0 hits relevantes** — apenas `undici/index.js` (definicao) e `node-fetch-native/dist/proxy.cjs` (nao importado por nenhum dos pacotes ativos no dev runtime). |
| (B) `dns.setDefaultResultOrder("verbatim")` em algum import | Verbatim e o **default Node 22+**, nao foi setado por ninguem. Mesmo se fosse `ipv4first` nao mudaria nada — o DNS Docker (`127.0.0.11`) so existe dentro do container, nao no host. O Windows host nao tem entrada para `api`, por isso ENOTFOUND. |
| (C) VM sandbox/Worker limitando `net` | Falso. O container roda vinxi normal, e raw `http.request` funciona la dentro. |
| (D) IPv4 vs IPv6 (undici AAAA) | **Parcialmente verdadeiro mas no nivel errado:** o problema de IPv6 nao e o undici escolhendo AAAA pro upstream — e o **client (browser/curl) resolvendo `localhost` -> `::1`** e caindo no listener IPv6 do **processo host errado**. |
| (E) Dual undici por pnpm symlinks | Falso. Container nao usa pnpm (Dockerfile faz `npm ci`). `ls /app/node_modules/.pnpm` nao existe no container; existe so no host. |
| (F) HTTP/2 ou TLS reescrito | Falso. Tudo HTTP/1.1 plain. |
| (G) Proxy env injetado | Falso. `HTTP_PROXY`, `HTTPS_PROXY`, `NO_PROXY` todos `null` no host vinxi (per diag). |

**A causa real:** o usuario (provavelmente em algum momento de troubleshooting ou IDE auto-start) executou `pnpm dev` no diretorio `frontend/client` no host Windows. Vinxi bindou em `0.0.0.0:5173` e `[::]:5173`. O `compose.yaml` tambem expoe `127.0.0.1:5173` -> container 5173. Os dois listeners coexistem (Windows permite porque sao endereco-especificos diferentes), mas quando algo resolve `localhost`:

1. Windows tem `localhost` IPv6-first por default (sem entrada no `hosts` file, vai via DNS interno que devolve `::1` antes de `127.0.0.1`).
2. Browser/curl tenta `::1:5173` primeiro -> bate em `[::]:5173` -> **processo host** atende.
3. Host vinxi roda `fetch("http://api:8080")` -> resolve `api` via Windows DNS -> ENOTFOUND (host nao conhece nomes Docker).
4. h3/Nitro transforma a `TypeError: fetch failed` em **503 Server Unavailable** com o template HTML que o usuario viu.

**Evidencia direta (arquivo:linha):** nao ha codigo "culpado" no repo. O culpado e `package.json:7`:
```json
"scripts": { "dev": "vinxi dev --port 5173 --host", ... }
```
combinado com `pnpm dev` executado no shell do Windows. Nenhuma instrucao no README, CLAUDE.md, ou docs/ explicita que `pnpm dev` no host esta **proibido** porque conflita com o compose.

## Why it ONLY fails in the long-running Vinxi process

A pergunta original era *"por que `node -e fetch(...)` fresh funciona dentro do container mas o Vinxi long-running falha?"* — a premissa esta errada. **Os dois ambientes nao sao o mesmo processo:**

- `docker compose exec frontend-client node -e 'fetch(...)'` cria um Node **novo dentro do container**, mesma network, mesmo DNS resolver (`127.0.0.11`), enxerga a bridge Docker -> resolve `api` -> sucesso.
- "Vinxi long-running" no diagnostico do usuario era na verdade o **vinxi do host**, que esta em outro espaco de rede (Windows host, sem rota pra `onboarding-net`). Ele nunca conseguiu nem vai conseguir resolver `api` ou alcancar `172.18.0.10`.

**A diferenca mecanica nao e fresh-vs-long-running. E container-vs-host.** O que confunde e que `curl http://localhost:5173` no host nao deixa claro qual dos dois listeners atendeu, e nada nos logs do `docker compose logs frontend-client` mostra os requests que estao caindo no host (porque eles nunca chegam ao container).

Os logs do container so registraram os startup banners porque o container reinicia, mas **nenhum POST /api/... aparece la** — outra pista forte que os requests nao estao chegando.

## Recommended fix

### Opcao primaria (minima invasiva, recomendada)

**Disciplina de workflow + guard automatico.** Tres mudancas pequenas, todas em arquivos ja existentes ou de configuracao:

1. **Matar o processo host imediatamente** (sem comitar nada):
   ```powershell
   Stop-Process -Id 50572 -Force        # host vinxi client :5173
   Stop-Process -Id 30044 -Force        # host vinxi backoffice :5174
   Stop-Process -Id 50972 -Force        # host vinxi backoffice :5174 (segundo PID)
   Stop-Process -Id 17212 -Force        # host vinxi client :5173 (segundo PID)
   ```
   Depois disso, `curl http://localhost:5173/api/companies/registration` vai retornar 422 ou 200 conforme payload — o request roteia para `127.0.0.1:5173` -> container, **bug some**.

2. **Hosts file shim para forcar `localhost` -> 127.0.0.1 IPv4 quando o usuario testar via browser** (opcional, mas robusto contra recidiva):
   - Adicionar em `C:\Windows\System32\drivers\etc\hosts`:
     ```
     127.0.0.1 localhost
     ```
     comentando ou removendo o `::1` se existir. Isso garante que mesmo se um processo host stale renascer, `localhost` vai para o port mapping do Docker primeiro.
   - **Caveat seguranca:** mexer no hosts file e mudanca por-maquina. Documentar em `docs/dev-setup.md` (criar se nao existe) e nao automatizar.

3. **Guard em `scripts/dev-up.sh`** (criar se nao existe) ou adicao em `package.json` scripts a nivel root para abortar `pnpm dev`/`npm run dev` se detectar containers ja em execucao:
   ```js
   // scripts/check-dev-env.mjs (Node, multiplatform)
   import { execSync } from "node:child_process";
   try {
     const ps = execSync("docker compose ps --status running --services", { encoding: "utf-8" });
     if (ps.split("\n").map(s => s.trim()).filter(Boolean).includes("frontend-client")) {
       console.error("[abort] frontend-client container is already running. Use 'docker compose logs -f frontend-client' or 'docker compose exec frontend-client npm run dev' instead of `pnpm dev` on the host.");
       process.exit(1);
     }
   } catch { /* docker not running, ok */ }
   ```
   Hookar via `"predev": "node ../../scripts/check-dev-env.mjs"` em `frontend/client/package.json` e `frontend/backoffice/package.json`.

4. **Documentar no `README.md` e `CONTRIBUTING.md`** o **unico workflow valido** para desenvolvimento: `docker compose up` na raiz. Se precisar de hot reload, editar files (bind mounts ja em compose.yaml:104-109 reflete dentro do container). Nunca rodar `pnpm dev` no host.

### Side-effects esperados / regressoes a testar

- Mudanca em `hosts` afeta **todo** uso de `localhost` no host. Se algum dev tem ferramenta dependendo de `::1`, vai parar. Avaliar com o usuario antes de aplicar.
- O `predev` guard pode bloquear legitimamente alguem que **quer** rodar Vinxi fora do container (ex: debugger anexado). Solucao: aceitar env var `ALLOW_HOST_DEV=1` pra burlar. Documentar.
- Outra alternativa: mudar `frontend/client/playwright.config.ts:22` e `frontend/backoffice/playwright.config.ts:27` de `baseURL: 'http://localhost:5173'` para `http://127.0.0.1:5173`. Isso garante que **os testes** sempre falham pelo motivo certo (forcando IPv4). Mas nao protege o browser do dev quando ele testa manualmente.

### Opcao alternativa (se primaria custar muito)

**Forcar Docker port mapping em `0.0.0.0`** ao inves de `127.0.0.1`, e usar uma porta diferente da que o vinxi-host costuma usar. Por exemplo em `compose.yaml:120`:

```yaml
ports:
  - "127.0.0.1:5273:5173"   # +100 offset, fora do range default do vinxi
```

E ajustar `KEYCLOAK_REDIRECT_URI`, `FRONTEND_URL`, configs Keycloak (`backoffice-realm.json:50-56`, `client-realm.json:58-63`) e Playwright (`playwright.config.ts:22,27`) pra `:5273`/`:5274`. **Custo alto:** mexe em redirect URIs do Keycloak que tem que dar match exato, pode quebrar OAuth flow se algum lugar esquecido ainda referenciar `:5173`. **Nao recomendado** porque o fundo da questao (devs rodando dois servidores) continua, so muda o sintoma.

## Files to modify (for JDI plan)

Especialista atribuido: **`jdi-doer-onboarding-keycloak-frontend-vinext`** (todos os arquivos sao `frontend/**` ou scripts/docs root).

| File:line | Mudanca proposta | Escopo |
|---|---|---|
| `scripts/check-dev-env.mjs` (NOVO) | Implementar guard Node que detecta `docker compose ps --status running --services` contendo `frontend-client` ou `frontend-backoffice` e aborta `pnpm dev`. Bypass via `ALLOW_HOST_DEV=1`. | Novo arquivo, ~25 linhas, sem deps externas |
| `frontend/client/package.json:6` | Adicionar `"predev": "node ../../scripts/check-dev-env.mjs frontend-client"` antes de `"dev"`. | 1 linha |
| `frontend/backoffice/package.json:6` | Mesma adicao para `frontend-backoffice`. | 1 linha |
| `docs/dev-setup.md` (NOVO) | Documentar workflow oficial: `docker compose up`, NUNCA `pnpm dev` no host. Listar como confirmar via `netstat`/`Get-NetTCPConnection` se ha vinxi-host stale. Listar como limpar (`Stop-Process` no Windows, `pkill node` no *nix). | ~40 linhas docs |
| `README.md` | Adicionar secao "Local development" linkando para `docs/dev-setup.md` e incluindo aviso curto: "Do not run `pnpm dev` directly on the host — use `docker compose up`. See [dev-setup.md](docs/dev-setup.md)." | +5-10 linhas |
| `CONTRIBUTING.md` | Mencao explicita do mesmo workflow no setup inicial. | +3-5 linhas |
| `frontend/client/playwright.config.ts:22` | Trocar `'http://localhost:5173'` por `'http://127.0.0.1:5173'`. Garante que testes Playwright sempre roteiam para IPv4 (port mapping Docker), evitando falsos negativos se um vinxi-host residual existir. | 1 linha |
| `frontend/client/pw-no-setup.config.ts:12` | Mesma troca. | 1 linha |
| `frontend/backoffice/playwright.config.ts:27` | Mesma troca para `:5174`. | 1 linha |

**Nada em `server.ts`, `auth-server.ts`, nem `app.config.ts` precisa mudar.** Codigo do proxy esta correto.

## Tests to add (Vitest + Playwright)

### Vitest (rapido, local)

1. **`scripts/check-dev-env.test.mjs`** — testar o guard:
   - Sucesso: `docker compose ps` retorna sem frontend services -> exit 0.
   - Falha: simular saida com `frontend-client` -> exit 1 com mensagem clara.
   - Bypass: `ALLOW_HOST_DEV=1` -> exit 0 mesmo com container ativo.

### Playwright (e2e contra stack docker compose)

2. **`frontend/client/playwright/api-proxy.spec.ts`** (NOVO):
   - **Cenario "proxy reaches backend":** `POST /api/companies/registration` com payload invalido -> espera **422 com body JSON contendo erros de validacao** (nunca 503 HTML). Garante que o request realmente saiu do proxy e chegou no .NET.
   - **Cenario "proxy reaches healthz":** `GET /api/healthz/live` -> 200 "Healthy". (Esse path existe na API.)
   - **Cenario "host vinxi shadowed":** rodar `Get-NetTCPConnection -LocalPort 5173 -State Listen` (PowerShell) ou `ss -tlpn` (Linux) antes da suite — fail-fast se houver mais de UM listener em `:5173`. Implementar via Playwright `globalSetup`.

3. **`frontend/backoffice/playwright/api-proxy.spec.ts`** (NOVO):
   - Equivalente para `:5174`. Mesmos 3 cenarios.

4. **Em ambos:** Atualizar testes existentes para garantir que `baseURL` aponta para `127.0.0.1` (nao `localhost`), passando como assert no setup.

### Doc test (manual, parte do CONTRIBUTING)

5. Listar no `docs/dev-setup.md` um "smoke test 30s" que o dev faz logo apos clonar: `docker compose up -d && curl -sf http://127.0.0.1:5173/api/healthz/live`. Falhou? Vai pro troubleshooting.

## Open questions

1. **Porque o `pnpm dev` host esta rodando?** Cmdline mostra `pnpm dev` sem flags, e os PIDs pai mostram `cmd.exe /d /s /c vinxi dev --port 5173 --host`. Provavelmente foi o usuario em outro chat/sessao explorando o codigo, ou uma extensao de IDE (VSCode `npm.autoDetect`?) que dispara scripts ao abrir o workspace. Vale checar `.vscode/launch.json`/`tasks.json` se existir, e o `.idea/` (do JetBrains) para auto-startup tasks.
2. **O `@/router` import-fail no SSR (item 8 da lista de findings) e na verdade do host vinxi tambem?** Quase certamente sim — o host nao tem o mesmo `tsconfig.json` paths resolvido como o container (caminho Windows `D:\REPO\...` vs `/app/src/...` que o `tsconfigPaths()` plugin esta esperando). Confirmar apos matar o processo: o log do container *real* nao deve ter esse warning.
3. **Backoffice (porta :5174) tem mesmo problema?** Sim, ja confirmado via `netstat` que ha um vinxi-host em `:5174` (PIDs 30044 + 50972). Mesma correcao se aplica.
4. **Os 4 vinxi processes no host estao em pares (cliente x backoffice, cada um 2 PIDs)** — provavelmente cada `pnpm dev` foi disparado **duas vezes** (talvez por hot-reload do pnpm watcher, talvez por um restart manual sem matar o anterior). Isso reforca a necessidade do guard `predev`.
5. **Vale documentar tambem que `docker compose restart frontend-client` nao reseta o vinxi-host stale?** Sim — adicionar nota no troubleshooting porque foi exatamente o ponto que confundiu a investigacao inicial.

## Annex: command snippets used during investigation (re-runnable)

```bash
# Listar processos node no host (PowerShell)
Get-CimInstance Win32_Process -Filter "Name='node.exe'" \
  | Select-Object ProcessId, ParentProcessId, CreationDate, CommandLine

# Listar listeners em 5173/5174 (PowerShell)
Get-NetTCPConnection -LocalPort 5173,5174 -State Listen \
  | Select-Object LocalAddress, LocalPort, OwningProcess, \
    @{N='Name';E={(Get-Process -Id $_.OwningProcess).ProcessName}}

# Hit a porta IPv4 forcado (esquiva do host vinxi)
curl -sS -4 http://127.0.0.1:5173/api/healthz/live

# Hit a porta IPv6 (atinge host vinxi se ele estiver up)
curl -sS 'http://[::1]:5173/api/healthz/live'

# Confirmar processo Vinxi no container
docker compose exec frontend-client sh -c 'ps -ef | grep vinxi'

# Run fetch fresh dentro do container (sempre funciona)
docker compose exec frontend-client node -e 'fetch("http://api:8080/healthz/live").then(r=>r.text()).then(console.log)'
```
