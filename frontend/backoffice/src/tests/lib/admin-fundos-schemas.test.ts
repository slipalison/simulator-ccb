import { describe, it, expect } from "vitest";
import {
  adminFundoSchema,
  adminCedenteSchema,
  adminConsultoriaFundoSchema,
  adminCustodianteSchema,
  adminRelFundoCedenteSchema,
  adminRelFundoTipoAtivoSchema,
  adminRelCedenteTipoAtivoSchema,
  adminListSearchSchema,
  paginatedResultSchema,
  auditLogEntrySchema,
} from "@/lib/admin-fundos-schemas";

const VALID_UUID = "123e4567-e89b-12d3-a456-426614174000";

describe("adminFundoSchema", () => {
  it("parses a valid AdminFundoDto", () => {
    const raw = {
      id: VALID_UUID,
      clienteId: VALID_UUID,
      empresaNome: "Empresa Teste",
      nome: "Fundo Teste",
      cnpj: "12345678000195",
      consultoriaFundoId: VALID_UUID,
      custodianteId: VALID_UUID,
      tipoFundo: 0,
      classeAnbima: null,
      segmento: "Renda Fixa",
      dataConstituicao: null,
      status: "ATIVO",
      createdAt: "2024-01-01T00:00:00Z",
    };
    const result = adminFundoSchema.parse(raw);
    expect(result.nome).toBe("Fundo Teste");
    expect(result.status).toBe("ATIVO");
    expect(result.classeAnbima).toBeNull();
  });

  it("rejects invalid uuid for id", () => {
    expect(() =>
      adminFundoSchema.parse({ id: "not-a-uuid", clienteId: VALID_UUID, empresaNome: "", nome: "", cnpj: "", consultoriaFundoId: VALID_UUID, custodianteId: VALID_UUID, tipoFundo: 0, classeAnbima: null, segmento: null, dataConstituicao: null, status: "ATIVO", createdAt: "" })
    ).toThrow();
  });
});

describe("adminCedenteSchema", () => {
  it("parses PF cedente", () => {
    const raw = {
      id: VALID_UUID,
      clienteId: VALID_UUID,
      empresaNome: "Empresa X",
      documento: "12345678901",
      nome: "João Silva",
      email: null,
      telefone: null,
      endereco: null,
      cedenteTipo: 0,
      status: "ATIVO",
      createdAt: "2024-01-01T00:00:00Z",
    };
    const result = adminCedenteSchema.parse(raw);
    expect(result.cedenteTipo).toBe(0);
    expect(result.email).toBeNull();
  });
});

describe("adminConsultoriaFundoSchema", () => {
  it("parses valid consultoria", () => {
    const raw = {
      id: VALID_UUID,
      clienteId: VALID_UUID,
      empresaNome: "Empresa Y",
      razaoSocial: "Consultoria Ltda",
      nomeFantasia: "ConsultLtda",
      cnpj: "12345678000195",
      email: "contato@consultoria.com",
      telefone: null,
      status: "ATIVO",
      createdAt: "2024-01-01T00:00:00Z",
    };
    const result = adminConsultoriaFundoSchema.parse(raw);
    expect(result.razaoSocial).toBe("Consultoria Ltda");
    expect(result.nomeFantasia).toBe("ConsultLtda");
  });
});

describe("adminCustodianteSchema", () => {
  it("parses valid custodiante", () => {
    const raw = {
      id: VALID_UUID,
      clienteId: VALID_UUID,
      empresaNome: "Empresa Z",
      razaoSocial: "Custodiante SA",
      codigoInterno: "CUS-001",
      cnpj: "12345678000195",
      email: null,
      telefone: null,
      status: "ATIVO",
      createdAt: "2024-01-01T00:00:00Z",
    };
    const result = adminCustodianteSchema.parse(raw);
    expect(result.codigoInterno).toBe("CUS-001");
  });
});

describe("adminRelFundoCedenteSchema", () => {
  it("parses valid fundo-cedente relation", () => {
    const raw = {
      id: VALID_UUID,
      clienteId: VALID_UUID,
      empresaNome: "Empresa W",
      fundoId: VALID_UUID,
      fundoNome: "Fundo Alpha",
      cedenteId: VALID_UUID,
      limitePercentual: 25.5,
      limiteValor: null,
      dataInicio: "2024-01-01T00:00:00Z",
      dataFim: null,
      status: "ATIVO",
      createdAt: "2024-01-01T00:00:00Z",
    };
    const result = adminRelFundoCedenteSchema.parse(raw);
    expect(result.fundoNome).toBe("Fundo Alpha");
    expect(result.limitePercentual).toBe(25.5);
    expect(result.dataFim).toBeNull();
  });
});

describe("adminRelFundoTipoAtivoSchema", () => {
  it("parses valid fundo-tipo-ativo relation", () => {
    const raw = {
      id: VALID_UUID,
      clienteId: VALID_UUID,
      empresaNome: "Empresa V",
      fundoId: VALID_UUID,
      fundoNome: "Fundo Beta",
      tipoAtivoId: VALID_UUID,
      limitePercentual: null,
      limiteValor: 100000,
      dataInicio: "2024-01-01T00:00:00Z",
      dataFim: "2025-01-01T00:00:00Z",
      status: "INATIVO",
      createdAt: "2024-01-01T00:00:00Z",
    };
    const result = adminRelFundoTipoAtivoSchema.parse(raw);
    expect(result.limiteValor).toBe(100000);
    expect(result.status).toBe("INATIVO");
  });
});

describe("adminRelCedenteTipoAtivoSchema", () => {
  it("parses valid cedente-tipo-ativo relation", () => {
    const raw = {
      id: VALID_UUID,
      clienteId: VALID_UUID,
      empresaNome: "Empresa U",
      cedenteId: VALID_UUID,
      tipoAtivoId: VALID_UUID,
      limitePercentual: 10,
      limiteValor: null,
      dataInicio: "2024-01-01T00:00:00Z",
      dataFim: null,
      status: "HISTORICO",
      createdAt: "2024-01-01T00:00:00Z",
    };
    const result = adminRelCedenteTipoAtivoSchema.parse(raw);
    expect(result.status).toBe("HISTORICO");
  });
});

describe("adminListSearchSchema", () => {
  it("defaults page to 1 when missing", () => {
    const result = adminListSearchSchema.parse({});
    expect(result.page).toBe(1);
  });

  it("defaults page to 1 when invalid", () => {
    const result = adminListSearchSchema.parse({ page: -5 });
    expect(result.page).toBe(1);
  });

  it("passes valid page, search, empresaId", () => {
    const result = adminListSearchSchema.parse({
      page: 3,
      search: "test",
      empresaId: VALID_UUID,
    });
    expect(result.page).toBe(3);
    expect(result.search).toBe("test");
    expect(result.empresaId).toBe(VALID_UUID);
  });

  it("returns undefined for invalid empresaId (not uuid)", () => {
    const result = adminListSearchSchema.parse({ empresaId: "not-a-uuid" });
    expect(result.empresaId).toBeUndefined();
  });
});

describe("paginatedResultSchema", () => {
  it("wraps item schema correctly", () => {
    const schema = paginatedResultSchema(adminFundoSchema);
    const raw = {
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 20,
    };
    const result = schema.parse(raw);
    expect(result.items).toEqual([]);
    expect(result.totalCount).toBe(0);
  });
});

describe("auditLogEntrySchema", () => {
  it("parses valid audit entry without entity fields", () => {
    const raw = {
      id: "audit-1",
      timestamp: "2024-01-01T12:00:00Z",
      adminUserId: "admin-1",
      adminUserName: "admin@test.com",
      actionType: "STATUS_CHANGE",
      targetUserId: null,
      targetUserName: null,
      details: "RASCUNHO -> ATIVO",
      ipAddress: "127.0.0.1",
    };
    const result = auditLogEntrySchema.parse(raw);
    expect(result.actionType).toBe("STATUS_CHANGE");
    expect(result.details).toBe("RASCUNHO -> ATIVO");
    // entityType and entityId are optional — should be undefined when absent
    expect(result.entityType).toBeUndefined();
    expect(result.entityId).toBeUndefined();
  });

  it("parses valid audit entry WITH entityType and entityId", () => {
    const raw = {
      id: "audit-2",
      timestamp: "2024-01-01T12:00:00Z",
      adminUserId: "admin-1",
      adminUserName: "admin@test.com",
      actionType: "STATUS_CHANGE",
      targetUserId: null,
      targetUserName: null,
      details: null,
      ipAddress: null,
      entityType: "Fundo",
      entityId: "123e4567-e89b-12d3-a456-426614174000",
    };
    const result = auditLogEntrySchema.parse(raw);
    expect(result.entityType).toBe("Fundo");
    expect(result.entityId).toBe("123e4567-e89b-12d3-a456-426614174000");
  });

  it("parses audit entry with null entityType and entityId", () => {
    const raw = {
      id: "audit-3",
      timestamp: "2024-01-01T12:00:00Z",
      adminUserId: "admin-1",
      adminUserName: "admin@test.com",
      actionType: "CREATE",
      targetUserId: null,
      targetUserName: null,
      details: null,
      ipAddress: null,
      entityType: null,
      entityId: null,
    };
    const result = auditLogEntrySchema.parse(raw);
    expect(result.entityType).toBeNull();
    expect(result.entityId).toBeNull();
  });

  it("rejects invalid uuid for entityId", () => {
    const raw = {
      id: "audit-4",
      timestamp: "2024-01-01T12:00:00Z",
      adminUserId: "admin-1",
      adminUserName: "admin@test.com",
      actionType: "CREATE",
      targetUserId: null,
      targetUserName: null,
      details: null,
      ipAddress: null,
      entityType: "Fundo",
      entityId: "not-a-uuid",
    };
    expect(() => auditLogEntrySchema.parse(raw)).toThrow();
  });
});
