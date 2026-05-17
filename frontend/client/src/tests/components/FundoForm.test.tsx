// ---------------------------------------------------------------------------
// FundoForm.test.tsx — render + interaction + a11y (D-2)
// ---------------------------------------------------------------------------

import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { FundoForm } from "@/components/organisms/FundoForm";

vi.mock("@/lib/fundos-api", () => ({
  listConsultoriasFundo: vi.fn().mockResolvedValue({ items: [{ id: "cons-1", razaoSocial: "Consultoria SA" }], totalCount: 1, page: 1, pageSize: 100, totalPages: 1 }),
  listCustodiantes: vi.fn().mockResolvedValue({ items: [{ id: "cust-1", razaoSocial: "Custodiante SA" }], totalCount: 1, page: 1, pageSize: 100, totalPages: 1 }),
}));

function renderForm(props: Partial<React.ComponentProps<typeof FundoForm>> = {}) {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: 0 } } });
  return render(
    <QueryClientProvider client={qc}>
      <FundoForm
        mode="create"
        onSubmit={vi.fn()}
        onCancel={vi.fn()}
        {...props}
      />
    </QueryClientProvider>
  );
}

describe("FundoForm — create mode", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("renders form with create label", () => {
    renderForm();
    expect(screen.getByRole("form", { name: /criar fundo/i })).toBeInTheDocument();
  });

  it("renders nome, cnpj fields", () => {
    renderForm();
    expect(screen.getByLabelText(/nome do fundo/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/cnpj/i)).toBeInTheDocument();
  });

  it("renders Criar Fundo submit button", () => {
    renderForm();
    expect(screen.getByRole("button", { name: /criar fundo/i })).toBeInTheDocument();
  });

  it("calls onCancel when cancel button clicked", () => {
    const onCancel = vi.fn();
    renderForm({ onCancel });
    fireEvent.click(screen.getByRole("button", { name: /cancelar/i }));
    expect(onCancel).toHaveBeenCalled();
  });

  it("shows validation error when nome is empty on submit", async () => {
    renderForm();
    fireEvent.click(screen.getByRole("button", { name: /criar fundo/i }));
    await waitFor(() => {
      const errors = screen.getAllByText(/campo obrigatório/i);
      expect(errors.length).toBeGreaterThan(0);
    });
  });

  it("disables submit button when isSubmitting", () => {
    renderForm({ isSubmitting: true });
    expect(screen.getByRole("button", { name: /criar fundo/i })).toBeDisabled();
  });
});

describe("FundoForm — edit mode", () => {
  const initial = {
    id: "uuid-fundo-1",
    nome: "Fundo Alpha",
    cnpj: "11222333000181",
    consultoriaFundoId: "uuid-cons-1",
    custodianteId: "uuid-cust-1",
    tipoFundo: "Multimercado" as const,
    classeAnbima: "Macro",
    segmento: null,
    dataConstituicao: "2020-01-01",
    status: "ATIVO" as const,
    createdAt: "2020-01-01T00:00:00Z",
  };

  it("renders Salvar alterações button in edit mode", () => {
    renderForm({ mode: "edit", initial });
    expect(screen.getByRole("button", { name: /salvar alterações/i })).toBeInTheDocument();
  });

  it("does not render CNPJ field in edit mode", () => {
    renderForm({ mode: "edit", initial });
    expect(screen.queryByLabelText(/cnpj/i)).not.toBeInTheDocument();
  });

  it("pre-fills nome from initial data", () => {
    renderForm({ mode: "edit", initial });
    expect(screen.getByLabelText(/nome do fundo/i)).toHaveValue("Fundo Alpha");
  });
});
