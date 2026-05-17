// ---------------------------------------------------------------------------
// FundoDetailPage.test.tsx — render + interaction + a11y (D-2, T-6)
// ---------------------------------------------------------------------------

import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, waitFor, fireEvent, act } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { FundoDetailPage } from "@/components/pages/FundoDetailPage";

// Router mocks
const mockNavigate = vi.fn();
vi.mock("@tanstack/react-router", async (importOriginal) => {
  const mod = await importOriginal<typeof import("@tanstack/react-router")>();
  return {
    ...mod,
    useParams: () => ({ fundoId: "uuid-fundo-1" }),
    useNavigate: () => mockNavigate,
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

const mockUpdateFundo = vi.fn().mockResolvedValue({ id: "uuid-fundo-1" });
const mockTransitionFundoStatus = vi.fn().mockResolvedValue({});

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
  updateFundo: (...args: unknown[]) => mockUpdateFundo(...args),
  transitionFundoStatus: (...args: unknown[]) => mockTransitionFundoStatus(...args),
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

// Mock StatusTransitionDropdown
vi.mock("@/components/organisms/StatusTransitionDropdown", () => ({
  StatusTransitionDropdown: ({ currentStatus, onTransition }: { currentStatus: string; onTransition: (s: string) => void }) => {
    return (
      <div data-testid="status-dropdown">
        {currentStatus}
        <button type="button" onClick={() => onTransition("SUSPENSO")}>Transitar SUSPENSO</button>
      </div>
    );
  },
}));

// Mock sub-tab pages
vi.mock("@/components/pages/FundoCedentesTabPage", () => ({
  FundoCedentesTabPage: () => <div data-testid="cedentes-tab">Cedentes</div>,
}));
vi.mock("@/components/pages/FundoTiposAtivosTabPage", () => ({
  FundoTiposAtivosTabPage: () => <div data-testid="tipos-ativos-tab">Tipos Ativos</div>,
}));

// Mock FundoForm to capture onSubmit
vi.mock("@/components/organisms/FundoForm", () => ({
  FundoForm: ({ onSubmit, onCancel }: { onSubmit: (d: unknown) => Promise<void>; onCancel: () => void }) => (
    <form aria-label="Editar fundo">
      <button type="button" onClick={onCancel}>Cancelar</button>
      <button type="button" onClick={() => onSubmit({ nome: "Fundo Alpha Editado", cnpj: "11222333000181", tipoFundo: "Multimercado", classeAnbima: "Macro", dataConstituicao: "2020-01-01", consultoriaFundoId: "uuid-cons-1", custodianteId: "uuid-cust-1" })}>
        Submeter
      </button>
    </form>
  ),
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
    mockNavigate.mockReset();
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

  it("switches to Tipos de Ativo tab on click", async () => {
    renderPage();
    await waitFor(() => screen.getByRole("tab", { name: /tipos de ativo/i }));
    fireEvent.click(screen.getByRole("tab", { name: /tipos de ativo/i }));
    await waitFor(() => {
      expect(screen.getByTestId("tipos-ativos-tab")).toBeInTheDocument();
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

  it("opens edit dialog when Editar button clicked", async () => {
    renderPage();
    await waitFor(() => screen.getByRole("button", { name: /editar fundo/i }));
    fireEvent.click(screen.getByRole("button", { name: /editar fundo/i }));
    await waitFor(() => {
      expect(screen.getByRole("dialog")).toBeInTheDocument();
    });
  });

  it("calls handleUpdateSubmit when edit form submits", async () => {
    renderPage();
    await waitFor(() => screen.getByRole("button", { name: /editar fundo/i }));
    fireEvent.click(screen.getByRole("button", { name: /editar fundo/i }));
    await waitFor(() => screen.getByRole("dialog"));
    const submitBtn = screen.getByRole("button", { name: /submeter/i });
    await act(async () => {
      fireEvent.click(submitBtn);
    });
    await waitFor(() => {
      expect(mockUpdateFundo).toHaveBeenCalled();
    });
  });

  it("calls handleTransition when status transition triggered", async () => {
    renderPage();
    await waitFor(() => screen.getByTestId("status-dropdown"));
    const transitBtn = screen.getByRole("button", { name: /transitar suspenso/i });
    await act(async () => {
      fireEvent.click(transitBtn);
    });
    await waitFor(() => {
      expect(mockTransitionFundoStatus).toHaveBeenCalledWith("uuid-fundo-1", "SUSPENSO");
    });
  });

  it("renders back navigation button", async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByRole("button", { name: /voltar/i })).toBeInTheDocument();
    });
  });

  it("renders fundo CNPJ in detail section", async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText(/11222333000181/)).toBeInTheDocument();
    });
  });

  it("renders canManage-only features with funds:manage permission", async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId("status-dropdown")).toBeInTheDocument();
    });
  });

  it("shows not-found message when fundo is null", async () => {
    const fundosApi = await import("@/lib/fundos-api");
    vi.mocked(fundosApi.getFundo).mockResolvedValueOnce(null as any);
    renderPage();
    await waitFor(() => {
      expect(screen.getByText(/fundo não encontrado/i)).toBeInTheDocument();
    });
  });

  it("calls navigate when back button is clicked", async () => {
    renderPage();
    await waitFor(() => screen.getByRole("button", { name: /voltar/i }));
    fireEvent.click(screen.getByRole("button", { name: /voltar/i }));
    expect(mockNavigate).toHaveBeenCalled();
  });

  it("closes edit dialog when cancel button in form clicked", async () => {
    renderPage();
    await waitFor(() => screen.getByRole("button", { name: /editar fundo/i }));
    fireEvent.click(screen.getByRole("button", { name: /editar fundo/i }));
    await waitFor(() => screen.getByRole("dialog"));
    fireEvent.click(screen.getByRole("button", { name: /cancelar/i }));
    await waitFor(() => {
      expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
    });
  });

  it("handles Response error in handleUpdateSubmit catch path", async () => {
    mockUpdateFundo.mockRejectedValueOnce(
      new Response(JSON.stringify({ type: "ValidationError", errors: [] }), { status: 422 })
    );
    renderPage();
    await waitFor(() => screen.getByRole("button", { name: /editar fundo/i }));
    fireEvent.click(screen.getByRole("button", { name: /editar fundo/i }));
    await waitFor(() => screen.getByRole("dialog"));
    const submitBtn = screen.getByRole("button", { name: /submeter/i });
    await act(async () => {
      fireEvent.click(submitBtn);
    });
    await waitFor(() => {
      expect(mockUpdateFundo).toHaveBeenCalled();
    });
  });

  it("handles Response error in handleTransition catch path", async () => {
    mockTransitionFundoStatus.mockRejectedValueOnce(
      new Response(JSON.stringify({ type: "ValidationError", errors: [] }), { status: 422 })
    );
    renderPage();
    await waitFor(() => screen.getByTestId("status-dropdown"));
    const transitBtn = screen.getByRole("button", { name: /transitar suspenso/i });
    await act(async () => {
      fireEvent.click(transitBtn);
    });
    await waitFor(() => {
      expect(mockTransitionFundoStatus).toHaveBeenCalled();
    });
  });
});
