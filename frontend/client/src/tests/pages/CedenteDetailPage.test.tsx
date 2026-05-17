// ---------------------------------------------------------------------------
// CedenteDetailPage.test.tsx — render + interaction + a11y (D-2, T-4)
// ---------------------------------------------------------------------------

import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, waitFor, fireEvent } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { CedenteDetailPage } from "@/components/pages/CedenteDetailPage";

const mockNavigate = vi.fn();

vi.mock("@/router", () => ({
  cedenteDetailRouteApi: {
    useParams: () => ({ cedenteId: "uuid-ced-1" }),
  },
}));
// useNavigate is still imported directly from @tanstack/react-router in CedenteDetailPage
vi.mock("@tanstack/react-router", async (importOriginal) => {
  const mod = await importOriginal<typeof import("@tanstack/react-router")>();
  return {
    ...mod,
    useNavigate: () => mockNavigate,
  };
});

let getCedenteMock = vi.fn().mockResolvedValue({
  id: "uuid-ced-1",
  documento: "12345678901",
  nome: "João Silva",
  email: "joao@example.com",
  telefone: null,
  endereco: null,
  cedenteTipo: "PF",
  status: "ATIVO",
  createdAt: "2020-01-01T00:00:00Z",
});

vi.mock("@/lib/fundos-api", () => ({
  get getCedente() { return getCedenteMock; },
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
    getCedenteMock = vi.fn().mockResolvedValue({
      id: "uuid-ced-1",
      documento: "12345678901",
      nome: "João Silva",
      email: "joao@example.com",
      telefone: null,
      endereco: null,
      cedenteTipo: "PF",
      status: "ATIVO",
      createdAt: "2020-01-01T00:00:00Z",
    });
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

  it("back button calls navigate to /cedentes", async () => {
    renderPage();
    await waitFor(() => screen.getByRole("button", { name: /voltar para lista de cedentes/i }));
    fireEvent.click(screen.getByRole("button", { name: /voltar para lista de cedentes/i }));
    expect(mockNavigate).toHaveBeenCalledWith(
      expect.objectContaining({ to: "/cedentes" })
    );
  });

  it("renders 'Cedente não encontrado' when query returns null", async () => {
    getCedenteMock = vi.fn().mockResolvedValue(null);
    renderPage();
    await waitFor(() => {
      expect(screen.getByText(/cedente não encontrado/i)).toBeInTheDocument();
    });
  });

  it("renders telefone fallback dash when null", async () => {
    renderPage();
    await waitFor(() => {
      // telefone is null → renders "—"
      const dds = screen.getAllByText("—");
      expect(dds.length).toBeGreaterThan(0);
    });
  });

  it("renders status as non-ATIVO correctly (INATIVO)", async () => {
    getCedenteMock = vi.fn().mockResolvedValue({
      id: "uuid-ced-1",
      documento: "12345678901",
      nome: "João Inativo",
      email: null,
      telefone: null,
      endereco: null,
      cedenteTipo: "PF",
      status: "INATIVO",
      createdAt: "2020-01-01T00:00:00Z",
    });
    renderPage();
    await waitFor(() => {
      expect(screen.getByText("Inativo")).toBeInTheDocument();
    });
  });
});
