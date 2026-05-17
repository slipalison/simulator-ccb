// ---------------------------------------------------------------------------
// TiposAtivoListPage.test.tsx — render + interaction + a11y (T-3)
// ---------------------------------------------------------------------------

import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, waitFor, fireEvent, act } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { TiposAtivoListPage } from "@/components/pages/TiposAtivoListPage";

const mockNavigate = vi.fn();

vi.mock("@/router", () => ({
  tiposAtivoRouteApi: {
    useSearch: () => ({ page: 1, search: "" }),
    useNavigate: () => mockNavigate,
  },
}));

// Auth mock — writable so individual tests can override permissions
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

const mockCreateTipoAtivo = vi.fn().mockResolvedValue({ id: "uuid-new", codigo: "NEW", descricao: "New", categoria: "Outros", subcategoria: null, status: "ATIVO", ordemExibicao: 0 });
const mockUpdateTipoAtivo = vi.fn().mockResolvedValue({ id: "uuid-1", codigo: "RF001", descricao: "Updated", categoria: "RendaFixa", subcategoria: null, status: "ATIVO", ordemExibicao: 1 });

// Mutable list mock so some tests can use totalPages > 1
let listTiposAtivoMock = vi.fn().mockResolvedValue({
  items: [
    {
      id: "uuid-1",
      codigo: "RF001",
      descricao: "Renda Fixa Gov",
      categoria: "RendaFixa",
      subcategoria: null,
      status: "ATIVO",
      ordemExibicao: 1,
    },
  ],
  totalCount: 1,
  page: 1,
  pageSize: 20,
  totalPages: 1,
});

// Mock fundos-api
vi.mock("@/lib/fundos-api", () => ({
  get listTiposAtivo() { return listTiposAtivoMock; },
  createTipoAtivo: (...args: unknown[]) => mockCreateTipoAtivo(...args),
  updateTipoAtivo: (...args: unknown[]) => mockUpdateTipoAtivo(...args),
}));

// Mock sonner to avoid real toast
vi.mock("sonner", () => ({ toast: { success: vi.fn(), error: vi.fn() } }));

// Mock SearchInput so onChange fires immediately (no 300ms debounce)
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

// Mock TipoAtivoForm
vi.mock("@/components/organisms/TipoAtivoForm", () => ({
  TipoAtivoForm: ({ onSubmit, onCancel, mode }: { onSubmit: (d: unknown) => Promise<void>; onCancel: () => void; mode: string }) => {
    return (
      <form aria-label={mode === "create" ? "Criar tipo de ativo" : "Editar tipo de ativo"}>
        <button type="button" onClick={onCancel}>Cancelar</button>
        <button type="button" onClick={() => onSubmit({ codigo: "NEW", descricao: "New", categoria: "Outros", subcategoria: "", ordemExibicao: 0 })}>
          Submeter
        </button>
      </form>
    );
  },
}));

function renderPage() {
  const qc = new QueryClient({
    defaultOptions: { queries: { retry: 0 } },
  });
  return render(
    <QueryClientProvider client={qc}>
      <TiposAtivoListPage />
    </QueryClientProvider>
  );
}

describe("TiposAtivoListPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockPermissions = ["funds:read", "funds:write"];
    listTiposAtivoMock = vi.fn().mockResolvedValue({
      items: [
        { id: "uuid-1", codigo: "RF001", descricao: "Renda Fixa Gov", categoria: "RendaFixa", subcategoria: null, status: "ATIVO", ordemExibicao: 1 },
      ],
      totalCount: 1,
      page: 1,
      pageSize: 20,
      totalPages: 1,
    });
  });

  it("renders page heading", () => {
    renderPage();
    expect(screen.getByRole("heading", { name: /tipos de ativo/i })).toBeInTheDocument();
  });

  it("renders create button when user has funds:write", async () => {
    renderPage();
    await waitFor(() => {
      expect(
        screen.getByRole("button", { name: /novo tipo de ativo/i })
      ).toBeInTheDocument();
    });
  });

  it("renders table with fetched items", async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText("RF001")).toBeInTheDocument();
      expect(screen.getByText("Renda Fixa Gov")).toBeInTheDocument();
    });
  });

  it("renders search input", () => {
    renderPage();
    expect(
      screen.getByPlaceholderText(/buscar por código ou descrição/i)
    ).toBeInTheDocument();
  });

  it("does not render create button when user lacks funds:write", () => {
    mockPermissions = ["funds:read"];
    renderPage();
    expect(screen.queryByRole("button", { name: /novo tipo de ativo/i })).not.toBeInTheDocument();
  });

  it("calls navigate when search input changes (handleSearch)", async () => {
    renderPage();
    await waitFor(() => screen.getByPlaceholderText(/buscar por código ou descrição/i));
    fireEvent.change(screen.getByPlaceholderText(/buscar por código ou descrição/i), { target: { value: "RF001" } });
    expect(mockNavigate).toHaveBeenCalledWith(
      expect.objectContaining({ search: expect.objectContaining({ search: "RF001" }) })
    );
  });

  it("opens create dialog when create button is clicked", async () => {
    renderPage();
    await waitFor(() => screen.getByRole("button", { name: /novo tipo de ativo/i }));
    fireEvent.click(screen.getByRole("button", { name: /novo tipo de ativo/i }));
    await waitFor(() => {
      expect(screen.getByRole("dialog")).toBeInTheDocument();
    });
  });

  it("opens edit dialog when edit callback is invoked", async () => {
    renderPage();
    await waitFor(() => screen.getByText("RF001"));
    const editButtons = screen.getAllByRole("button", { name: /editar/i });
    if (editButtons.length > 0) {
      fireEvent.click(editButtons[0]);
      await waitFor(() => {
        expect(screen.getByRole("dialog")).toBeInTheDocument();
      });
    }
  });

  it("calls handleSubmit on create form submission", async () => {
    renderPage();
    await waitFor(() => screen.getByRole("button", { name: /novo tipo de ativo/i }));
    fireEvent.click(screen.getByRole("button", { name: /novo tipo de ativo/i }));
    await waitFor(() => screen.getByRole("dialog"));
    // Find the Submeter button inside the mocked form and click it
    const submitBtn = screen.getByRole("button", { name: /submeter/i });
    await act(async () => {
      fireEvent.click(submitBtn);
    });
    await waitFor(() => {
      expect(mockCreateTipoAtivo).toHaveBeenCalled();
    });
  });

  it("calls handleSubmit on edit form submission", async () => {
    renderPage();
    await waitFor(() => screen.getByText("RF001"));
    const editButtons = screen.getAllByRole("button", { name: /editar/i });
    if (editButtons.length > 0) {
      fireEvent.click(editButtons[0]);
      await waitFor(() => screen.getByRole("dialog"));
      const submitBtn = screen.getByRole("button", { name: /submeter/i });
      await act(async () => {
        fireEvent.click(submitBtn);
      });
      await waitFor(() => {
        expect(mockUpdateTipoAtivo).toHaveBeenCalled();
      });
    }
  });

  it("renders Paginator when totalPages > 1 and calls handlePageChange", async () => {
    listTiposAtivoMock = vi.fn().mockResolvedValue({
      items: [{ id: "uuid-1", codigo: "RF001", descricao: "Renda Fixa Gov", categoria: "RendaFixa", subcategoria: null, status: "ATIVO", ordemExibicao: 1 }],
      totalCount: 40,
      page: 1,
      pageSize: 20,
      totalPages: 3,
    });
    renderPage();
    await waitFor(() => screen.getByText("RF001"));
    // Paginator should render with page 2 button
    await waitFor(() => {
      expect(screen.getByRole("navigation", { name: /paginação/i })).toBeInTheDocument();
    });
    fireEvent.click(screen.getByRole("button", { name: "Página 2" }));
    expect(mockNavigate).toHaveBeenCalledWith(
      expect.objectContaining({ search: expect.objectContaining({ page: 2 }) })
    );
  });

  it("does not render Paginator when totalPages is 1", async () => {
    renderPage();
    await waitFor(() => screen.getByText("RF001"));
    // totalPages=1 → Paginator renders null
    expect(screen.queryByRole("navigation", { name: /paginação/i })).not.toBeInTheDocument();
  });

  it("closes dialog on Escape key (onOpenChange)", async () => {
    renderPage();
    await waitFor(() => screen.getByRole("button", { name: /novo tipo de ativo/i }));
    fireEvent.click(screen.getByRole("button", { name: /novo tipo de ativo/i }));
    await waitFor(() => screen.getByRole("dialog"));
    fireEvent.keyDown(document, { key: "Escape" });
    await waitFor(() => {
      expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
    });
  });

  it("closes dialog when cancel button clicked", async () => {
    renderPage();
    await waitFor(() => screen.getByRole("button", { name: /novo tipo de ativo/i }));
    fireEvent.click(screen.getByRole("button", { name: /novo tipo de ativo/i }));
    await waitFor(() => screen.getByRole("dialog"));
    fireEvent.click(screen.getByRole("button", { name: /cancelar/i }));
    await waitFor(() => {
      expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
    });
  });
});
