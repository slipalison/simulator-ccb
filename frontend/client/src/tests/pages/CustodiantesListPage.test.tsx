// ---------------------------------------------------------------------------
// CustodiantesListPage.test.tsx — render + interaction (D-2, T-5)
// ---------------------------------------------------------------------------

import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { CustodiantesListPage } from "@/components/pages/CustodiantesListPage";

vi.mock("@tanstack/react-router", async (importOriginal) => {
  const mod = await importOriginal<typeof import("@tanstack/react-router")>();
  return {
    ...mod,
    useSearch: () => ({ page: 1, search: "" }),
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
  listCustodiantes: vi.fn().mockResolvedValue({
    items: [
      { id: "cust-1", razaoSocial: "Custodiante Banco SA", codigoInterno: "CUST-01", cnpj: "11222333000181", email: null, telefone: null, status: "ATIVO", createdAt: "2020-01-01T00:00:00Z" },
    ],
    totalCount: 1,
    page: 1,
    pageSize: 20,
    totalPages: 1,
  }),
  createCustodiante: vi.fn(),
  updateCustodiante: vi.fn(),
}));

vi.mock("@/components/organisms/CustodianteForm", () => ({
  CustodianteForm: () => <form aria-label="Criar custodiante" />,
}));

function renderPage() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: 0 } } });
  return render(
    <QueryClientProvider client={qc}>
      <CustodiantesListPage />
    </QueryClientProvider>
  );
}

describe("CustodiantesListPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("renders page heading", () => {
    renderPage();
    expect(screen.getByRole("heading", { name: /custodiantes/i })).toBeInTheDocument();
  });

  it("renders create button when user has funds:write", () => {
    renderPage();
    expect(screen.getByRole("button", { name: /novo custodiante/i })).toBeInTheDocument();
  });

  it("renders search input", () => {
    renderPage();
    expect(screen.getByPlaceholderText(/buscar/i)).toBeInTheDocument();
  });

  it("renders table with fetched custodiante", async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText("Custodiante Banco SA")).toBeInTheDocument();
    });
  });
});
