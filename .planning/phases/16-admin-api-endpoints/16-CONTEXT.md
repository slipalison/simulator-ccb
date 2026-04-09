# Phase 16 Context: Admin API Endpoints

## Vision

CRUD puro com Keycloak como fonte principal de auth status. Endpoints que leem do app_db para dados cadastrais e integram com Keycloak Admin API para status do usuário (bloqueado, ativo, deletado). Tudo via CQRS handlers com injeção direta de DI (sem MediatR).

O admin acessa os endpoints com `[Authorize(Roles = "admin")]` — não-admin recebe 403 Forbidden.

## Essential

### Deleção LGPD Robusta (prioridade máxima)
- DELETE exige body com email do usuário como confirmação — se não bater, retorna 400
- Anonimiza dados no PostgreSQL (não é hard delete — é scrub de PII)
- Deleta usuário no Keycloak via Admin API
- Gera audit log com timestamp, admin executor, e ação performed
- Se Keycloak delete falhar após anonymize: compensação/rollback strategy necessária

### Block/Unblock Atômicos
- POST /admin/users/{id}/block e /admin/users/{id}/unblock
- Estado não pode ficar inconsistente entre app_db e Keycloak
- Audit log em cada ação

### Listagem Paginada
- GET /admin/users com paginação, busca (nome, CPF/CNPJ, email) e filtro por status
- Retorna dados unificados: app_db + Keycloak status

### Update com Validação
- PUT /admin/users/{id} com validação server-side completa (FluentValidation)
- Audit log de mudanças

## Specifics

### Audit Logging em Tudo
Cada ação admin (create, read, update, block, unblock, delete) gera um registro de auditoria no app_db contendo:
- Quem fez (admin ID)
- O que fez (ação)
- Quando fez (timestamp UTC)
- Em quem fez (target user ID)
- Snapshot antes/depois (para update)

Isso é essencial para compliance LGPD e investigação de incidentes.

### Formato de Confirmação LGPD
```json
POST /api/admin/users/{id}/delete
{
  "confirmEmail": "usuario@email.com"
}
```
Se `confirmEmail` não bater com o email do usuário → 400 Bad Request.

## Notes

- Phase 16 é **só a API** — frontend vem na Phase 18/19
- Stack: Controllers ASP.NET (sem Minimal API), CQRS manual via DI, FluentValidation, Keycloak Admin SDK
- 3 planos: DTOs/validação, controller/handlers, auth middleware + role mapping
- Depende de Phase 5 (Registration API) e Phase 6 (Authentication API) já estarem funcionais
