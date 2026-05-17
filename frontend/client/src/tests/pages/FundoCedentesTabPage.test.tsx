// ---------------------------------------------------------------------------
// FundoCedentesTabPage.test.tsx — render + interaction (D-2, T-7)
// ---------------------------------------------------------------------------

import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, waitFor, fireEvent } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { FundoCedentesTabPage } from "@/components/pages/FundoCedentesTabPage";

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
  listFundoCedentes: vi.fn().mockResolvedValue({
    items: [
      { id: "assoc-1", fundoId: "fundo-1", cedenteId: "ced-1", limitePercentual: 10, limiteValor: null, dataInicio: "2024-01-01T00:00:00Z", dataFim: null, status: "ATIVO", createdAt: "2024-01-01T00:00:00Z" },
    ],
    totalCount: 1,
    page: 1,
    pageSize: 20,
    totalPages: 1,
  }),
  listCedentes: vi.fn().mockResolvedValue({ items: [{ id: "ced-1", nome: "João Silva" }], totalCount: 1, page: 1, pageSize: 100, totalPages: 1 }),
  createFundoCedente: vi.fn(),
  transitionFundoCedenteStatus: vi.fn(),
}));

vi.mock("@/lib/use-allowed-transitions", () => ({
  useFundoCedenteAllowedTransitions: () => ({ data: ["INATIVO"], isLoading: false }),
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
      <FundoCedentesTabPage fundoId="fundo-1" />
    </QueryClientProvider>
  );
}

describe("FundoCedentesTabPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("renders section heading", () => {
    renderPage();
    expect(screen.getByText(/cedentes associados/i)).toBeInTheDocument();
  });

  it("shows Associar Cedente button for users with funds:write", () => {
    renderPage();
    expect(screen.getByRole("button", { name: /associar cedente/i })).toBeInTheDocument();
  });

  it("renders association rows after fetch", async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText("10%")).toBeInTheDocument();
    });
  });

  it("opens create dialog on button click", async () => {
    renderPage();
    await waitFor(() => screen.getByRole("button", { name: /associar cedente/i }));
    fireEvent.click(screen.getByRole("button", { name: /associar cedente/i }));
    await waitFor(() => {
      expect(screen.getByText(/associar cedente ao fundo/i)).toBeInTheDocument();
    });
  });
});
