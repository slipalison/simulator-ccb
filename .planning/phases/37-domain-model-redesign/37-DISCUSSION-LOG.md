# Phase 37: Domain Model Redesign - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-04-25
**Phase:** 37-domain-model-redesign
**Areas discussed:** Estrutura dos Aggregates, TermsAcceptance & IpAddress, Migration Strategy, Remoção PF — Escopo exato

---

## Estrutura dos Aggregates

| Option | Description | Selected |
|--------|-------------|----------|
| Company como aggregate root isolado | Employee é aggregate root separado com FK. Company não tem lista de Employees. | ✔ |
| Company com Employees como child entities | Company tem `List<Employee>` interno. | |

**User's choice:** Company como aggregate root isolado

| Option | Description | Selected |
|--------|-------------|----------|
| GuidId como FK | EF Core configura FK. Domain sem navegação. | ✔ |
| CompanyId explícito no Entity | Employee tem propriedade CompanyId no domain. | |

**User's choice:** GuidId como FK

| Option | Description | Selected |
|--------|-------------|----------|
| Enum EmployeeAccessGroup | 3 valores fixos. Simples mas não configurável. | |
| String/ValueObject | Flexível mas sem validação. | |
| Entidade AccessGroup configurável | Tabela no banco, permissões granulares, PJ pode criar grupos. | ✔ |

**User's choice:** Entidade AccessGroup configurável (o usuário rejeitou enum e pediu grupos configuráveis com permissões por grupo)

| Option | Description | Selected |
|--------|-------------|----------|
| Flags booleanas | `can_manage_employees`, `can_view_audit`, etc. | |
| Resource:Action pattern | `employees:read`, `audit:read`, etc. | ✔ |

**User's choice:** Resource:Action pattern

| Option | Description | Selected |
|--------|-------------|----------|
| Permissões predefinidas (enum/const) | Backend conhece todas as permissões possíveis. Validação fácil. | ✔ |
| Strings arbitrárias | Máxima flexibilidade, risco de typo. | |

**User's choice:** Permissões predefinidas

| Option | Description | Selected |
|--------|-------------|----------|
| AccessGroup com CompanyId FK | Cada PJ gerencia seus próprios grupos. Isolamento natural. | ✔ |
| AccessGroup global (sem CompanyId) | Compartilhado entre empresas. PJ não pode customizar. | |

**User's choice:** AccessGroup com CompanyId FK

| Option | Description | Selected |
|--------|-------------|----------|
| Auto-sync banco → Keycloak | Backend sincroniza automaticamente. Fonte da verdade: banco. | ✔ |
| Sync assíncrono/eventual | Job/evento. Pode ter lag. | |

**User's choice:** Auto-sync banco → Keycloak

| Option | Description | Selected |
|--------|-------------|----------|
| Seed 3 grupos padrão | admin-empresa, viewer, dashboard criados automaticamente no registro. | ✔ |
| Sem grupos padrão | PJ cria tudo manualmente. | |

**User's choice:** Seed 3 grupos padrão

| Option | Description | Selected |
|--------|-------------|----------|
| Propriedades já definidas no ROADMAP | CNPJ, RazãoSocial, Email, Telefone. YAGNI. | ✔ |
| Campos adicionais da empresa | NomeFantasia, Endereco, etc. | |

**User's choice:** Propriedades já definidas no ROADMAP

| Option | Description | Selected |
|--------|-------------|----------|
| Propriedades mínimas do ROADMAP | CPF, Nome, Email, Telefone, CompanyId, AccessGroupId. YAGNI. | ✔ |
| Campos adicionais do funcionário | Cargo, Departamento, etc. | |

**User's choice:** Propriedades mínimas do ROADMAP

---

## TermsAcceptance & IpAddress

| Option | Description | Selected |
|--------|-------------|----------|
| Value object com 3 campos | TermsAcceptance com AcceptedAt, TermsVersion, IpAddress. | ✔ |
| Campos soltos no aggregate | Menos coeso. | |

**User's choice:** Value object com 3 campos

| Option | Description | Selected |
|--------|-------------|----------|
| X-Forwarded-For + RemoteIpAddress fallback | Compatível com Docker/reverse proxy. | ✔ |
| Sem IpAddress | Menos rastro para auditoria. | |

**User's choice:** X-Forwarded-For + RemoteIpAddress fallback

| Option | Description | Selected |
|--------|-------------|----------|
| Versão hardcoded | Constante `TermsCurrentVersion = "1.0"`. Suficiente para mock. | ✔ |
| Versão configurável | appsettings/env var. YAGNI. | |

**User's choice:** Versão hardcoded

| Option | Description | Selected |
|--------|-------------|----------|
| Obrigatório | Consistente com REG-04. | ✔ |
| Opcional | Inconsistente com REG-04. | |

**User's choice:** Obrigatório

---

## Migration Strategy

| Option | Description | Selected |
|--------|-------------|----------|
| Drop + Create | Drop tabela clients, criar companies/employees/access_groups. Base zerada. | ✔ |
| Rename + Alter | Preserva dados mas complexo e arriscado. | |

**User's choice:** Drop + Create

| Option | Description | Selected |
|--------|-------------|----------|
| Preservar tabelas auxiliares | admin_audit_logs e password_reset_tokens intocáveis. | ✔ |
| Recriar tudo | Mais limpo mas perde dados auxiliares. | |

**User's choice:** Preservar tabelas auxiliares

---

## Remoção PF — Escopo exato

| Option | Description | Selected |
|--------|-------------|----------|
| Fase 37: domain + migration + testes. Admin endpoints migram na 38 | Menor escopo mas janela de quebra. | |
| Fase 37: tudo junto — domain + API + admin | Maior escopo mas zero janelas de quebra. | ✔ |

**User's choice:** Fase 37: tudo junto — domain + API + admin

| Option | Description | Selected |
|--------|-------------|----------|
| Remoção total sem vestígios | Deletar tudo Client/PF/PJ. Zero vestígios. | ✔ |
| Marcar como obsolete | Deixa sujeira no código. | |

**User's choice:** Remoção total sem vestígios

| Option | Description | Selected |
|--------|-------------|----------|
| Deletar + reescrever | Base zerada nos testes também. | ✔ |
| Adaptar testes existentes | Viés dos testes antigos. | |

**User's choice:** Deletar + reescrever

| Option | Description | Selected |
|--------|-------------|----------|
| Reutilizar sem mudanças | Cpf e Cnpj VOs já validam check-digit. | ✔ |
| Refatorar | Desperdício. | |

**User's choice:** Reutilizar sem mudanças

---

## Claude's Discretion

- Número e nomes exatos de permissões resource:action
- Estrutura de pastas dos novos arquivos domain
- Detalhes da migration EF Core
- Como refatorar AdminUserController (651 lines) para Company/Employee

## Deferred Ideas

None — discussão ficou dentro do escopo.