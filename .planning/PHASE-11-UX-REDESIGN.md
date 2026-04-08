# Proposta — Phase 11: UX Redesign

**Motivação:** Jornada do usuário muito fragmentada — muitos clicks, telas desnecessárias, UX não intuitiva

---

## 🎯 Objetivos

1. **Reduzir clicks** — Cadastro em 1 tela, não 2 (type selection → form)
2. **Single form inteligente** — Radio button PF/PJ, campos adaptativos
3. **Password UX** — Security meter + show/hide + confirmação
4. **Login-first** — Página inicial é login, não home genérica
5. **Auto-redirect** — Usuário logado vai direto para perfil
6. **Forgot password** — Fluxo de recuperação sem email provider

---

## 📋 Requisitos

### UX-01: Formulário Único de Cadastro

**Problema Atual:**
```
Tela 1: Escolher PF ou PJ (RegistrationTypeSelector)
  ↓ click
Tela 2: Formulário específico (PfRegistrationForm OU PjRegistrationForm)
```

**Solução Proposta:**
```
Tela Única: RegistrationForm com:
  - Radio button: [●] Pessoa Física  [ ] Pessoa Jurídica
  - Campos mudam DINAMICAMENTE conforme seleção:
    PF: Nome*, CPF*, Email*, Telefone*, Senha*, Confirmar Senha*
    PJ: Razão Social*, CNPJ*, Email*, Telefone*, Senha*, Confirmar Senha*
  - Validação em tempo real (onBlur)
  - Botão "Criar conta" desabilitado até form válido
```

**Entregáveis:**
- [ ] `RegistrationForm.tsx` — Formulário único substituindo RegistrationTypeSelector + 2 forms
- [ ] `PersonTypeRadio.tsx` — Radio button group (PF/PJ)
- [ ] Campos condicionais: CPF aparece só pra PF, CNPJ/Razão Social só pra PJ
- [ ] Validation schema Zod dinâmico (`.refine()` baseado em personType)
- [ ] `useForm` com `watch('personType')` para reatividade
- [ ] Remover: `RegistrationTypeSelector.tsx`, `PfRegistrationForm.tsx`, `PjRegistrationForm.tsx`

**Testes:**
- [ ] Selecionar PF → mostra campos CPF/Nome, esconde CNPJ/Razão Social
- [ ] Selecionar PJ → mostra campos CNPJ/Razão Social, esconde CPF/Nome
- [ ] Trocar PF→PJ limpa campos específicos do tipo anterior
- [ ] Submissão PF com dados válidos → 201
- [ ] Submissão PJ com dados válidos → 201
- [ ] Validação CPF/CNPJ em tempo real

---

### UX-02: Password Security Meter

**Problema Atual:**
- Campo de senha sem feedback visual de força
- Usuário não sabe se senha é fraca/forte até submeter

**Solução Proposta:**
```
┌─────────────────────────────────────┐
│ Senha                               │
│ [●●●●●●●●●●●●       ]  [👁] Mostrar│
│                                     │
│ ████████░░░░░░░░░░░░░░░ Forte      │
│                                     │
│ ✓ Mínimo 8 caracteres               │
│ ✓ Contém letra maiúscula            │
│ ✗ Contém número                     │
│ ✗ Contém caractere especial         │
└─────────────────────────────────────┘
```

**Níveis de Força:**
| Nível | Cor | Critérios |
|-------|-----|-----------|
| **Muito Fraca** | Vermelho escuro | < 6 chars ou all-same |
| **Fraca** | Vermelho | 6-7 chars, só letras |
| **Média** | Amarelo | 8+ chars, upper+lower |
| **Forte** | Verde claro | 8+ chars, upper+lower+digit |
| **Muito Forte** | Verde escuro | 12+ chars, upper+lower+digit+special |

**Entregáveis:**
- [ ] `PasswordStrengthMeter.tsx` — Componente visual com barra de progresso + texto
- [ ] `calculatePasswordStrength(password)` — Função pure returning `{score, level, checks}`
- [ ] Checklist visual com ✓/✗ para cada critério
- [ ] Integração com Zod schema (feedback em tempo real)

**Testes:**
- [ ] "abc" → Muito Fraca (vermelho escuro)
- [ ] "abcdefgh" → Fraca (vermelho) — só letras
- [ ] "Abcdefgh" → Média (amarelo) — upper+lower
- [ ] "Abcdefg1" → Forte (verde claro) — upper+lower+digit
- [ ] "Abcdefg1!" → Forte (verde claro) — upper+lower+digit+special
- [ ] "Abcdefg1!xyz" → Muito Forte (verde escuro) — 12+ chars, todos critérios

---

### UX-03: Show/Hide Password + Confirm Password

**Show/Hide:**
```
[●●●●●●●●●●   ] [👁]  ← ícone toggle (eye/eye-off)
```

**Entregáveis:**
- [ ] `PasswordField.tsx` — Input com botão toggle (usa `type="password"` ↔ `type="text"`)
- [ ] Ícone de olho aberto/fechado (lucide-react `Eye` / `EyeOff`)
- [ ] Estado local `showPassword: boolean`

**Confirm Password:**
```
┌─────────────────────────────────┐
│ Senha                           │
│ [●●●●●●●●   ] [👁]              │
│ ████████░░░░ Forte              │
│                                 │
│ Confirmar Senha                 │
│ [          ] [👁]               │
│ ✗ As senhas não coincidem       │
└─────────────────────────────────┘
```

**Entregáveis:**
- [ ] Campo `confirmPassword` no form
- [ ] Validação Zod: `.refine(data => data.password === data.confirmPassword)`
- [ ] Mensagem de erro inline: "As senhas não coincidem"
- [ ] Ambos os campos têm show/hide independentes

**Testes:**
- [ ] Digitar senhas diferentes → erro inline "As senhas não coincidem"
- [ ] Digitar senhas iguais → erro desaparece
- [ ] Toggle show/hide em cada campo é independente
- [ ] Submit bloqueado se senhas não coincidem

---

### UX-04: Login-First Navigation

**Problema Atual:**
- Home page genérica com links para registration e login
- Usuário precisa navegar para encontrar login

**Solução Proposta:**
```
Route `/` → LoginPage (agora é a página inicial)
Route `/register` → RegistrationForm (link na LoginPage)
Route `/profile` → ProfilePage (auth guard mantém)
```

**Fluxo de Boot:**
```
1. User acessa `/`
2. AuthContext verifica isAuthenticated
3. Se logado → redirect automático para `/profile`
4. Se não logado → renderiza LoginPage
```

**Entregáveis:**
- [ ] `index.tsx` route → redireciona para LoginPage (ou ProfilePage se logado)
- [ ] LoginPage: adicionar link "Criar conta" → `/register`
- [ ] AuthContext: `useEffect` no app root verifica auth e redireciona
- [ ] Remover home page genérica (se existir)

**Testes:**
- [ ] Usuário não logado acessa `/` → vê LoginPage
- [ ] Usuário logado acessa `/` → redireciona para `/profile`
- [ ] Usuário logado acessa `/login` → redireciona para `/profile`
- [ ] Link "Criar conta" na LoginPage navega para `/register`

---

### UX-05: Forgot Password Flow

**Problema Atual:**
- Sem mecanismo de recuperação de senha
- Sem email provider para enviar links de reset

**Solução Proposta (3 opções):**

**Opção A: Security Questions (Recomendada para v2 sem email)**
```
1. Usuário clica "Esqueci minha senha" na LoginPage
2. Informa email
3. Responde perguntas de segurança (cadastradas no registro)
4. Se correto → permite definir nova senha
5. Nova senha enviada para Keycloak via Admin API
```

**Prós:** Sem dependência externa, simples
**Contras:** Perguntas de segurança são menos seguras que email

**Opção B: Email via Serviço Free**
```
Serviços free tier:
- Resend.com: 3.000 emails/mês free (API moderna, SDK TS)
- SendGrid: 100 emails/dia free
- Mailgun: 5.000 emails/mês (30 dias trial)
- Brevo (Sendinblue): 300 emails/dia free

Fluxo:
1. Usuário clica "Esqueci minha senha"
2. Informa email
3. Backend gera token de reset (UUID + expiry 15min)
4. Envia email via Resend/SendGrid com link: /reset-password?token=xxx
5. Usuário clica link → define nova senha
6. Backend chama Keycloak Admin API para atualizar senha
```

**Prós:** Fluxo padrão da indústria, mais seguro
**Contras:** Dependência de serviço externo, setup de API key

**Opção C: Admin Reset (Fallback)**
```
1. Usuário contacta suporte (email/telefone)
2. Admin reseta senha via Keycloak Admin Console
3. Senha temporária enviada ao usuário
4. Usuário troca no primeiro login
```

**Prós:** Zero código novo
**Contras:** Não self-service, depende de humano

**Recomendação:** Opção B (Resend.com) — free tier generoso, API moderna, SDK TypeScript oficial

**Entregáveis (Opção B — Resend):**
- [ ] `POST /api/auth/forgot-password` — Endpoint que gera reset token + envia email
- [ ] `POST /api/auth/reset-password` — Endpoint que valida token + atualiza senha via Keycloak Admin API
- [ ] `ForgotPasswordPage.tsx` — Formulário: "Informe seu email"
- [ ] `ResetPasswordPage.tsx` — Formulário: "Nova senha" + "Confirmar senha" (acessado via link do email)
- [ ] Integração com Resend SDK (`resend` npm package)
- [ ] Reset tokens armazenados em DB temporário (expira em 15min)
- [ ] Email template HTML com link de reset

**Testes:**
- [ ] Forgot password com email existente → 200 + email enviado
- [ ] Forgot password com email inexistente → 200 genérico (sem info disclosure)
- [ ] Reset password com token válido → 200 + senha atualizada no Keycloak
- [ ] Reset password com token expirado → 400 "Token expirado"
- [ ] Reset password com token inválido → 400 "Token inválido"
- [ ] Login com nova senha → funciona

**Entregáveis (Opção A — Security Questions):**
- [ ] `SecurityQuestions.tsx` — 5 perguntas padrão + seleção de 3 no cadastro
- [ ] `ForgotPasswordPage.tsx` — Flow: email → perguntas → nova senha
- [ ] Backend: store hashed answers, verify on reset
- [ ] Keycloak Admin API: update password

---

### UX-06: Auto-Login Pós-Cadastro

**Problema Atual:**
- Após cadastro, usuário redirecionado para `/login` e precisa logar manualmente

**Solução Proposta:**
```
1. Usuário preenche RegistrationForm → submit
2. Backend cria usuário no app_db + Keycloak
3. Frontend recebe 201 + { id }
4. Frontend automaticamente faz login com credenciais informadas
5. Redirect para `/profile` — sem tela de login intermediária
```

**Entregáveis:**
- [ ] RegistrationForm: após 201, chamar `loginClient(email, password)` automaticamente
- [ ] Se login automático falhar → fallback para `/login` com mensagem "Cadastro criado. Faça login."
- [ ] Se login automático sucesso → redirect para `/profile`
- [ ] Mensagem de boas-vindas no ProfilePage: "Bem-vindo, {nome}!"

**Testes:**
- [ ] Cadastro PF válido → auto-login → redirect `/profile`
- [ ] Cadastro PJ válido → auto-login → redirect `/profile`
- [ ] Auto-login falha (Keycloak indisponível) → redirect `/login` com mensagem

---

## 📐 Fluxo de Navegação Proposto

```
/ (root)
  ├─ Não logado → LoginPage
  │   ├─ Link: "Criar conta" → /register
  │   ├─ Link: "Esqueci minha senha" → /forgot-password
  │   └─ Login sucesso → /profile
  │
  ├─ Logado → /profile (auto-redirect)
  │   ├─ Botão: "Sair" → /login
  │   └─ (futuro) Botão: "Editar dados" → /profile/edit

/register
  ├─ Formulário único PF/PJ com radio button
  ├─ Password strength meter
  ├─ Show/hide password
  ├─ Confirm password field
  ├─ Link: "Já tem conta? Faça login" → /login
  └─ Submit sucesso → auto-login → /profile

/forgot-password
  ├─ Formulário: email
  ├─ Link: "Voltar para login" → /login
  └─ Submit sucesso → mensagem "Email enviado"

/reset-password?token=xxx
  ├─ Formulário: nova senha + confirmar senha
  ├─ Password strength meter
  ├─ Show/hide password
  └─ Submit sucesso → redirect /login com mensagem "Senha alterada"
```

---

## 🔧 Impacto Técnico

### Arquivos a Criar
| Arquivo | Descrição |
|---------|-----------|
| `frontend/src/components/molecules/RegistrationForm.tsx` | Formulário único PF/PJ |
| `frontend/src/components/molecules/PersonTypeRadio.tsx` | Radio button group |
| `frontend/src/components/molecules/PasswordStrengthMeter.tsx` | Barra de força |
| `frontend/src/components/molecules/PasswordField.tsx` | Input com show/hide |
| `frontend/src/components/pages/ForgotPasswordPage.tsx` | Página forgot password |
| `frontend/src/components/pages/ResetPasswordPage.tsx` | Página reset password |
| `frontend/src/lib/password-strength.ts` | Lógica de cálculo de força |
| `src/Onboarding.API/Controllers/AuthController.cs` | Adicionar endpoints forgot/reset |
| `src/Onboarding.Application/Auth/Commands/ForgotPasswordCommand.cs` | CQRS forgot |
| `src/Onboarding.Application/Auth/Commands/ResetPasswordCommand.cs` | CQRS reset |

### Arquivos a Modificar
| Arquivo | Mudança |
|---------|---------|
| `frontend/src/router.tsx` | Reorganizar rotas (`/` → LoginPage, `/register` → novo form) |
| `frontend/src/components/pages/LoginPage.tsx` | Adicionar links "Criar conta" e "Esqueci minha senha" |
| `frontend/src/lib/auth-context.tsx` | Auto-login pós-cadastro |
| `frontend/src/components/pages/ProfilePage.tsx` | Mensagem de boas-vindas |
| `frontend/src/lib/validation-schemas.ts` | Schema dinâmico para PF/PJ |
| `frontend/src/lib/api.ts` | Adicionar `forgotPasswordClient`, `resetPasswordClient` |

### Arquivos a Remover
| Arquivo | Motivo |
|---------|--------|
| `frontend/src/components/molecules/RegistrationTypeSelector.tsx` | Substituído por PersonTypeRadio |
| `frontend/src/components/molecules/PfRegistrationForm.tsx` | Substituído por RegistrationForm |
| `frontend/src/components/molecules/PjRegistrationForm.tsx` | Substituído por RegistrationForm |

---

## 📊 Métricas de Sucesso

| Métrica | Antes | Depois | Como Medir |
|---------|-------|--------|------------|
| **Clicks para cadastro** | 4+ (escolher tipo → preencher → submit → login) | 2 (preencher → submit) | Contar clicks em teste |
| **Campos visíveis** | 5 (PF) ou 5 (PJ) em telas separadas | 6-7 em tela única (dinâmicos) | UX review |
| **Senha feedback** | Nenhum | 5 níveis + checklist | Visual inspection |
| **Tempo para login pós-cadastro** | 30s+ (redirecionar, digitar credenciais) | 0s (auto-login) | Cronometrar fluxo |
| **Página inicial** | Home genérica | Login (ação direta) | Analytics/heatmap |

---

## ⚠️ Riscos e Mitigações

| Risco | Impacto | Mitigação |
|-------|---------|-----------|
| Radio button PF/PJ confuso | Alto | Label claro: "Pessoa Física (CPF)" / "Pessoa Jurídica (CNPJ)" |
| Password strength meter subjetivo | Médio | Usar algoritmo objetivo (zxcvbn library ou regra própria) |
| Resend.com free tier insuficiente | Baixo | 3.000 emails/mês suporta ~100 cadastros/dia — suficiente para v2 |
| Auto-login falhar silenciosamente | Alto | Fallback explícito para `/login` com mensagem de erro |
| Confirm password irrita usuário | Baixo | Padrão da indústria — esperado pelos usuários |

---

## 🚦 Dependências

- **Phase 10 (Profile UI)** — base para auto-login e redirect
- **Phase 06 (Authentication API)** — base para forgot/reset password endpoints
- **Resend.com API key** — necessário para envio de emails (free tier)
- **Keycloak Admin API** — já configurada para update de senha

---

## 📝 Notas de Implementação

### Password Strength Algorithm

```typescript
function calculatePasswordStrength(password: string): {
  score: number;       // 0-100
  level: "very-weak" | "weak" | "medium" | "strong" | "very-strong";
  checks: {
    minLength: boolean;      // >= 8
    hasUpper: boolean;       // A-Z
    hasLower: boolean;       // a-z
    hasDigit: boolean;       // 0-9
    hasSpecial: boolean;     // !@#$%^&*
    noRepeats: boolean;      // não "aaaa" ou "1234"
  };
}
```

**Cálculo:**
- Base: 0 pontos
- `minLength` (>= 8): +20
- `hasUpper`: +15
- `hasLower`: +15
- `hasDigit`: +15
- `hasSpecial`: +20
- `length >= 12`: +15
- `noRepeats`: bonus de não penalizar

**Níveis:**
- 0-19: Muito Fraca
- 20-39: Fraca
- 40-59: Média
- 60-79: Forte
- 80-100: Muito Forte

### Zod Dynamic Schema

```typescript
const registrationSchema = z.object({
  personType: z.enum(["PF", "PJ"]),
  email: z.string().email("Email invalido"),
  phone: z.string().min(8, "Telefone invalido"),
  password: z.string().min(8, "Minimo 8 caracteres"),
  confirmPassword: z.string(),
  // Conditional fields
  nome: z.string().optional(),
  cpf: z.string().optional(),
  razaoSocial: z.string().optional(),
  cnpj: z.string().optional(),
}).superRefine((data, ctx) => {
  if (data.personType === "PF") {
    if (!data.nome || data.nome.trim() === "") {
      ctx.addIssue({ code: z.ZodIssueCode.custom, message: "Nome obrigatorio", path: ["nome"] });
    }
    if (!data.cpf || !validateCpf(data.cpf)) {
      ctx.addIssue({ code: z.ZodIssueCode.custom, message: "CPF invalido", path: ["cpf"] });
    }
  }
  if (data.personType === "PJ") {
    if (!data.razaoSocial || data.razaoSocial.trim() === "") {
      ctx.addIssue({ code: z.ZodIssueCode.custom, message: "Razao Social obrigatoria", path: ["razaoSocial"] });
    }
    if (!data.cnpj || !validateCnpj(data.cnpj)) {
      ctx.addIssue({ code: z.ZodIssueCode.custom, message: "CNPJ invalido", path: ["cnpj"] });
    }
  }
  if (data.password !== data.confirmPassword) {
    ctx.addIssue({ code: z.ZodIssueCode.custom, message: "Senhas nao coincidem", path: ["confirmPassword"] });
  }
});
```

---

*Documento criado em 2026-04-08*
*Aguardando aprovação para criação do plano de execução*
