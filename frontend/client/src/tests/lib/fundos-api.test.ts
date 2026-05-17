// ---------------------------------------------------------------------------
// fundos-api.test.ts — unit tests for all typed API functions (D-2)
// Mocks apiFetch from api.ts; asserts URL/method/body + response parsing.
// ---------------------------------------------------------------------------

import { describe, it, expect, vi, beforeEach } from "vitest";

// Mock apiFetch from api.ts before importing fundos-api
vi.mock("@/lib/api", () => ({
  apiFetch: vi.fn(),
}));

import { apiFetch } from "@/lib/api";
import {
  listTiposAtivo,
  getTipoAtivo,
  createTipoAtivo,
  updateTipoAtivo,
  listConsultoriasFundo,
  getConsultoriaFundo,
  createConsultoriaFundo,
  updateConsultoriaFundo,
  listCustodiantes,
  getCustodiante,
  createCustodiante,
  updateCustodiante,
  listCedentes,
  getCedente,
  createCedentePf,
  createCedentePj,
  updateCedente,
  listFundos,
  getFundo,
  createFundo,
  updateFundo,
  transitionFundoStatus,
  getFundoAllowedTransitions,
  listFundoCedentes,
  getFundoCedente,
  createFundoCedente,
  updateFundoCedenteLimits,
  transitionFundoCedenteStatus,
  getFundoCedenteAllowedTransitions,
  listFundoTiposAtivo,
  createFundoTipoAtivo,
  updateFundoTipoAtivoLimits,
  transitionFundoTipoAtivoStatus,
  getFundoTipoAtivoAllowedTransitions,
  listCedenteTiposAtivo,
  createCedenteTipoAtivo,
  updateCedenteTipoAtivoLimits,
  transitionCedenteTipoAtivoStatus,
  getCedenteTipoAtivoAllowedTransitions,
} from "@/lib/fundos-api";

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

function mockOkJson(body: unknown): Response {
  return {
    ok: true,
    status: 200,
    json: () => Promise.resolve(body),
  } as unknown as Response;
}

function mockNotOk(status: number): Response {
  return {
    ok: false,
    status,
    json: () => Promise.resolve({ title: "Error" }),
  } as unknown as Response;
}

const mockApiFetch = vi.mocked(apiFetch);

beforeEach(() => {
  vi.clearAllMocks();
});

// ---------------------------------------------------------------------------
// TipoAtivo
// ---------------------------------------------------------------------------

describe("listTiposAtivo", () => {
  it("calls apiFetch with correct URL and returns parsed json", async () => {
    const expected = { items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0 };
    mockApiFetch.mockResolvedValueOnce(mockOkJson(expected));

    const result = await listTiposAtivo({ page: 1, pageSize: 20, search: "" });

    expect(mockApiFetch).toHaveBeenCalledWith(
      expect.stringContaining("/api/fundos/tipos-ativo?")
    );
    expect(result).toEqual(expected);
  });

  it("includes page and pageSize in query string", async () => {
    mockApiFetch.mockResolvedValueOnce(mockOkJson({ items: [], totalCount: 0, page: 2, pageSize: 10, totalPages: 0 }));

    await listTiposAtivo({ page: 2, pageSize: 10, search: "" });

    const url = mockApiFetch.mock.calls[0][0] as string;
    expect(url).toContain("page=2");
    expect(url).toContain("pageSize=10");
  });

  it("includes search param when provided", async () => {
    mockApiFetch.mockResolvedValueOnce(mockOkJson({ items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0 }));

    await listTiposAtivo({ page: 1, pageSize: 20, search: "renda" });

    expect(mockApiFetch.mock.calls[0][0]).toContain("search=renda");
  });

  it("throws response on non-ok status", async () => {
    const errRes = mockNotOk(500);
    mockApiFetch.mockResolvedValueOnce(errRes);

    await expect(listTiposAtivo({ page: 1, pageSize: 20, search: "" })).rejects.toBe(errRes);
  });
});

describe("getTipoAtivo", () => {
  it("calls correct endpoint and returns dto", async () => {
    const dto = { id: "uuid-1", codigo: "RF001", descricao: "desc", categoria: "RendaFixa", subcategoria: null, status: "ATIVO", ordemExibicao: 0 };
    mockApiFetch.mockResolvedValueOnce(mockOkJson(dto));

    const result = await getTipoAtivo("uuid-1");

    expect(mockApiFetch).toHaveBeenCalledWith("/api/fundos/tipos-ativo/uuid-1");
    expect(result).toEqual(dto);
  });

  it("throws on 404", async () => {
    const res404 = mockNotOk(404);
    mockApiFetch.mockResolvedValueOnce(res404);
    await expect(getTipoAtivo("bad-id")).rejects.toBe(res404);
  });
});

describe("createTipoAtivo", () => {
  it("sends POST with body and returns dto", async () => {
    const dto = { id: "uuid-1", codigo: "RF001", descricao: "Renda Fixa", categoria: "RendaFixa", subcategoria: null, status: "ATIVO", ordemExibicao: 0 };
    mockApiFetch.mockResolvedValueOnce(mockOkJson(dto));

    const result = await createTipoAtivo({ codigo: "RF001", descricao: "Renda Fixa", categoria: "RendaFixa", ordemExibicao: 0 });

    expect(mockApiFetch).toHaveBeenCalledWith("/api/fundos/tipos-ativo", expect.objectContaining({ method: "POST" }));
    expect(result).toEqual(dto);
  });

  it("throws on 409 (duplicate)", async () => {
    const res409 = mockNotOk(409);
    mockApiFetch.mockResolvedValueOnce(res409);
    await expect(createTipoAtivo({ codigo: "X", descricao: "dup", categoria: "Outros", ordemExibicao: 0 })).rejects.toBe(res409);
  });
});

describe("updateTipoAtivo", () => {
  it("sends PUT with id in URL", async () => {
    const dto = { id: "uuid-1", codigo: "RF001", descricao: "updated", categoria: "RendaFixa", subcategoria: null, status: "ATIVO", ordemExibicao: 1 };
    mockApiFetch.mockResolvedValueOnce(mockOkJson(dto));

    await updateTipoAtivo("uuid-1", { descricao: "updated", status: "ATIVO", ordemExibicao: 1 });

    expect(mockApiFetch).toHaveBeenCalledWith("/api/fundos/tipos-ativo/uuid-1", expect.objectContaining({ method: "PUT" }));
  });
});

// ---------------------------------------------------------------------------
// ConsultoriaFundo
// ---------------------------------------------------------------------------

describe("listConsultoriasFundo", () => {
  it("calls correct base URL", async () => {
    mockApiFetch.mockResolvedValueOnce(mockOkJson({ items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0 }));

    await listConsultoriasFundo({ page: 1, pageSize: 20, search: "" });

    expect(mockApiFetch.mock.calls[0][0]).toContain("/api/fundos/consultorias?");
  });
});

describe("getConsultoriaFundo", () => {
  it("calls endpoint with id", async () => {
    mockApiFetch.mockResolvedValueOnce(mockOkJson({ id: "uuid-cons-1" }));
    await getConsultoriaFundo("uuid-cons-1");
    expect(mockApiFetch).toHaveBeenCalledWith("/api/fundos/consultorias/uuid-cons-1");
  });
});

describe("createConsultoriaFundo", () => {
  it("sends POST to /api/fundos/consultorias", async () => {
    mockApiFetch.mockResolvedValueOnce(mockOkJson({ id: "uuid-cons-1" }));
    await createConsultoriaFundo({ razaoSocial: "Consultoria SA", cnpj: "11222333000181" });
    expect(mockApiFetch).toHaveBeenCalledWith(
      "/api/fundos/consultorias",
      expect.objectContaining({ method: "POST" })
    );
  });
});

describe("updateConsultoriaFundo", () => {
  it("sends PUT with id", async () => {
    mockApiFetch.mockResolvedValueOnce(mockOkJson({ id: "uuid-cons-1" }));
    await updateConsultoriaFundo("uuid-cons-1", { razaoSocial: "Updated SA", status: "ATIVO" });
    expect(mockApiFetch).toHaveBeenCalledWith(
      "/api/fundos/consultorias/uuid-cons-1",
      expect.objectContaining({ method: "PUT" })
    );
  });
});

// ---------------------------------------------------------------------------
// Custodiante
// ---------------------------------------------------------------------------

describe("listCustodiantes", () => {
  it("calls correct base URL", async () => {
    mockApiFetch.mockResolvedValueOnce(mockOkJson({ items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0 }));
    await listCustodiantes({ page: 1, pageSize: 20, search: "" });
    expect(mockApiFetch.mock.calls[0][0]).toContain("/api/fundos/custodiantes?");
  });
});

describe("getCustodiante", () => {
  it("calls endpoint with id", async () => {
    mockApiFetch.mockResolvedValueOnce(mockOkJson({ id: "uuid-cust-1" }));
    await getCustodiante("uuid-cust-1");
    expect(mockApiFetch).toHaveBeenCalledWith("/api/fundos/custodiantes/uuid-cust-1");
  });
});

describe("createCustodiante", () => {
  it("sends POST and throws on 401 retry exhausted (non-ok)", async () => {
    const res401 = mockNotOk(401);
    mockApiFetch.mockResolvedValueOnce(res401);
    await expect(createCustodiante({ razaoSocial: "Cust SA", cnpj: "11222333000181" })).rejects.toBe(res401);
  });

  it("sends POST on success path", async () => {
    mockApiFetch.mockResolvedValueOnce(mockOkJson({ id: "uuid-cust-1" }));
    await createCustodiante({ razaoSocial: "Cust SA", cnpj: "11222333000181" });
    expect(mockApiFetch).toHaveBeenCalledWith(
      "/api/fundos/custodiantes",
      expect.objectContaining({ method: "POST" })
    );
  });
});

describe("updateCustodiante", () => {
  it("sends PUT with id in URL", async () => {
    mockApiFetch.mockResolvedValueOnce(mockOkJson({ id: "uuid-cust-1" }));
    await updateCustodiante("uuid-cust-1", { razaoSocial: "Updated Cust", status: "ATIVO" });
    expect(mockApiFetch).toHaveBeenCalledWith(
      "/api/fundos/custodiantes/uuid-cust-1",
      expect.objectContaining({ method: "PUT" })
    );
  });
});

// ---------------------------------------------------------------------------
// Cedente
// ---------------------------------------------------------------------------

describe("listCedentes", () => {
  it("calls correct URL", async () => {
    mockApiFetch.mockResolvedValueOnce(mockOkJson({ items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0 }));
    await listCedentes({ page: 1, pageSize: 20, search: "" });
    expect(mockApiFetch.mock.calls[0][0]).toContain("/api/fundos/cedentes?");
  });
});

describe("getCedente", () => {
  it("calls endpoint with id", async () => {
    mockApiFetch.mockResolvedValueOnce(mockOkJson({ id: "uuid-ced-1" }));
    await getCedente("uuid-ced-1");
    expect(mockApiFetch).toHaveBeenCalledWith("/api/fundos/cedentes/uuid-ced-1");
  });
});

describe("createCedentePf", () => {
  it("sends POST to /cedentes/pf", async () => {
    mockApiFetch.mockResolvedValueOnce(mockOkJson({ id: "uuid-ced-1" }));
    await createCedentePf({ cpf: "11144477735", nome: "João Silva" });
    expect(mockApiFetch).toHaveBeenCalledWith(
      "/api/fundos/cedentes/pf",
      expect.objectContaining({ method: "POST" })
    );
  });

  it("throws on 409 duplicate CPF", async () => {
    const res409 = mockNotOk(409);
    mockApiFetch.mockResolvedValueOnce(res409);
    await expect(createCedentePf({ cpf: "11144477735", nome: "João" })).rejects.toBe(res409);
  });
});

describe("createCedentePj", () => {
  it("sends POST to /cedentes/pj", async () => {
    mockApiFetch.mockResolvedValueOnce(mockOkJson({ id: "uuid-ced-2" }));
    await createCedentePj({ cnpj: "11222333000181", razaoSocial: "Empresa SA" });
    expect(mockApiFetch).toHaveBeenCalledWith(
      "/api/fundos/cedentes/pj",
      expect.objectContaining({ method: "POST" })
    );
  });
});

describe("updateCedente", () => {
  it("sends PUT with id", async () => {
    mockApiFetch.mockResolvedValueOnce(mockOkJson({ id: "uuid-ced-1" }));
    await updateCedente("uuid-ced-1", { nome: "Updated", status: "ATIVO" });
    expect(mockApiFetch).toHaveBeenCalledWith(
      "/api/fundos/cedentes/uuid-ced-1",
      expect.objectContaining({ method: "PUT" })
    );
  });
});

// ---------------------------------------------------------------------------
// Fundo
// ---------------------------------------------------------------------------

describe("listFundos", () => {
  it("calls correct URL with pagination", async () => {
    mockApiFetch.mockResolvedValueOnce(mockOkJson({ items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0 }));
    await listFundos({ page: 1, pageSize: 20, search: "" });
    expect(mockApiFetch.mock.calls[0][0]).toContain("/api/fundos?");
  });

  it("includes status param when provided", async () => {
    mockApiFetch.mockResolvedValueOnce(mockOkJson({ items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0 }));
    await listFundos({ page: 1, pageSize: 20, search: "", status: "ATIVO" });
    expect(mockApiFetch.mock.calls[0][0]).toContain("status=ATIVO");
  });
});

describe("getFundo", () => {
  it("calls /api/fundos/{id}", async () => {
    mockApiFetch.mockResolvedValueOnce(mockOkJson({ id: "uuid-fundo-1" }));
    await getFundo("uuid-fundo-1");
    expect(mockApiFetch).toHaveBeenCalledWith("/api/fundos/uuid-fundo-1");
  });
});

describe("createFundo", () => {
  it("sends POST to /api/fundos", async () => {
    mockApiFetch.mockResolvedValueOnce(mockOkJson({ id: "uuid-fundo-1" }));
    await createFundo({
      nome: "Fundo Alpha",
      cnpj: "11222333000181",
      consultoriaFundoId: "uuid-cons-1",
      custodianteId: "uuid-cust-1",
      tipoFundo: "RendaFixa",
    });
    expect(mockApiFetch).toHaveBeenCalledWith(
      "/api/fundos",
      expect.objectContaining({ method: "POST" })
    );
  });

  it("throws on 409 duplicate CNPJ", async () => {
    const res409 = mockNotOk(409);
    mockApiFetch.mockResolvedValueOnce(res409);
    await expect(createFundo({
      nome: "F",
      cnpj: "11222333000181",
      consultoriaFundoId: "uuid-cons-1",
      custodianteId: "uuid-cust-1",
      tipoFundo: "RendaFixa",
    })).rejects.toBe(res409);
  });
});

describe("updateFundo", () => {
  it("sends PUT with id in URL", async () => {
    mockApiFetch.mockResolvedValueOnce(mockOkJson({ id: "uuid-fundo-1" }));
    await updateFundo("uuid-fundo-1", { nome: "Fundo Beta" });
    expect(mockApiFetch).toHaveBeenCalledWith(
      "/api/fundos/uuid-fundo-1",
      expect.objectContaining({ method: "PUT" })
    );
  });
});

describe("transitionFundoStatus", () => {
  it("sends POST to /{id}/status with newStatus body", async () => {
    mockApiFetch.mockResolvedValueOnce(mockOkJson({ id: "uuid-fundo-1", status: "ATIVO" }));
    await transitionFundoStatus("uuid-fundo-1", "ATIVO");

    const [url, init] = mockApiFetch.mock.calls[0] as [string, RequestInit];
    expect(url).toBe("/api/fundos/uuid-fundo-1/status");
    expect(init.method).toBe("POST");
    expect(JSON.parse(init.body as string)).toEqual({ newStatus: "ATIVO" });
  });

  it("throws on 400 invalid transition", async () => {
    const res400 = mockNotOk(400);
    mockApiFetch.mockResolvedValueOnce(res400);
    await expect(transitionFundoStatus("uuid-fundo-1", "ENCERRADO")).rejects.toBe(res400);
  });
});

describe("getFundoAllowedTransitions", () => {
  it("calls GET /{id}/allowed-transitions and returns array", async () => {
    mockApiFetch.mockResolvedValueOnce(mockOkJson(["ATIVO"]));
    const result = await getFundoAllowedTransitions("uuid-fundo-1");
    expect(mockApiFetch).toHaveBeenCalledWith("/api/fundos/uuid-fundo-1/allowed-transitions");
    expect(result).toEqual(["ATIVO"]);
  });
});

// ---------------------------------------------------------------------------
// FundoCedente association
// ---------------------------------------------------------------------------

describe("listFundoCedentes", () => {
  it("calls /api/fundos/{fundoId}/cedentes with pagination", async () => {
    mockApiFetch.mockResolvedValueOnce(mockOkJson({ items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0 }));
    await listFundoCedentes("fundo-1", { page: 1, pageSize: 20, search: "" });
    expect(mockApiFetch.mock.calls[0][0]).toContain("/api/fundos/fundo-1/cedentes?");
  });
});

describe("getFundoCedente", () => {
  it("calls correct endpoint", async () => {
    mockApiFetch.mockResolvedValueOnce(mockOkJson({ id: "assoc-1" }));
    await getFundoCedente("fundo-1", "assoc-1");
    expect(mockApiFetch).toHaveBeenCalledWith("/api/fundos/fundo-1/cedentes/assoc-1");
  });
});

describe("createFundoCedente", () => {
  it("sends POST with association payload", async () => {
    mockApiFetch.mockResolvedValueOnce(mockOkJson({ id: "assoc-1" }));
    await createFundoCedente("fundo-1", { cedenteId: "ced-1", dataInicio: "2024-01-01" });
    expect(mockApiFetch).toHaveBeenCalledWith(
      "/api/fundos/fundo-1/cedentes",
      expect.objectContaining({ method: "POST" })
    );
  });
});

describe("updateFundoCedenteLimits", () => {
  it("sends PATCH to /limits", async () => {
    mockApiFetch.mockResolvedValueOnce(mockOkJson({ id: "assoc-1" }));
    await updateFundoCedenteLimits("fundo-1", "assoc-1", { limitePercentual: 10 });
    expect(mockApiFetch).toHaveBeenCalledWith(
      "/api/fundos/fundo-1/cedentes/assoc-1/limits",
      expect.objectContaining({ method: "PATCH" })
    );
  });
});

describe("transitionFundoCedenteStatus", () => {
  it("sends POST to /status", async () => {
    mockApiFetch.mockResolvedValueOnce(mockOkJson({ id: "assoc-1", status: "INATIVO" }));
    await transitionFundoCedenteStatus("fundo-1", "assoc-1", "INATIVO");
    const [url, init] = mockApiFetch.mock.calls[0] as [string, RequestInit];
    expect(url).toBe("/api/fundos/fundo-1/cedentes/assoc-1/status");
    expect(JSON.parse(init.body as string)).toEqual({ newStatus: "INATIVO" });
  });
});

describe("getFundoCedenteAllowedTransitions", () => {
  it("calls correct endpoint", async () => {
    mockApiFetch.mockResolvedValueOnce(mockOkJson(["INATIVO", "HISTORICO"]));
    const result = await getFundoCedenteAllowedTransitions("fundo-1", "assoc-1");
    expect(mockApiFetch).toHaveBeenCalledWith(
      "/api/fundos/fundo-1/cedentes/assoc-1/allowed-transitions"
    );
    expect(result).toEqual(["INATIVO", "HISTORICO"]);
  });
});

// ---------------------------------------------------------------------------
// FundoTipoAtivo association
// ---------------------------------------------------------------------------

describe("listFundoTiposAtivo", () => {
  it("calls /api/fundos/{fundoId}/tipos-ativos", async () => {
    mockApiFetch.mockResolvedValueOnce(mockOkJson({ items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0 }));
    await listFundoTiposAtivo("fundo-1", { page: 1, pageSize: 20, search: "" });
    expect(mockApiFetch.mock.calls[0][0]).toContain("/api/fundos/fundo-1/tipos-ativos?");
  });
});

describe("createFundoTipoAtivo", () => {
  it("sends POST with tipoAtivoId", async () => {
    mockApiFetch.mockResolvedValueOnce(mockOkJson({ id: "assoc-2" }));
    await createFundoTipoAtivo("fundo-1", { tipoAtivoId: "tipo-1", dataInicio: "2024-01-01" });
    expect(mockApiFetch).toHaveBeenCalledWith(
      "/api/fundos/fundo-1/tipos-ativos",
      expect.objectContaining({ method: "POST" })
    );
  });
});

describe("updateFundoTipoAtivoLimits", () => {
  it("sends PATCH to /limits", async () => {
    mockApiFetch.mockResolvedValueOnce(mockOkJson({ id: "assoc-2" }));
    await updateFundoTipoAtivoLimits("fundo-1", "assoc-2", { limiteValor: 500000 });
    expect(mockApiFetch).toHaveBeenCalledWith(
      "/api/fundos/fundo-1/tipos-ativos/assoc-2/limits",
      expect.objectContaining({ method: "PATCH" })
    );
  });
});

describe("transitionFundoTipoAtivoStatus", () => {
  it("sends POST to /status with body", async () => {
    mockApiFetch.mockResolvedValueOnce(mockOkJson({ id: "assoc-2", status: "HISTORICO" }));
    await transitionFundoTipoAtivoStatus("fundo-1", "assoc-2", "HISTORICO");
    const [url, init] = mockApiFetch.mock.calls[0] as [string, RequestInit];
    expect(url).toBe("/api/fundos/fundo-1/tipos-ativos/assoc-2/status");
    expect(JSON.parse(init.body as string)).toEqual({ newStatus: "HISTORICO" });
  });
});

describe("getFundoTipoAtivoAllowedTransitions", () => {
  it("returns allowed transitions array", async () => {
    mockApiFetch.mockResolvedValueOnce(mockOkJson(["INATIVO"]));
    const result = await getFundoTipoAtivoAllowedTransitions("fundo-1", "assoc-2");
    expect(mockApiFetch).toHaveBeenCalledWith(
      "/api/fundos/fundo-1/tipos-ativos/assoc-2/allowed-transitions"
    );
    expect(result).toEqual(["INATIVO"]);
  });
});

// ---------------------------------------------------------------------------
// CedenteTipoAtivo association
// ---------------------------------------------------------------------------

describe("listCedenteTiposAtivo", () => {
  it("calls /api/cedentes/{cedenteId}/tipos-ativos", async () => {
    mockApiFetch.mockResolvedValueOnce(mockOkJson({ items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0 }));
    await listCedenteTiposAtivo("ced-1", { page: 1, pageSize: 20, search: "" });
    expect(mockApiFetch.mock.calls[0][0]).toContain("/api/cedentes/ced-1/tipos-ativos?");
  });
});

describe("createCedenteTipoAtivo", () => {
  it("sends POST with tipoAtivoId", async () => {
    mockApiFetch.mockResolvedValueOnce(mockOkJson({ id: "assoc-3" }));
    await createCedenteTipoAtivo("ced-1", { tipoAtivoId: "tipo-1", dataInicio: "2024-01-01" });
    expect(mockApiFetch).toHaveBeenCalledWith(
      "/api/cedentes/ced-1/tipos-ativos",
      expect.objectContaining({ method: "POST" })
    );
  });
});

describe("updateCedenteTipoAtivoLimits", () => {
  it("sends PATCH to /limits", async () => {
    mockApiFetch.mockResolvedValueOnce(mockOkJson({ id: "assoc-3" }));
    await updateCedenteTipoAtivoLimits("ced-1", "assoc-3", { limitePercentual: 5 });
    expect(mockApiFetch).toHaveBeenCalledWith(
      "/api/cedentes/ced-1/tipos-ativos/assoc-3/limits",
      expect.objectContaining({ method: "PATCH" })
    );
  });
});

describe("transitionCedenteTipoAtivoStatus", () => {
  it("sends POST to /status", async () => {
    mockApiFetch.mockResolvedValueOnce(mockOkJson({ id: "assoc-3", status: "INATIVO" }));
    await transitionCedenteTipoAtivoStatus("ced-1", "assoc-3", "INATIVO");
    const [url, init] = mockApiFetch.mock.calls[0] as [string, RequestInit];
    expect(url).toBe("/api/cedentes/ced-1/tipos-ativos/assoc-3/status");
    expect(JSON.parse(init.body as string)).toEqual({ newStatus: "INATIVO" });
  });
});

describe("getCedenteTipoAtivoAllowedTransitions", () => {
  it("returns allowed transitions", async () => {
    mockApiFetch.mockResolvedValueOnce(mockOkJson(["ATIVO", "HISTORICO"]));
    const result = await getCedenteTipoAtivoAllowedTransitions("ced-1", "assoc-3");
    expect(mockApiFetch).toHaveBeenCalledWith(
      "/api/cedentes/ced-1/tipos-ativos/assoc-3/allowed-transitions"
    );
    expect(result).toEqual(["ATIVO", "HISTORICO"]);
  });
});

// ---------------------------------------------------------------------------
// Error branch coverage — non-ok responses throw for all mutation paths
// ---------------------------------------------------------------------------

describe("Error branches — 401 unauthorized responses", () => {
  it("listConsultoriasFundo throws on 401", async () => {
    const res401 = mockNotOk(401);
    mockApiFetch.mockResolvedValueOnce(res401);
    await expect(listConsultoriasFundo({ page: 1, pageSize: 20, search: "" })).rejects.toBe(res401);
  });

  it("getConsultoriaFundo throws on 404", async () => {
    const res404 = mockNotOk(404);
    mockApiFetch.mockResolvedValueOnce(res404);
    await expect(getConsultoriaFundo("bad-id")).rejects.toBe(res404);
  });

  it("updateConsultoriaFundo throws on 400", async () => {
    const res400 = mockNotOk(400);
    mockApiFetch.mockResolvedValueOnce(res400);
    await expect(updateConsultoriaFundo("id", { razaoSocial: "X", status: "ATIVO" })).rejects.toBe(res400);
  });

  it("listCustodiantes throws on 500", async () => {
    const res500 = mockNotOk(500);
    mockApiFetch.mockResolvedValueOnce(res500);
    await expect(listCustodiantes({ page: 1, pageSize: 20, search: "" })).rejects.toBe(res500);
  });

  it("getCustodiante throws on 404", async () => {
    const res404 = mockNotOk(404);
    mockApiFetch.mockResolvedValueOnce(res404);
    await expect(getCustodiante("bad-id")).rejects.toBe(res404);
  });

  it("updateCustodiante throws on 409", async () => {
    const res409 = mockNotOk(409);
    mockApiFetch.mockResolvedValueOnce(res409);
    await expect(updateCustodiante("id", { razaoSocial: "X", status: "ATIVO" })).rejects.toBe(res409);
  });

  it("listCedentes throws on 401", async () => {
    const res401 = mockNotOk(401);
    mockApiFetch.mockResolvedValueOnce(res401);
    await expect(listCedentes({ page: 1, pageSize: 20, search: "" })).rejects.toBe(res401);
  });

  it("getCedente throws on 404", async () => {
    const res404 = mockNotOk(404);
    mockApiFetch.mockResolvedValueOnce(res404);
    await expect(getCedente("bad-id")).rejects.toBe(res404);
  });

  it("updateCedente throws on 404", async () => {
    const res404 = mockNotOk(404);
    mockApiFetch.mockResolvedValueOnce(res404);
    await expect(updateCedente("id", { nome: "X", status: "ATIVO" })).rejects.toBe(res404);
  });

  it("getFundo throws on 404", async () => {
    const res404 = mockNotOk(404);
    mockApiFetch.mockResolvedValueOnce(res404);
    await expect(getFundo("bad-id")).rejects.toBe(res404);
  });

  it("updateFundo throws on 400 FluentValidation", async () => {
    const res400 = mockNotOk(400);
    mockApiFetch.mockResolvedValueOnce(res400);
    await expect(updateFundo("id", { nome: "F" })).rejects.toBe(res400);
  });

  it("getFundoAllowedTransitions throws on 404", async () => {
    const res404 = mockNotOk(404);
    mockApiFetch.mockResolvedValueOnce(res404);
    await expect(getFundoAllowedTransitions("bad-id")).rejects.toBe(res404);
  });

  it("listFundoCedentes throws on 404", async () => {
    const res404 = mockNotOk(404);
    mockApiFetch.mockResolvedValueOnce(res404);
    await expect(listFundoCedentes("fundo-bad", { page: 1, pageSize: 20, search: "" })).rejects.toBe(res404);
  });

  it("getFundoCedente throws on 404", async () => {
    const res404 = mockNotOk(404);
    mockApiFetch.mockResolvedValueOnce(res404);
    await expect(getFundoCedente("fundo-bad", "assoc-bad")).rejects.toBe(res404);
  });

  it("createFundoCedente throws on 409", async () => {
    const res409 = mockNotOk(409);
    mockApiFetch.mockResolvedValueOnce(res409);
    await expect(createFundoCedente("fundo-1", { cedenteId: "ced-1", dataInicio: "2024-01-01" })).rejects.toBe(res409);
  });

  it("updateFundoCedenteLimits throws on 400", async () => {
    const res400 = mockNotOk(400);
    mockApiFetch.mockResolvedValueOnce(res400);
    await expect(updateFundoCedenteLimits("fundo-1", "assoc-1", { limitePercentual: 200 })).rejects.toBe(res400);
  });

  it("getFundoCedenteAllowedTransitions throws on 404", async () => {
    const res404 = mockNotOk(404);
    mockApiFetch.mockResolvedValueOnce(res404);
    await expect(getFundoCedenteAllowedTransitions("fundo-bad", "assoc-bad")).rejects.toBe(res404);
  });

  it("listFundoTiposAtivo throws on 401", async () => {
    const res401 = mockNotOk(401);
    mockApiFetch.mockResolvedValueOnce(res401);
    await expect(listFundoTiposAtivo("fundo-1", { page: 1, pageSize: 20, search: "" })).rejects.toBe(res401);
  });

  it("createFundoTipoAtivo throws on 409", async () => {
    const res409 = mockNotOk(409);
    mockApiFetch.mockResolvedValueOnce(res409);
    await expect(createFundoTipoAtivo("fundo-1", { tipoAtivoId: "tipo-1", dataInicio: "2024-01-01" })).rejects.toBe(res409);
  });

  it("updateFundoTipoAtivoLimits throws on 400", async () => {
    const res400 = mockNotOk(400);
    mockApiFetch.mockResolvedValueOnce(res400);
    await expect(updateFundoTipoAtivoLimits("fundo-1", "assoc-2", { limitePercentual: 200 })).rejects.toBe(res400);
  });

  it("transitionFundoTipoAtivoStatus throws on 400", async () => {
    const res400 = mockNotOk(400);
    mockApiFetch.mockResolvedValueOnce(res400);
    await expect(transitionFundoTipoAtivoStatus("fundo-1", "assoc-2", "HISTORICO")).rejects.toBe(res400);
  });

  it("getFundoTipoAtivoAllowedTransitions throws on 404", async () => {
    const res404 = mockNotOk(404);
    mockApiFetch.mockResolvedValueOnce(res404);
    await expect(getFundoTipoAtivoAllowedTransitions("fundo-bad", "assoc-bad")).rejects.toBe(res404);
  });

  it("listCedenteTiposAtivo throws on 401", async () => {
    const res401 = mockNotOk(401);
    mockApiFetch.mockResolvedValueOnce(res401);
    await expect(listCedenteTiposAtivo("ced-1", { page: 1, pageSize: 20, search: "" })).rejects.toBe(res401);
  });

  it("createCedenteTipoAtivo throws on 409", async () => {
    const res409 = mockNotOk(409);
    mockApiFetch.mockResolvedValueOnce(res409);
    await expect(createCedenteTipoAtivo("ced-1", { tipoAtivoId: "tipo-1", dataInicio: "2024-01-01" })).rejects.toBe(res409);
  });

  it("updateCedenteTipoAtivoLimits throws on 400", async () => {
    const res400 = mockNotOk(400);
    mockApiFetch.mockResolvedValueOnce(res400);
    await expect(updateCedenteTipoAtivoLimits("ced-1", "assoc-3", { limitePercentual: 200 })).rejects.toBe(res400);
  });

  it("getCedenteTipoAtivoAllowedTransitions throws on 404", async () => {
    const res404 = mockNotOk(404);
    mockApiFetch.mockResolvedValueOnce(res404);
    await expect(getCedenteTipoAtivoAllowedTransitions("ced-bad", "assoc-bad")).rejects.toBe(res404);
  });
});

// ---------------------------------------------------------------------------
// buildPaginatedParams — search param branches
// ---------------------------------------------------------------------------

describe("buildPaginatedParams — search branch coverage", () => {
  it("omits search param when search is empty string", async () => {
    mockApiFetch.mockResolvedValueOnce(mockOkJson({ items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0 }));
    await listTiposAtivo({ page: 1, pageSize: 20, search: "" });
    const url = mockApiFetch.mock.calls[0][0] as string;
    expect(url).not.toContain("search=");
  });

  it("uses default page=1 when page not provided (undefined coerces)", async () => {
    mockApiFetch.mockResolvedValueOnce(mockOkJson({ items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0 }));
    // page undefined coerces to 1 via ?? 1
    await listTiposAtivo({ page: undefined as unknown as number, pageSize: 20, search: "" });
    const url = mockApiFetch.mock.calls[0][0] as string;
    expect(url).toContain("page=1");
  });

  it("omits status param when status undefined in listFundos", async () => {
    mockApiFetch.mockResolvedValueOnce(mockOkJson({ items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0 }));
    await listFundos({ page: 1, pageSize: 20, search: "" });
    const url = mockApiFetch.mock.calls[0][0] as string;
    expect(url).not.toContain("status=");
  });
});
