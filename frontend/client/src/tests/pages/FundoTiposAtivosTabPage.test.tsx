// ---------------------------------------------------------------------------
// FundoTiposAtivosTabPage.test.tsx — render + interaction (D-2, T-7)
// ---------------------------------------------------------------------------

import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, waitFor, fireEvent, act } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { FundoTiposAtivosTabPage } from "@/components/pages/FundoTiposAtivosTabPage";

let mockPermissions = ["funds:read", "funds:write"];
vi.mock("@/lib/auth-context", () => ({
  useAuth: () => ({
    auth: {
      isAuthenticated: true,
      accessGroup: "admin-empresa",
      get permissions() { return mockPermissions; },
      companyId: "company-1",
    },
  }),
}));

const mockListFundoTiposAtivo = vi.fn();
const mockCreateFundoTipoAtivo = vi.fn().mockResolvedValue({ id: "assoc-new" });
const mockTransitionFundoTipoAtivoStatus = vi.fn().mockResolvedValue({});

vi.mock("@/lib/fundos-api", () => ({
  listFundoTiposAtivo: (...args: unknown[]) => mockListFundoTiposAtivo(...args),
  listTiposAtivo: vi.fn().mockResolvedValue({ items: [{ id: "tipo-1", codigo: "RF", descricao: "Renda Fixa CDB" }], totalCount: 1, page: 1, pageSize: 100, totalPages: 1 }),
  createFundoTipoAtivo: (...args: unknown[]) => mockCreateFundoTipoAtivo(...args),
  transitionFundoTipoAtivoStatus: (...args: unknown[]) => mockTransitionFundoTipoAtivoStatus(...args),
}));

vi.mock("sonner", () => ({ toast: { success: vi.fn(), error: vi.fn() } }));

vi.mock("@/lib/use-allowed-transitions", () => ({
  useFundoTipoAtivoAllowedTransitions: () => ({ data: ["INATIVO"], isLoading: false }),
}));

vi.mock("@/components/organisms/StatusTransitionDropdown", () => ({
  StatusTransitionDropdown: ({ onTransition }: { onTransition: (s: string) => void }) => (
    <button type="button" onClick={() => onTransition("INATIVO")}>Transição</button>
  ),
}));

vi.mock("@/components/organisms/AssociationForm", () => ({
  AssociationForm: ({ onSubmit, onCancel }: { onSubmit: (d: unknown) => Promise<void>; onCancel: () => void }) => (
    <form aria-label="Nova associação">
      <button type="button" onClick={onCancel}>Cancelar</button>
      <button type="button" onClick={() => onSubmit({ targetId: "tipo-1", limitePercentual: 5, limiteValor: null, dataInicio: "2024-01-01", dataFim: null })}>
        Criar Associação
      </button>
    </form>
  ),
}));

const DEFAULT_RESPONSE = {
  items: [
    { id: "assoc-1", fundoId: "fundo-1", tipoAtivoId: "tipo-1", limitePercentual: 5, limiteValor: null, dataInicio: "2024-01-01T00:00:00Z", dataFim: null, status: "ATIVO", createdAt: "2024-01-01T00:00:00Z" },
  ],
  totalCount: 1, page: 1, pageSize: 20, totalPages: 1,
};

function renderPage() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: 0 } } });
  return render(
    <QueryClientProvider client={qc}>
      <FundoTiposAtivosTabPage fundoId="fundo-1" />
    </QueryClientProvider>
  );
}

describe("FundoTiposAtivosTabPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockPermissions = ["funds:read", "funds:write"];
    mockListFundoTiposAtivo.mockResolvedValue(DEFAULT_RESPONSE);
  });

  it("renders section heading", () => {
    renderPage();
    expect(screen.getByText(/tipos de ativo associados/i)).toBeInTheDocument();
  });

  it("shows Associar Tipo de Ativo button for users with funds:write", () => {
    renderPage();
    expect(screen.getByRole("button", { name: /associar tipo de ativo/i })).toBeInTheDocument();
  });

  it("renders association row after fetch", async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText("5%")).toBeInTheDocument();
    });
  });

  it("opens create dialog on button click", async () => {
    renderPage();
    await waitFor(() => screen.getByRole("button", { name: /associar tipo de ativo/i }));
    fireEvent.click(screen.getByRole("button", { name: /associar tipo de ativo/i }));
    await waitFor(() => {
      expect(screen.getByText(/associar tipo de ativo ao fundo/i)).toBeInTheDocument();
    });
  });

  it("calls handleCreate when AssociationForm submits", async () => {
    renderPage();
    await waitFor(() => screen.getByRole("button", { name: /associar tipo de ativo/i }));
    fireEvent.click(screen.getByRole("button", { name: /associar tipo de ativo/i }));
    await waitFor(() => screen.getByText(/associar tipo de ativo ao fundo/i));
    const submitBtn = screen.getByRole("button", { name: /criar associação/i });
    await act(async () => {
      fireEvent.click(submitBtn);
    });
    await waitFor(() => {
      expect(mockCreateFundoTipoAtivo).toHaveBeenCalled();
    });
  });

  it("renders read-only: no associar button when lacking funds:write", () => {
    mockPermissions = ["funds:read"];
    renderPage();
    expect(screen.queryByRole("button", { name: /associar tipo de ativo/i })).not.toBeInTheDocument();
  });

  it("renders Paginator when totalPages > 1 and calls setPage", async () => {
    mockListFundoTiposAtivo.mockResolvedValue({
      items: [{ id: "assoc-1", fundoId: "fundo-p", tipoAtivoId: "tipo-1", limitePercentual: 5, limiteValor: null, dataInicio: "2024-01-01T00:00:00Z", dataFim: null, status: "ATIVO", createdAt: "2024-01-01T00:00:00Z" }],
      totalCount: 40, page: 1, pageSize: 20, totalPages: 3,
    });
    const qc = new QueryClient({ defaultOptions: { queries: { retry: 0, gcTime: 0, staleTime: 0 } } });
    render(
      <QueryClientProvider client={qc}>
        <FundoTiposAtivosTabPage fundoId="fundo-paginator-test" />
      </QueryClientProvider>
    );
    await waitFor(() => {
      expect(screen.getByRole("navigation", { name: /paginação/i })).toBeInTheDocument();
    }, { timeout: 5000 });
    fireEvent.click(screen.getByRole("button", { name: "Página 2" }));
    // After page click, query key changes to page:2 — data becomes undefined during fetch,
    // Paginator temporarily disappears. Verify setPage called by checking mock with page:2.
    await waitFor(() => {
      expect(mockListFundoTiposAtivo).toHaveBeenCalledWith(
        "fundo-paginator-test",
        expect.objectContaining({ page: 2 })
      );
    }, { timeout: 3000 });
  });

  it("does not render Paginator when totalPages = 1", async () => {
    renderPage();
    await waitFor(() => screen.getByText("5%"));
    expect(screen.queryByRole("navigation", { name: /paginação/i })).not.toBeInTheDocument();
  });

  it("fires status transition when Transição button clicked", async () => {
    renderPage();
    await waitFor(() => screen.getByText("5%"));
    const transBtn = screen.getByRole("button", { name: /transição/i });
    await act(async () => {
      fireEvent.click(transBtn);
    });
    await waitFor(() => {
      expect(mockTransitionFundoTipoAtivoStatus).toHaveBeenCalled();
    });
  });

  it("closes dialog when AssociationForm cancel button clicked", async () => {
    renderPage();
    await waitFor(() => screen.getByRole("button", { name: /associar tipo de ativo/i }));
    fireEvent.click(screen.getByRole("button", { name: /associar tipo de ativo/i }));
    await waitFor(() => screen.getByText(/associar tipo de ativo ao fundo/i));
    fireEvent.click(screen.getByRole("button", { name: /cancelar/i }));
    await waitFor(() => {
      expect(screen.queryByText(/associar tipo de ativo ao fundo/i)).not.toBeInTheDocument();
    });
  });
});
