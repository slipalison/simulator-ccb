// ---------------------------------------------------------------------------
// TiposAtivoListPage.test.tsx — render + interaction + a11y (T-3)
// ---------------------------------------------------------------------------

import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { TiposAtivoListPage } from "@/components/pages/TiposAtivoListPage";

// Mock react-router hooks
vi.mock("@tanstack/react-router", async (importOriginal) => {
  const mod = await importOriginal<typeof import("@tanstack/react-router")>();
  return {
    ...mod,
    useSearch: () => ({ page: 1, search: "" }),
    useNavigate: () => () => {},
  };
});

// Mock auth context
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

// Mock fundos-api
vi.mock("@/lib/fundos-api", () => ({
  listTiposAtivo: vi.fn().mockResolvedValue({
    items: [
      {
        id: "uuid-1",
        codigo: "RF001",
        descricao: "Renda Fixa Gov",
        categoria: "RendaFixa",
        subcategoria: null,
        status: "ATIVO",
        ordemExibicao: 1,
      },
    ],
    totalCount: 1,
    page: 1,
    pageSize: 20,
    totalPages: 1,
  }),
  createTipoAtivo: vi.fn(),
  updateTipoAtivo: vi.fn(),
}));

function renderPage() {
  const qc = new QueryClient({
    defaultOptions: { queries: { retry: 0 } },
  });
  return render(
    <QueryClientProvider client={qc}>
      <TiposAtivoListPage />
    </QueryClientProvider>
  );
}

describe("TiposAtivoListPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("renders page heading", () => {
    renderPage();
    expect(screen.getByRole("heading", { name: /tipos de ativo/i })).toBeInTheDocument();
  });

  it("renders create button when user has funds:write", async () => {
    renderPage();
    await waitFor(() => {
      expect(
        screen.getByRole("button", { name: /novo tipo de ativo/i })
      ).toBeInTheDocument();
    });
  });

  it("renders table with fetched items", async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText("RF001")).toBeInTheDocument();
      expect(screen.getByText("Renda Fixa Gov")).toBeInTheDocument();
    });
  });

  it("renders search input", () => {
    renderPage();
    expect(
      screen.getByPlaceholderText(/buscar por código ou descrição/i)
    ).toBeInTheDocument();
  });
});
