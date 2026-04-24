# Phase 36: Admin Management UI - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-04-24
**Phase:** 36-admin-management-ui
**Areas discussed:** Ações por linha, One-time password UX, Self-action (SEC-01) UX, Campos de busca

---

## Ações por linha

| Option | Description | Selected |
|--------|-------------|----------|
| Dropdown menu | Botão ⋯ por linha abre menu com as ações. Mais limpo com 4 itens. | ✓ |
| Botões inline | 4 botões com ícones na coluna Ações (padrão atual AdminUsersTable). | |
| Botões contextuais | 3 botões inline; status muda dinamicamente (Desativar vs Reativar). | |

**User's choice:** Dropdown menu

---

| Option | Description | Selected |
|--------|-------------|----------|
| Contextual | Dropdown mostra Desativar para ativo, Reativar para inativo. | ✓ |
| Fixo — sempre 4 itens | Sempre mostra os 4 itens; irrelevantes ficam desabilitados. | |

**User's choice:** Contextual — Desativar para ativo, Reativar para inativo. Nunca os dois juntos.

---

## One-time password UX

| Option | Description | Selected |
|--------|-------------|----------|
| Dialog com confirmação | Campo visível + botão copiar + aviso "não pode ser recuperada" + botão Fechar. | ✓ |
| Dialog com checkbox | Campo + botão copiar + checkbox "Confirmo que copiei" antes de habilitar OK. | |
| Dialog com countdown | Fechar desabilitado por 5s com contador regressivo. | |

**User's choice:** Dialog com confirmação simples — sem countdown, sem checkbox obrigatório.
**Notes:** Aviso proeminente ⚠️ mas sem bloqueio forçado do fechamento.

---

## Self-action (SEC-01) UX

| Option | Description | Selected |
|--------|-------------|----------|
| Desabilitar com tooltip | Botão ⋯ aparece desabilitado; hover mostra "Você não pode modificar a própria conta". | ✓ |
| Ocultar botão de ações | Coluna Ações fica vazia para a própria linha. | |
| Badge 'Você' sem ações | Badge "Você" na coluna Ações, sem botões. | |

**User's choice:** Desabilitar com tooltip explicativo.

---

## Campos de busca

| Option | Description | Selected |
|--------|-------------|----------|
| Dois campos distintos | Input "Buscar por nome" + Input "Buscar por email" separados. | ✓ |
| Campo único | Um input envia o mesmo valor para name e email simultaneamente. | |

**User's choice:** Dois campos distintos — aproveita os parâmetros separados do backend.

---

## Claude's Discretion

- Estrutura interna dos modais de editar/desativar/reativar
- Validação inline do form de edição
- Número e ordem das colunas da tabela

## Deferred Ideas

Nenhuma ideia fora do escopo surgiu durante a discussão.
