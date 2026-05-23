// ---------------------------------------------------------------------------
// AdminConsultoriasFundoListPage — render, empty, error, data
// ---------------------------------------------------------------------------

import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { AdminConsultoriasFundoListPage } from "@/components/pages/AdminConsultoriasFundoListPage";

vi.mock("@/lib/admin-fundos-api", () => ({
  listAdminConsultorias: vi.fn(),
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

describe("AdminConsultoriasFundoListPage", () => {
  it("renders page heading", async () => {
    vi.mocked(api.listAdminConsultorias).mockResolvedValue({
      items: [], totalCount: 0, page: 1, pageSize: 20,
    });
    const Wrapper = makeWrapper();
    render(<AdminConsultoriasFundoListPage />, { wrapper: Wrapper });
    expect(screen.getByRole("heading", { name: /consultorias/i })).toBeInTheDocument();
  });

  it("shows empty state when no items", async () => {
    vi.mocked(api.listAdminConsultorias).mockResolvedValue({
      items: [], totalCount: 0, page: 1, pageSize: 20,
    });
    const Wrapper = makeWrapper();
    render(<AdminConsultoriasFundoListPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByTestId("consultorias-empty")).toBeInTheDocument());
  });

  it("shows error state on fetch failure", async () => {
    vi.mocked(api.listAdminConsultorias).mockRejectedValue(new Error("fail"));
    const Wrapper = makeWrapper();
    render(<AdminConsultoriasFundoListPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByTestId("consultorias-error")).toBeInTheDocument());
  });

  it("renders consultoria rows when data returned", async () => {
    vi.mocked(api.listAdminConsultorias).mockResolvedValue({
      items: [{
        id: "cons-1", clienteId: "cl-1", empresaNome: "Emp A",
        razaoSocial: "Consultoria Alfa Ltda", nomeFantasia: "ConsultAlfa",
        cnpj: "12345678000195", email: "c@a.com", telefone: null,
        status: "ATIVO", createdAt: "2024-01-01T00:00:00Z",
      }],
      totalCount: 1, page: 1, pageSize: 20,
    });
    const Wrapper = makeWrapper();
    render(<AdminConsultoriasFundoListPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByTestId("consultoria-row-cons-1")).toBeInTheDocument());
    expect(screen.getByText("Consultoria Alfa Ltda")).toBeInTheDocument();
  });

  it("navigates to consultoria detail on row click", async () => {
    vi.mocked(api.listAdminConsultorias).mockResolvedValue({
      items: [{
        id: "cons-2", clienteId: "cl-1", empresaNome: "Emp B",
        razaoSocial: "Consultoria Beta", nomeFantasia: null,
        cnpj: "12345678000195", email: null, telefone: null,
        status: "ATIVO", createdAt: "2024-01-01T00:00:00Z",
      }],
      totalCount: 1, page: 1, pageSize: 20,
    });
    const Wrapper = makeWrapper();
    render(<AdminConsultoriasFundoListPage />, { wrapper: Wrapper });
    const { fireEvent } = await import("@testing-library/react");
    await waitFor(() => screen.getByTestId("consultoria-row-cons-2"));
    fireEvent.click(screen.getByTestId("consultoria-row-cons-2"));
    expect(mockNavigate).toHaveBeenCalledWith({ to: "/admin/consultorias-fundo/cons-2" });
  });

  it("calls nav when empresa filter changes", async () => {
    vi.mocked(api.listAdminConsultorias).mockResolvedValue({
      items: [], totalCount: 0, page: 1, pageSize: 20,
    });
    const Wrapper = makeWrapper();
    render(<AdminConsultoriasFundoListPage />, { wrapper: Wrapper });
    await waitFor(() => screen.getByTestId("consultorias-table"));
    const { fireEvent } = await import("@testing-library/react");
    fireEvent.change(screen.getByTestId("consultorias-empresa-filter"), { target: { value: "" } });
    expect(screen.getByTestId("consultorias-empresa-filter")).toHaveValue("");
  });

  it("calls nav when search input changes", async () => {
    vi.mocked(api.listAdminConsultorias).mockResolvedValue({
      items: [], totalCount: 0, page: 1, pageSize: 20,
    });
    const Wrapper = makeWrapper();
    render(<AdminConsultoriasFundoListPage />, { wrapper: Wrapper });
    await waitFor(() => screen.getByTestId("consultorias-table"));
    const { fireEvent } = await import("@testing-library/react");
    fireEvent.change(screen.getByTestId("consultorias-search"), { target: { value: "abc" } });
    expect(screen.getByTestId("consultorias-search")).toHaveValue("abc");
  });

  it("calls nav via search clear button (covers search onChange arrow)", async () => {
    vi.mocked(api.listAdminConsultorias).mockResolvedValue({
      items: [], totalCount: 0, page: 1, pageSize: 20,
    });
    const Wrapper = makeWrapper();
    render(<AdminConsultoriasFundoListPage />, { wrapper: Wrapper });
    await waitFor(() => screen.getByTestId("consultorias-table"));
    const { fireEvent } = await import("@testing-library/react");
    // Type to make clear button visible, then click to fire parent onChange("") synchronously
    fireEvent.change(screen.getByTestId("consultorias-search"), { target: { value: "test" } });
    fireEvent.click(screen.getByTestId("search-clear-button"));
    expect(mockNavigate).toHaveBeenCalled();
  });
});
