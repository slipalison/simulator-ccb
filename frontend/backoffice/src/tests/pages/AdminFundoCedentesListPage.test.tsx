// ---------------------------------------------------------------------------
// AdminFundoCedentesListPage — render, empty, error, data
// ---------------------------------------------------------------------------

import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { AdminFundoCedentesListPage } from "@/components/pages/AdminFundoCedentesListPage";

vi.mock("@/lib/admin-fundos-api", () => ({
  listAdminFundoCedentes: vi.fn(),
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

describe("AdminFundoCedentesListPage", () => {
  it("renders page heading", async () => {
    vi.mocked(api.listAdminFundoCedentes).mockResolvedValue({
      items: [], totalCount: 0, page: 1, pageSize: 20,
    });
    const Wrapper = makeWrapper();
    render(<AdminFundoCedentesListPage />, { wrapper: Wrapper });
    expect(screen.getByRole("heading", { name: /fundo.cedentes/i })).toBeInTheDocument();
  });

  it("shows error state on fetch failure", async () => {
    vi.mocked(api.listAdminFundoCedentes).mockRejectedValue(new Error("fail"));
    const Wrapper = makeWrapper();
    render(<AdminFundoCedentesListPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByTestId("fundo-cedentes-error")).toBeInTheDocument());
  });

  it("renders association table container when loaded", async () => {
    vi.mocked(api.listAdminFundoCedentes).mockResolvedValue({
      items: [], totalCount: 0, page: 1, pageSize: 20,
    });
    const Wrapper = makeWrapper();
    render(<AdminFundoCedentesListPage />, { wrapper: Wrapper });
    await waitFor(() =>
      expect(screen.getByTestId("fundo-cedentes-table-container")).toBeInTheDocument()
    );
  });

  it("calls listAdminFundoCedentes with page params", async () => {
    vi.mocked(api.listAdminFundoCedentes).mockResolvedValue({
      items: [], totalCount: 0, page: 1, pageSize: 20,
    });
    const Wrapper = makeWrapper();
    render(<AdminFundoCedentesListPage />, { wrapper: Wrapper });
    await waitFor(() => {
      expect(api.listAdminFundoCedentes).toHaveBeenCalledWith(
        expect.objectContaining({ page: 1 })
      );
    });
  });

  it("calls nav when empresa filter changes", async () => {
    vi.mocked(api.listAdminFundoCedentes).mockResolvedValue({
      items: [], totalCount: 0, page: 1, pageSize: 20,
    });
    const Wrapper = makeWrapper();
    render(<AdminFundoCedentesListPage />, { wrapper: Wrapper });
    await waitFor(() => screen.getByTestId("fundo-cedentes-table-container"));
    const { fireEvent } = await import("@testing-library/react");
    fireEvent.change(screen.getByTestId("fundo-cedentes-empresa-filter"), { target: { value: "" } });
    expect(mockNavigate).toHaveBeenCalled();
  });

  it("renders association table with items (covers rows map arrow)", async () => {
    const VALID_UUID = "123e4567-e89b-12d3-a456-426614174000";
    vi.mocked(api.listAdminFundoCedentes).mockResolvedValue({
      items: [{
        id: "rel-1", clienteId: "cl-1", empresaNome: "Emp A",
        fundoId: VALID_UUID, fundoNome: "Fundo A",
        cedenteId: VALID_UUID, limitePercentual: 10, limiteValor: null,
        dataInicio: "2024-01-01T00:00:00Z", dataFim: null,
        status: "ATIVO", createdAt: "2024-01-01T00:00:00Z",
      }],
      totalCount: 1, page: 1, pageSize: 20,
    });
    const Wrapper = makeWrapper();
    render(<AdminFundoCedentesListPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByTestId("assoc-row-rel-1")).toBeInTheDocument());
    expect(screen.getByText("Fundo A")).toBeInTheDocument();
  });
});
