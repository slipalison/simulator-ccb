---
status: complete
phase: 35-backoffice-admin-management-pagina-o-filtros-reset-senha-edi
source: [35-01-SUMMARY.md]
started: 2026-04-24T00:00:00Z
updated: 2026-04-24T11:00:00Z
---

## Current Test

[testing complete]

## Tests

### 1. Lista paginada de administradores
expected: GET /api/admin/administrators/paginated?page=1&pageSize=20 com token backoffice válido retorna 200 com objeto paginado { items, totalCount, page, pageSize }. Cada item contém id, firstName, lastName, email, enabled.
result: pass

### 2. Filtro por nome na lista paginada
expected: GET /api/admin/administrators/paginated?page=1&pageSize=20&name=alg (com nome parcial) retorna somente admins cujo firstName ou lastName contém a string filtrada. Admins sem match não aparecem.
result: pass

### 3. Filtro por status na lista paginada
expected: GET /api/admin/administrators/paginated?status=inactive retorna somente admins com enabled=false. GET ?status=active retorna somente enabled=true.
result: pass

### 4. Editar administrador — sucesso
expected: PUT /api/admin/administrators/{outroAdminId} com body { fullName, email novo } retorna 204. Admin tem novo nome/email no Keycloak.
result: pass

### 5. Editar administrador — bloqueio de auto-edição (SEC-01)
expected: PUT /api/admin/administrators/{seuProprioId} retorna 422 (não permite editar a própria conta).
result: pass
note: "Falhou inicialmente — sub ausente do token (basic scope faltando no realm). Fix aplicado inline: adicionado basic scope + claim.name nos mappers. Verificado: 422 com 'An administrator cannot edit their own account.'"

### 6. Editar administrador — email duplicado (SEC-04)
expected: PUT /api/admin/administrators/{outroAdminId} com email já pertencente a outro admin retorna 409 Conflict.
result: pass

### 7. Reset de senha — sucesso
expected: POST /api/admin/administrators/{outroAdminId}/reset-password retorna 200 com { temporaryPassword: "..." }. Senha tem 16+ chars.
result: pass

### 8. Reset de senha — bloqueio de auto-reset (SEC-01)
expected: POST /api/admin/administrators/{seuProprioId}/reset-password retorna 422.
result: pass

### 9. Desativar administrador — sucesso
expected: POST /api/admin/administrators/{outroAdminId}/toggle-status com { activate: false } retorna 204. Admin fica isEnabled: false no Keycloak.
result: pass

### 10. Desativar administrador — bloqueio de auto-desativação (SEC-01)
expected: POST /api/admin/administrators/{seuProprioId}/toggle-status com { activate: false } retorna 422.
result: pass

### 11. Desativar último admin ativo (SEC-05)
expected: Quando há apenas 1 admin ativo, POST toggle-status com { activate: false } retorna 400.
result: pass

### 12. Reativar administrador
expected: POST /api/admin/administrators/{adminDesativadoId}/toggle-status com { activate: true } retorna 204. Admin volta isEnabled: true.
result: pass

### 13. Endpoints requerem autenticação (SEC-02)
expected: Qualquer endpoint de /api/admin/administrators/* sem token retorna 401 Unauthorized.
result: pass

## Summary

total: 13
passed: 13
issues: 0
pending: 0
skipped: 0
blocked: 0

## Gaps

- truth: "PUT /api/admin/administrators/{seuProprioId} deve retornar 422 com 'An administrator cannot edit their own account.'"
  status: fixed
  reason: "User reported: 204. Ele permitiu a edição."
  severity: blocker
  test: 5
  root_cause: "access token do client onboarding-backoffice não contém claim 'sub' — backoffice-realm.json não define scope 'basic' nem mapper de sub. GetAuditContextSafe() retorna ActorSub='unknown'. Validator check 'targetId != ActorSub' compara GUID != 'unknown' → sempre true → SEC-01 bypassado em todos os endpoints."
  fix_applied: "Adicionado scope 'basic' com oidc-sub-mapper ao backoffice-realm.json. Adicionado 'basic' a defaultClientScopes do client onboarding-backoffice. Adicionado claim.name aos mappers de email e preferred_username. Volume Keycloak recriado para re-importar realm."
  artifacts:
    - path: "keycloak/backoffice-realm.json"
      issue: "clientScopes sem 'basic' scope; mappers sem claim.name"
