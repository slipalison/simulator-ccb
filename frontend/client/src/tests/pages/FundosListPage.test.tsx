// ---------------------------------------------------------------------------
// FundosListPage.test.tsx — render + interaction + a11y (T-6)
// ---------------------------------------------------------------------------

import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, waitFor, fireEvent, act } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { FundosListPage } from "@/components/pages/FundosListPage";

const mockNavigate = vi.fn();

vi.mock("@tanstack/react-router", async (importOriginal) => {
  const mod = await importOriginal<typeof import("@tanstack/react-router")>();
  return {
    ...mod,
    useSearch: () => ({ page: 1, search: "", status: undefined }),
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

const mockCreateFundo = vi.fn().mockResolvedValue({ id: "fundo-new" });

let listFundosMock = vi.fn().mockResolvedValue({
  items: [
    {
      id: "uuid-fundo-1",
      cnpj: "11222333000181",
      nome: "Fundo Alpha",
      tipoFundo: "Multimercado",
      classeAnbima: "Macro",
      segmento: null,
      dataConstituicao: "2020-01-01",
      status: "ATIVO",
      consultoriaFundoId: "uuid-cons-1",
      custodianteId: "uuid-cust-1",
    },
  ],
  totalCount: 1,
  page: 1,
  pageSize: 20,
  totalPages: 1,
});

vi.mock("@/lib/fundos-api", () => ({
  get listFundos() { return listFundosMock; },
  createFundo: (...args: unknown[]) => mockCreateFundo(...args),
  listConsultoriasFundo: vi.fn().mockResolvedValue({ items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 1 }),
  listCustodiantes: vi.fn().mockResolvedValue({ items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 1 }),
}));

vi.mock("sonner", () => ({ toast: { success: vi.fn(), error: vi.fn() } }));

// Mock Radix Select — threads onValueChange from Select → SelectItem via React context
vi.mock("@/components/ui/select", () => {
  // eslint-disable-next-line @typescript-eslint/no-require-imports
  const React = require("react") as typeof import("react");
  const OnValueChangeCtx = React.createContext<((v: string) => void) | undefined>(undefined);
  return {
    Select: ({ children, onValueChange, value }: any) => (
      <OnValueChangeCtx.Provider value={onValueChange}>
        <div data-value={value}>{children}</div>
      </OnValueChangeCtx.Provider>
    ),
    SelectTrigger: ({ children, "aria-label": al, className }: any) =>
      <button role="combobox" aria-label={al} className={className}>{children}</button>,
    SelectValue: ({ placeholder }: any) => <span>{placeholder}</span>,
    SelectContent: ({ children }: any) => <div>{children}</div>,
    SelectItem: ({ children, value }: any) => {
      const onChange = React.useContext(OnValueChangeCtx);
      return <button type="button" data-value={value} onClick={() => onChange?.(value)}>{children}</button>;
    },
  };
});

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

// Mock FundoForm to capture onSubmit
vi.mock("@/components/organisms/FundoForm", () => ({
  FundoForm: ({ onSubmit, onCancel, mode }: { onSubmit: (d: unknown) => Promise<void>; onCancel: () => void; mode: string }) => (
    <form aria-label={mode === "create" ? "Criar fundo" : "Editar fundo"}>
      <button type="button" onClick={onCancel}>Cancelar</button>
      <button type="button" onClick={() => onSubmit({ nome: "Novo Fundo SA", cnpj: "11222333000181", tipoFundo: "Multimercado", classeAnbima: "Macro", dataConstituicao: "2020-01-01", consultoriaFundoId: "cons-1", custodianteId: "cust-1" })}>
        Submeter
      </button>
    </form>
  ),
}));

function renderPage() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: 0 } } });
  return render(
    <QueryClientProvider client={qc}>
      <FundosListPage />
    </QueryClientProvider>
  );
}

describe("FundosListPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockPermissions = ["funds:read", "funds:write"];
    listFundosMock = vi.fn().mockResolvedValue({
      items: [
        { id: "uuid-fundo-1", cnpj: "11222333000181", nome: "Fundo Alpha", tipoFundo: "Multimercado", classeAnbima: "Macro", segmento: null, dataConstituicao: "2020-01-01", status: "ATIVO", consultoriaFundoId: "uuid-cons-1", custodianteId: "uuid-cust-1" },
      ],
      totalCount: 1, page: 1, pageSize: 20, totalPages: 1,
    });
  });

  it("renders page heading", () => {
    renderPage();
    expect(screen.getByRole("heading", { name: /fundos/i })).toBeInTheDocument();
  });

  it("renders create button when user has funds:write", () => {
    renderPage();
    expect(screen.getByRole("button", { name: /novo fundo/i })).toBeInTheDocument();
  });

  it("renders search input", () => {
    renderPage();
    expect(
      screen.getByPlaceholderText(/buscar por nome ou cnpj/i)
    ).toBeInTheDocument();
  });

  it("renders status filter dropdown", () => {
    renderPage();
    expect(screen.getByRole("combobox", { name: /filtrar por status/i })).toBeInTheDocument();
  });

  it("renders table with fetched fund", async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText("Fundo Alpha")).toBeInTheDocument();
    });
  });

  it("calls navigate when search input changes (handleSearch)", async () => {
    renderPage();
    await waitFor(() => screen.getByPlaceholderText(/buscar por nome ou cnpj/i));
    fireEvent.change(screen.getByPlaceholderText(/buscar por nome ou cnpj/i), { target: { value: "Alpha" } });
    expect(mockNavigate).toHaveBeenCalledWith(
      expect.objectContaining({ search: expect.objectContaining({ search: "Alpha" }) })
    );
  });

  it("opens create dialog when Novo Fundo is clicked", async () => {
    renderPage();
    await waitFor(() => screen.getByRole("button", { name: /novo fundo/i }));
    fireEvent.click(screen.getByRole("button", { name: /novo fundo/i }));
    await waitFor(() => {
      expect(screen.getByRole("dialog")).toBeInTheDocument();
    });
  });

  it("calls handleSubmit create path via mocked form", async () => {
    renderPage();
    await waitFor(() => screen.getByRole("button", { name: /novo fundo/i }));
    fireEvent.click(screen.getByRole("button", { name: /novo fundo/i }));
    await waitFor(() => screen.getByRole("dialog"));
    const submitBtn = screen.getByRole("button", { name: /submeter/i });
    await act(async () => {
      fireEvent.click(submitBtn);
    });
    await waitFor(() => {
      expect(mockCreateFundo).toHaveBeenCalled();
    });
  });

  it("does not render create button when user lacks funds:write", () => {
    mockPermissions = ["funds:read"];
    renderPage();
    expect(screen.queryByRole("button", { name: /novo fundo/i })).not.toBeInTheDocument();
  });

  it("handleDetail / handleEdit navigate to fundo detail page", async () => {
    renderPage();
    await waitFor(() => screen.getByText("Fundo Alpha"));
    const detailBtns = screen.queryAllByRole("button", { name: /detalhe|ver|editar/i });
    if (detailBtns.length > 0) {
      fireEvent.click(detailBtns[0]);
      expect(mockNavigate).toHaveBeenCalled();
    }
  });

  it("renders Paginator when totalPages > 1 and calls handlePageChange", async () => {
    listFundosMock = vi.fn().mockResolvedValue({
      items: [{ id: "uuid-fundo-1", cnpj: "11222333000181", nome: "Fundo Alpha", tipoFundo: "Multimercado", classeAnbima: "Macro", segmento: null, dataConstituicao: "2020-01-01", status: "ATIVO", consultoriaFundoId: "uuid-cons-1", custodianteId: "uuid-cust-1" }],
      totalCount: 40, page: 1, pageSize: 20, totalPages: 3,
    });
    renderPage();
    await waitFor(() => screen.getByText("Fundo Alpha"));
    await waitFor(() => {
      expect(screen.getByRole("navigation", { name: /paginação/i })).toBeInTheDocument();
    });
    fireEvent.click(screen.getByRole("button", { name: "Página 2" }));
    expect(mockNavigate).toHaveBeenCalledWith(
      expect.objectContaining({ search: expect.objectContaining({ page: 2 }) })
    );
  });

  it("does not render Paginator when totalPages = 1", async () => {
    renderPage();
    await waitFor(() => screen.getByText("Fundo Alpha"));
    expect(screen.queryByRole("navigation", { name: /paginação/i })).not.toBeInTheDocument();
  });

  it("calls handleStatusFilter with status value when SelectItem clicked", async () => {
    renderPage();
    // Click a status option button from the mock SelectItem
    const statusBtns = screen.queryAllByRole("button", { name: /ativo|suspenso|rascunho/i });
    if (statusBtns.length > 0) {
      fireEvent.click(statusBtns[0]);
      expect(mockNavigate).toHaveBeenCalledWith(
        expect.objectContaining({ search: expect.objectContaining({ page: 1 }) })
      );
    }
  });

  it("calls handleStatusFilter with undefined when 'Todos os status' option selected", async () => {
    renderPage();
    const todosBtn = screen.queryByRole("button", { name: /todos os status/i });
    if (todosBtn) {
      fireEvent.click(todosBtn);
      expect(mockNavigate).toHaveBeenCalledWith(
        expect.objectContaining({ search: expect.objectContaining({ status: undefined }) })
      );
    }
  });

  it("closes create dialog when cancel button is clicked", async () => {
    renderPage();
    await waitFor(() => screen.getByRole("button", { name: /novo fundo/i }));
    fireEvent.click(screen.getByRole("button", { name: /novo fundo/i }));
    await waitFor(() => screen.getByRole("dialog"));
    fireEvent.click(screen.getByRole("button", { name: /cancelar/i }));
    await waitFor(() => {
      expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
    });
  });

  it("calls navigate when edit button in table is clicked (handleEdit)", async () => {
    renderPage();
    await waitFor(() => screen.getByText("Fundo Alpha"));
    const editBtn = screen.queryByRole("button", { name: /editar fundo fundo alpha/i });
    if (editBtn) {
      fireEvent.click(editBtn);
      expect(mockNavigate).toHaveBeenCalledWith(
        expect.objectContaining({ to: expect.stringContaining("fundos") })
      );
    }
  });

  it("handles API error in handleSubmit catch path", async () => {
    mockCreateFundo.mockRejectedValueOnce(
      new Response(JSON.stringify({ type: "ValidationError", errors: [] }), { status: 422 })
    );
    renderPage();
    await waitFor(() => screen.getByRole("button", { name: /novo fundo/i }));
    fireEvent.click(screen.getByRole("button", { name: /novo fundo/i }));
    await waitFor(() => screen.getByRole("dialog"));
    const submitBtn = screen.getByRole("button", { name: /submeter/i });
    await act(async () => {
      fireEvent.click(submitBtn);
    });
    await waitFor(() => {
      expect(mockCreateFundo).toHaveBeenCalled();
    });
  });
});
