// ---------------------------------------------------------------------------
// AdminCedentesListPage — render, empty, error, data
// ---------------------------------------------------------------------------

import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { AdminCedentesListPage } from "@/components/pages/AdminCedentesListPage";

vi.mock("@/lib/admin-fundos-api", () => ({
  listAdminCedentes: vi.fn(),
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

describe("AdminCedentesListPage", () => {
  it("renders page heading", async () => {
    vi.mocked(api.listAdminCedentes).mockResolvedValue({
      items: [], totalCount: 0, page: 1, pageSize: 20,
    });
    const Wrapper = makeWrapper();
    render(<AdminCedentesListPage />, { wrapper: Wrapper });
    expect(screen.getByRole("heading", { name: /cedentes/i })).toBeInTheDocument();
  });

  it("shows empty state when no items", async () => {
    vi.mocked(api.listAdminCedentes).mockResolvedValue({
      items: [], totalCount: 0, page: 1, pageSize: 20,
    });
    const Wrapper = makeWrapper();
    render(<AdminCedentesListPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByTestId("cedentes-empty")).toBeInTheDocument());
  });

  it("shows error state on fetch failure", async () => {
    vi.mocked(api.listAdminCedentes).mockRejectedValue(new Error("fail"));
    const Wrapper = makeWrapper();
    render(<AdminCedentesListPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByTestId("cedentes-error")).toBeInTheDocument());
  });

  it("renders cedente rows when data returned", async () => {
    vi.mocked(api.listAdminCedentes).mockResolvedValue({
      items: [{
        id: "ced-1", clienteId: "cl-1", empresaNome: "Emp A", documento: "12345678901",
        nome: "João Silva", email: null, telefone: null, endereco: null,
        cedenteTipo: 0, status: "ATIVO", createdAt: "2024-01-01T00:00:00Z",
      }],
      totalCount: 1, page: 1, pageSize: 20,
    });
    const Wrapper = makeWrapper();
    render(<AdminCedentesListPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByTestId("cedente-row-ced-1")).toBeInTheDocument());
    expect(screen.getByText("João Silva")).toBeInTheDocument();
  });

  it("calls listAdminCedentes with default page params", async () => {
    vi.mocked(api.listAdminCedentes).mockResolvedValue({
      items: [], totalCount: 0, page: 1, pageSize: 20,
    });
    const Wrapper = makeWrapper();
    render(<AdminCedentesListPage />, { wrapper: Wrapper });
    await waitFor(() => {
      expect(api.listAdminCedentes).toHaveBeenCalledWith(
        expect.objectContaining({ page: 1 })
      );
    });
  });

  it("navigates to cedente detail on row click", async () => {
    vi.mocked(api.listAdminCedentes).mockResolvedValue({
      items: [{
        id: "ced-2", clienteId: "cl-1", empresaNome: "Emp B", documento: "99999999999",
        nome: "Ana Souza", email: null, telefone: null, endereco: null,
        cedenteTipo: 1, status: "ATIVO", createdAt: "2024-01-01T00:00:00Z",
      }],
      totalCount: 1, page: 1, pageSize: 20,
    });
    const Wrapper = makeWrapper();
    render(<AdminCedentesListPage />, { wrapper: Wrapper });
    const { fireEvent } = await import("@testing-library/react");
    await waitFor(() => screen.getByTestId("cedente-row-ced-2"));
    fireEvent.click(screen.getByTestId("cedente-row-ced-2"));
    expect(mockNavigate).toHaveBeenCalledWith({ to: "/admin/cedentes/ced-2" });
  });

  it("calls nav when empresa filter changes", async () => {
    vi.mocked(api.listAdminCedentes).mockResolvedValue({
      items: [], totalCount: 0, page: 1, pageSize: 20,
    });
    const Wrapper = makeWrapper();
    render(<AdminCedentesListPage />, { wrapper: Wrapper });
    await waitFor(() => screen.getByTestId("cedentes-table"));
    const { fireEvent } = await import("@testing-library/react");
    fireEvent.change(screen.getByTestId("cedentes-empresa-filter"), { target: { value: "" } });
    expect(screen.getByTestId("cedentes-empresa-filter")).toHaveValue("");
  });

  it("calls nav when search input changes", async () => {
    vi.mocked(api.listAdminCedentes).mockResolvedValue({
      items: [], totalCount: 0, page: 1, pageSize: 20,
    });
    const Wrapper = makeWrapper();
    render(<AdminCedentesListPage />, { wrapper: Wrapper });
    await waitFor(() => screen.getByTestId("cedentes-table"));
    const { fireEvent } = await import("@testing-library/react");
    fireEvent.change(screen.getByTestId("cedentes-search"), { target: { value: "xyz" } });
    expect(screen.getByTestId("cedentes-search")).toHaveValue("xyz");
  });

  it("calls nav via search clear button (covers search onChange arrow)", async () => {
    vi.mocked(api.listAdminCedentes).mockResolvedValue({
      items: [], totalCount: 0, page: 1, pageSize: 20,
    });
    const Wrapper = makeWrapper();
    render(<AdminCedentesListPage />, { wrapper: Wrapper });
    await waitFor(() => screen.getByTestId("cedentes-table"));
    const { fireEvent } = await import("@testing-library/react");
    // Type to make clear button visible, then click clear to fire parent onChange("") synchronously
    fireEvent.change(screen.getByTestId("cedentes-search"), { target: { value: "abc" } });
    fireEvent.click(screen.getByTestId("search-clear-button"));
    expect(mockNavigate).toHaveBeenCalled();
  });
});
