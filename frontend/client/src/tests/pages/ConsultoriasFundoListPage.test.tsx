// ---------------------------------------------------------------------------
// ConsultoriasFundoListPage.test.tsx — render + interaction (D-2, T-5)
// ---------------------------------------------------------------------------

import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { ConsultoriasFundoListPage } from "@/components/pages/ConsultoriasFundoListPage";

vi.mock("@tanstack/react-router", async (importOriginal) => {
  const mod = await importOriginal<typeof import("@tanstack/react-router")>();
  return {
    ...mod,
    useSearch: () => ({ page: 1, search: "" }),
    useNavigate: () => () => {},
  };
});

vi.mock("@/lib/auth-context", () => ({
  useAuth: () => ({
    auth: {
      isAuthenticated: true,
      accessGroup: "admin-empresa",
      permissions: ["funds:read", "funds:write"],
      companyId: "company-1",
    },
  }),
}));

vi.mock("@/lib/fundos-api", () => ({
  listConsultoriasFundo: vi.fn().mockResolvedValue({
    items: [
      { id: "cons-1", razaoSocial: "Consultoria Alpha SA", nomeFantasia: null, cnpj: "11222333000181", email: null, telefone: null, status: "ATIVO", createdAt: "2020-01-01T00:00:00Z" },
    ],
    totalCount: 1,
    page: 1,
    pageSize: 20,
    totalPages: 1,
  }),
  createConsultoriaFundo: vi.fn(),
  updateConsultoriaFundo: vi.fn(),
}));

// Mock forms to avoid Radix issues
vi.mock("@/components/organisms/ConsultoriaFundoForm", () => ({
  ConsultoriaFundoForm: () => <form aria-label="Criar consultoria de fundo" />,
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
});
