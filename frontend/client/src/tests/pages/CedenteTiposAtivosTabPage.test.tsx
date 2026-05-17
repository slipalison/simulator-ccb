// ---------------------------------------------------------------------------
// CedenteTiposAtivosTabPage.test.tsx — render + interaction (D-2, T-7)
// ---------------------------------------------------------------------------

import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, waitFor, fireEvent } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { CedenteTiposAtivosTabPage } from "@/components/pages/CedenteTiposAtivosTabPage";

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
  listCedenteTiposAtivo: vi.fn().mockResolvedValue({
    items: [
      { id: "assoc-1", cedenteId: "ced-1", tipoAtivoId: "tipo-1", limitePercentual: null, limiteValor: null, dataInicio: "2024-01-01T00:00:00Z", dataFim: null, status: "ATIVO", createdAt: "2024-01-01T00:00:00Z" },
    ],
    totalCount: 1,
    page: 1,
    pageSize: 20,
    totalPages: 1,
  }),
  listTiposAtivo: vi.fn().mockResolvedValue({ items: [], totalCount: 0, page: 1, pageSize: 100, totalPages: 0 }),
  createCedenteTipoAtivo: vi.fn(),
  transitionCedenteTipoAtivoStatus: vi.fn(),
}));

vi.mock("@/lib/use-allowed-transitions", () => ({
  useCedenteTipoAtivoAllowedTransitions: () => ({ data: ["INATIVO"], isLoading: false }),
}));

vi.mock("@/components/organisms/StatusTransitionDropdown", () => ({
  StatusTransitionDropdown: () => <button>Transição</button>,
}));

vi.mock("@/components/organisms/AssociationForm", () => ({
  AssociationForm: () => <form aria-label="Nova associação" />,
}));

function renderPage() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: 0 } } });
  return render(
    <QueryClientProvider client={qc}>
      <CedenteTiposAtivosTabPage cedenteId="ced-1" />
    </QueryClientProvider>
  );
}

describe("CedenteTiposAtivosTabPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("renders section heading", () => {
    renderPage();
    expect(screen.getByText(/tipos de ativo associados/i)).toBeInTheDocument();
  });

  it("shows Associar Tipo de Ativo button for users with funds:write", () => {
    renderPage();
    expect(screen.getByRole("button", { name: /associar tipo de ativo/i })).toBeInTheDocument();
  });

  it("renders association row after fetch", async () => {
    renderPage();
    await waitFor(() => {
      // row status badge
      expect(screen.getByText("Ativo")).toBeInTheDocument();
    });
  });

  it("opens create dialog on button click", async () => {
    renderPage();
    await waitFor(() => screen.getByRole("button", { name: /associar tipo de ativo/i }));
    fireEvent.click(screen.getByRole("button", { name: /associar tipo de ativo/i }));
    await waitFor(() => {
      expect(screen.getByText(/associar tipo de ativo ao cedente/i)).toBeInTheDocument();
    });
  });
});
