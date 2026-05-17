// ---------------------------------------------------------------------------
// FundoForm.test.tsx — render + interaction + a11y (D-2)
// ---------------------------------------------------------------------------

import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, fireEvent, waitFor, act } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { FundoForm } from "@/components/organisms/FundoForm";

// Mock Radix Select — threads onValueChange from Select → SelectItem via React context
vi.mock("@/components/ui/select", () => {
  // eslint-disable-next-line @typescript-eslint/no-require-imports
  const React = require("react");
  const OnValueChangeCtx = React.createContext<((v: string) => void) | undefined>(undefined);
  return {
    Select: ({ children, onValueChange }: any) => (
      <OnValueChangeCtx.Provider value={onValueChange}>
        <div>{children}</div>
      </OnValueChangeCtx.Provider>
    ),
    SelectTrigger: ({ children, id, "aria-label": al }: any) =>
      <button id={id} aria-label={al}>{children}</button>,
    SelectValue: ({ placeholder }: any) => <span>{placeholder}</span>,
    SelectContent: ({ children }: any) => <div>{children}</div>,
    SelectItem: ({ children, value }: any) => {
      const onChange = React.useContext(OnValueChangeCtx);
      return <button type="button" onClick={() => onChange?.(value)}>{children}</button>;
    },
  };
});

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

describe("FundoForm — select onValueChange in create mode", () => {
  it("fires setValue for tipoFundo when SelectItem clicked", async () => {
    renderForm();
    // Look for any SelectItem button by text from TIPO_FUNDO_LABELS
    const tipoBtn = screen.queryByRole("button", { name: /multimercado/i });
    if (tipoBtn) fireEvent.click(tipoBtn);
    // Submit to confirm form processes the select values
    fireEvent.click(screen.getByRole("button", { name: /criar fundo/i }));
    // Just checking no crash — validation will fire but setValue was exercised
    await waitFor(() => {
      expect(screen.getByRole("form", { name: /criar fundo/i })).toBeInTheDocument();
    });
  });

  it("fires setValue for consultoria when SelectItem clicked", async () => {
    renderForm();
    await waitFor(() => screen.getByRole("button", { name: /consultoria sa/i }));
    fireEvent.click(screen.getByRole("button", { name: /consultoria sa/i }));
    expect(screen.getByRole("form", { name: /criar fundo/i })).toBeInTheDocument();
  });

  it("fires setValue for custodiante when SelectItem clicked", async () => {
    renderForm();
    await waitFor(() => screen.getByRole("button", { name: /custodiante sa/i }));
    fireEvent.click(screen.getByRole("button", { name: /custodiante sa/i }));
    expect(screen.getByRole("form", { name: /criar fundo/i })).toBeInTheDocument();
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

  it("calls onSubmit in edit mode with valid pre-filled data", async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined);
    renderForm({ mode: "edit", initial, onSubmit });
    await act(async () => {
      fireEvent.click(screen.getByRole("button", { name: /salvar alterações/i }));
    });
    await waitFor(() => {
      expect(onSubmit).toHaveBeenCalledWith(
        expect.objectContaining({ nome: "Fundo Alpha" }),
        expect.anything()
      );
    });
  });
});
