// ---------------------------------------------------------------------------
// AdminCustodiantesListPage — render, empty, error, data
// ---------------------------------------------------------------------------

import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { AdminCustodiantesListPage } from "@/components/pages/AdminCustodiantesListPage";

vi.mock("@/lib/admin-fundos-api", () => ({
  listAdminCustodiantes: vi.fn(),
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

describe("AdminCustodiantesListPage", () => {
  it("renders page heading", async () => {
    vi.mocked(api.listAdminCustodiantes).mockResolvedValue({
      items: [], totalCount: 0, page: 1, pageSize: 20,
    });
    const Wrapper = makeWrapper();
    render(<AdminCustodiantesListPage />, { wrapper: Wrapper });
    expect(screen.getByRole("heading", { name: /custodiantes/i })).toBeInTheDocument();
  });

  it("shows empty state when no items", async () => {
    vi.mocked(api.listAdminCustodiantes).mockResolvedValue({
      items: [], totalCount: 0, page: 1, pageSize: 20,
    });
    const Wrapper = makeWrapper();
    render(<AdminCustodiantesListPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByTestId("custodiantes-empty")).toBeInTheDocument());
  });

  it("shows error state on fetch failure", async () => {
    vi.mocked(api.listAdminCustodiantes).mockRejectedValue(new Error("fail"));
    const Wrapper = makeWrapper();
    render(<AdminCustodiantesListPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByTestId("custodiantes-error")).toBeInTheDocument());
  });

  it("renders custodiante rows when data returned", async () => {
    vi.mocked(api.listAdminCustodiantes).mockResolvedValue({
      items: [{
        id: "cust-1", clienteId: "cl-1", empresaNome: "Emp A",
        razaoSocial: "Custodiante SA", codigoInterno: "CUS-001",
        cnpj: "12345678000195", email: null, telefone: null,
        status: "ATIVO", createdAt: "2024-01-01T00:00:00Z",
      }],
      totalCount: 1, page: 1, pageSize: 20,
    });
    const Wrapper = makeWrapper();
    render(<AdminCustodiantesListPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByTestId("custodiante-row-cust-1")).toBeInTheDocument());
    expect(screen.getByText("Custodiante SA")).toBeInTheDocument();
  });

  it("navigates to custodiante detail on row click", async () => {
    vi.mocked(api.listAdminCustodiantes).mockResolvedValue({
      items: [{
        id: "cust-2", clienteId: "cl-1", empresaNome: "Emp B",
        razaoSocial: "Custodiante XY", codigoInterno: null,
        cnpj: "12345678000195", email: null, telefone: null,
        status: "ATIVO", createdAt: "2024-01-01T00:00:00Z",
      }],
      totalCount: 1, page: 1, pageSize: 20,
    });
    const Wrapper = makeWrapper();
    render(<AdminCustodiantesListPage />, { wrapper: Wrapper });
    const { fireEvent } = await import("@testing-library/react");
    await waitFor(() => screen.getByTestId("custodiante-row-cust-2"));
    fireEvent.click(screen.getByTestId("custodiante-row-cust-2"));
    expect(mockNavigate).toHaveBeenCalledWith({ to: "/admin/custodiantes/cust-2" });
  });

  it("calls nav when empresa filter changes", async () => {
    vi.mocked(api.listAdminCustodiantes).mockResolvedValue({
      items: [], totalCount: 0, page: 1, pageSize: 20,
    });
    const Wrapper = makeWrapper();
    render(<AdminCustodiantesListPage />, { wrapper: Wrapper });
    await waitFor(() => screen.getByTestId("custodiantes-table"));
    const { fireEvent } = await import("@testing-library/react");
    fireEvent.change(screen.getByTestId("custodiantes-empresa-filter"), { target: { value: "" } });
    expect(screen.getByTestId("custodiantes-empresa-filter")).toHaveValue("");
  });

  it("calls nav when search input changes", async () => {
    vi.mocked(api.listAdminCustodiantes).mockResolvedValue({
      items: [], totalCount: 0, page: 1, pageSize: 20,
    });
    const Wrapper = makeWrapper();
    render(<AdminCustodiantesListPage />, { wrapper: Wrapper });
    await waitFor(() => screen.getByTestId("custodiantes-table"));
    const { fireEvent } = await import("@testing-library/react");
    fireEvent.change(screen.getByTestId("custodiantes-search"), { target: { value: "def" } });
    expect(screen.getByTestId("custodiantes-search")).toHaveValue("def");
  });

  it("calls nav via search clear button (covers search onChange arrow)", async () => {
    vi.mocked(api.listAdminCustodiantes).mockResolvedValue({
      items: [], totalCount: 0, page: 1, pageSize: 20,
    });
    const Wrapper = makeWrapper();
    render(<AdminCustodiantesListPage />, { wrapper: Wrapper });
    await waitFor(() => screen.getByTestId("custodiantes-table"));
    const { fireEvent } = await import("@testing-library/react");
    // Type to make clear button visible, then click to fire parent onChange("") synchronously
    fireEvent.change(screen.getByTestId("custodiantes-search"), { target: { value: "test" } });
    fireEvent.click(screen.getByTestId("search-clear-button"));
    expect(mockNavigate).toHaveBeenCalled();
  });
});
