// ---------------------------------------------------------------------------
// AdminFundoTiposAtivosListPage — render, error, data
// ---------------------------------------------------------------------------

import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { AdminFundoTiposAtivosListPage } from "@/components/pages/AdminFundoTiposAtivosListPage";

vi.mock("@/lib/admin-fundos-api", () => ({
  listAdminFundoTiposAtivos: vi.fn(),
}));

vi.mock("@/lib/admin-companies-api", () => ({
  listCompaniesForFilter: vi.fn().mockResolvedValue([]),
}));

const mockNavigate = vi.fn();
vi.mock("@tanstack/react-router", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@tanstack/react-router")>();
  return {
    ...actual,
    useSearch: () => ({ page: 1, search: "", empresaId: undefined }),
    useNavigate: () => mockNavigate,
  };
});

import * as api from "@/lib/admin-fundos-api";

function makeWrapper() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: 0 } } });
  return function Wrapper({ children }: { children: React.ReactNode }) {
    return <QueryClientProvider client={qc}>{children}</QueryClientProvider>;
  };
}

beforeEach(() => vi.clearAllMocks());

describe("AdminFundoTiposAtivosListPage", () => {
  it("renders page heading", async () => {
    vi.mocked(api.listAdminFundoTiposAtivos).mockResolvedValue({
      items: [], totalCount: 0, page: 1, pageSize: 20,
    });
    const Wrapper = makeWrapper();
    render(<AdminFundoTiposAtivosListPage />, { wrapper: Wrapper });
    expect(screen.getByRole("heading", { name: /fundo.tipos ativos/i })).toBeInTheDocument();
  });

  it("shows error state on fetch failure", async () => {
    vi.mocked(api.listAdminFundoTiposAtivos).mockRejectedValue(new Error("fail"));
    const Wrapper = makeWrapper();
    render(<AdminFundoTiposAtivosListPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByTestId("fundo-tipos-ativos-error")).toBeInTheDocument());
  });

  it("renders table container when loaded", async () => {
    vi.mocked(api.listAdminFundoTiposAtivos).mockResolvedValue({
      items: [], totalCount: 0, page: 1, pageSize: 20,
    });
    const Wrapper = makeWrapper();
    render(<AdminFundoTiposAtivosListPage />, { wrapper: Wrapper });
    await waitFor(() =>
      expect(screen.getByTestId("fundo-tipos-ativos-table-container")).toBeInTheDocument()
    );
  });

  it("calls listAdminFundoTiposAtivos with page params", async () => {
    vi.mocked(api.listAdminFundoTiposAtivos).mockResolvedValue({
      items: [], totalCount: 0, page: 1, pageSize: 20,
    });
    const Wrapper = makeWrapper();
    render(<AdminFundoTiposAtivosListPage />, { wrapper: Wrapper });
    await waitFor(() => {
      expect(api.listAdminFundoTiposAtivos).toHaveBeenCalledWith(
        expect.objectContaining({ page: 1 })
      );
    });
  });

  it("calls nav when empresa filter changes", async () => {
    vi.mocked(api.listAdminFundoTiposAtivos).mockResolvedValue({
      items: [], totalCount: 0, page: 1, pageSize: 20,
    });
    const Wrapper = makeWrapper();
    render(<AdminFundoTiposAtivosListPage />, { wrapper: Wrapper });
    await waitFor(() => screen.getByTestId("fundo-tipos-ativos-table-container"));
    const { fireEvent } = await import("@testing-library/react");
    fireEvent.change(screen.getByTestId("fundo-tipos-ativos-empresa-filter"), { target: { value: "" } });
    expect(mockNavigate).toHaveBeenCalled();
  });

  it("renders association table rows when items returned (covers rows map arrow)", async () => {
    const VALID_UUID = "123e4567-e89b-12d3-a456-426614174000";
    vi.mocked(api.listAdminFundoTiposAtivos).mockResolvedValue({
      items: [{
        id: "rel-2", clienteId: "cl-1", empresaNome: "Emp A",
        fundoId: VALID_UUID, fundoNome: "Fundo B",
        tipoAtivoId: VALID_UUID, limitePercentual: null, limiteValor: 50000,
        dataInicio: "2024-01-01T00:00:00Z", dataFim: null,
        status: "ATIVO", createdAt: "2024-01-01T00:00:00Z",
      }],
      totalCount: 1, page: 1, pageSize: 20,
    });
    const Wrapper = makeWrapper();
    render(<AdminFundoTiposAtivosListPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByTestId("assoc-row-rel-2")).toBeInTheDocument());
    expect(screen.getByText("Fundo B")).toBeInTheDocument();
  });
});
