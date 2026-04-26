# Requirements — Milestone v7.0: PJ-Only Onboarding + Gestão de Funcionários

> **Isolamento entre empresas é requisito de primeira classe.** Qualquer bug que permita
> PJ ver/editar dados de funcionários de outra PJ é vulnerabilidade crítica de segurança.
> **Base zerada:** `docker compose down -v` — migration cria schemas novos (Company + Employee).

---

## REG — Cadastro PJ

- [ ] **REG-01**: PJ pode se cadastrar com razão social, CNPJ, email, telefone, senha + aceite de termos de uso
- [ ] **REG-02**: CNPJ deve ser único no sistema — conflito retorna 409 com mensagem clara
- [ ] **REG-03**: PJ pode cadastrar funcionários PF vinculados à sua empresa (nome, CPF, email, telefone, senha temporária)
- [ ] **REG-04**: Aceite de termos de uso obrigatório no cadastro — armazena timestamp e versão dos termos (texto mock por enquanto)
- [ ] **REG-05**: Remover completamente o fluxo de cadastro PF do frontend (client) e da API — cadastro é exclusivamente PJ

---

## MGMT — Gestão de Funcionários

- [x] **MGMT-01**: PJ pode visualizar lista paginada de funcionários da sua empresa (20 por página) com filtros (nome, status)
- [x] **MGMT-02**: PJ pode bloquear/desbloquear funcionários (disable/enable no Keycloak — preserva dados para auditoria)
- [x] **MGMT-03**: PJ pode resetar senha de funcionário — gera senha temporária exibida uma vez, Keycloak força troca no próximo login
- [x] **MGMT-04**: PJ pode editar dados do funcionário (nome, email, telefone) — persiste no Keycloak
- [x] **MGMT-05**: PJ pode excluir funcionário (LGPD) — anonimiza dados no PostgreSQL + delete no Keycloak

---

## PERM — Permissões e Grupos de Acesso

- [x] **PERM-01**: Funcionário com role `admin-empresa` tem mesmos poderes do PJ dono (gerenciar funcionários, ver audit, atribuir grupos)
- [x] **PERM-02**: Funcionário com role `viewer` pode visualizar dados de funcionários da empresa mas não pode editar, bloquear ou excluir
- [x] **PERM-03**: Funcionário com role `dashboard` pode acessar a tela de dashboard
- [x] **PERM-04**: PJ pode atribuir/remover grupos de acesso dos seus funcionários (transições entre admin-empresa, viewer, dashboard)
- [x] **PERM-05**: Isolamento estrito entre empresas — PJ nunca vê/edita dados de funcionários de outra PJ (enforced no backend via filtro de empresa)

---

## AUD — Auditoria de Funcionários

- [ ] **AUD-01**: PJ e Admin Empresa podem visualizar log de ações dos seus funcionários com filtros (data, tipo de ação, ator)
- [ ] **AUD-02**: Todas as ações de funcionários (login, edição, bloqueio, etc.) são registradas automaticamente no audit log existente (append-only)

---

## DASH — Dashboard

- [ ] **DASH-01**: Tela de dashboard com dados estáticos (mock) — total de funcionários ativos/inativos, logins recentes, ações por período

---

## ADM — BackOffice

- [ ] **ADM-01**: BackOffice pode visualizar funcionários de qualquer empresa com filtros (empresa, nome, status)
- [ ] **ADM-02**: BackOffice pode forçar reset de senha, bloquear/desbloquear qualquer funcionário de qualquer empresa

---

## CI — CI/CD Coverage

- [ ] **CI-01**: GitHub Actions com cobertura de testes >= 80% no backend (.NET) e no frontend (React/Vinxi)

---

## Future Requirements (deferred)

- Dashboard com dados reais e dinâmicos — mock estático por enquanto, dados reais em milestone futuro
- Notificação por email ao funcionário quando senha é resetada — requer integração SMTP
- Funcionário pode editar seus próprios dados — v7.0 é read-only para o funcionário
- 2FA obrigatório para PJ e Admin Empresa — Keycloak suporta mas requer configuração de realm separada
- Motivo obrigatório ao bloquear funcionário — útil para auditoria mas não bloqueador para v7.0
- Exportação de relatórios (CSV/PDF) de audit log — deferido para futuro
- Fluxo de convite por email (PJ envia email, funcionário completa cadastro) — complexidade adicional sem valor imediato

---

## Out of Scope

| Feature | Reason |
|---------|--------|
| Cadastro PF | Removido do sistema — v7.0 é PJ-only. Base zerada. |
| Bit Flags no JWT para permissões | Keycloak roles/groups nativo é abordagem escolhida — sem custom mapper |
| Dashboard com dados dinâmicos | Mock estático é suficiente para apresentação; dados reais em futuro |
| Impersonação de funcionários | Fora do escopo de segurança aceitável |
| Login social (Google, GitHub, etc.) | Complexidade adicional sem valor imediato |
| Edição de dados pelo funcionário | v7.0 funcionário é read-only; PF edita dados em milestone futuro |
| Celular/app mobile | Web-first |
| Multi-tenancy com realm separado por empresa | Mesmo realm, isolamento via companyId FK — Keycloak Groups para roles |

---

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| REG-01 | Phase 38 | Pending |
| REG-02 | Phase 37 | Pending |
| REG-03 | Phase 38 | Pending |
| REG-04 | Phase 37 | Pending |
| REG-05 | Phase 37 | Pending |
| MGMT-01 | Phase 40 | ✅ Complete |
| MGMT-02 | Phase 40 | ✅ Complete |
| MGMT-03 | Phase 40 | ✅ Complete |
| MGMT-04 | Phase 40 | ✅ Complete |
| MGMT-05 | Phase 40 | ✅ Complete |
| PERM-01 | Phase 39 | ✅ Complete |
| PERM-02 | Phase 39 | ✅ Complete |
| PERM-03 | Phase 39 | ✅ Complete |
| PERM-04 | Phase 39 | ✅ Complete |
| PERM-05 | Phase 39 | ✅ Complete |
| AUD-01 | Phase 41 | Pending |
| AUD-02 | Phase 41 | Pending |
| DASH-01 | Phase 40 | Pending |
| ADM-01 | Phase 41 | Pending |
| ADM-02 | Phase 41 | Pending |
| CI-01 | Phase 42 | Pending |

**Coverage:**
- v7.0 requirements: 21 total
- Mapped to phases: 21
- Unmapped: 0 ✓

---
*Requirements defined: 2026-04-25*
*Last updated: 2026-04-25 after milestone v7.0 roadmap creation*