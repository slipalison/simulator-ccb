import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, waitFor, fireEvent } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { AdminCedenteDetailPage } from "@/components/pages/AdminCedenteDetailPage";

vi.mock("@/lib/admin-fundos-api", () => ({
  getAdminCedente: vi.fn(),
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
    useParams: () => ({ cedenteId: "cedente-uuid-123" }),
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

const mockCedente = {
  id: "cedente-uuid-123",
  clienteId: "client-uuid",
  empresaNome: "Empresa Beta",
  documento: "12345678901",
  nome: "Maria Cedente",
  email: "maria@example.com",
  telefone: "(11) 99999-0000",
  endereco: "Rua A, 123",
  cedenteTipo: 0,
  status: "ATIVO",
  createdAt: "2021-03-10T09:00:00Z",
};

describe("AdminCedenteDetailPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(api.getAuditHistory).mockResolvedValue({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 10,
    });
  });

  it("fetches via GET /api/admin/fundos/cedentes/{id} and renders cedente data", async () => {
    vi.mocked(api.getAdminCedente).mockResolvedValue(mockCedente);
    const Wrapper = makeWrapper();
    render(<AdminCedenteDetailPage />, { wrapper: Wrapper });

    await waitFor(() =>
      expect(screen.getByTestId("cedente-detail-page")).toBeInTheDocument()
    );

    expect(api.getAdminCedente).toHaveBeenCalledWith("cedente-uuid-123");
    expect(api.getAdminCedente).toHaveBeenCalledTimes(1);
    expect(screen.getByTestId("cedente-documento")).toHaveTextContent("12345678901");
    expect(screen.getByTestId("cedente-empresa")).toHaveTextContent("Empresa Beta");
  });

  it("shows 404 not-found state on AdminApiError status 404", async () => {
    vi.mocked(api.getAdminCedente).mockRejectedValue(
      new adminApi.AdminApiError("Cedente não encontrado", 404)
    );
    const Wrapper = makeWrapper();
    render(<AdminCedenteDetailPage />, { wrapper: Wrapper });

    await waitFor(() =>
      expect(screen.getByTestId("cedente-detail-not-found")).toBeInTheDocument()
    );
  });

  it("shows generic error state on non-404 failure", async () => {
    vi.mocked(api.getAdminCedente).mockRejectedValue(
      new adminApi.AdminApiError("Server error", 500)
    );
    const Wrapper = makeWrapper();
    render(<AdminCedenteDetailPage />, { wrapper: Wrapper });

    await waitFor(() =>
      expect(screen.getByTestId("cedente-detail-error")).toBeInTheDocument()
    );
  });

  it("back button on 404 navigates to /admin/cedentes", async () => {
    vi.mocked(api.getAdminCedente).mockRejectedValue(
      new adminApi.AdminApiError("Cedente não encontrado", 404)
    );
    const Wrapper = makeWrapper();
    render(<AdminCedenteDetailPage />, { wrapper: Wrapper });

    await waitFor(() =>
      expect(screen.getByTestId("cedente-detail-not-found")).toBeInTheDocument()
    );

    fireEvent.click(screen.getByRole("button"));
    expect(mockNavigate).toHaveBeenCalledWith({ to: "/admin/cedentes" });
  });

  it("entity header back button navigates to /admin/cedentes when loaded", async () => {
    vi.mocked(api.getAdminCedente).mockResolvedValue(mockCedente);
    const Wrapper = makeWrapper();
    render(<AdminCedenteDetailPage />, { wrapper: Wrapper });

    await waitFor(() => screen.getByTestId("cedente-detail-page"));
    fireEvent.click(screen.getByTestId("entity-back-button"));
    expect(mockNavigate).toHaveBeenCalledWith({ to: "/admin/cedentes" });
  });

  it("renders PJ label when cedenteTipo is 1", async () => {
    vi.mocked(api.getAdminCedente).mockResolvedValue({ ...mockCedente, cedenteTipo: 1 });
    const Wrapper = makeWrapper();
    render(<AdminCedenteDetailPage />, { wrapper: Wrapper });

    await waitFor(() => screen.getByTestId("cedente-detail-page"));
    expect(screen.getByTestId("cedente-tipo")).toHaveTextContent("Pessoa Jurídica");
  });
});
