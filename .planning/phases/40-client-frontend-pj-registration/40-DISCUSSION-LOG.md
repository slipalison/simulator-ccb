# Phase 40: Client Frontend — PJ Registration & Employee Management - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions captured in CONTEXT.md — this log preserves the discussion.

**Date:** 2026-04-26
**Phase:** 40-client-frontend-pj-registration
**Mode:** discuss (interactive)

## Areas Discussed

### Navegação e Layout
| Question | Options Presented | Selection |
|----------|-------------------|-----------|
| Como organizar navegação pós-login? | Sidebar (Recommended), Nav horizontal | Sidebar |
| Quais rotas principais pós-login? | / /employees /profile, / /employees /company/profile | / (dashboard) /employees /profile |
| Como lidar com telas sem permissão? | Ocultar rotas (Recommended), Mostrar tudo bloquear conteúdo | Ocultar rotas sem permissão |
| Header precisa mudanças? | Header + badge de ruolo (Recommended), Header igual sem badge | Manter Header + badge de ruolo |

### Formulário de Cadastro PJ
| Question | Options Presented | Selection |
|----------|-------------------|-----------|
| Como estruturar formulário? | Formulário único PJ (Recommended), Wizard em 2 passos | Wizard em 2 passos |
| TermsAcceptance? | Checkbox + modal (Recommended), Checkbox + página separada | Checkbox + modal com texto |
| Password UX? | Manter atual (Recommended), Sem confirm password | Manter atual |

### Gestão de Funcionários UI
| Question | Options Presented | Selection |
|----------|-------------------|-----------|
| Quais colunas na tabela? | 5 colunas + actions (Recommended), 4 colunas + actions | 5 colunas (Nome, Email, Group Badge, Status Badge, Actions) |
| Como apresentar ações? | Dropdown (Recommended), Botões inline | Dropdown de ações |
| LGPD Delete confirmação? | Confirmar digitando email (Recommended), Confirmar simples | Confirmar digitando email |
| Reset senha exibição? | Modal one-time reveal (Recommended), Clipboard automático | Modal one-time reveal |

### Dashboard Simulação
| Question | Options Presented | Selection |
|----------|-------------------|-----------|
| Como apresentar dados? | Cards com números (Recommended), Cards + mini charts | Cards + mini charts |
| Período ações/logins? | Últimos 7 dias (Recommended), Últimos 30 dias | Últimos 7 dias |
| Biblioteca de charts? | Recharts (Recommended), Chart.js | Chart.js |
| Quantos cards? | 4 cards (Recommended), 6 cards | 6 cards |

## Deferred Ideas
- BackOffice employee views e audit — Phase 41
- Dashboard com dados reais — futuro
- Notificação por email — requer SMTP
- Funcionário edita próprios dados — v7.0 read-only