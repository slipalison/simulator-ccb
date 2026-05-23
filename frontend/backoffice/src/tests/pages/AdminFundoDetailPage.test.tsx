import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import { fireEvent } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { AdminFundoDetailPage } from "@/components/pages/AdminFundoDetailPage";

// ---------------------------------------------------------------------------
// Mocks
// ---------------------------------------------------------------------------

vi.mock("@/lib/admin-fundos-api", () => ({
  getAdminFundo: vi.fn(),
  getAuditHistory: vi.fn(),
}));

vi.mock("@/lib/admin-api", () => ({
  AdminApiError: class AdminApiError extends Error {
    public status?: number;
    constructor(message: string, status?: number) {
      super(message);
      this.name = "AdminApiError";
      this.status = status;
    }
  },
}));

vi.mock("sonner", () => ({ toast: { error: vi.fn(), success: vi.fn() } }));

const mockNavigate = vi.fn();

vi.mock("@tanstack/react-router", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@tanstack/react-router")>();
  return {
    ...actual,
    useNavigate: () => mockNavigate,
    useParams: () => ({ fundoId: "fundo-uuid-123" }),
  };
});

import * as api from "@/lib/admin-fundos-api";
import * as adminApi from "@/lib/admin-api";

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

function makeWrapper() {
  const qc = new QueryClient({
    defaultOptions: { queries: { retry: 0, retryDelay: 0 } },
  });
  return function Wrapper({ children }: { children: React.ReactNode }) {
    return <QueryClientProvider client={qc}>{children}</QueryClientProvider>;
  };
}

const mockFundo = {
  id: "fundo-uuid-123",
  clienteId: "client-uuid",
  empresaNome: "Empresa Alpha",
  nome: "Fundo Alpha",
  cnpj: "12345678000199",
  consultoriaFundoId: "consult-uuid",
  custodianteId: "custod-uuid",
  tipoFundo: 0,
  classeAnbima: "Renda Fixa",
  segmento: "Varejo",
  dataConstituicao: "2020-01-15T00:00:00Z",
  status: "ATIVO",
  createdAt: "2020-01-15T10:00:00Z",
};

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

describe("AdminFundoDetailPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(api.getAuditHistory).mockResolvedValue({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 10,
    });
  });

  it("renders loading spinner initially", () => {
    vi.mocked(api.getAdminFundo).mockImplementation(() => new Promise(() => {}));
    const Wrapper = makeWrapper();
    render(<AdminFundoDetailPage />, { wrapper: Wrapper });
    // sr-only span confirms loading state is active
    expect(screen.getByText("Carregando...")).toBeInTheDocument();
  });

  it("fetches via GET /api/admin/fundos/{id} and renders fundo data", async () => {
    vi.mocked(api.getAdminFundo).mockResolvedValue(mockFundo);
    const Wrapper = makeWrapper();
    render(<AdminFundoDetailPage />, { wrapper: Wrapper });

    await waitFor(() =>
      expect(screen.getByTestId("fundo-detail-page")).toBeInTheDocument()
    );

    expect(api.getAdminFundo).toHaveBeenCalledWith("fundo-uuid-123");
    expect(api.getAdminFundo).toHaveBeenCalledTimes(1);
    // Confirm no list API called
    expect(api.getAdminFundo).not.toHaveBeenCalledWith(
      expect.objectContaining({ pageSize: 200 })
    );
    expect(screen.getByTestId("fundo-cnpj")).toHaveTextContent("12345678000199");
    expect(screen.getByTestId("fundo-empresa")).toHaveTextContent("Empresa Alpha");
  });

  it("shows 404 not-found state on AdminApiError status 404", async () => {
    vi.mocked(api.getAdminFundo).mockRejectedValue(
      new adminApi.AdminApiError("Fundo não encontrado", 404)
    );
    const Wrapper = makeWrapper();
    render(<AdminFundoDetailPage />, { wrapper: Wrapper });

    await waitFor(() =>
      expect(screen.getByTestId("fundo-detail-not-found")).toBeInTheDocument()
    );
  });

  it("shows generic error state on non-404 failure", async () => {
    vi.mocked(api.getAdminFundo).mockRejectedValue(
      new adminApi.AdminApiError("Server error", 500)
    );
    const Wrapper = makeWrapper();
    render(<AdminFundoDetailPage />, { wrapper: Wrapper });

    await waitFor(() =>
      expect(screen.getByTestId("fundo-detail-error")).toBeInTheDocument()
    );
  });

  it("back button on 404 state navigates to /admin/fundos", async () => {
    vi.mocked(api.getAdminFundo).mockRejectedValue(
      new adminApi.AdminApiError("Fundo não encontrado", 404)
    );
    const Wrapper = makeWrapper();
    render(<AdminFundoDetailPage />, { wrapper: Wrapper });

    await waitFor(() =>
      expect(screen.getByTestId("fundo-detail-not-found")).toBeInTheDocument()
    );

    fireEvent.click(screen.getByRole("button"));
    expect(mockNavigate).toHaveBeenCalledWith({ to: "/admin/fundos" });
  });
});
