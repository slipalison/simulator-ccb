// ---------------------------------------------------------------------------
// fundos-schemas.test.ts — unit tests for Zod schemas (T-2)
// Validates that schemas match backend FluentValidation rules
// ---------------------------------------------------------------------------

import { describe, it, expect } from "vitest";
import {
  createTipoAtivoSchema,
  updateTipoAtivoSchema,
  createConsultoriaFundoSchema,
  updateConsultoriaFundoSchema,
  createCustodianteSchema,
  updateCustodianteSchema,
  createCedentePfSchema,
  createCedentePjSchema,
  updateCedenteSchema,
  createFundoSchema,
  updateFundoSchema,
  createAssociationSchema,
  updateAssociationLimitsSchema,
  paginatedSearchSchema,
  fundoListSearchSchema,
  paginatedResult,
  FundoStatusEnum,
  RelationshipStatusEnum,
  TipoFundoEnum,
  TipoAtivoCategoriaEnum,
  CedenteTipoEnum,
  FUNDO_STATUS_TRANSITIONS,
  FUNDO_STATUS_LABELS,
  FUNDO_STATUS_COLORS,
  RELATIONSHIP_STATUS_TRANSITIONS,
  RELATIONSHIP_STATUS_LABELS,
  SIMPLE_STATUS_LABELS,
  TIPO_FUNDO_LABELS,
  TIPO_ATIVO_CATEGORIA_LABELS,
} from "@/lib/fundos-schemas";
import { z } from "zod";

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

// ---------------------------------------------------------------------------
// updateTipoAtivoSchema
// ---------------------------------------------------------------------------

describe("updateTipoAtivoSchema", () => {
  it("accepts valid update data", () => {
    const result = updateTipoAtivoSchema.safeParse({
      descricao: "Nova Descrição",
      status: "ATIVO",
    });
    expect(result.success).toBe(true);
  });

  it("rejects empty descricao", () => {
    const result = updateTipoAtivoSchema.safeParse({ descricao: "", status: "ATIVO" });
    expect(result.success).toBe(false);
  });

  it("rejects invalid status value", () => {
    const result = updateTipoAtivoSchema.safeParse({ descricao: "Desc", status: "INVALID" });
    expect(result.success).toBe(false);
  });
});

// ---------------------------------------------------------------------------
// updateConsultoriaFundoSchema
// ---------------------------------------------------------------------------

describe("updateConsultoriaFundoSchema", () => {
  it("accepts valid update with all fields", () => {
    const result = updateConsultoriaFundoSchema.safeParse({
      razaoSocial: "Consultoria SA",
      status: "ATIVO",
      email: "contact@example.com",
    });
    expect(result.success).toBe(true);
  });

  it("rejects invalid email format", () => {
    const result = updateConsultoriaFundoSchema.safeParse({
      razaoSocial: "Consultoria SA",
      status: "ATIVO",
      email: "not-an-email",
    });
    expect(result.success).toBe(false);
  });

  it("accepts null email", () => {
    const result = updateConsultoriaFundoSchema.safeParse({
      razaoSocial: "Consultoria SA",
      status: "ATIVO",
      email: null,
    });
    expect(result.success).toBe(true);
  });
});

// ---------------------------------------------------------------------------
// createCustodianteSchema / updateCustodianteSchema
// ---------------------------------------------------------------------------

describe("createCustodianteSchema", () => {
  const validCnpj = "11222333000181";

  it("accepts valid data", () => {
    const result = createCustodianteSchema.safeParse({
      razaoSocial: "Banco SA",
      cnpj: validCnpj,
    });
    expect(result.success).toBe(true);
  });

  it("rejects invalid CNPJ digits", () => {
    const result = createCustodianteSchema.safeParse({
      razaoSocial: "Banco SA",
      cnpj: "00000000000000",
    });
    expect(result.success).toBe(false);
  });

  it("rejects invalid email", () => {
    const result = createCustodianteSchema.safeParse({
      razaoSocial: "Banco SA",
      cnpj: validCnpj,
      email: "bad-email",
    });
    expect(result.success).toBe(false);
  });

  it("accepts valid email", () => {
    const result = createCustodianteSchema.safeParse({
      razaoSocial: "Banco SA",
      cnpj: validCnpj,
      email: "banco@example.com",
    });
    expect(result.success).toBe(true);
  });
});

describe("updateCustodianteSchema", () => {
  it("accepts valid update", () => {
    const result = updateCustodianteSchema.safeParse({
      razaoSocial: "Banco SA Atualizado",
      status: "ATIVO",
    });
    expect(result.success).toBe(true);
  });

  it("rejects empty razaoSocial", () => {
    const result = updateCustodianteSchema.safeParse({
      razaoSocial: "",
      status: "ATIVO",
    });
    expect(result.success).toBe(false);
  });
});

// ---------------------------------------------------------------------------
// updateCedenteSchema
// ---------------------------------------------------------------------------

describe("updateCedenteSchema", () => {
  it("accepts valid update", () => {
    const result = updateCedenteSchema.safeParse({
      nome: "João Atualizado",
      status: "ATIVO",
    });
    expect(result.success).toBe(true);
  });

  it("rejects empty nome", () => {
    const result = updateCedenteSchema.safeParse({ nome: "", status: "ATIVO" });
    expect(result.success).toBe(false);
  });

  it("rejects invalid email", () => {
    const result = updateCedenteSchema.safeParse({
      nome: "João",
      status: "ATIVO",
      email: "bad",
    });
    expect(result.success).toBe(false);
  });
});

// ---------------------------------------------------------------------------
// createCedentePjSchema — email refine branch
// ---------------------------------------------------------------------------

describe("createCedentePjSchema — email refinement", () => {
  const validCnpj = "11222333000181";

  it("accepts valid email", () => {
    const result = createCedentePjSchema.safeParse({
      cnpj: validCnpj,
      razaoSocial: "Empresa SA",
      email: "empresa@example.com",
    });
    expect(result.success).toBe(true);
  });

  it("rejects invalid email", () => {
    const result = createCedentePjSchema.safeParse({
      cnpj: validCnpj,
      razaoSocial: "Empresa SA",
      email: "not-email",
    });
    expect(result.success).toBe(false);
  });

  it("accepts null email", () => {
    const result = createCedentePjSchema.safeParse({
      cnpj: validCnpj,
      razaoSocial: "Empresa SA",
      email: null,
    });
    expect(result.success).toBe(true);
  });
});

// ---------------------------------------------------------------------------
// createCedentePfSchema — email refine branch
// ---------------------------------------------------------------------------

describe("createCedentePfSchema — email refinement", () => {
  const validCpf = "52998224725";

  it("rejects invalid email for PF", () => {
    const result = createCedentePfSchema.safeParse({
      cpf: validCpf,
      nome: "João",
      email: "bad-email",
    });
    expect(result.success).toBe(false);
  });

  it("accepts valid email for PF", () => {
    const result = createCedentePfSchema.safeParse({
      cpf: validCpf,
      nome: "João",
      email: "joao@example.com",
    });
    expect(result.success).toBe(true);
  });
});

// ---------------------------------------------------------------------------
// createFundoSchema / updateFundoSchema
// ---------------------------------------------------------------------------

describe("createFundoSchema", () => {
  const validCnpj = "11222333000181";
  const validUuid = "550e8400-e29b-41d4-a716-446655440000";

  it("accepts valid fund data", () => {
    const result = createFundoSchema.safeParse({
      nome: "Fundo Alpha",
      cnpj: validCnpj,
      consultoriaFundoId: validUuid,
      custodianteId: validUuid,
      tipoFundo: "Multimercado",
    });
    expect(result.success).toBe(true);
  });

  it("rejects empty nome", () => {
    const result = createFundoSchema.safeParse({
      nome: "",
      cnpj: validCnpj,
      consultoriaFundoId: validUuid,
      custodianteId: validUuid,
      tipoFundo: "Multimercado",
    });
    expect(result.success).toBe(false);
  });

  it("rejects invalid CNPJ digits for fundo", () => {
    const result = createFundoSchema.safeParse({
      nome: "Fundo",
      cnpj: "00000000000000",
      consultoriaFundoId: validUuid,
      custodianteId: validUuid,
      tipoFundo: "RendaFixa",
    });
    expect(result.success).toBe(false);
  });

  it("rejects invalid UUID for consultoriaFundoId", () => {
    const result = createFundoSchema.safeParse({
      nome: "Fundo",
      cnpj: validCnpj,
      consultoriaFundoId: "not-a-uuid",
      custodianteId: validUuid,
      tipoFundo: "RendaFixa",
    });
    expect(result.success).toBe(false);
  });

  it("rejects invalid tipoFundo value", () => {
    const result = createFundoSchema.safeParse({
      nome: "Fundo",
      cnpj: validCnpj,
      consultoriaFundoId: validUuid,
      custodianteId: validUuid,
      tipoFundo: "InvalidType",
    });
    expect(result.success).toBe(false);
  });
});

describe("updateFundoSchema", () => {
  it("accepts valid update data", () => {
    const result = updateFundoSchema.safeParse({ nome: "Fundo Atualizado" });
    expect(result.success).toBe(true);
  });

  it("rejects empty nome", () => {
    const result = updateFundoSchema.safeParse({ nome: "" });
    expect(result.success).toBe(false);
  });
});

// ---------------------------------------------------------------------------
// updateAssociationLimitsSchema
// ---------------------------------------------------------------------------

describe("updateAssociationLimitsSchema", () => {
  it("accepts empty object (both limits optional)", () => {
    const result = updateAssociationLimitsSchema.safeParse({});
    expect(result.success).toBe(true);
  });

  it("accepts limitePercentual within range", () => {
    const result = updateAssociationLimitsSchema.safeParse({ limitePercentual: 50 });
    expect(result.success).toBe(true);
  });

  it("rejects limitePercentual > 100", () => {
    const result = updateAssociationLimitsSchema.safeParse({ limitePercentual: 101 });
    expect(result.success).toBe(false);
  });

  it("accepts limiteValor 0", () => {
    const result = updateAssociationLimitsSchema.safeParse({ limiteValor: 0 });
    expect(result.success).toBe(true);
  });

  it("accepts null for both limits", () => {
    const result = updateAssociationLimitsSchema.safeParse({
      limitePercentual: null,
      limiteValor: null,
    });
    expect(result.success).toBe(true);
  });
});

// ---------------------------------------------------------------------------
// paginatedSearchSchema
// ---------------------------------------------------------------------------

describe("paginatedSearchSchema", () => {
  it("parses with defaults when empty", () => {
    const result = paginatedSearchSchema.parse({});
    expect(result.page).toBe(1);
    expect(result.search).toBe("");
    expect(result.pageSize).toBe(20);
  });

  it("coerces string page to number", () => {
    const result = paginatedSearchSchema.parse({ page: "3" });
    expect(result.page).toBe(3);
  });

  it("rejects page < 1", () => {
    const result = paginatedSearchSchema.safeParse({ page: 0 });
    expect(result.success).toBe(false);
  });
});

// ---------------------------------------------------------------------------
// fundoListSearchSchema
// ---------------------------------------------------------------------------

describe("fundoListSearchSchema", () => {
  it("accepts valid fundo list search with status", () => {
    const result = fundoListSearchSchema.safeParse({ status: "ATIVO" });
    expect(result.success).toBe(true);
    if (result.success) expect(result.data.status).toBe("ATIVO");
  });

  it("accepts without status (optional)", () => {
    const result = fundoListSearchSchema.safeParse({});
    expect(result.success).toBe(true);
  });

  it("rejects invalid status value", () => {
    const result = fundoListSearchSchema.safeParse({ status: "INVALID_STATUS" });
    expect(result.success).toBe(false);
  });
});

// ---------------------------------------------------------------------------
// paginatedResult factory
// ---------------------------------------------------------------------------

describe("paginatedResult", () => {
  it("validates paginated result structure", () => {
    const schema = paginatedResult(z.object({ id: z.string() }));
    const result = schema.safeParse({
      items: [{ id: "uuid-1" }],
      totalCount: 1,
      page: 1,
      pageSize: 20,
      totalPages: 1,
    });
    expect(result.success).toBe(true);
  });

  it("rejects invalid items", () => {
    const schema = paginatedResult(z.object({ id: z.string().uuid() }));
    const result = schema.safeParse({
      items: [{ id: "not-a-uuid" }],
      totalCount: 1,
      page: 1,
      pageSize: 20,
      totalPages: 1,
    });
    expect(result.success).toBe(false);
  });
});

// ---------------------------------------------------------------------------
// TipoFundoEnum, TipoAtivoCategoriaEnum, CedenteTipoEnum
// ---------------------------------------------------------------------------

describe("TipoFundoEnum", () => {
  it("contains all 5 fund types", () => {
    expect(TipoFundoEnum.options).toEqual([
      "RendaFixa",
      "RendaVariavel",
      "Cambial",
      "Multimercado",
      "Outros",
    ]);
  });
});

describe("TipoAtivoCategoriaEnum", () => {
  it("contains all 5 categories", () => {
    expect(TipoAtivoCategoriaEnum.options).toEqual([
      "RendaFixa",
      "RendaVariavel",
      "Derivativo",
      "Misto",
      "Outros",
    ]);
  });
});

describe("CedenteTipoEnum", () => {
  it("contains PF and PJ", () => {
    expect(CedenteTipoEnum.options).toEqual(["PF", "PJ"]);
  });
});

// ---------------------------------------------------------------------------
// Status / label maps
// ---------------------------------------------------------------------------

describe("FUNDO_STATUS_LABELS", () => {
  it("has label for each FundoStatus", () => {
    expect(FUNDO_STATUS_LABELS.RASCUNHO).toBe("Rascunho");
    expect(FUNDO_STATUS_LABELS.ATIVO).toBe("Ativo");
    expect(FUNDO_STATUS_LABELS.SUSPENSO).toBe("Suspenso");
    expect(FUNDO_STATUS_LABELS.EM_LIQUIDACAO).toBe("Em Liquidação");
    expect(FUNDO_STATUS_LABELS.ENCERRADO).toBe("Encerrado");
  });
});

describe("FUNDO_STATUS_COLORS", () => {
  it("has color class for each FundoStatus", () => {
    expect(FUNDO_STATUS_COLORS.RASCUNHO).toContain("gray");
    expect(FUNDO_STATUS_COLORS.ATIVO).toContain("green");
    expect(FUNDO_STATUS_COLORS.SUSPENSO).toContain("yellow");
    expect(FUNDO_STATUS_COLORS.EM_LIQUIDACAO).toContain("orange");
    expect(FUNDO_STATUS_COLORS.ENCERRADO).toContain("red");
  });
});

describe("RELATIONSHIP_STATUS_LABELS", () => {
  it("has label for each RelationshipStatus", () => {
    expect(RELATIONSHIP_STATUS_LABELS.ATIVO).toBe("Ativo");
    expect(RELATIONSHIP_STATUS_LABELS.INATIVO).toBe("Inativo");
    expect(RELATIONSHIP_STATUS_LABELS.HISTORICO).toBe("Histórico");
  });
});

describe("SIMPLE_STATUS_LABELS", () => {
  it("has ATIVO and INATIVO labels", () => {
    expect(SIMPLE_STATUS_LABELS.ATIVO).toBe("Ativo");
    expect(SIMPLE_STATUS_LABELS.INATIVO).toBe("Inativo");
  });
});

describe("TIPO_FUNDO_LABELS", () => {
  it("has label for each TipoFundo", () => {
    expect(TIPO_FUNDO_LABELS.RendaFixa).toBe("Renda Fixa");
    expect(TIPO_FUNDO_LABELS.Multimercado).toBe("Multimercado");
    expect(TIPO_FUNDO_LABELS.Cambial).toBe("Cambial");
  });
});

describe("TIPO_ATIVO_CATEGORIA_LABELS", () => {
  it("has label for each TipoAtivoCategoria", () => {
    expect(TIPO_ATIVO_CATEGORIA_LABELS.RendaFixa).toBe("Renda Fixa");
    expect(TIPO_ATIVO_CATEGORIA_LABELS.Derivativo).toBe("Derivativo");
    expect(TIPO_ATIVO_CATEGORIA_LABELS.Misto).toBe("Misto");
    expect(TIPO_ATIVO_CATEGORIA_LABELS.Outros).toBe("Outros");
  });
});

describe("RELATIONSHIP_STATUS_TRANSITIONS extended", () => {
  it("INATIVO → ATIVO and HISTORICO", () => {
    expect(RELATIONSHIP_STATUS_TRANSITIONS.INATIVO).toContain("ATIVO");
    expect(RELATIONSHIP_STATUS_TRANSITIONS.INATIVO).toContain("HISTORICO");
  });
});
