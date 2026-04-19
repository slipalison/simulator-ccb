# Isolar Backoffice e Client em Realms Separados

Este plano detalha a refatoração necessária para dividir o ambiente atual de um único realm (`onboarding`) em dois realms separados (`backoffice` e `client`). Isso resolve de forma arquitetural e segura o problema de Single Sign-On cruzado entre os dois portais.

## User Review Required

> [!WARNING]
> Essa é uma mudança arquitetural significativa. O banco de dados do Keycloak atual (no Docker) perderá os usuários que foram criados manualmente no realm `onboarding` antigo. Será necessário recriar o usuário admin inicial e zerar a base (compose down -v).
> Você está de acordo com essa mudança e limpeza do banco de dados local?

## Proposed Changes

---

### 1. Keycloak Configs & Docker

- Excluir `keycloak/onboarding-realm.json`.
- Criar **dois novos arquivos** exportados a partir de templates:
#### [NEW] `keycloak/backoffice-realm.json`
- Realm ID: `backoffice`.
- Clientes: `onboarding-backoffice` e `onboarding-api-admin` (Client do Backend).
- Roles configuradas para os admins.
#### [NEW] `keycloak/client-realm.json`
- Realm ID: `client`.
- Clientes: `onboarding-client-acf` e `onboarding-client-ropc`.
#### [MODIFY] `docker-compose.yml`
- Atualizar a importação no serviço `keycloak` para carregar a pasta inteira (`/opt/keycloak/data/import/`).
- Atualizar as variáveis de ambiente dos serviços do frontend (o backoffice apontará para `KEYCLOAK_REALM: backoffice` e o client para `KEYCLOAK_REALM: client`).

---

### 2. Backend Infrastructure (`KeycloakUserService` & `KeycloakTokenService`)

Atualmente a SDK do Keycloak no Backend aponta estaticamente para um único realm (`onboarding`). 

#### [MODIFY] `src/Onboarding.Infrastructure/Keycloak/IKeycloakUserService.cs` (e implementações)
- Adicionar suporte a roteamento de Realm nas chamadas de Admin API. Métodos como `CreateUserAsync` ou `SetPasswordAsync` precisarão de um parâmetro de contexto indicando se é do Client ou do Backoffice.
- O TokenService usará credenciais do realm `client` (via ROPC, se for o caso do client) ou o backend usará a config de Admin. Precisaremos dar acesso cross-realm para a API ou usar um ClientAdmin em ambos os realms. O mais fácil é criar um `onboarding-api-admin` em CADA realm e abstrair a autenticação.

---

### 3. Backend API Auth Pipeline (`Program.cs`)

Como haverão dois realms ativados emitindo tokens, o Backend deve esperar e validar duas assinaturas distintas (Issuers diferentes).

#### [MODIFY] `src/Onboarding.API/Program.cs`
- Substituir o `.AddJwtBearer()` por **dois esquemas independentes**:
  - `AddJwtBearer("BearerBackoffice")` (com discovery no Realm backoffice)
  - `AddJwtBearer("BearerClient")` (com discovery no Realm client)
- Adicionar um middleware de Autorização customizado (ou atributos no Controller) `[Authorize(AuthenticationSchemes = "BearerBackoffice")]` para endpoints `/api/admin` e `"BearerClient"` para `/api/clients`.

## Open Questions

1. O serviço Administrativo da API de `.NET` deverá acessar o Keycloak para registrar tanto Admins quanto Clients. Ao dividirmos o Realm, a API se comunicará com os dois. Queremos duplicar a conta de Serviço `onboarding-api-admin` (uma em cada realm) para que a API possa fazer chamadas de gerenciamento de forma limpa?
2. Precisaremos de um `docker compose down -v` para descartar o `onboarding` atual. Esta parte está de acordo para você?

## Verification Plan

### Testes
- Atualizar os testes automatizados (.NET) que usam configurações single-realm.
- Subir a stack com Docker Compose, verificar a importação do Keycloak (que deve estar com dois Realms limpos).
- Verificar na UI que o login em `localhost:5174` não reaproveita o cookie pro `localhost:5173`. A isolação será garantida em nível nativo pelo Keycloak.
