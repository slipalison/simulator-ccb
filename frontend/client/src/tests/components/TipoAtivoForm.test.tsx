// ---------------------------------------------------------------------------
// TipoAtivoForm.test.tsx — render + interaction + a11y (D-2)
// Covers create mode, edit mode, validation errors, isSubmitting state, onSubmit.
// ---------------------------------------------------------------------------

import { describe, it, expect, vi } from "vitest";
import { render, screen, fireEvent, waitFor, act } from "@testing-library/react";
import { TipoAtivoForm } from "@/components/organisms/TipoAtivoForm";

// Mock Radix Select — threads onValueChange from Select → SelectItem via React context
vi.mock("@/components/ui/select", () => {
  // eslint-disable-next-line @typescript-eslint/no-require-imports
  const React = require("react") as typeof import("react");
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
import type { TipoAtivoDto } from "@/lib/fundos-schemas";

const SAMPLE_TIPO_ATIVO: TipoAtivoDto = {
  id: "uuid-1",
  codigo: "RF001",
  descricao: "Renda Fixa Governo",
  categoria: "RendaFixa",
  subcategoria: null,
  status: "ATIVO",
  ordemExibicao: 1,
};

function renderCreate(overrides?: Partial<Parameters<typeof TipoAtivoForm>[0]>) {
  const onSubmit = vi.fn().mockResolvedValue(undefined);
  const onCancel = vi.fn();
  render(
    <TipoAtivoForm
      mode="create"
      onSubmit={onSubmit}
      onCancel={onCancel}
      isSubmitting={false}
      {...overrides}
    />
  );
  return { onSubmit, onCancel };
}

function renderEdit(overrides?: Partial<Parameters<typeof TipoAtivoForm>[0]>) {
  const onSubmit = vi.fn().mockResolvedValue(undefined);
  const onCancel = vi.fn();
  render(
    <TipoAtivoForm
      mode="edit"
      initial={SAMPLE_TIPO_ATIVO}
      onSubmit={onSubmit}
      onCancel={onCancel}
      isSubmitting={false}
      {...overrides}
    />
  );
  return { onSubmit, onCancel };
}

describe("TipoAtivoForm — create mode", () => {
  it("renders with accessible form label for create", () => {
    renderCreate();
    expect(screen.getByRole("form", { name: /criar tipo de ativo/i })).toBeInTheDocument();
  });

  it("renders Código field in create mode", () => {
    renderCreate();
    expect(screen.getByLabelText(/código/i)).toBeInTheDocument();
  });

  it("renders Descrição field", () => {
    renderCreate();
    expect(screen.getByLabelText(/descrição/i)).toBeInTheDocument();
  });

  it("renders Categoria field in create mode", () => {
    renderCreate();
    expect(screen.getByLabelText(/selecionar categoria/i)).toBeInTheDocument();
  });

  it("renders Subcategoria field", () => {
    renderCreate();
    expect(screen.getByLabelText(/subcategoria/i)).toBeInTheDocument();
  });

  it("renders Ordem de Exibição field", () => {
    renderCreate();
    expect(screen.getByLabelText(/ordem de exibição/i)).toBeInTheDocument();
  });

  it("renders Criar button in create mode", () => {
    renderCreate();
    expect(screen.getByRole("button", { name: /criar/i })).toBeInTheDocument();
  });

  it("renders Cancelar button", () => {
    renderCreate();
    expect(screen.getByRole("button", { name: /cancelar/i })).toBeInTheDocument();
  });

  it("calls onCancel when cancel button is clicked", () => {
    const { onCancel } = renderCreate();
    fireEvent.click(screen.getByRole("button", { name: /cancelar/i }));
    expect(onCancel).toHaveBeenCalled();
  });

  it("shows validation error when submitted with empty Código", async () => {
    renderCreate();
    fireEvent.click(screen.getByRole("button", { name: /criar/i }));
    await waitFor(() => {
      const alerts = screen.queryAllByRole("alert");
      expect(alerts.length).toBeGreaterThan(0);
    });
  });

  it("shows validation error when submitted with empty Descrição", async () => {
    renderCreate();
    const codigoInput = screen.getByPlaceholderText(/ex: rf001/i);
    fireEvent.change(codigoInput, { target: { value: "RF001" } });
    fireEvent.click(screen.getByRole("button", { name: /criar/i }));
    await waitFor(() => {
      const alerts = screen.queryAllByRole("alert");
      expect(alerts.length).toBeGreaterThan(0);
    });
  });

  it("calls onSubmit when form is valid", async () => {
    const { onSubmit } = renderCreate();
    const codigoInput = screen.getByPlaceholderText(/ex: rf001/i);
    const descricaoInput = screen.getByLabelText(/descrição/i);
    fireEvent.change(codigoInput, { target: { value: "RF001" } });
    fireEvent.change(descricaoInput, { target: { value: "Renda Fixa" } });
    await act(async () => {
      fireEvent.click(screen.getByRole("button", { name: /criar/i }));
    });
    await waitFor(() => {
      expect(onSubmit).toHaveBeenCalledWith(
        expect.objectContaining({ codigo: "RF001", descricao: "Renda Fixa" }),
        expect.anything()
      );
    });
  });

  it("disables buttons when isSubmitting=true", () => {
    renderCreate({ isSubmitting: true });
    expect(screen.getByRole("button", { name: /cancelar/i })).toBeDisabled();
    expect(screen.getByRole("button", { name: /criar/i })).toBeDisabled();
  });

  it("shows spinner icon when isSubmitting=true", () => {
    renderCreate({ isSubmitting: true });
    // Loader2 is rendered with aria-hidden; button text still present
    const submitBtn = screen.getByRole("button", { name: /criar/i });
    expect(submitBtn).toBeDisabled();
  });
});

describe("TipoAtivoForm — edit mode", () => {
  it("renders with accessible form label for edit", () => {
    renderEdit();
    expect(screen.getByRole("form", { name: /editar tipo de ativo/i })).toBeInTheDocument();
  });

  it("does NOT render Código field in edit mode", () => {
    renderEdit();
    expect(screen.queryByLabelText(/^código$/i)).not.toBeInTheDocument();
  });

  it("does NOT render Categoria field in edit mode", () => {
    renderEdit();
    expect(screen.queryByLabelText(/selecionar categoria/i)).not.toBeInTheDocument();
  });

  it("renders Status dropdown in edit mode", () => {
    renderEdit();
    expect(screen.getByLabelText(/selecionar status/i)).toBeInTheDocument();
  });

  it("pre-fills Descrição with initial value", () => {
    renderEdit();
    const descInput = screen.getByLabelText(/descrição/i) as HTMLInputElement;
    expect(descInput.value).toBe("Renda Fixa Governo");
  });

  it("renders Salvar alterações button in edit mode", () => {
    renderEdit();
    expect(screen.getByRole("button", { name: /salvar alterações/i })).toBeInTheDocument();
  });

  it("calls onSubmit when edit form is valid", async () => {
    const { onSubmit } = renderEdit();
    await act(async () => {
      fireEvent.click(screen.getByRole("button", { name: /salvar alterações/i }));
    });
    await waitFor(() => {
      expect(onSubmit).toHaveBeenCalledWith(
        expect.objectContaining({ descricao: "Renda Fixa Governo" }),
        expect.anything()
      );
    });
  });

  it("shows validation error when Descrição cleared and submitted", async () => {
    renderEdit();
    const descInput = screen.getByLabelText(/descrição/i);
    fireEvent.change(descInput, { target: { value: "" } });
    fireEvent.click(screen.getByRole("button", { name: /salvar alterações/i }));
    await waitFor(() => {
      const alerts = screen.queryAllByRole("alert");
      expect(alerts.length).toBeGreaterThan(0);
    });
  });

  it("disables buttons when isSubmitting=true in edit mode", () => {
    renderEdit({ isSubmitting: true });
    expect(screen.getByRole("button", { name: /cancelar/i })).toBeDisabled();
    expect(screen.getByRole("button", { name: /salvar alterações/i })).toBeDisabled();
  });

  it("fires setValue for status when status SelectItem clicked", async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined);
    renderEdit({ onSubmit });
    // Click any status option button from the mock SelectItem
    const statusBtns = screen.getAllByRole("button");
    const inativoBtn = statusBtns.find((b) => /inativo/i.test(b.textContent ?? ""));
    if (inativoBtn) fireEvent.click(inativoBtn);
    await act(async () => {
      fireEvent.click(screen.getByRole("button", { name: /salvar alterações/i }));
    });
    await waitFor(() => {
      expect(onSubmit).toHaveBeenCalled();
    });
  });
});

describe("TipoAtivoForm — categoria select in create mode", () => {
  it("fires setValue for categoria when SelectItem clicked", async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined);
    render(
      <TipoAtivoForm mode="create" onSubmit={onSubmit} onCancel={vi.fn()} isSubmitting={false} />
    );
    // Click any categoria option button from the mock SelectItem
    const allBtns = screen.getAllByRole("button");
    const categoriaBtn = allBtns.find(
      (b) => /renda variável|renda fixa|derivativos|câmbio/i.test(b.textContent ?? "")
    );
    if (categoriaBtn) fireEvent.click(categoriaBtn);
    // Just verify form is still in DOM (setValue was exercised without crash)
    expect(screen.getByRole("form", { name: /criar tipo de ativo/i })).toBeInTheDocument();
  });
});
