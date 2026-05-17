// ---------------------------------------------------------------------------
// FundoDetailPage.test.tsx — render + interaction + a11y (D-2, T-6)
// ---------------------------------------------------------------------------

import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, waitFor, fireEvent } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { FundoDetailPage } from "@/components/pages/FundoDetailPage";

// Router mocks
vi.mock("@tanstack/react-router", async (importOriginal) => {
  const mod = await importOriginal<typeof import("@tanstack/react-router")>();
  return {
    ...mod,
    useParams: () => ({ fundoId: "uuid-fundo-1" }),
    useNavigate: () => vi.fn(),
  };
});

// Auth mock with permissions
vi.mock("@/lib/auth-context", () => ({
  useAuth: () => ({
    auth: {
      isAuthenticated: true,
      accessGroup: "admin-empresa",
      permissions: ["funds:read", "funds:write", "funds:manage"],
      companyId: "company-1",
    },
  }),
}));

// Mock fundos-api
vi.mock("@/lib/fundos-api", () => ({
  getFundo: vi.fn().mockResolvedValue({
    id: "uuid-fundo-1",
    nome: "Fundo Alpha",
    cnpj: "11222333000181",
    consultoriaFundoId: "uuid-cons-1",
    custodianteId: "uuid-cust-1",
    tipoFundo: "Multimercado",
    classeAnbima: "Macro",
    segmento: null,
    dataConstituicao: "2020-01-01T00:00:00Z",
    status: "ATIVO",
    createdAt: "2020-01-01T00:00:00Z",
  }),
  updateFundo: vi.fn(),
  transitionFundoStatus: vi.fn(),
  listFundoCedentes: vi.fn().mockResolvedValue({ items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0 }),
  listCedentes: vi.fn().mockResolvedValue({ items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0 }),
  listFundoTiposAtivo: vi.fn().mockResolvedValue({ items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0 }),
  getFundoAllowedTransitions: vi.fn().mockResolvedValue(["SUSPENSO"]),
  listConsultoriasFundo: vi.fn().mockResolvedValue({ items: [], totalCount: 0, page: 1, pageSize: 100, totalPages: 0 }),
  listCustodiantes: vi.fn().mockResolvedValue({ items: [], totalCount: 0, page: 1, pageSize: 100, totalPages: 0 }),
}));

// Mock use-allowed-transitions
vi.mock("@/lib/use-allowed-transitions", () => ({
  useFundoAllowedTransitions: () => ({ data: ["SUSPENSO"], isLoading: false }),
  useFundoCedenteAllowedTransitions: () => ({ data: [], isLoading: false }),
  useFundoTipoAtivoAllowedTransitions: () => ({ data: [], isLoading: false }),
}));

// Mock StatusTransitionDropdown to avoid Radix portal issues
vi.mock("@/components/organisms/StatusTransitionDropdown", () => ({
  StatusTransitionDropdown: ({ currentStatus }: { currentStatus: string }) => (
    <div data-testid="status-dropdown">{currentStatus}</div>
  ),
}));

// Mock sub-tab pages
vi.mock("@/components/pages/FundoCedentesTabPage", () => ({
  FundoCedentesTabPage: () => <div data-testid="cedentes-tab">Cedentes</div>,
}));
vi.mock("@/components/pages/FundoTiposAtivosTabPage", () => ({
  FundoTiposAtivosTabPage: () => <div data-testid="tipos-ativos-tab">Tipos Ativos</div>,
}));

// Mock FundoForm to avoid Radix Select issues
vi.mock("@/components/organisms/FundoForm", () => ({
  FundoForm: () => <form aria-label="Editar fundo" />,
}));

function renderPage() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: 0 } } });
  return render(
    <QueryClientProvider client={qc}>
      <FundoDetailPage />
    </QueryClientProvider>
  );
}

describe("FundoDetailPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("renders fundo name after loading", async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByRole("heading", { name: /fundo alpha/i })).toBeInTheDocument();
    });
  });

  it("renders status badge", async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByLabelText(/status: ativo/i)).toBeInTheDocument();
    });
  });

  it("renders tabs: Dados, Cedentes, Tipos de Ativo", async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByRole("tab", { name: /dados/i })).toBeInTheDocument();
      expect(screen.getByRole("tab", { name: /cedentes/i })).toBeInTheDocument();
      expect(screen.getByRole("tab", { name: /tipos de ativo/i })).toBeInTheDocument();
    });
  });

  it("shows Dados tab content by default", async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByRole("tabpanel", { name: /dados do fundo/i })).toBeInTheDocument();
    });
  });

  it("switches to Cedentes tab on click", async () => {
    renderPage();
    await waitFor(() => screen.getByRole("tab", { name: /cedentes/i }));
    fireEvent.click(screen.getByRole("tab", { name: /cedentes/i }));
    await waitFor(() => {
      expect(screen.getByTestId("cedentes-tab")).toBeInTheDocument();
    });
  });

  it("shows Editar button for users with funds:write", async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByRole("button", { name: /editar fundo/i })).toBeInTheDocument();
    });
  });

  it("renders status transition dropdown", async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId("status-dropdown")).toBeInTheDocument();
    });
  });
});
