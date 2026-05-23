import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, waitFor, fireEvent } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { AdminConsultoriaFundoDetailPage } from "@/components/pages/AdminConsultoriaFundoDetailPage";

vi.mock("@/lib/admin-fundos-api", () => ({
  getAdminConsultoriaFundo: vi.fn(),
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
    useParams: () => ({ consultoriaId: "consultoria-uuid-123" }),
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

const mockConsultoria = {
  id: "consultoria-uuid-123",
  clienteId: "client-uuid",
  empresaNome: "Empresa Gamma",
  razaoSocial: "Consultoria Gamma Ltda",
  nomeFantasia: "Consultoria Gamma",
  cnpj: "98765432000111",
  email: "contato@gamma.com",
  telefone: "(21) 33330000",
  status: "ATIVO",
  createdAt: "2022-06-01T08:00:00Z",
};

describe("AdminConsultoriaFundoDetailPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(api.getAuditHistory).mockResolvedValue({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 10,
    });
  });

  it("fetches via GET /api/admin/fundos/consultorias/{id} and renders data", async () => {
    vi.mocked(api.getAdminConsultoriaFundo).mockResolvedValue(mockConsultoria);
    const Wrapper = makeWrapper();
    render(<AdminConsultoriaFundoDetailPage />, { wrapper: Wrapper });

    await waitFor(() =>
      expect(screen.getByTestId("consultoria-detail-page")).toBeInTheDocument()
    );

    expect(api.getAdminConsultoriaFundo).toHaveBeenCalledWith("consultoria-uuid-123");
    expect(api.getAdminConsultoriaFundo).toHaveBeenCalledTimes(1);
    expect(screen.getByTestId("consultoria-cnpj")).toHaveTextContent("98765432000111");
    expect(screen.getByTestId("consultoria-empresa")).toHaveTextContent("Empresa Gamma");
  });

  it("shows 404 not-found state on AdminApiError status 404", async () => {
    vi.mocked(api.getAdminConsultoriaFundo).mockRejectedValue(
      new adminApi.AdminApiError("Consultoria não encontrada", 404)
    );
    const Wrapper = makeWrapper();
    render(<AdminConsultoriaFundoDetailPage />, { wrapper: Wrapper });

    await waitFor(() =>
      expect(screen.getByTestId("consultoria-detail-not-found")).toBeInTheDocument()
    );
  });

  it("shows generic error state on non-404 failure", async () => {
    vi.mocked(api.getAdminConsultoriaFundo).mockRejectedValue(
      new adminApi.AdminApiError("Server error", 500)
    );
    const Wrapper = makeWrapper();
    render(<AdminConsultoriaFundoDetailPage />, { wrapper: Wrapper });

    await waitFor(() =>
      expect(screen.getByTestId("consultoria-detail-error")).toBeInTheDocument()
    );
  });

  it("back button on 404 navigates to /admin/consultorias-fundo", async () => {
    vi.mocked(api.getAdminConsultoriaFundo).mockRejectedValue(
      new adminApi.AdminApiError("Consultoria não encontrada", 404)
    );
    const Wrapper = makeWrapper();
    render(<AdminConsultoriaFundoDetailPage />, { wrapper: Wrapper });

    await waitFor(() =>
      expect(screen.getByTestId("consultoria-detail-not-found")).toBeInTheDocument()
    );

    fireEvent.click(screen.getByRole("button"));
    expect(mockNavigate).toHaveBeenCalledWith({ to: "/admin/consultorias-fundo" });
  });
});
