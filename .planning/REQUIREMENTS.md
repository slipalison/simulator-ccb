# Requirements — Milestone v6.0: Gestão Completa de Administradores

> **Segurança é requisito de primeira classe.** Toda operação de gestão de admins
> deve ser autenticada, autorizada, validada e auditada. Não existe "vamos adicionar
> segurança depois" neste milestone.

---

## MGMT — Gestão de Administradores

- [ ] **MGMT-01**: Admin pode visualizar lista paginada de administradores (20 por página)
- [ ] **MGMT-02**: Admin pode filtrar a lista por nome, email e status (ativo/inativo)
- [ ] **MGMT-03**: Admin pode editar nome e email de outro administrador (persiste no Keycloak)
- [ ] **MGMT-04**: Admin pode resetar senha de outro administrador — gera nova senha temporária exibida uma única vez; Keycloak força troca no próximo login via `UPDATE_PASSWORD` requiredAction
- [ ] **MGMT-05**: Admin pode desativar outro administrador (disable no Keycloak — conta preservada para histórico de auditoria)
- [ ] **MGMT-06**: Admin pode reativar um administrador desativado

---

## SEC — Segurança

> Segurança não é opcional. Cada item abaixo é um bloqueador de release.

- [ ] **SEC-01**: Admin não pode editar, resetar senha ou desativar a própria conta (prevenção de auto-bloqueio)
- [ ] **SEC-02**: Todos os endpoints de gestão de admins exigem sessão autenticada com role `admin` no realm `backoffice` (scheme `BearerBackoffice`)
- [ ] **SEC-03**: Reset de senha gera senha criptograficamente segura via `RandomNumberGenerator` (mínimo 16 chars, mix de upper/lower/digit/special) — mesma abordagem do criar admin
- [ ] **SEC-04**: Edição de email valida unicidade no Keycloak antes de persistir — conflito retorna 409 com mensagem clara
- [ ] **SEC-05**: Sistema bloqueia desativação do último administrador ativo (prevenção de lockout total do sistema)

---

## AUD — Auditoria (extensão do v5.0)

- [ ] **AUD-04**: Edição de admin registrada no audit log com actor, target, campos alterados (valores antigos e novos)
- [ ] **AUD-05**: Reset de senha de admin registrado no audit log (actor + target — senha nunca gravada no log)
- [ ] **AUD-06**: Desativação e reativação de admin registradas no audit log com actor, target e motivo (se fornecido)

---

## Future Requirements (deferred)

- Motivo obrigatório ao desativar admin — pode ser útil para auditoria mas não bloqueador para v6.0
- Notificação por email ao admin quando sua senha é resetada — requer integração SMTP
- Histórico de alterações por admin (quem alterou o quê ao longo do tempo) — consulta específica no audit log
- Permissões granulares (super admin vs admin) — qualquer admin pode tudo no v6.0

---

## Out of Scope

- Exclusão permanente de admin — desativar (MGMT-05) é suficiente; exclusão hard remove histórico de auditoria
- Transferência de propriedade de ações auditadas — audit log é imutável
- Login como outro admin (impersonation) — fora do escopo de segurança aceitável
- 2FA obrigatório para admins — Keycloak suporta mas requer configuração de realm separada

---

## Traceability

| REQ-ID | Phase | Status |
|--------|-------|--------|
| MGMT-01 | Phase 35 (backend) + Phase 36 (frontend) | ⬜ |
| MGMT-02 | Phase 35 (backend) + Phase 36 (frontend) | ⬜ |
| MGMT-03 | Phase 35 (backend) + Phase 36 (frontend) | ⬜ |
| MGMT-04 | Phase 35 (backend) + Phase 36 (frontend) | ⬜ |
| MGMT-05 | Phase 35 (backend) + Phase 36 (frontend) | ⬜ |
| MGMT-06 | Phase 35 (backend) + Phase 36 (frontend) | ⬜ |
| SEC-01 | Phase 35 (backend guard) | ⬜ |
| SEC-02 | Phase 35 (backend auth) | ⬜ |
| SEC-03 | Phase 35 (backend crypto) | ⬜ |
| SEC-04 | Phase 35 (backend validation) | ⬜ |
| SEC-05 | Phase 35 (backend guard) | ⬜ |
| AUD-04 | Phase 35 (backend audit) | ⬜ |
| AUD-05 | Phase 35 (backend audit) | ⬜ |
| AUD-06 | Phase 35 (backend audit) | ⬜ |
