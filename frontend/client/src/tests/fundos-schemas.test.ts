// ---------------------------------------------------------------------------
// fundos-schemas.test.ts — unit tests for Zod schemas (T-2)
// Validates that schemas match backend FluentValidation rules
// ---------------------------------------------------------------------------

import { describe, it, expect } from "vitest";
import {
  createTipoAtivoSchema,
  createConsultoriaFundoSchema,
  createCedentePfSchema,
  createCedentePjSchema,
  createAssociationSchema,
  FundoStatusEnum,
  RelationshipStatusEnum,
  FUNDO_STATUS_TRANSITIONS,
  RELATIONSHIP_STATUS_TRANSITIONS,
} from "@/lib/fundos-schemas";

// ---------------------------------------------------------------------------
// TipoAtivo schemas
// ---------------------------------------------------------------------------

describe("createTipoAtivoSchema", () => {
  it("accepts valid data", () => {
    const result = createTipoAtivoSchema.safeParse({
      codigo: "RF001",
      descricao: "Renda Fixa Gov",
      categoria: "RendaFixa",
      ordemExibicao: 1,
    });
    expect(result.success).toBe(true);
  });

  it("rejects empty codigo", () => {
    const result = createTipoAtivoSchema.safeParse({
      codigo: "",
      descricao: "Valid description",
      categoria: "RendaFixa",
    });
    expect(result.success).toBe(false);
  });

  it("rejects empty descricao", () => {
    const result = createTipoAtivoSchema.safeParse({
      codigo: "RF001",
      descricao: "",
      categoria: "RendaFixa",
    });
    expect(result.success).toBe(false);
  });

  it("rejects invalid categoria", () => {
    const result = createTipoAtivoSchema.safeParse({
      codigo: "RF001",
      descricao: "Desc",
      categoria: "InvalidCat",
    });
    expect(result.success).toBe(false);
  });
});

// ---------------------------------------------------------------------------
// ConsultoriaFundo schemas
// ---------------------------------------------------------------------------

describe("createConsultoriaFundoSchema", () => {
  const validCnpj = "11222333000181"; // valid CNPJ check digits

  it("accepts valid data", () => {
    const result = createConsultoriaFundoSchema.safeParse({
      razaoSocial: "Consultoria XYZ LTDA",
      cnpj: validCnpj,
    });
    expect(result.success).toBe(true);
  });

  it("rejects CNPJ with wrong length", () => {
    const result = createConsultoriaFundoSchema.safeParse({
      razaoSocial: "Valid Name",
      cnpj: "1234567890123",
    });
    expect(result.success).toBe(false);
  });

  it("rejects razaoSocial shorter than 2 chars", () => {
    const result = createConsultoriaFundoSchema.safeParse({
      razaoSocial: "A",
      cnpj: validCnpj,
    });
    expect(result.success).toBe(false);
  });

  it("accepts optional email/phone as null", () => {
    const result = createConsultoriaFundoSchema.safeParse({
      razaoSocial: "Valid Name",
      cnpj: validCnpj,
      email: null,
      telefone: null,
    });
    expect(result.success).toBe(true);
  });
});

// ---------------------------------------------------------------------------
// Cedente PF schema
// ---------------------------------------------------------------------------

describe("createCedentePfSchema", () => {
  const validCpf = "52998224725"; // valid CPF

  it("accepts valid PF data", () => {
    const result = createCedentePfSchema.safeParse({
      cpf: validCpf,
      nome: "João Silva",
    });
    expect(result.success).toBe(true);
  });

  it("rejects CPF with wrong length", () => {
    const result = createCedentePfSchema.safeParse({
      cpf: "1234567890",
      nome: "João Silva",
    });
    expect(result.success).toBe(false);
  });

  it("rejects empty nome", () => {
    const result = createCedentePfSchema.safeParse({
      cpf: validCpf,
      nome: "",
    });
    expect(result.success).toBe(false);
  });
});

// ---------------------------------------------------------------------------
// Cedente PJ schema
// ---------------------------------------------------------------------------

describe("createCedentePjSchema", () => {
  const validCnpj = "11222333000181";

  it("accepts valid PJ data", () => {
    const result = createCedentePjSchema.safeParse({
      cnpj: validCnpj,
      razaoSocial: "Empresa XYZ LTDA",
    });
    expect(result.success).toBe(true);
  });

  it("rejects invalid CNPJ", () => {
    const result = createCedentePjSchema.safeParse({
      cnpj: "00000000000000",
      razaoSocial: "Empresa XYZ",
    });
    expect(result.success).toBe(false);
  });
});

// ---------------------------------------------------------------------------
// Fundo status transitions
// ---------------------------------------------------------------------------

describe("FUNDO_STATUS_TRANSITIONS", () => {
  it("RASCUNHO → only ATIVO allowed", () => {
    expect(FUNDO_STATUS_TRANSITIONS.RASCUNHO).toEqual(["ATIVO"]);
  });

  it("ATIVO → SUSPENSO and EM_LIQUIDACAO", () => {
    expect(FUNDO_STATUS_TRANSITIONS.ATIVO).toContain("SUSPENSO");
    expect(FUNDO_STATUS_TRANSITIONS.ATIVO).toContain("EM_LIQUIDACAO");
  });

  it("ENCERRADO is terminal (empty)", () => {
    expect(FUNDO_STATUS_TRANSITIONS.ENCERRADO).toEqual([]);
  });

  it("SUSPENSO → back to ATIVO", () => {
    expect(FUNDO_STATUS_TRANSITIONS.SUSPENSO).toContain("ATIVO");
  });

  it("EM_LIQUIDACAO → ENCERRADO", () => {
    expect(FUNDO_STATUS_TRANSITIONS.EM_LIQUIDACAO).toContain("ENCERRADO");
  });
});

// ---------------------------------------------------------------------------
// Relationship status transitions
// ---------------------------------------------------------------------------

describe("RELATIONSHIP_STATUS_TRANSITIONS", () => {
  it("HISTORICO is terminal", () => {
    expect(RELATIONSHIP_STATUS_TRANSITIONS.HISTORICO).toEqual([]);
  });

  it("ATIVO → INATIVO and HISTORICO", () => {
    expect(RELATIONSHIP_STATUS_TRANSITIONS.ATIVO).toContain("INATIVO");
    expect(RELATIONSHIP_STATUS_TRANSITIONS.ATIVO).toContain("HISTORICO");
  });
});

// ---------------------------------------------------------------------------
// Association schema
// ---------------------------------------------------------------------------

describe("createAssociationSchema", () => {
  it("accepts valid association", () => {
    const result = createAssociationSchema.safeParse({
      targetId: "550e8400-e29b-41d4-a716-446655440000",
      dataInicio: "2024-01-01",
    });
    expect(result.success).toBe(true);
  });

  it("rejects dataFim before dataInicio", () => {
    const result = createAssociationSchema.safeParse({
      targetId: "550e8400-e29b-41d4-a716-446655440000",
      dataInicio: "2024-06-01",
      dataFim: "2024-01-01",
    });
    expect(result.success).toBe(false);
    if (!result.success) {
      // Zod 4: .issues array (Zod 3 used .errors)
      const issues = result.error.issues ?? [];
      const fields = issues.map((e) =>
        e.path.map((p) => String(p)).join(".")
      );
      expect(fields).toContain("dataFim");
    }
  });

  it("accepts limitePercentual within 0-100", () => {
    const result = createAssociationSchema.safeParse({
      targetId: "550e8400-e29b-41d4-a716-446655440000",
      dataInicio: "2024-01-01",
      limitePercentual: 50,
    });
    expect(result.success).toBe(true);
  });

  it("rejects limitePercentual > 100", () => {
    const result = createAssociationSchema.safeParse({
      targetId: "550e8400-e29b-41d4-a716-446655440000",
      dataInicio: "2024-01-01",
      limitePercentual: 150,
    });
    expect(result.success).toBe(false);
  });
});

// ---------------------------------------------------------------------------
// FundoStatusEnum
// ---------------------------------------------------------------------------

describe("FundoStatusEnum", () => {
  it("contains all 5 statuses", () => {
    expect(FundoStatusEnum.options).toEqual([
      "RASCUNHO",
      "ATIVO",
      "SUSPENSO",
      "EM_LIQUIDACAO",
      "ENCERRADO",
    ]);
  });
});

// ---------------------------------------------------------------------------
// RelationshipStatusEnum
// ---------------------------------------------------------------------------

describe("RelationshipStatusEnum", () => {
  it("contains ATIVO, INATIVO, HISTORICO", () => {
    expect(RelationshipStatusEnum.options).toEqual(["ATIVO", "INATIVO", "HISTORICO"]);
  });
});
