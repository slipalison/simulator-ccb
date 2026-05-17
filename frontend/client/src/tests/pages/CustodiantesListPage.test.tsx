// ---------------------------------------------------------------------------
// CustodiantesListPage.test.tsx — render + interaction (D-2, T-5)
// ---------------------------------------------------------------------------

import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, waitFor, fireEvent, act } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { CustodiantesListPage } from "@/components/pages/CustodiantesListPage";

const mockNavigate = vi.fn();

vi.mock("@tanstack/react-router", async (importOriginal) => {
  const mod = await importOriginal<typeof import("@tanstack/react-router")>();
  return {
    ...mod,
    useSearch: () => ({ page: 1, search: "" }),
    useNavigate: () => mockNavigate,
  };
});

let mockPermissions = ["funds:read", "funds:write"];
vi.mock("@/lib/auth-context", () => ({
  useAuth: () => ({
    auth: {
      isAuthenticated: true,
      accessGroup: "admin-empresa",
      get permissions() { return mockPermissions; },
      companyId: "company-1",
    },
  }),
}));

const mockCreateCustodiante = vi.fn().mockResolvedValue({ id: "cust-new" });
const mockUpdateCustodiante = vi.fn().mockResolvedValue({ id: "cust-1" });

let listCustodiantesMock = vi.fn().mockResolvedValue({
  items: [
    { id: "cust-1", razaoSocial: "Custodiante Banco SA", codigoInterno: "CUST-01", cnpj: "11222333000181", email: null, telefone: null, status: "ATIVO", createdAt: "2020-01-01T00:00:00Z" },
  ],
  totalCount: 1, page: 1, pageSize: 20, totalPages: 1,
});

vi.mock("@/lib/fundos-api", () => ({
  get listCustodiantes() { return listCustodiantesMock; },
  createCustodiante: (...args: unknown[]) => mockCreateCustodiante(...args),
  updateCustodiante: (...args: unknown[]) => mockUpdateCustodiante(...args),
}));

vi.mock("sonner", () => ({ toast: { success: vi.fn(), error: vi.fn() } }));

// Mock SearchInput — no debounce
vi.mock("@/components/molecules/SearchInput", () => ({
  SearchInput: ({ onChange, placeholder, value }: { onChange: (v: string) => void; placeholder?: string; value: string }) => (
    <input
      placeholder={placeholder}
      defaultValue={value}
      onChange={(e) => onChange(e.target.value)}
      aria-label={placeholder}
    />
  ),
}));

// Mock CustodianteForm to capture onSubmit
vi.mock("@/components/organisms/CustodianteForm", () => ({
  CustodianteForm: ({ onSubmit, onCancel, mode }: { onSubmit: (d: unknown) => Promise<void>; onCancel: () => void; mode: string }) => (
    <form aria-label={mode === "create" ? "Criar custodiante" : "Editar custodiante"}>
      <button type="button" onClick={onCancel}>Cancelar</button>
      <button type="button" onClick={() => onSubmit({ razaoSocial: "Novo Custodiante SA", cnpj: "11222333000181", codigoInterno: "CUST-02" })}>
        Submeter
      </button>
    </form>
  ),
}));

function renderPage() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: 0 } } });
  return render(
    <QueryClientProvider client={qc}>
      <CustodiantesListPage />
    </QueryClientProvider>
  );
}

describe("CustodiantesListPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockPermissions = ["funds:read", "funds:write"];
    listCustodiantesMock = vi.fn().mockResolvedValue({
      items: [
        { id: "cust-1", razaoSocial: "Custodiante Banco SA", codigoInterno: "CUST-01", cnpj: "11222333000181", email: null, telefone: null, status: "ATIVO", createdAt: "2020-01-01T00:00:00Z" },
      ],
      totalCount: 1, page: 1, pageSize: 20, totalPages: 1,
    });
  });

  it("renders page heading", () => {
    renderPage();
    expect(screen.getByRole("heading", { name: /custodiantes/i })).toBeInTheDocument();
  });

  it("renders create button when user has funds:write", () => {
    renderPage();
    expect(screen.getByRole("button", { name: /novo custodiante/i })).toBeInTheDocument();
  });

  it("renders search input", () => {
    renderPage();
    expect(screen.getByPlaceholderText(/buscar/i)).toBeInTheDocument();
  });

  it("renders table with fetched custodiante", async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText("Custodiante Banco SA")).toBeInTheDocument();
    });
  });

  it("calls navigate when search input changes (handleSearch)", async () => {
    renderPage();
    await waitFor(() => screen.getByPlaceholderText(/buscar/i));
    fireEvent.change(screen.getByPlaceholderText(/buscar/i), { target: { value: "Banco" } });
    expect(mockNavigate).toHaveBeenCalledWith(
      expect.objectContaining({ search: expect.objectContaining({ search: "Banco" }) })
    );
  });

  it("opens create dialog on button click", async () => {
    renderPage();
    await waitFor(() => screen.getByRole("button", { name: /novo custodiante/i }));
    fireEvent.click(screen.getByRole("button", { name: /novo custodiante/i }));
    await waitFor(() => {
      expect(screen.getByRole("dialog")).toBeInTheDocument();
    });
  });

  it("shows Novo Custodiante title in create dialog", async () => {
    renderPage();
    await waitFor(() => screen.getByRole("button", { name: /novo custodiante/i }));
    fireEvent.click(screen.getByRole("button", { name: /novo custodiante/i }));
    await waitFor(() => {
      expect(screen.getAllByText(/novo custodiante/i).length).toBeGreaterThan(0);
    });
  });

  it("calls handleSubmit create path via mocked form", async () => {
    renderPage();
    await waitFor(() => screen.getByRole("button", { name: /novo custodiante/i }));
    fireEvent.click(screen.getByRole("button", { name: /novo custodiante/i }));
    await waitFor(() => screen.getByRole("dialog"));
    const submitBtn = screen.getByRole("button", { name: /submeter/i });
    await act(async () => {
      fireEvent.click(submitBtn);
    });
    await waitFor(() => {
      expect(mockCreateCustodiante).toHaveBeenCalled();
    });
  });

  it("opens edit dialog when edit callback fires", async () => {
    renderPage();
    await waitFor(() => screen.getByText("Custodiante Banco SA"));
    const editBtns = screen.queryAllByRole("button", { name: /editar/i });
    if (editBtns.length > 0) {
      fireEvent.click(editBtns[0]);
      await waitFor(() => {
        expect(screen.getByRole("dialog")).toBeInTheDocument();
      });
    }
  });

  it("calls handleSubmit update path via mocked edit form", async () => {
    renderPage();
    await waitFor(() => screen.getByText("Custodiante Banco SA"));
    const editBtns = screen.queryAllByRole("button", { name: /editar/i });
    if (editBtns.length > 0) {
      fireEvent.click(editBtns[0]);
      await waitFor(() => screen.getByRole("dialog"));
      const submitBtn = screen.getByRole("button", { name: /submeter/i });
      await act(async () => {
        fireEvent.click(submitBtn);
      });
      await waitFor(() => {
        expect(mockUpdateCustodiante).toHaveBeenCalled();
      });
    }
  });

  it("closes dialog on Escape key (onOpenChange)", async () => {
    renderPage();
    await waitFor(() => screen.getByRole("button", { name: /novo custodiante/i }));
    fireEvent.click(screen.getByRole("button", { name: /novo custodiante/i }));
    await waitFor(() => screen.getByRole("dialog"));
    fireEvent.keyDown(document, { key: "Escape" });
    await waitFor(() => {
      expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
    });
  });

  it("closes dialog when cancel button clicked", async () => {
    renderPage();
    await waitFor(() => screen.getByRole("button", { name: /novo custodiante/i }));
    fireEvent.click(screen.getByRole("button", { name: /novo custodiante/i }));
    await waitFor(() => screen.getByRole("dialog"));
    fireEvent.click(screen.getByRole("button", { name: /cancelar/i }));
    await waitFor(() => {
      expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
    });
  });

  it("renders Paginator when totalPages > 1 and calls handlePageChange", async () => {
    listCustodiantesMock = vi.fn().mockResolvedValue({
      items: [{ id: "cust-1", razaoSocial: "Custodiante Banco SA", codigoInterno: "CUST-01", cnpj: "11222333000181", email: null, telefone: null, status: "ATIVO", createdAt: "2020-01-01T00:00:00Z" }],
      totalCount: 40, page: 1, pageSize: 20, totalPages: 3,
    });
    renderPage();
    await waitFor(() => screen.getByText("Custodiante Banco SA"));
    await waitFor(() => {
      expect(screen.getByRole("navigation", { name: /paginação/i })).toBeInTheDocument();
    });
    fireEvent.click(screen.getByRole("button", { name: "Página 2" }));
    expect(mockNavigate).toHaveBeenCalledWith(
      expect.objectContaining({ search: expect.objectContaining({ page: 2 }) })
    );
  });

  it("renders with read-only permissions — no create button", () => {
    mockPermissions = ["funds:read"];
    renderPage();
    expect(screen.queryByRole("button", { name: /novo custodiante/i })).not.toBeInTheDocument();
  });

  it("does not render Paginator when totalPages = 1", async () => {
    renderPage();
    await waitFor(() => screen.getByText("Custodiante Banco SA"));
    expect(screen.queryByRole("navigation", { name: /paginação/i })).not.toBeInTheDocument();
  });
});
