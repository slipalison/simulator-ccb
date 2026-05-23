// ---------------------------------------------------------------------------
// admin-fundos-api.ts unit tests — mocks adminFetch, tests buildQuery + error paths
// ---------------------------------------------------------------------------

import { describe, it, expect, vi, beforeEach } from "vitest";
import {
  listAdminFundos,
  listAdminConsultorias,
  listAdminCustodiantes,
  listAdminCedentes,
  listAdminFundoCedentes,
  listAdminFundoTiposAtivos,
  listAdminCedenteTiposAtivos,
  getAdminFundo,
  getAdminCedente,
  getAdminConsultoriaFundo,
  getAdminCustodiante,
  getAuditHistory,
} from "@/lib/admin-fundos-api";
import { AdminApiError } from "@/lib/admin-api";

vi.mock("@/lib/admin-http-interceptor", () => ({
  adminFetch: vi.fn(),
}));

import { adminFetch } from "@/lib/admin-http-interceptor";

const mockFetch = vi.mocked(adminFetch);

const VALID_UUID = "123e4567-e89b-12d3-a456-426614174000";

function makeOkResponse(data: unknown) {
  return {
    ok: true,
    status: 200,
    json: () => Promise.resolve(data),
  } as unknown as Response;
}

function makeErrorResponse(status: number) {
  return {
    ok: false,
    status,
    json: () => Promise.resolve({}),
  } as unknown as Response;
}

const EMPTY_PAGINATED = { items: [], totalCount: 0, page: 1, pageSize: 20 };

beforeEach(() => {
  vi.clearAllMocks();
});

describe("listAdminFundos", () => {
  it("fetches /api/admin/fundos with no params", async () => {
    mockFetch.mockResolvedValue(makeOkResponse(EMPTY_PAGINATED));
    const result = await listAdminFundos();
    expect(mockFetch).toHaveBeenCalledWith("/api/admin/fundos", { method: "GET" });
    expect(result.items).toEqual([]);
  });

  it("includes query params when provided", async () => {
    mockFetch.mockResolvedValue(makeOkResponse(EMPTY_PAGINATED));
    await listAdminFundos({ page: 2, pageSize: 10, search: "alfa", companyId: VALID_UUID });
    const url = mockFetch.mock.calls[0][0] as string;
    expect(url).toContain("page=2");
    expect(url).toContain("pageSize=10");
    expect(url).toContain("search=alfa");
    expect(url).toContain(`companyId=${VALID_UUID}`);
  });

  it("throws AdminApiError on non-ok response", async () => {
    mockFetch.mockResolvedValue(makeErrorResponse(500));
    await expect(listAdminFundos()).rejects.toBeInstanceOf(AdminApiError);
  });
});

describe("listAdminConsultorias", () => {
  it("fetches /api/admin/fundos/consultorias", async () => {
    mockFetch.mockResolvedValue(makeOkResponse(EMPTY_PAGINATED));
    await listAdminConsultorias({ search: "xyz" });
    const url = mockFetch.mock.calls[0][0] as string;
    expect(url).toContain("/api/admin/fundos/consultorias");
    expect(url).toContain("search=xyz");
  });

  it("throws on error", async () => {
    mockFetch.mockResolvedValue(makeErrorResponse(503));
    await expect(listAdminConsultorias()).rejects.toBeInstanceOf(AdminApiError);
  });
});

describe("listAdminCustodiantes", () => {
  it("fetches /api/admin/fundos/custodiantes", async () => {
    mockFetch.mockResolvedValue(makeOkResponse(EMPTY_PAGINATED));
    await listAdminCustodiantes({ page: 1 });
    const url = mockFetch.mock.calls[0][0] as string;
    expect(url).toContain("/api/admin/fundos/custodiantes");
  });

  it("throws on error", async () => {
    mockFetch.mockResolvedValue(makeErrorResponse(500));
    await expect(listAdminCustodiantes()).rejects.toBeInstanceOf(AdminApiError);
  });
});

describe("listAdminCedentes", () => {
  it("fetches /api/admin/fundos/cedentes", async () => {
    mockFetch.mockResolvedValue(makeOkResponse(EMPTY_PAGINATED));
    await listAdminCedentes({ companyId: VALID_UUID });
    const url = mockFetch.mock.calls[0][0] as string;
    expect(url).toContain("/api/admin/fundos/cedentes");
    expect(url).toContain(`companyId=${VALID_UUID}`);
  });

  it("throws on error", async () => {
    mockFetch.mockResolvedValue(makeErrorResponse(500));
    await expect(listAdminCedentes()).rejects.toBeInstanceOf(AdminApiError);
  });
});

describe("listAdminFundoCedentes", () => {
  it("fetches /api/admin/fundos/fundo-cedentes", async () => {
    mockFetch.mockResolvedValue(makeOkResponse(EMPTY_PAGINATED));
    await listAdminFundoCedentes();
    const url = mockFetch.mock.calls[0][0] as string;
    expect(url).toContain("fundo-cedentes");
  });

  it("throws on error", async () => {
    mockFetch.mockResolvedValue(makeErrorResponse(500));
    await expect(listAdminFundoCedentes()).rejects.toBeInstanceOf(AdminApiError);
  });
});

describe("listAdminFundoTiposAtivos", () => {
  it("fetches /api/admin/fundos/fundo-tipos-ativos", async () => {
    mockFetch.mockResolvedValue(makeOkResponse(EMPTY_PAGINATED));
    await listAdminFundoTiposAtivos({ pageSize: 5 });
    const url = mockFetch.mock.calls[0][0] as string;
    expect(url).toContain("fundo-tipos-ativos");
    expect(url).toContain("pageSize=5");
  });
});

describe("listAdminCedenteTiposAtivos", () => {
  it("fetches /api/admin/fundos/cedente-tipos-ativos", async () => {
    mockFetch.mockResolvedValue(makeOkResponse(EMPTY_PAGINATED));
    await listAdminCedenteTiposAtivos();
    const url = mockFetch.mock.calls[0][0] as string;
    expect(url).toContain("cedente-tipos-ativos");
  });
});

describe("getAdminFundo", () => {
  const FUNDO = {
    id: VALID_UUID, clienteId: VALID_UUID, empresaNome: "Emp", nome: "Fundo",
    cnpj: "12345678000195", consultoriaFundoId: VALID_UUID, custodianteId: VALID_UUID,
    tipoFundo: 0, classeAnbima: null, segmento: null, dataConstituicao: null,
    status: "ATIVO", createdAt: "2024-01-01T00:00:00Z",
  };

  it("returns fundo on 200", async () => {
    mockFetch.mockResolvedValue(makeOkResponse(FUNDO));
    const result = await getAdminFundo(VALID_UUID);
    expect(result.nome).toBe("Fundo");
    expect(mockFetch).toHaveBeenCalledWith(`/api/admin/fundos/${VALID_UUID}`, { method: "GET" });
  });

  it("throws 404 AdminApiError on not found", async () => {
    mockFetch.mockResolvedValue(makeErrorResponse(404));
    const err = await getAdminFundo(VALID_UUID).catch((e) => e);
    expect(err).toBeInstanceOf(AdminApiError);
    expect((err as AdminApiError).status).toBe(404);
  });

  it("throws AdminApiError on 500", async () => {
    mockFetch.mockResolvedValue(makeErrorResponse(500));
    await expect(getAdminFundo(VALID_UUID)).rejects.toBeInstanceOf(AdminApiError);
  });
});

describe("getAdminCedente", () => {
  it("throws 404 on not found", async () => {
    mockFetch.mockResolvedValue(makeErrorResponse(404));
    const err = await getAdminCedente(VALID_UUID).catch((e) => e);
    expect((err as AdminApiError).status).toBe(404);
  });

  it("throws on non-404 error", async () => {
    mockFetch.mockResolvedValue(makeErrorResponse(500));
    await expect(getAdminCedente(VALID_UUID)).rejects.toBeInstanceOf(AdminApiError);
  });
});

describe("getAdminConsultoriaFundo", () => {
  it("throws 404 on not found", async () => {
    mockFetch.mockResolvedValue(makeErrorResponse(404));
    const err = await getAdminConsultoriaFundo(VALID_UUID).catch((e) => e);
    expect((err as AdminApiError).status).toBe(404);
  });

  it("throws on non-404 error", async () => {
    mockFetch.mockResolvedValue(makeErrorResponse(503));
    await expect(getAdminConsultoriaFundo(VALID_UUID)).rejects.toBeInstanceOf(AdminApiError);
  });
});

describe("getAdminCustodiante", () => {
  it("throws 404 on not found", async () => {
    mockFetch.mockResolvedValue(makeErrorResponse(404));
    const err = await getAdminCustodiante(VALID_UUID).catch((e) => e);
    expect((err as AdminApiError).status).toBe(404);
  });

  it("throws on non-404 error", async () => {
    mockFetch.mockResolvedValue(makeErrorResponse(500));
    await expect(getAdminCustodiante(VALID_UUID)).rejects.toBeInstanceOf(AdminApiError);
  });
});

describe("getAuditHistory", () => {
  it("fetches /api/admin/audit-log with entityType and entityId", async () => {
    mockFetch.mockResolvedValue(makeOkResponse(EMPTY_PAGINATED));
    await getAuditHistory({ entityType: "Fundo", entityId: VALID_UUID, page: 1, pageSize: 10 });
    const url = mockFetch.mock.calls[0][0] as string;
    expect(url).toContain("/api/admin/audit-log");
    expect(url).toContain("entityType=Fundo");
    expect(url).toContain(`entityId=${VALID_UUID}`);
    expect(url).toContain("pageSize=10");
  });

  it("defaults pageSize to 10 when not provided", async () => {
    mockFetch.mockResolvedValue(makeOkResponse(EMPTY_PAGINATED));
    await getAuditHistory({ entityType: "Cedente", entityId: VALID_UUID });
    const url = mockFetch.mock.calls[0][0] as string;
    expect(url).toContain("pageSize=10");
  });

  it("throws on error", async () => {
    mockFetch.mockResolvedValue(makeErrorResponse(500));
    await expect(
      getAuditHistory({ entityType: "Fundo", entityId: VALID_UUID })
    ).rejects.toBeInstanceOf(AdminApiError);
  });
});
