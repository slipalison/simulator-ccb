// ---------------------------------------------------------------------------
// FundoCedentesTabPage.test.tsx — render + interaction (D-2, T-7)
// ---------------------------------------------------------------------------

import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { FundoCedentesTabPage } from "@/components/pages/FundoCedentesTabPage";

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

const mockListFundoCedentes = vi.fn();
const mockCreateFundoCedente = vi.fn().mockResolvedValue({ id: "assoc-new" });
const mockTransitionFundoCedenteStatus = vi.fn().mockResolvedValue({});

vi.mock("@/lib/fundos-api", () => ({
  listFundoCedentes: (...args: unknown[]) => mockListFundoCedentes(...args),
  listCedentes: vi.fn().mockResolvedValue({ items: [{ id: "ced-1", nome: "João Silva" }], totalCount: 1, page: 1, pageSize: 100, totalPages: 1 }),
  createFundoCedente: (...args: unknown[]) => mockCreateFundoCedente(...args),
  transitionFundoCedenteStatus: (...args: unknown[]) => mockTransitionFundoCedenteStatus(...args),
}));

vi.mock("sonner", () => ({ toast: { success: vi.fn(), error: vi.fn() } }));

vi.mock("@/lib/use-allowed-transitions", () => ({
  useFundoCedenteAllowedTransitions: () => ({ data: ["INATIVO"], isLoading: false }),
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
      <button type="button" onClick={() => onSubmit({ targetId: "ced-1", limitePercentual: 10, limiteValor: null, dataInicio: "2024-01-01", dataFim: null })}>
        Criar Associação
      </button>
    </form>
  ),
}));

const DEFAULT_RESPONSE = {
  items: [
    { id: "assoc-1", fundoId: "fundo-1", cedenteId: "ced-1", limitePercentual: 10, limiteValor: null, dataInicio: "2024-01-01T00:00:00Z", dataFim: null, status: "ATIVO", createdAt: "2024-01-01T00:00:00Z" },
  ],
  totalCount: 1, page: 1, pageSize: 20, totalPages: 1,
};

const PAGINATOR_RESPONSE = {
  items: [{ id: "assoc-1", fundoId: "fundo-p", cedenteId: "ced-1", limitePercentual: 10, limiteValor: null, dataInicio: "2024-01-01T00:00:00Z", dataFim: null, status: "ATIVO", createdAt: "2024-01-01T00:00:00Z" }],
  totalCount: 40, page: 1, pageSize: 20, totalPages: 3,
};

function renderPage() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: 0 } } });
  return render(
    <QueryClientProvider client={qc}>
      <FundoCedentesTabPage fundoId="fundo-1" />
    </QueryClientProvider>
  );
}

describe("FundoCedentesTabPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockPermissions = ["funds:read", "funds:write"];
    mockListFundoCedentes.mockResolvedValue(DEFAULT_RESPONSE);
    mockCreateFundoCedente.mockResolvedValue({ id: "assoc-new" });
    mockTransitionFundoCedenteStatus.mockResolvedValue({});
  });

  it("renders section heading", () => {
    renderPage();
    expect(screen.getByText(/cedentes associados/i)).toBeInTheDocument();
  });

  it("shows Associar Cedente button for users with funds:write", () => {
    renderPage();
    expect(screen.getByRole("button", { name: /associar cedente/i })).toBeInTheDocument();
  });

  it("renders association rows after fetch", async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText("10%")).toBeInTheDocument();
    });
  });

  it("opens create dialog on button click", async () => {
    const { getByRole } = renderPage();
    await waitFor(() => getByRole("button", { name: /associar cedente/i }));
    const { fireEvent } = await import("@testing-library/react");
    fireEvent.click(getByRole("button", { name: /associar cedente/i }));
    await waitFor(() => {
      expect(screen.getByText(/associar cedente ao fundo/i)).toBeInTheDocument();
    });
  });

  it("calls handleCreate when AssociationForm submits", async () => {
    const { getByRole } = renderPage();
    const { fireEvent, act } = await import("@testing-library/react");
    await waitFor(() => getByRole("button", { name: /associar cedente/i }));
    fireEvent.click(getByRole("button", { name: /associar cedente/i }));
    await waitFor(() => screen.getByText(/associar cedente ao fundo/i));
    const submitBtn = screen.getByRole("button", { name: /criar associação/i });
    await act(async () => {
      fireEvent.click(submitBtn);
    });
    await waitFor(() => {
      expect(mockCreateFundoCedente).toHaveBeenCalled();
    });
  });

  it("closes create dialog when dialog dismissed", async () => {
    const { getByRole } = renderPage();
    const { fireEvent } = await import("@testing-library/react");
    await waitFor(() => getByRole("button", { name: /associar cedente/i }));
    fireEvent.click(getByRole("button", { name: /associar cedente/i }));
    await waitFor(() => screen.getByText(/associar cedente ao fundo/i));
    fireEvent.keyDown(document, { key: "Escape" });
    await waitFor(() => {
      expect(screen.queryByText(/associar cedente ao fundo/i)).not.toBeInTheDocument();
    });
  });

  it("renders read-only: no associar button when lacking funds:write", () => {
    mockPermissions = ["funds:read"];
    renderPage();
    expect(screen.queryByRole("button", { name: /associar cedente/i })).not.toBeInTheDocument();
  });

  it("renders Paginator when totalPages > 1 and calls setPage", async () => {
    mockListFundoCedentes.mockReset();
    mockListFundoCedentes.mockImplementation(async (..._args: unknown[]) => {
      return PAGINATOR_RESPONSE;
    });
    const qc = new QueryClient({ defaultOptions: { queries: { retry: 0, gcTime: 0, staleTime: 0 } } });
    render(
      <QueryClientProvider client={qc}>
        <FundoCedentesTabPage fundoId="fundo-paginator-test" />
      </QueryClientProvider>
    );
    await waitFor(() => {
      expect(mockListFundoCedentes).toHaveBeenCalled();
    }, { timeout: 3000 });
    await waitFor(() => {
      expect(screen.getByRole("navigation", { name: /paginação/i })).toBeInTheDocument();
    }, { timeout: 5000 });
    const { fireEvent } = await import("@testing-library/react");
    fireEvent.click(screen.getByRole("button", { name: "Página 2" }));
    // After page click, query key changes to page:2 — data becomes undefined during fetch,
    // so Paginator temporarily disappears. Verify setPage was called by checking mock called with page:2.
    await waitFor(() => {
      expect(mockListFundoCedentes).toHaveBeenCalledWith(
        "fundo-paginator-test",
        expect.objectContaining({ page: 2 })
      );
    }, { timeout: 3000 });
  });

  it("does not render Paginator when totalPages = 1", async () => {
    renderPage();
    await waitFor(() => screen.getByText("10%"));
    expect(screen.queryByRole("navigation", { name: /paginação/i })).not.toBeInTheDocument();
  });

  it("fires status transition when Transição button clicked", async () => {
    renderPage();
    const { fireEvent, act } = await import("@testing-library/react");
    await waitFor(() => screen.getByText("10%"));
    const transBtn = screen.getByRole("button", { name: /transição/i });
    await act(async () => {
      fireEvent.click(transBtn);
    });
    await waitFor(() => {
      expect(mockTransitionFundoCedenteStatus).toHaveBeenCalled();
    });
  });

  it("closes dialog when AssociationForm cancel button clicked", async () => {
    const { fireEvent } = await import("@testing-library/react");
    const { getByRole } = renderPage();
    await waitFor(() => getByRole("button", { name: /associar cedente/i }));
    fireEvent.click(getByRole("button", { name: /associar cedente/i }));
    await waitFor(() => screen.getByText(/associar cedente ao fundo/i));
    fireEvent.click(screen.getByRole("button", { name: /cancelar/i }));
    await waitFor(() => {
      expect(screen.queryByText(/associar cedente ao fundo/i)).not.toBeInTheDocument();
    });
  });
});
