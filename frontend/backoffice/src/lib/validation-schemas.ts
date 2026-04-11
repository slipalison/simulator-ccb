import { z } from "zod";

// ---------------------------------------------------------------------------
// Admin Edit User Schema — partial fields for user editing
// ---------------------------------------------------------------------------
// All fields are optional (partial update), but when provided must be valid.
// ---------------------------------------------------------------------------

export const adminEditUserSchema = z.object({
  name: z
    .string()
    .min(2, "Nome deve ter pelo menos 2 caracteres")
    .max(100, "Nome deve ter no maximo 100 caracteres")
    .or(z.literal("")),
  email: z
    .string()
    .refine((val) => val === "" || /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(val), "Email invalido"),
  phone: z
    .string()
    .regex(/^\(\d{2}\)\s?\d{4,5}-\d{4}$/, "Telefone deve estar no formato (XX) XXXXX-XXXX")
    .or(z.literal("")),
  address: z
    .string()
    .max(300, "Endereco deve ter no maximo 300 caracteres")
    .or(z.literal("")),
});

export type AdminEditUserInput = z.infer<typeof adminEditUserSchema>;

// ---------------------------------------------------------------------------
// Block User Schema — reason required, min 10 chars
// ---------------------------------------------------------------------------

export const adminBlockUserSchema = z.object({
  reason: z
    .string()
    .min(10, "Motivo deve ter pelo menos 10 caracteres")
    .max(500, "Motivo deve ter no maximo 500 caracteres"),
});

export type AdminBlockUserInput = z.infer<typeof adminBlockUserSchema>;

// ---------------------------------------------------------------------------
// Unblock User Schema — reason is optional
// ---------------------------------------------------------------------------

export const adminUnblockUserSchema = z.object({
  reason: z
    .string()
    .max(500, "Motivo deve ter no maximo 500 caracteres")
    .optional()
    .or(z.literal("")),
});

export type AdminUnblockUserInput = z.infer<typeof adminUnblockUserSchema>;
