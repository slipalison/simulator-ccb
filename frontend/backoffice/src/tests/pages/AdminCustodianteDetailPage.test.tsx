import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, waitFor, fireEvent } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { AdminCustodianteDetailPage } from "@/components/pages/AdminCustodianteDetailPage";

vi.mock("@/lib/admin-fundos-api", () => ({
  getAdminCustodiante: vi.fn(),
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
    useParams: () => ({ custodianteId: "custodiante-uuid-123" }),
  };
});

import * as api from "@/lib/admin-fundos-api";
import * as adminApi from "@/lib/admin-api";

function makeWrapper() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: 0, retryDelay: 0 } } });
  return function Wrapper({ children }: { children: React.ReactNode }) {
    return <QueryClientProvider client={qc}>{children}</QueryClientProvider>;
  };
}

const mockCustodiante = {
  id: "custodiante-uuid-123",
  clienteId: "client-uuid",
  empresaNome: "Empresa Delta",
  razaoSocial: "Custodiante Delta S/A",
  codigoInterno: "CUST-001",
  cnpj: "11223344000155",
  email: "ops@delta.com",
  telefone: "(11) 44440000",
  status: "ATIVO",
  createdAt: "2023-01-20T14:00:00Z",
};

describe("AdminCustodianteDetailPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(api.getAuditHistory).mockResolvedValue({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 10,
    });
  });

  it("fetches via GET /api/admin/fundos/custodiantes/{id} and renders data", async () => {
    vi.mocked(api.getAdminCustodiante).mockResolvedValue(mockCustodiante);
    const Wrapper = makeWrapper();
    render(<AdminCustodianteDetailPage />, { wrapper: Wrapper });

    await waitFor(() =>
      expect(screen.getByTestId("custodiante-detail-page")).toBeInTheDocument()
    );

    expect(api.getAdminCustodiante).toHaveBeenCalledWith("custodiante-uuid-123");
    expect(api.getAdminCustodiante).toHaveBeenCalledTimes(1);
    expect(screen.getByTestId("custodiante-cnpj")).toHaveTextContent("11223344000155");
    expect(screen.getByTestId("custodiante-empresa")).toHaveTextContent("Empresa Delta");
    expect(screen.getByTestId("custodiante-codigo-interno")).toHaveTextContent("CUST-001");
  });

  it("shows 404 not-found state on AdminApiError status 404", async () => {
    vi.mocked(api.getAdminCustodiante).mockRejectedValue(
      new adminApi.AdminApiError("Custodiante não encontrado", 404)
    );
    const Wrapper = makeWrapper();
    render(<AdminCustodianteDetailPage />, { wrapper: Wrapper });

    await waitFor(() =>
      expect(screen.getByTestId("custodiante-detail-not-found")).toBeInTheDocument()
    );
  });

  it("shows generic error state on non-404 failure", async () => {
    vi.mocked(api.getAdminCustodiante).mockRejectedValue(
      new adminApi.AdminApiError("Server error", 500)
    );
    const Wrapper = makeWrapper();
    render(<AdminCustodianteDetailPage />, { wrapper: Wrapper });

    await waitFor(() =>
      expect(screen.getByTestId("custodiante-detail-error")).toBeInTheDocument()
    );
  });

  it("back button on 404 navigates to /admin/custodiantes", async () => {
    vi.mocked(api.getAdminCustodiante).mockRejectedValue(
      new adminApi.AdminApiError("Custodiante não encontrado", 404)
    );
    const Wrapper = makeWrapper();
    render(<AdminCustodianteDetailPage />, { wrapper: Wrapper });

    await waitFor(() =>
      expect(screen.getByTestId("custodiante-detail-not-found")).toBeInTheDocument()
    );

    fireEvent.click(screen.getByRole("button"));
    expect(mockNavigate).toHaveBeenCalledWith({ to: "/admin/custodiantes" });
  });

  it("entity header back button navigates to /admin/custodiantes when loaded", async () => {
    vi.mocked(api.getAdminCustodiante).mockResolvedValue(mockCustodiante);
    const Wrapper = makeWrapper();
    render(<AdminCustodianteDetailPage />, { wrapper: Wrapper });

    await waitFor(() => screen.getByTestId("custodiante-detail-page"));
    fireEvent.click(screen.getByTestId("entity-back-button"));
    expect(mockNavigate).toHaveBeenCalledWith({ to: "/admin/custodiantes" });
  });
});
