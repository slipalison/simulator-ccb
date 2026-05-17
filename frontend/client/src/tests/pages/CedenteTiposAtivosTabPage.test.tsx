// ---------------------------------------------------------------------------
// CedenteTiposAtivosTabPage.test.tsx — render + interaction (D-2, T-7)
// ---------------------------------------------------------------------------

import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, waitFor, fireEvent, act } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { CedenteTiposAtivosTabPage } from "@/components/pages/CedenteTiposAtivosTabPage";

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

const mockListCedenteTiposAtivo = vi.fn();
const mockCreateCedenteTipoAtivo = vi.fn().mockResolvedValue({ id: "assoc-new" });
const mockTransitionCedenteTipoAtivoStatus = vi.fn().mockResolvedValue({});

vi.mock("@/lib/fundos-api", () => ({
  listCedenteTiposAtivo: (...args: unknown[]) => mockListCedenteTiposAtivo(...args),
  listTiposAtivo: vi.fn().mockResolvedValue({ items: [], totalCount: 0, page: 1, pageSize: 100, totalPages: 0 }),
  createCedenteTipoAtivo: (...args: unknown[]) => mockCreateCedenteTipoAtivo(...args),
  transitionCedenteTipoAtivoStatus: (...args: unknown[]) => mockTransitionCedenteTipoAtivoStatus(...args),
}));

vi.mock("sonner", () => ({ toast: { success: vi.fn(), error: vi.fn() } }));

vi.mock("@/lib/use-allowed-transitions", () => ({
  useCedenteTipoAtivoAllowedTransitions: () => ({ data: ["INATIVO"], isLoading: false }),
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
      <button type="button" onClick={() => onSubmit({ targetId: "tipo-1", limitePercentual: null, limiteValor: null, dataInicio: "2024-01-01", dataFim: null })}>
        Criar Associação
      </button>
    </form>
  ),
}));

const DEFAULT_RESPONSE = {
  items: [
    { id: "assoc-1", cedenteId: "ced-1", tipoAtivoId: "tipo-1", limitePercentual: null, limiteValor: null, dataInicio: "2024-01-01T00:00:00Z", dataFim: null, status: "ATIVO", createdAt: "2024-01-01T00:00:00Z" },
  ],
  totalCount: 1, page: 1, pageSize: 20, totalPages: 1,
};

function renderPage() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: 0 } } });
  return render(
    <QueryClientProvider client={qc}>
      <CedenteTiposAtivosTabPage cedenteId="ced-1" />
    </QueryClientProvider>
  );
}

describe("CedenteTiposAtivosTabPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockPermissions = ["funds:read", "funds:write"];
    mockListCedenteTiposAtivo.mockResolvedValue(DEFAULT_RESPONSE);
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
      expect(screen.getByText("Ativo")).toBeInTheDocument();
    });
  });

  it("opens create dialog on button click", async () => {
    renderPage();
    await waitFor(() => screen.getByRole("button", { name: /associar tipo de ativo/i }));
    fireEvent.click(screen.getByRole("button", { name: /associar tipo de ativo/i }));
    await waitFor(() => {
      expect(screen.getByText(/associar tipo de ativo ao cedente/i)).toBeInTheDocument();
    });
  });

  it("calls handleCreate when AssociationForm submits", async () => {
    renderPage();
    await waitFor(() => screen.getByRole("button", { name: /associar tipo de ativo/i }));
    fireEvent.click(screen.getByRole("button", { name: /associar tipo de ativo/i }));
    await waitFor(() => screen.getByText(/associar tipo de ativo ao cedente/i));
    const submitBtn = screen.getByRole("button", { name: /criar associação/i });
    await act(async () => {
      fireEvent.click(submitBtn);
    });
    await waitFor(() => {
      expect(mockCreateCedenteTipoAtivo).toHaveBeenCalled();
    });
  });

  it("renders read-only: no associar button when lacking funds:write", () => {
    mockPermissions = ["funds:read"];
    renderPage();
    expect(screen.queryByRole("button", { name: /associar tipo de ativo/i })).not.toBeInTheDocument();
  });

  it("renders Paginator when totalPages > 1 and calls setPage", async () => {
    mockListCedenteTiposAtivo.mockResolvedValue({
      items: [{ id: "assoc-1", cedenteId: "ced-p", tipoAtivoId: "tipo-1", limitePercentual: null, limiteValor: null, dataInicio: "2024-01-01T00:00:00Z", dataFim: null, status: "ATIVO", createdAt: "2024-01-01T00:00:00Z" }],
      totalCount: 40, page: 1, pageSize: 20, totalPages: 3,
    });
    const qc = new QueryClient({ defaultOptions: { queries: { retry: 0, gcTime: 0, staleTime: 0 } } });
    render(
      <QueryClientProvider client={qc}>
        <CedenteTiposAtivosTabPage cedenteId="ced-paginator-test" />
      </QueryClientProvider>
    );
    await waitFor(() => {
      expect(screen.getByRole("navigation", { name: /paginação/i })).toBeInTheDocument();
    }, { timeout: 5000 });
    fireEvent.click(screen.getByRole("button", { name: "Página 2" }));
    // After page click, query key changes to page:2 — data becomes undefined during fetch,
    // Paginator temporarily disappears. Verify setPage called by checking mock with page:2.
    await waitFor(() => {
      expect(mockListCedenteTiposAtivo).toHaveBeenCalledWith(
        "ced-paginator-test",
        expect.objectContaining({ page: 2 })
      );
    }, { timeout: 3000 });
  });

  it("does not render Paginator when totalPages = 1", async () => {
    renderPage();
    await waitFor(() => screen.getByText("Ativo"));
    expect(screen.queryByRole("navigation", { name: /paginação/i })).not.toBeInTheDocument();
  });

  it("fires status transition when Transição button clicked", async () => {
    renderPage();
    await waitFor(() => screen.getByText("Ativo"));
    const transBtn = screen.getByRole("button", { name: /transição/i });
    await act(async () => {
      fireEvent.click(transBtn);
    });
    await waitFor(() => {
      expect(mockTransitionCedenteTipoAtivoStatus).toHaveBeenCalled();
    });
  });

  it("closes dialog when AssociationForm cancel button clicked", async () => {
    renderPage();
    await waitFor(() => screen.getByRole("button", { name: /associar tipo de ativo/i }));
    fireEvent.click(screen.getByRole("button", { name: /associar tipo de ativo/i }));
    await waitFor(() => screen.getByText(/associar tipo de ativo ao cedente/i));
    fireEvent.click(screen.getByRole("button", { name: /cancelar/i }));
    await waitFor(() => {
      expect(screen.queryByText(/associar tipo de ativo ao cedente/i)).not.toBeInTheDocument();
    });
  });
});
