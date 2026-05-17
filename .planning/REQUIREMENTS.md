# Requirements: Onboarding de Clientes

**Defined:** 2026-05-02
**Core Value:** Cadastro seguro PJ com gestão de funcionários e permissões via Keycloak — isolamento entre empresas é requisito de primeira classe.

## v8 Requirements — Gestão de Fundos

### CAD — Cadastros

**ConsultoriaFundo:**

- [ ] **CAD-01**: PJ can register ConsultoriaFundo with razao social, CNPJ (check-digit validated), optional nome fantasia, email, telefone, status ATIVO/INATIVO
- [ ] **CAD-02**: PJ can list ConsultoriaFundo with pagination (20/page) and search by razao social or CNPJ
- [ ] **CAD-03**: PJ can update ConsultoriaFundo fields (razao social, nome fantasia, email, telefone, status)
- [ ] **CAD-04**: Duplicate CNPJ for ConsultoriaFundo within same company returns 409

**Custodiante:**

- [ ] **CAD-05**: PJ can register Custodiante with razao social, CNPJ (validated), optional codigo interno, email, telefone, status
- [ ] **CAD-06**: PJ can list Custodiante with pagination and search
- [ ] **CAD-07**: PJ can update Custodiante fields
- [ ] **CAD-08**: Duplicate CNPJ for Custodiante within same company returns 409

**Fundo:**

- [ ] **CAD-09**: PJ can register Fundo with nome, CNPJ (validated), ConsultoriaFundo, Custodiante, TipoFundo enum, optional classe anbima, segmento, data constituicao
- [ ] **CAD-10**: PJ can list Fundo with pagination and search by nome/CNPJ
- [ ] **CAD-11**: PJ can update Fundo data (nome, consultoria, custodiante, tipo, classe, segmento, datas)
- [ ] **CAD-12**: Duplicate CNPJ for Fundo within same company returns 409
- [ ] **CAD-13**: Fundo status follows state machine: RASCUNHO→ATIVO↔SUSPENSO→EM_LIQUIDACAO→ENCERRADO — invalid transitions rejected with 400

**Cedente:**

- [ ] **CAD-14**: PJ can register Cedente PF with validated CPF, nome, email, telefone, endereco, status
- [ ] **CAD-15**: PJ can register Cedente PJ with validated CNPJ, razao social, email, telefone, endereco, status
- [ ] **CAD-16**: PJ can list Cedente with pagination and search by nome/CPF/CNPJ
- [ ] **CAD-17**: PJ can update Cedente data
- [ ] **CAD-18**: Duplicate CPF or CNPJ for Cedente within same company returns 409

**TipoAtivo:**

- [ ] **CAD-19**: Admin can create TipoAtivo with unique codigo, descricao, categoria enum, optional subcategoria, status, ordem exibicao
- [ ] **CAD-20**: Admin can list TipoAtivo with pagination (global catalog)
- [ ] **CAD-21**: Admin can update TipoAtivo data
- [ ] **CAD-22**: Duplicate codigo for TipoAtivo (global) returns 409

### REL — Relacionamentos

- [ ] **REL-01**: PJ can associate a Cedente to a Fundo with exposure limits (% e valor) and date range
- [ ] **REL-02**: PJ can list Cedentes associated to a Fundo with their exposure limits and status
- [ ] **REL-03**: PJ can update FundoCedente exposure limits, dates, and status (ATIVO/INATIVO)
- [ ] **REL-04**: PJ can associate Tipos de Ativo to a Cedente (defining which assets they can work with)
- [ ] **REL-05**: PJ can list Tipos de Ativo associated to a Cedente and remove associations
- [ ] **REL-06**: PJ can associate Tipos de Ativo to a Fundo (defining investment mandate)
- [ ] **REL-07**: PJ can list Tipos de Ativo associated to a Fundo and remove associations
- [ ] **REL-08**: LimiteExposicaoPercentual supports "unlimited" via sentinel value (-1)
- [ ] **REL-09**: FundoCedente enforces at most ONE active association per Fundo-Cedente pair

### TEN — Multi-tenancy

- [ ] **TEN-01**: Fundo, FundoCedente data is company-scoped with HasQueryFilter — no cross-company data leakage
- [ ] **TEN-02**: ConsultoriaFundo, Custodiante, Cedente are company-scoped with HasQueryFilter
- [ ] **TEN-03**: TipoAtivo is global — shared across all companies, no ClienteId, no HasQueryFilter

### PERM — Permissões

- [ ] **PERM-01**: New fund permissions added to Permissions.cs: funds:read, funds:write, funds:delete, funds:manage
- [ ] **PERM-02**: Fund CRUD endpoints require appropriate permission claims
- [ ] **PERM-03**: Existing access groups (admin-empresa, viewer) extended with fund permissions by default

### ADM — Admin Backoffice

- [ ] **ADM-01**: Backoffice admin can list Fundo across all companies with pagination
- [ ] **ADM-02**: Backoffice admin can view Fundo details including consultoria, custodiante, cedentes
- [ ] **ADM-03**: Backoffice admin can list ConsultoriaFundo, Custodiante, Cedente across all companies (ignoring HasQueryFilter)
- [ ] **ADM-04**: All fund management actions are logged to existing audit trail

### FRO — Frontend

- [ ] **FRO-01**: Client sidebar includes Fundos section with sub-navigation (Fundos, Consultorias, Custodiantes, Cedentes)
- [ ] **FRO-02**: FundosPage shows list with search, pagination, status badges
- [ ] **FRO-03**: Fund/Cedente/Consultoria/Custodiante forms use Zod validation mirroring backend rules
- [ ] **FRO-04**: Backoffice admin fund views are read-only for auditing
- [ ] **FRO-05**: Fundo status dropdown restricted by state machine — only valid transitions shown

## v2 Requirements (Deferred)

### Relatórios e Dashboard

- **RPT-01**: Dashboard com KPIs de fundos por tipo, status, exposição
- **RPT-02**: Relatório de exposição por cedente com exportação CSV/PDF
- **RPT-03**: Histórico de alterações de status do fundo com audit trail visual

### Migração de Dados

- **MIG-01**: Bulk import de fundos via CSV
- **MIG-02**: Bulk import de cedentes via CSV

### Validação CVM

- **CVM-01**: Validar CNPJ do fundo contra receita federal
- **CVM-02**: Validar classe ANBIMA contra catálogo oficial
- **CVM-03**: Alertas de conformidade regulatória

## Out of Scope

| Feature | Reason |
|---------|--------|
| Processamento financeiro | Módulo é cadastral — sem movimentação financeira |
| Upload de documentos | Complexidade alta, deferido para v2+ |
| Fluxos de aprovação/workflow | Fora do escopo de cadastro puro |
| Integração com sistemas externos (CVM, BACEN) | Requer APIs externas, deferido |
| Soft delete de entidades | Fundos usam status transitions (RASCUNHO→ENCERRADO), entidades auxiliares usam ATIVO/INATIVO |
| Dashboard com dados dinâmicos | Mock existe — dados reais de fundos é v2 |

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| D-01 | Cedente/Custodiante têm ClienteId | Escopo multi-tenant — cada empresa cadastra os seus | ✓ v8.0 |
| D-02 | FundoStatus = state machine | RASCUNHO→ATIVO↔SUSPENSO→EM_LIQUIDACAO→ENCERRADO | ✓ v8.0 |
| D-03 | TipoAtivo é global (sem ClienteId) | Catálogo CVM — padrão compartilhado | ✓ v8.0 |
| D-04 | LimiteExposicao ilimitado = sentinel (-1) | Simples, explícito, evita nullable confusion | ✓ v8.0 |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| CAD-01 | Phase 47 | Pending |
| CAD-02 | Phase 47 | Pending |
| CAD-03 | Phase 47 | Pending |
| CAD-04 | Phase 46 | Pending |
| CAD-05 | Phase 47 | Pending |
| CAD-06 | Phase 47 | Pending |
| CAD-07 | Phase 47 | Pending |
| CAD-08 | Phase 46 | Pending |
| CAD-09 | Phase 47 | Pending |
| CAD-10 | Phase 47 | Pending |
| CAD-11 | Phase 47 | Pending |
| CAD-12 | Phase 46 | Pending |
| CAD-13 | Phase 45 | Pending |
| CAD-14 | Phase 47 | Pending |
| CAD-15 | Phase 47 | Pending |
| CAD-16 | Phase 47 | Pending |
| CAD-17 | Phase 47 | Pending |
| CAD-18 | Phase 46 | Pending |
| CAD-19 | Phase 47 | Pending |
| CAD-20 | Phase 47 | Pending |
| CAD-21 | Phase 47 | Pending |
| CAD-22 | Phase 46 | Pending |
| REL-01 | Phase 49 | Pending |
| REL-02 | Phase 49 | Pending |
| REL-03 | Phase 49 | Pending |
| REL-04 | Phase 49 | Pending |
| REL-05 | Phase 49 | Pending |
| REL-06 | Phase 49 | Pending |
| REL-07 | Phase 49 | Pending |
| REL-08 | Phase 45 | Pending |
| REL-09 | Phase 49 | Pending |
| TEN-01 | Phase 46 | Pending |
| TEN-02 | Phase 46 | Pending |
| TEN-03 | Phase 45 | Pending |
| PERM-01 | Phase 45 | Pending |
| PERM-02 | Phase 48 | Pending |
| PERM-03 | Phase 48 | Pending |
| ADM-01 | Phase 51 | Pending |
| ADM-02 | Phase 51 | Pending |
| ADM-03 | Phase 51 | Pending |
| ADM-04 | Phase 47 | Pending |
| FRO-01 | Phase 50 | Pending |
| FRO-02 | Phase 50 | Pending |
| FRO-03 | Phase 50 | Pending |
| FRO-04 | Phase 51 | Pending |
| FRO-05 | Phase 50 | Pending |

**Coverage:**
- v8 requirements: 46 total
- Mapped to phases: 46
- Unmapped: 0 ✓

---
*Requirements defined: 2026-05-02*
*Last updated: 2026-05-02 after v8.0 milestone definition*