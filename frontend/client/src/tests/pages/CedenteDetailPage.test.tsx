// ---------------------------------------------------------------------------
// CedenteDetailPage.test.tsx — render + interaction + a11y (D-2, T-4)
// ---------------------------------------------------------------------------

import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { CedenteDetailPage } from "@/components/pages/CedenteDetailPage";

vi.mock("@tanstack/react-router", async (importOriginal) => {
  const mod = await importOriginal<typeof import("@tanstack/react-router")>();
  return {
    ...mod,
    useParams: () => ({ cedenteId: "uuid-ced-1" }),
    useNavigate: () => vi.fn(),
  };
});

vi.mock("@/lib/fundos-api", () => ({
  getCedente: vi.fn().mockResolvedValue({
    id: "uuid-ced-1",
    documento: "12345678901",
    nome: "João Silva",
    email: "joao@example.com",
    telefone: null,
    endereco: null,
    cedenteTipo: "PF",
    status: "ATIVO",
    createdAt: "2020-01-01T00:00:00Z",
  }),
  listCedenteTiposAtivo: vi.fn().mockResolvedValue({ items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0 }),
}));

vi.mock("@/lib/use-allowed-transitions", () => ({
  useCedenteTipoAtivoAllowedTransitions: () => ({ data: [], isLoading: false }),
}));

vi.mock("@/components/pages/CedenteTiposAtivosTabPage", () => ({
  CedenteTiposAtivosTabPage: () => <div data-testid="tipos-ativos-tab">Tipos Ativos</div>,
}));

function renderPage() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: 0 } } });
  return render(
    <QueryClientProvider client={qc}>
      <CedenteDetailPage />
    </QueryClientProvider>
  );
}

describe("CedenteDetailPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("renders cedente name after loading", async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByRole("heading", { name: /joão silva/i })).toBeInTheDocument();
    });
  });

  it("renders cedente tipo and document", async () => {
    renderPage();
    await waitFor(() => {
      // "PF · 12345678901" is rendered as a single paragraph
      expect(screen.getByText(/pf/i)).toBeInTheDocument();
      expect(screen.getByText(/12345678901/)).toBeInTheDocument();
    });
  });

  it("renders status badge (ATIVO)", async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText("Ativo")).toBeInTheDocument();
    });
  });

  it("renders dados section with email", async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByRole("region", { name: /dados do cedente/i })).toBeInTheDocument();
      expect(screen.getByText("joao@example.com")).toBeInTheDocument();
    });
  });

  it("renders Tipos de Ativo section", async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId("tipos-ativos-tab")).toBeInTheDocument();
    });
  });

  it("renders back navigation button", async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByRole("button", { name: /voltar para lista de cedentes/i })).toBeInTheDocument();
    });
  });
});
