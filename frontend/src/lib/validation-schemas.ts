import { z } from "zod";

// ---------------------------------------------------------------------------
// CPF validation (modulo 11 algorithm)
// ---------------------------------------------------------------------------

function validateCpf(cpf: string): boolean {
  const digits = cpf.replace(/\D/g, "");
  if (digits.length !== 11) return false;

  // Reject known invalid patterns (all same digit)
  if (/^(\d)\1{10}$/.test(digits)) return false;

  // First check digit (position 9, 0-indexed)
  let sum = 0;
  for (let i = 0; i < 9; i++) {
    sum += parseInt(digits[i], 10) * (10 - i);
  }
  let remainder = (sum * 10) % 11;
  if (remainder === 10) remainder = 0;
  if (remainder !== parseInt(digits[9], 10)) return false;

  // Second check digit (position 10)
  sum = 0;
  for (let i = 0; i < 10; i++) {
    sum += parseInt(digits[i], 10) * (11 - i);
  }
  remainder = (sum * 10) % 11;
  if (remainder === 10) remainder = 0;
  if (remainder !== parseInt(digits[10], 10)) return false;

  return true;
}

// ---------------------------------------------------------------------------
// CNPJ validation (modulo 11 algorithm)
// ---------------------------------------------------------------------------

function validateCnpj(cnpj: string): boolean {
  const digits = cnpj.replace(/\D/g, "");
  if (digits.length !== 14) return false;

  // Reject known invalid patterns (all same digit)
  if (/^(\d)\1{13}$/.test(digits)) return false;

  // First check digit (position 12, 0-indexed)
  const weights1 = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
  let sum = 0;
  for (let i = 0; i < 12; i++) {
    sum += parseInt(digits[i], 10) * weights1[i];
  }
  let remainder = sum % 11;
  const firstCheck = remainder < 2 ? 0 : 11 - remainder;
  if (firstCheck !== parseInt(digits[12], 10)) return false;

  // Second check digit (position 13)
  const weights2 = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
  sum = 0;
  for (let i = 0; i < 13; i++) {
    sum += parseInt(digits[i], 10) * weights2[i];
  }
  remainder = sum % 11;
  const secondCheck = remainder < 2 ? 0 : 11 - remainder;
  if (secondCheck !== parseInt(digits[13], 10)) return false;

  return true;
}

// ---------------------------------------------------------------------------
// Password validation — mirrors Keycloak password policy (SEC-02)
// ---------------------------------------------------------------------------

const passwordSchema = z
  .string()
  .min(8, "Senha deve ter pelo menos 8 caracteres")
  .regex(/[A-Z]/, "Senha deve conter pelo menos uma letra maiúscula")
  .regex(/[a-z]/, "Senha deve conter pelo menos uma letra minúscula")
  .regex(/\d/, "Senha deve conter pelo menos um número")
  .regex(
    /[!@#$%^&*()_+\-=\[\]{};':"\\|,.<>\/?]/,
    "Senha deve conter pelo menos um caractere especial"
  );

// ---------------------------------------------------------------------------
// PF Registration Schema
// ---------------------------------------------------------------------------

export const pfRegistrationSchema = z.object({
  nome: z
    .string()
    .min(1, "Nome é obrigatório")
    .min(2, "Nome deve ter pelo menos 2 caracteres"),
  cpf: z
    .string()
    .min(1, "CPF é obrigatório")
    .regex(/^\d{11}$/, "CPF deve conter 11 dígitos")
    .refine((val) => validateCpf(val), {
      message: "CPF inválido",
    }),
  email: z.string().min(1, "Email é obrigatório").email("Email inválido"),
  phone: z
    .string()
    .min(1, "Telefone é obrigatório")
    .regex(/^\+?\d{10,11}$/, "Telefone deve conter 10 ou 11 dígitos"),
  password: passwordSchema,
});

export type PfRegistrationData = z.infer<typeof pfRegistrationSchema>;

// ---------------------------------------------------------------------------
// PJ Registration Schema
// ---------------------------------------------------------------------------

export const pjRegistrationSchema = z.object({
  razaoSocial: z
    .string()
    .min(1, "Razão Social é obrigatória")
    .min(2, "Razão Social deve ter pelo menos 2 caracteres"),
  cnpj: z
    .string()
    .min(1, "CNPJ é obrigatório")
    .regex(/^\d{14}$/, "CNPJ deve conter 14 dígitos")
    .refine((val) => validateCnpj(val), {
      message: "CNPJ inválido",
    }),
  email: z.string().min(1, "Email é obrigatório").email("Email inválido"),
  phone: z
    .string()
    .min(1, "Telefone é obrigatório")
    .regex(/^\+?\d{10,11}$/, "Telefone deve conter 10 ou 11 dígitos"),
  password: passwordSchema,
});

export type PjRegistrationData = z.infer<typeof pjRegistrationSchema>;
