# Phase 35: backoffice-admin-management-pagina-o-filtros-reset-senha-edi - Context

**Gathered:** 2026-04-21
**Status:** Ready for planning

<domain>
## Phase Boundary

Gestão Avançada de Administradores no Backend. 
Implementar endpoints paginados de lista (filtros), edição de dados (nome/email com unicidade), bloqueio, reativação e reset de senha para contas de administrador.
</domain>

<decisions>
## Implementation Decisions

### Arquitetura dos Controllers
- Manter em `AdminUserController`: Os endpoints de GET e POST de admin já estão definidos na rota `api/admin/administrators`. Os novos endpoints (edição, reset, bloqueio) devem ser adicionados na mesma controller para reaproveitar as dependências injetadas.

### Revogação de Sessão (Desativação)
- Forçar logout imediato: Além de efetivarmos "disable" (disable no Keycloak) do user, devemos disparar um encerramento imediato de todas as sessões ativas (logoutAll) desta conta pelo Keycloak Admin API, pra que não fique navegando com token antigo.

### Geração de Senha Temporária
- Remover caracteres ambíguos: Ao gerar a senha criptográfica para reset ou criação (~16 caracteres), remover caracteres visualmente similares que causam confusão (`l`, `1`, `I`, `O`, `0`) uma vez que essa senha será anotada ou repassada visualmente uma única vez.
</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Controllers e Core
- `src/Onboarding.API/Controllers/AdminUserController.cs` — A controller existente onde os métodos serão implementados.
- `src/Onboarding.Application/Admin/Commands/` — Usar CQRS commands adequados com injeção de dependências.

</canonical_refs>

<specifics>
## Specific Ideas
- Como definido nos requisitos globais, todas essas ações devem ter auditoria obrigatória injetada via `IAuditService` mantendo quem fez a alteração nos administradores. O token `BearerBackoffice` fornece o email e o `sub` do autor.
</specifics>

<deferred>
## Deferred Ideas
None
</deferred>

---
*Phase: 35-backoffice-admin-management-pagina-o-filtros-reset-senha-edi*
*Context gathered: 2026-04-21*
