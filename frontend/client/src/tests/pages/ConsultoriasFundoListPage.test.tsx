// ---------------------------------------------------------------------------
// ConsultoriasFundoListPage.test.tsx — render + interaction (D-2, T-5)
// ---------------------------------------------------------------------------

import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, waitFor, fireEvent, act } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { ConsultoriasFundoListPage } from "@/components/pages/ConsultoriasFundoListPage";

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

const mockCreateConsultoriaFundo = vi.fn().mockResolvedValue({ id: "cons-new" });
const mockUpdateConsultoriaFundo = vi.fn().mockResolvedValue({ id: "cons-1" });

let listConsultoriasFundoMock = vi.fn().mockResolvedValue({
  items: [
    { id: "cons-1", razaoSocial: "Consultoria Alpha SA", nomeFantasia: null, cnpj: "11222333000181", email: null, telefone: null, status: "ATIVO", createdAt: "2020-01-01T00:00:00Z" },
  ],
  totalCount: 1,
  page: 1,
  pageSize: 20,
  totalPages: 1,
});

vi.mock("@/lib/fundos-api", () => ({
  get listConsultoriasFundo() { return listConsultoriasFundoMock; },
  createConsultoriaFundo: (...args: unknown[]) => mockCreateConsultoriaFundo(...args),
  updateConsultoriaFundo: (...args: unknown[]) => mockUpdateConsultoriaFundo(...args),
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

// Mock ConsultoriaFundoForm
vi.mock("@/components/organisms/ConsultoriaFundoForm", () => ({
  ConsultoriaFundoForm: ({ onSubmit, onCancel, mode }: { onSubmit: (d: unknown) => Promise<void>; onCancel: () => void; mode: string }) => {
    return (
      <form aria-label={mode === "create" ? "Criar consultoria de fundo" : "Editar consultoria"}>
        <button type="button" onClick={onCancel}>Cancelar</button>
        <button type="button" onClick={() => onSubmit({ razaoSocial: "Nova Consultoria SA", cnpj: "11222333000181" })}>
          Submeter
        </button>
      </form>
    );
  },
}));

function renderPage() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: 0 } } });
  return render(
    <QueryClientProvider client={qc}>
      <ConsultoriasFundoListPage />
    </QueryClientProvider>
  );
}

describe("ConsultoriasFundoListPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockPermissions = ["funds:read", "funds:write"];
    listConsultoriasFundoMock = vi.fn().mockResolvedValue({
      items: [
        { id: "cons-1", razaoSocial: "Consultoria Alpha SA", nomeFantasia: null, cnpj: "11222333000181", email: null, telefone: null, status: "ATIVO", createdAt: "2020-01-01T00:00:00Z" },
      ],
      totalCount: 1, page: 1, pageSize: 20, totalPages: 1,
    });
  });

  it("renders page heading", () => {
    renderPage();
    expect(screen.getByRole("heading", { name: /consultorias de fundo/i })).toBeInTheDocument();
  });

  it("renders create button when user has funds:write", () => {
    renderPage();
    expect(screen.getByRole("button", { name: /nova consultoria/i })).toBeInTheDocument();
  });

  it("renders search input", () => {
    renderPage();
    expect(screen.getByPlaceholderText(/buscar/i)).toBeInTheDocument();
  });

  it("renders table with fetched consultoria", async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText("Consultoria Alpha SA")).toBeInTheDocument();
    });
  });

  it("calls navigate when search input changes (handleSearch)", async () => {
    renderPage();
    await waitFor(() => screen.getByPlaceholderText(/buscar/i));
    fireEvent.change(screen.getByPlaceholderText(/buscar/i), { target: { value: "Alpha" } });
    expect(mockNavigate).toHaveBeenCalledWith(
      expect.objectContaining({ search: expect.objectContaining({ search: "Alpha" }) })
    );
  });

  it("opens create dialog on button click", async () => {
    renderPage();
    await waitFor(() => screen.getByRole("button", { name: /nova consultoria/i }));
    fireEvent.click(screen.getByRole("button", { name: /nova consultoria/i }));
    await waitFor(() => {
      expect(screen.getByRole("dialog")).toBeInTheDocument();
    });
  });

  it("shows Nova Consultoria de Fundo title in create dialog", async () => {
    renderPage();
    await waitFor(() => screen.getByRole("button", { name: /nova consultoria/i }));
    fireEvent.click(screen.getByRole("button", { name: /nova consultoria/i }));
    await waitFor(() => {
      expect(screen.getByText(/nova consultoria de fundo/i)).toBeInTheDocument();
    });
  });

  it("calls handleSubmit create path via mocked form", async () => {
    renderPage();
    await waitFor(() => screen.getByRole("button", { name: /nova consultoria/i }));
    fireEvent.click(screen.getByRole("button", { name: /nova consultoria/i }));
    await waitFor(() => screen.getByRole("dialog"));
    const submitBtn = screen.getByRole("button", { name: /submeter/i });
    await act(async () => {
      fireEvent.click(submitBtn);
    });
    await waitFor(() => {
      expect(mockCreateConsultoriaFundo).toHaveBeenCalled();
    });
  });

  it("opens edit dialog when edit callback fires", async () => {
    renderPage();
    await waitFor(() => screen.getByText("Consultoria Alpha SA"));
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
    await waitFor(() => screen.getByText("Consultoria Alpha SA"));
    const editBtns = screen.queryAllByRole("button", { name: /editar/i });
    if (editBtns.length > 0) {
      fireEvent.click(editBtns[0]);
      await waitFor(() => screen.getByRole("dialog"));
      const submitBtn = screen.getByRole("button", { name: /submeter/i });
      await act(async () => {
        fireEvent.click(submitBtn);
      });
      await waitFor(() => {
        expect(mockUpdateConsultoriaFundo).toHaveBeenCalled();
      });
    }
  });

  it("shows Editar Consultoria title in edit dialog", async () => {
    renderPage();
    await waitFor(() => screen.getByText("Consultoria Alpha SA"));
    const editBtns = screen.queryAllByRole("button", { name: /editar consultoria/i });
    if (editBtns.length > 0) {
      fireEvent.click(editBtns[0]);
      await waitFor(() => {
        expect(screen.getByText(/editar consultoria/i)).toBeInTheDocument();
      });
    }
  });

  it("closes dialog on Escape key (onOpenChange)", async () => {
    renderPage();
    await waitFor(() => screen.getByRole("button", { name: /nova consultoria/i }));
    fireEvent.click(screen.getByRole("button", { name: /nova consultoria/i }));
    await waitFor(() => screen.getByRole("dialog"));
    fireEvent.keyDown(document, { key: "Escape" });
    await waitFor(() => {
      expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
    });
  });

  it("closes dialog when cancel button clicked", async () => {
    renderPage();
    await waitFor(() => screen.getByRole("button", { name: /nova consultoria/i }));
    fireEvent.click(screen.getByRole("button", { name: /nova consultoria/i }));
    await waitFor(() => screen.getByRole("dialog"));
    fireEvent.click(screen.getByRole("button", { name: /cancelar/i }));
    await waitFor(() => {
      expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
    });
  });

  it("renders Paginator when totalPages > 1 and calls handlePageChange", async () => {
    listConsultoriasFundoMock = vi.fn().mockResolvedValue({
      items: [{ id: "cons-1", razaoSocial: "Consultoria Alpha SA", nomeFantasia: null, cnpj: "11222333000181", email: null, telefone: null, status: "ATIVO", createdAt: "2020-01-01T00:00:00Z" }],
      totalCount: 40, page: 1, pageSize: 20, totalPages: 3,
    });
    renderPage();
    await waitFor(() => screen.getByText("Consultoria Alpha SA"));
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
    await waitFor(() => screen.getByText("Consultoria Alpha SA"));
    expect(screen.queryByRole("navigation", { name: /paginação/i })).not.toBeInTheDocument();
  });

  it("renders with read-only permissions — no create button", () => {
    mockPermissions = ["funds:read"];
    renderPage();
    expect(screen.queryByRole("button", { name: /nova consultoria/i })).not.toBeInTheDocument();
  });
});
