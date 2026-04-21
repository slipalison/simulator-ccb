# Restrições Permanentes — Fase 29 e toda autenticação do backoffice

> **Feedback do usuário (2026-04-15) — NÃO IGNORAR.**
> Esta fase foi executada com Auth Code Flow (ACF + PKCE) e depois revertida
> para ROPC por decisão explícita do usuário. As restrições abaixo são definitivas.

---

## Restrição 1 — O usuário NUNCA deve ver o Keycloak

```
"nenhum usuário deve saber que o Keycloak existe"
```

- O login do backoffice **obrigatoriamente** deve ser um formulário próprio (email + senha) dentro da interface do backoffice
- **Proibido** qualquer redirect para a UI do Keycloak (Authorization Code Flow, Device Flow, etc.)
- O Keycloak é infraestrutura interna — os admins do backoffice NÃO precisam saber que ele existe
- A autenticação do backoffice usa **ROPC (Resource Owner Password Credentials)** via endpoint `/api/admin/auth/login` do backend .NET

## Restrição 2 — Admin do Keycloak ≠ Admin do Backoffice

```
"o usuário Administrador do Keycloak DEVE ser diferente do usuário ADMIN do BACKOFFICE — são pessoas diferentes"
```

| Papel | Quem é | Onde acessa | O que faz |
|-------|--------|-------------|-----------|
| **Admin do Keycloak** | DevOps / Infra | Keycloak Admin Console (`/admin`) | Gerencia o sistema Keycloak em si (realms, clients, configuração) |
| **Admin do Backoffice** | Operações / Suporte | Backoffice app (`:5174`) | Gerencia usuários de onboarding (PF/PJ) — bloquear, editar, criar admins, ver audit log |

- O Admin do Backoffice é um usuário comum no realm `onboarding` com a role `admin`
- Ele NÃO tem acesso ao Keycloak Admin Console
- Nunca misturar a conta `admin@keycloak` (admin do Keycloak) com a conta `admin@onboarding.local` (admin do backoffice)

## Impacto Arquitetural

- Fluxo de login do backoffice: formulário → `POST /api/admin/auth/login` → backend faz ROPC → cookie `adminRefreshToken` httpOnly
- `AdminSessionMiddleware.cs` lê o cookie `adminRefreshToken` (Path = `/api/admin`)
- O client Keycloak usado para ROPC do backoffice é `onboarding-app` (ou um client dedicado futuro)
- **Se uma fase futura precisar de ACF**: o usuário deve ser consultado novamente — a decisão de 2026-04-15 foi rever para ROPC

---

*Adicionado em resposta ao feedback do usuário após execução da fase. Ver commit de reversão.*
