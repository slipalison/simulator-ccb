// ---------------------------------------------------------------------------
// FundosListPage.test.tsx — render + interaction + a11y (T-6)
// ---------------------------------------------------------------------------

import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { FundosListPage } from "@/components/pages/FundosListPage";

vi.mock("@tanstack/react-router", async (importOriginal) => {
  const mod = await importOriginal<typeof import("@tanstack/react-router")>();
  return {
    ...mod,
    useSearch: () => ({ page: 1, search: "", status: undefined }),
    useNavigate: () => () => {},
  };
});

vi.mock("@/lib/auth-context", () => ({
  useAuth: () => ({
    auth: {
      isAuthenticated: true,
      accessGroup: "admin-empresa",
      permissions: ["funds:read", "funds:write"],
      companyId: "company-1",
    },
  }),
}));

vi.mock("@/lib/fundos-api", () => ({
  listFundos: vi.fn().mockResolvedValue({
    items: [
      {
        id: "uuid-fundo-1",
        cnpj: "11222333000181",
        nome: "Fundo Alpha",
        tipoFundo: "Multimercado",
        classeAnbima: "Macro",
        segmento: null,
        dataConstituicao: "2020-01-01",
        status: "ATIVO",
        consultoriaFundoId: "uuid-cons-1",
        custodianteId: "uuid-cust-1",
      },
    ],
    totalCount: 1,
    page: 1,
    pageSize: 20,
    totalPages: 1,
  }),
  createFundo: vi.fn(),
  listConsultoriasFundo: vi.fn().mockResolvedValue({ items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 1 }),
  listCustodiantes: vi.fn().mockResolvedValue({ items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 1 }),
}));

function renderPage() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: 0 } } });
  return render(
    <QueryClientProvider client={qc}>
      <FundosListPage />
    </QueryClientProvider>
  );
}

describe("FundosListPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("renders page heading", () => {
    renderPage();
    expect(screen.getByRole("heading", { name: /fundos/i })).toBeInTheDocument();
  });

  it("renders create button when user has funds:write", () => {
    renderPage();
    expect(screen.getByRole("button", { name: /novo fundo/i })).toBeInTheDocument();
  });

  it("renders search input", () => {
    renderPage();
    expect(
      screen.getByPlaceholderText(/buscar por nome ou cnpj/i)
    ).toBeInTheDocument();
  });

  it("renders status filter dropdown", () => {
    renderPage();
    expect(screen.getByRole("combobox", { name: /filtrar por status/i })).toBeInTheDocument();
  });

  it("renders table with fetched fund", async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText("Fundo Alpha")).toBeInTheDocument();
    });
  });
});
