# Proposta — Phase 12: UI Redesign com shadcn/ui + Temas

**Motivação:** Telas atuais extremamente feias — componentes básicos sem polimento visual, sem tema dark/light

---

## 🎯 Objetivos

1. **Adotar shadcn/ui** — Componentes profissionais, acessíveis, customizáveis
2. **Implementar tema Dark/Light** — Toggle no header, persistência em localStorage
3. **Redesign completo** — Todas as telas (Login, Registration, Profile, Forgot/Reset Password)
4. **Consistência visual** — Design system unificado com tokens de cor, tipografia, espaçamento

---

## 📋 Requisitos

### UI-01: shadcn/ui Setup

**Entregáveis:**
- [ ] Instalar shadcn/ui CLI (`npx shadcn@latest init`)
- [ ] Configurar Tailwind CSS com tokens shadcn (CSS variables)
- [ ] Instalar componentes base:
  - `button` — Variantes: default, destructive, outline, secondary, ghost, link
  - `input` — Com suporte a labels, errors, icons
  - `label` — Acessibilidade correta
  - `card` — Containers com header, content, footer
  - `form` — Integration com React Hook Form + Zod
  - `radio-group` — Seleção PF/PJ
  - `alert` — Mensagens de erro/sucesso
  - `alert-dialog` — Confirmações (logout, etc.)
  - `dropdown-menu` — Menu de usuário no header
  - `separator` — Divisores visuais
  - `skeleton` — Loading states
  - `toast` — Notificações (sucesso, erro)
  - `theme-provider` — Context para dark/light mode

**Configuração Tailwind:**
```css
@layer base {
  :root {
    --background: 0 0% 100%;
    --foreground: 222.2 84% 4.9%;
    --card: 0 0% 100%;
    --card-foreground: 222.2 84% 4.9%;
    --primary: 222.2 47.4% 11.2%;
    --primary-foreground: 210 40% 98%;
    --secondary: 210 40% 96.1%;
    --secondary-foreground: 222.2 47.4% 11.2%;
    --muted: 210 40% 96.1%;
    --muted-foreground: 215.4 16.3% 46.9%;
    --accent: 210 40% 96.1%;
    --accent-foreground: 222.2 47.4% 11.2%;
    --destructive: 0 84.2% 60.2%;
    --destructive-foreground: 210 40% 98%;
    --border: 214.3 31.8% 91.4%;
    --input: 214.3 31.8% 91.4%;
    --ring: 222.2 84% 4.9%;
    --radius: 0.5rem;
  }

  .dark {
    --background: 222.2 84% 4.9%;
    --foreground: 210 40% 98%;
    --card: 222.2 84% 4.9%;
    --card-foreground: 210 40% 98%;
    --primary: 210 40% 98%;
    --primary-foreground: 222.2 47.4% 11.2%;
    --secondary: 217.2 32.6% 17.5%;
    --secondary-foreground: 210 40% 98%;
    --muted: 217.2 32.6% 17.5%;
    --muted-foreground: 215 20.2% 65.1%;
    --accent: 217.2 32.6% 17.5%;
    --accent-foreground: 210 40% 98%;
    --destructive: 0 62.8% 30.6%;
    --destructive-foreground: 210 40% 98%;
    --border: 217.2 32.6% 17.5%;
    --input: 217.2 32.6% 17.5%;
    --ring: 212.7 26.8% 83.9%;
  }
}
```

**Dependências:**
```bash
npx shadcn@latest init
npx shadcn@latest add button input label card form radio-group
npx shadcn@latest add alert alert-dialog dropdown-menu separator
npx shadcn@latest add skeleton toast
npm install lucide-react  # Icons
npm install next-themes   # Theme provider (funciona com Vinxi)
npm install @hookform/resolvers  # Zod resolver para shadcn form
```

---

### UI-02: Theme Toggle (Dark/Light)

**Entregáveis:**
- [ ] `ThemeProvider.tsx` — Wrapper usando `next-themes`
- [ ] `ThemeToggle.tsx` — Botão no header (sol/lua ícone)
- [ ] Persistência em localStorage (next-themes faz automaticamente)
- [ ] Detecção de preferência do sistema (`prefers-color-scheme`)
- [ ] Transição suave entre temas (`transition-colors` no body)

**UI:**
```
┌─────────────────────────────────────┐
│  Onboarding               [🌙] [👤]│
│                           Dark     │
└─────────────────────────────────────┘
```

**Comportamento:**
- Click no toggle → muda tema instantaneamente
- Refresh da página → mantém tema escolhido
- Primeira visita → respeita `prefers-color-scheme` do OS

**Testes:**
- [ ] Renderiza com tema light por padrão
- [ ] Toggle para dark → aplica classe `.dark` no `<html>`
- [ ] Refresh → tema persiste
- [ ] Transição entre temas é suave (sem flash)

---

### UI-03: LoginPage Redesign

**Mockup Light Mode:**
```
┌──────────────────────────────────────────┐
│                                          │
│          ┌────────────────────┐          │
│          │  🏢 Onboarding     │          │
│          │                    │          │
│          │  Bem-vindo de      │          │
│          │  volta!            │          │
│          │                    │          │
│          │  Email             │          │
│          │  ┌──────────────┐  │          │
│          │  │              │  │          │
│          │  └──────────────┘  │          │
│          │                    │          │
│          │  Senha             │          │
│          │  ┌──────────────┐  │          │
│          │  │              │  │          │
│          │  └──────────────┘  │          │
│          │                    │          │
│          │  ┌──────────────┐  │          │
│          │  │    Entrar    │  │          │
│          │  └──────────────┘  │          │
│          │                    │          │
│          │  Esqueceu a senha? │          │
│          │  Não tem conta?    │          │
│          │  Criar conta →     │          │
│          └────────────────────┘          │
│                                          │
│          © 2026 Onboarding               │
└──────────────────────────────────────────┘
```

**Entregáveis:**
- [ ] `LoginPage.tsx` redesenhado com shadcn Card, Form, Input, Button
- [ ] Layout centralizado (vertical + horizontal)
- [ ] Logo/título no topo do card
- [ ] Links: "Esqueceu a senha?" → `/forgot-password`, "Criar conta" → `/register`
- [ ] Alert de erro genérico (credenciais inválidas)
- [ ] Loading state no botão (spinner + disabled)
- [ ] Dark mode: card com background escuro, bordas sutis

**Componentes shadcn usados:**
- `Card` (container)
- `CardHeader`, `CardTitle`, `CardDescription`
- `CardContent`
- `FormField`, `FormItem`, `FormLabel`, `FormControl`, `FormMessage`
- `Input`
- `Button`
- `Alert` (erro de login)

---

### UI-04: RegistrationPage Redesign

**Mockup Light Mode:**
```
┌──────────────────────────────────────────┐
│                                          │
│          ┌────────────────────┐          │
│          │  Criar sua conta   │          │
│          │                    │          │
│          │  Tipo de pessoa:   │          │
│          │  ◉ Pessoa Física   │          │
│          │  ○ Pessoa Jurídica │          │
│          │                    │          │
│          │  Nome completo     │          │
│          │  ┌──────────────┐  │          │
│          │  │              │  │          │
│          │  └──────────────┘  │          │
│          │                    │          │
│          │  CPF               │          │
│          │  ┌──────────────┐  │          │
│          │  │ 000.000.000  │  │          │
│          │  └──────────────┘  │          │
│          │                    │          │
│          │  Email             │          │
│          │  ┌──────────────┐  │          │
│          │  │              │  │          │
│          │  └──────────────┘  │          │
│          │                    │          │
│          │  Telefone          │          │
│          │  ┌──────────────┐  │          │
│          │  │ (00) 00000-  │  │          │
│          │  └──────────────┘  │          │
│          │                    │          │
│          │  Senha             │          │
│          │  ┌──────────────┐  │          │
│          │  │ ●●●●●●●●  👁 │  │          │
│          │  └──────────────┘  │          │
│          │  ████████░░ Forte  │          │
│          │  ✓ 8+ caracteres   │          │
│          │  ✓ Maiúscula       │          │
│          │  ✓ Número          │          │
│          │  ✗ Especial        │          │
│          │                    │          │
│          │  Confirmar senha   │          │
│          │  ┌──────────────┐  │          │
│          │  │ ●●●●●●●●  👁 │  │          │
│          │  └──────────────┘  │          │
│          │  ✓ As senhas       │          │
│          │    coincidem       │          │
│          │                    │          │
│          │  ┌──────────────┐  │          │
│          │  │  Criar conta │  │          │
│          │  └──────────────┘  │          │
│          │                    │          │
│          │  Já tem conta?     │          │
│          │  Fazer login →     │          │
│          └────────────────────┘          │
│                                          │
└──────────────────────────────────────────┘
```

**Mudança para PJ (radio button):**
```
│  Tipo de pessoa:   │
│  ○ Pessoa Física   │
│  ◉ Pessoa Jurídica │
│                    │
│  Razão Social      │
│  ┌──────────────┐  │
│  │              │  │
│  └──────────────┘  │
│                    │
│  CNPJ              │
│  ┌──────────────┐  │
│  │ 00.000.000/  │  │
│  └──────────────┘  │
```

**Entregáveis:**
- [ ] `RegistrationForm.tsx` com shadcn Form, RadioGroup, Input, Button
- [ ] Radio group estilizado para PF/PJ (não radio nativo)
- [ ] PasswordStrengthMeter integrado ao form (barra + checklist)
- [ ] PasswordField com show/hide toggle (ícone lucide `Eye`/`EyeOff`)
- [ ] Confirm password field com validação em tempo real
- [ ] Campos condicionais com transição suave (fade in/out)
- [ ] Loading state no botão de submit
- [ ] Toast de sucesso no cadastro
- [ ] Alert de erro genérico (duplicado, indisponível)
- [ ] Link "Já tem conta? Fazer login" → `/login`

**Componentes shadcn usados:**
- Todos da LoginPage + `RadioGroup`, `RadioGroupItem`
- `Toast` (notificação de sucesso)

**Componentes custom mantidos:**
- `PasswordStrengthMeter.tsx` (barra de força — não existe no shadcn)
- `PasswordField.tsx` (input com toggle — shadcn Input + custom wrapper)

---

### UI-05: ProfilePage Redesign

**Mockup Light Mode:**
```
┌──────────────────────────────────────────┐
│  Onboarding              [🌙] [👤 ▼]    │
├──────────────────────────────────────────┤
│                                          │
│          ┌────────────────────┐          │
│          │  Meu Perfil        │          │
│          │                    │          │
│          │  ┌──────────────┐  │          │
│          │  │ Pessoa Física│  │          │
│          │  └──────────────┘  │          │
│          │                    │          │
│          │  Nome completo     │          │
│          │  João da Silva     │          │
│          │                    │          │
│          │  CPF               │          │
│          │  123.456.789-00    │          │
│          │                    │          │
│          │  Email             │          │
│          │  joao@teste.com    │          │
│          │                    │          │
│          │  Telefone          │          │
│          │  (11) 99999-8888   │          │
│          │                    │          │
│          │  ┌──────────────┐  │          │
│          │  │    Sair      │  │          │
│          │  └──────────────┘  │          │
│          └────────────────────┘          │
│                                          │
└──────────────────────────────────────────┘
```

**Entregáveis:**
- [ ] `ProfilePage.tsx` com shadcn Card, Avatar (futuro), Badge, Button
- [ ] Header fixo com logo, theme toggle, user menu
- [ ] Badge PF/PJ estilizado (verde/azul — manter distinção visual)
- [ ] Dados em formato label-value (não inputs)
- [ ] Skeleton loading state (enquanto busca dados)
- [ ] Empty state se dados faltando
- [ ] Botão "Sair" no footer do card (variant: destructive ou outline)
- [ ] Toast de confirmação de logout

**Componentes shadcn usados:**
- `Card`, `CardHeader`, `CardTitle`, `CardContent`
- `Badge` (PF/PJ indicator)
- `Skeleton` (loading state)
- `Button` (logout)
- `DropdownMenu` (user menu — futuro)
- `Toast` (logout confirmation)

---

### UI-06: Header/Navigation Component

**Entregáveis:**
- [ ] `Header.tsx` — Componente orgânismo com:
  - Logo à esquerda
  - Theme toggle (sol/lua) à direita
  - User menu (dropdown) à direita:
    - "Meu Perfil" → `/profile`
    - "Sair" → logout
- [ ] Header fixo no topo (sticky ou fixed)
- [ ] Responsivo: hamburger menu em mobile (futuro)
- [ ] Dark mode: header com background escuro, bordas inferiores sutis

**Componentes shadcn usados:**
- `DropdownMenu`, `DropdownMenuTrigger`, `DropdownMenuContent`
- `DropdownMenuItem`
- `Separator` (divisor no dropdown)

---

### UI-07: Forgot/Reset Password Pages

**ForgotPasswordPage:**
```
┌────────────────────────────────┐
│  Recuperar senha               │
│                                │
│  Informe seu email para        │
│  receber um link de            │
│  recuperação.                  │
│                                │
│  Email                         │
│  ┌──────────────────────────┐  │
│  │                          │  │
│  └──────────────────────────┘  │
│                                │
│  ┌──────────────────────────┐  │
│  │  Enviar link             │  │
│  └──────────────────────────┘  │
│                                │
│  Voltar para login →           │
└────────────────────────────────┘
```

**ResetPasswordPage:**
```
┌────────────────────────────────┐
│  Nova senha                    │
│                                │
│  Senha                         │
│  ┌──────────────────────────┐  │
│  │ ●●●●●●●●  👁             │  │
│  └──────────────────────────┘  │
│  ████████░░ Forte              │
│                                │
│  Confirmar senha               │
│  ┌──────────────────────────┐  │
│  │ ●●●●●●●●  👁             │  │
│  └──────────────────────────┘  │
│  ✓ As senhas coincidem         │
│                                │
│  ┌──────────────────────────┐  │
│  │  Alterar senha           │  │
│  └──────────────────────────┘  │
│                                │
│  Voltar para login →           │
└────────────────────────────────┘
```

**Entregáveis:**
- [ ] `ForgotPasswordPage.tsx` com shadcn Card, Form, Input, Button
- [ ] `ResetPasswordPage.tsx` com PasswordField + ConfirmPassword + StrengthMeter
- [ ] Mensagem de sucesso após envio do email
- [ ] Toast de erro/sucesso
- [ ] Links para voltar ao login

---

## 📐 Design System

### Cores Primárias (Customizável via CSS Variables)

```css
--primary: 222.2 47.4% 11.2%;       /* Azul escuro (light) / Branco (dark) */
--primary-foreground: 210 40% 98%;  /* Branco (light) / Azul escuro (dark) */
--destructive: 0 84.2% 60.2%;       /* Vermelho para erros */
--success: 142 76% 36%;             /* Verde para sucesso (custom) */
--warning: 38 92% 50%;              /* Amarelo para avisos (custom) */
```

### Tipografia

```css
/* Fontes do sistema (shadcn default) */
font-sans: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif
font-mono: ui-monospace, SFMono-Regular, "SF Mono", Menlo, Consolas, monospace

/* Tamanhos */
text-xs: 0.75rem    /* 12px — labels secundários */
text-sm: 0.875rem   /* 14px — inputs, body text */
text-base: 1rem     /* 16px — body */
text-lg: 1.125rem   /* 18px — subtítulos */
text-xl: 1.25rem    /* 20px — títulos de card */
text-2xl: 1.5rem    /* 24px — títulos de página */
```

### Espaçamento

```css
spacing-1: 0.25rem  /* 4px */
spacing-2: 0.5rem   /* 8px */
spacing-3: 0.75rem  /* 12px */
spacing-4: 1rem     /* 16px */
spacing-6: 1.5rem   /* 24px */
spacing-8: 2rem     /* 32px */
```

### Border Radius

```css
--radius: 0.5rem;  /* 8px — padrão para inputs, cards, buttons */
rounded-sm: 0.125rem  /* 2px — badges */
rounded-md: 0.375rem  /* 6px — inputs */
rounded-lg: 0.5rem    /* 8px — cards */
rounded-xl: 0.75rem   /* 12px — modais */
```

---

## 🔧 Impacto Técnico

### Arquivos a Criar
| Arquivo | Descrição |
|---------|-----------|
| `frontend/components.json` | shadcn/ui config |
| `frontend/src/lib/theme-provider.tsx` | Theme context com next-themes |
| `frontend/src/components/atoms/ThemeToggle.tsx` | Botão de troca de tema |
| `frontend/src/components/organisms/Header.tsx` | Header com logo, theme, user menu |
| `frontend/src/components/molecules/PasswordField.tsx` | Input com show/hide (reutilizável) |
| `frontend/src/components/pages/ForgotPasswordPage.tsx` | Página forgot password |
| `frontend/src/components/pages/ResetPasswordPage.tsx` | Página reset password |
| `frontend/src/styles/globals.css` | Tailwind + shadcn CSS variables |

### Arquivos a Modificar
| Arquivo | Mudança |
|---------|---------|
| `frontend/tailwind.config.ts` | Adicionar tokens shadcn |
| `frontend/app.config.ts` | Adicionar theme provider |
| `frontend/src/components/pages/LoginPage.tsx` | Redesign completo com shadcn |
| `frontend/src/components/pages/RegistrationPage.tsx` | Redesign completo com shadcn |
| `frontend/src/components/pages/ProfilePage.tsx` | Redesign completo com shadcn |
| `frontend/src/components/molecules/RegistrationForm.tsx` | Usar shadcn Form, Input, RadioGroup |
| `frontend/src/components/molecules/LoginForm.tsx` | Usar shadcn Form, Input |
| `frontend/src/components/molecules/PasswordStrengthMeter.tsx` | Estilizar com tokens shadcn |
| `frontend/src/components/atoms/ProfileField.tsx` | Usar shadcn tipografia |
| `frontend/src/components/atoms/ProfileBadge.tsx` | Usar shadcn Badge |
| `frontend/src/router.tsx` | Adicionar rotas forgot/reset |
| `frontend/src/main.tsx` | Adicionar ThemeProvider + Header |

### Arquivos a Remover
| Arquivo | Motivo |
|---------|--------|
| `frontend/src/components/atoms/LabeledField.tsx` | Substituído por shadcn Form + Input |
| `frontend/src/components/atoms/AppButton.tsx` | Substituído por shadcn Button |
| `frontend/src/components/molecules/PageLayout.tsx` | Substituído por shadcn Card + Header |
| `frontend/src/components/organisms/ExampleForm.tsx` | Exemplo de scaffold, não mais necessário |

---

## 📊 Checklist de Componentes shadcn

### Installados Obrigatoriamente

| Componente | Uso | Obrigatório |
|------------|-----|-------------|
| `button` | Login, Register, Logout, Submit | ✅ |
| `input` | Email, password, CPF, CNPJ, etc | ✅ |
| `label` | Labels de formulários | ✅ |
| `card` | Containers de páginas | ✅ |
| `form` | Integration RHF + Zod | ✅ |
| `radio-group` | Seleção PF/PJ | ✅ |
| `alert` | Erros de login, registro | ✅ |
| `toast` | Notificações de sucesso/erro | ✅ |
| `skeleton` | Loading states | ✅ |
| `badge` | Indicador PF/PJ no profile | ✅ |
| `separator` | Divisores visuais | ✅ |
| `dropdown-menu` | User menu no header | ✅ |
| `alert-dialog` | Confirmação de logout | Opcional |
| `avatar` | Avatar do usuário (futuro) | Futuro |

---

## 🎨 Paleta de Cores Sugerida

### Light Mode

| Elemento | Cor | HEX | Uso |
|----------|-----|-----|-----|
| **Primary** | Azul escuro | `#0f172a` | Botões, links, títulos |
| **Primary FG** | Branco | `#f8fafc` | Texto em botões primários |
| **Background** | Branco | `#ffffff` | Fundo da página |
| **Card** | Branco | `#ffffff` | Fundo de cards |
| **Border** | Cinza claro | `#e2e8f0` | Bordas de inputs/cards |
| **Muted** | Cinza | `#f1f5f9` | Backgrounds secundários |
| **Muted FG** | Cinza médio | `#64748b` | Texto secundário |
| **Destructive** | Vermelho | `#ef4444` | Erros, botão sair |
| **Success** | Verde | `#22c55e` | Sucesso, toast |
| **Warning** | Amarelo | `#f59e0b` | Avisos, password fraca |

### Dark Mode

| Elemento | Cor | HEX | Uso |
|----------|-----|-----|-----|
| **Primary** | Branco | `#f8fafc` | Botões, links, títulos |
| **Primary FG** | Azul escuro | `#0f172a` | Texto em botões primários |
| **Background** | Azul muito escuro | `#020817` | Fundo da página |
| **Card** | Azul escuro | `#0f172a` | Fundo de cards |
| **Border** | Azul acinzentado | `#1e293b` | Bordas de inputs/cards |
| **Muted** | Azul acinzentado | `#1e293b` | Backgrounds secundários |
| **Muted FG** | Cinza azulado | `#94a3b8` | Texto secundário |

---

## ⚠️ Riscos e Mitigações

| Risco | Impacto | Mitigação |
|-------|---------|-----------|
| shadcn/ui incompatível com Vinxi | Alto | Testar com `npx shadcn@latest init` antes de começar; shadcn é agnóstico ao framework |
| Tailwind v4 conflita com shadcn | Médio | shadcn suporta Tailwind v4 — verificar versão no init |
| next-themes não funciona com Vinxi | Médio | next-themes é React-only, deve funcionar; se não, implementar provider custom |
| Tema dark com contraste ruim | Alto | Testar com ferramenta de acessibilidade (axe DevTools) |
| Transição de temas causa flash | Médio | Usar `suppressHydrationWarning` + CSS `transition-colors` |
| ícones lucide-react aumentam bundle | Baixo | Tree-shaking remove não usados; ~5KB gzipped |
| shadcn form conflita com RHF existente | Baixo | shadcn form É um wrapper para RHF — integração nativa |

---

## 🚦 Dependências

- **Phase 11 (UX Redesign)** — Formulários e fluxo definidos antes de estilizar
- **Tailwind CSS v4** — Já instalado (verificar compatibilidade com shadcn)
- **React Hook Form** — Já instalado, integração nativa com shadcn form
- **Zod** — Já instalado, integração nativa com shadcn form via `@hookform/resolvers`

---

## 📝 Notas de Implementação

### shadcn/ui Architecture

shadcn/ui **não é um npm package**. É uma coleção de componentes que você copia para o projeto:

```bash
npx shadcn@latest add button
# Copia components/ui/button.tsx para seu projeto
# Você pode modificar livremente — é seu código
```

**Estrutura gerada:**
```
frontend/src/
├── components/
│   ├── ui/
│   │   ├── button.tsx
│   │   ├── input.tsx
│   │   ├── card.tsx
│   │   └── ...
│   ├── atoms/
│   ├── molecules/
│   └── organisms/
```

### Theme Provider com next-themes

```tsx
// src/lib/theme-provider.tsx
"use client"
import * as React from "react"
import { ThemeProvider as NextThemesProvider } from "next-themes"

export function ThemeProvider({ children, ...props }: React.ComponentProps<typeof NextThemesProvider>) {
  return <NextThemesProvider {...props}>{children}</NextThemesProvider>
}
```

**Uso no app root:**
```tsx
// src/main.tsx
import { ThemeProvider } from "./lib/theme-provider"

<ThemeProvider attribute="class" defaultTheme="system" enableSystem>
  <AuthProvider>
    <RouterProvider router={router} />
  </AuthProvider>
</ThemeProvider>
```

**Toggle:**
```tsx
// src/components/atoms/ThemeToggle.tsx
import { useTheme } from "next-themes"
import { Moon, Sun } from "lucide-react"
import { Button } from "@/components/ui/button"

export function ThemeToggle() {
  const { theme, setTheme } = useTheme()
  
  return (
    <Button variant="ghost" size="icon" onClick={() => setTheme(theme === "light" ? "dark" : "light")}>
      <Sun className="h-[1.2rem] w-[1.2rem] rotate-0 scale-100 transition-all dark:-rotate-90 dark:scale-0" />
      <Moon className="absolute h-[1.2rem] w-[1.2rem] rotate-90 scale-0 transition-all dark:rotate-0 dark:scale-100" />
      <span className="sr-only">Toggle theme</span>
    </Button>
  )
}
```

### shadcn Form + RHF + Zod Integration

```tsx
import { useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod"
import * as z from "zod"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from "@/components/ui/form"

const formSchema = z.object({
  email: z.string().email(),
  password: z.string().min(8),
})

function LoginForm() {
  const form = useForm<z.infer<typeof formSchema>>({
    resolver: zodResolver(formSchema),
    defaultValues: { email: "", password: "" },
  })

  function onSubmit(values: z.infer<typeof formSchema>) {
    // values.email, values.password
  }

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(onSubmit)}>
        <FormField
          control={form.control}
          name="email"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Email</FormLabel>
              <FormControl>
                <Input placeholder="seu@email.com" {...field} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />
        <Button type="submit">Entrar</Button>
      </form>
    </Form>
  )
}
```

---

## 📐 Entregáveis por Task

### Task 1: shadcn Setup + Theme Infrastructure
- [ ] `npx shadcn@latest init` + componentes base
- [ ] Tailwind config com tokens
- [ ] ThemeProvider com next-themes
- [ ] ThemeToggle button
- [ ] globals.css com CSS variables light/dark
- [ ] Test: toggle funciona, persiste em refresh

### Task 2: LoginPage + RegistrationPage Redesign
- [ ] LoginPage com shadcn Card, Form, Input, Button
- [ ] RegistrationForm com shadcn RadioGroup, Form, Input
- [ ] PasswordStrengthMeter integrado
- [ ] PasswordField com show/hide
- [ ] Confirm password field
- [ ] Test: forms renderizam, validam, submetem

### Task 3: ProfilePage + Header + Forgot/Reset Pages
- [ ] Header com logo, theme toggle, user menu
- [ ] ProfilePage com shadcn Card, Badge, Skeleton
- [ ] ForgotPasswordPage com shadcn Form
- [ ] ResetPasswordPage com PasswordField + StrengthMeter
- [ ] Remover componentes antigos (LabeledField, AppButton, PageLayout)
- [ ] Test: navegação, theme, logout

---

*Documento criado em 2026-04-08*
*Aguardando aprovação para criação do plano de execução*
