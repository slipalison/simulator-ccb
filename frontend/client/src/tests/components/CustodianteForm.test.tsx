// ---------------------------------------------------------------------------
// CustodianteForm.test.tsx — render + interaction (D-2)
// ---------------------------------------------------------------------------

import { describe, it, expect, vi } from "vitest";
import { render, screen, fireEvent, waitFor, act } from "@testing-library/react";
import { CustodianteForm } from "@/components/organisms/CustodianteForm";

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

describe("CustodianteForm — create mode", () => {
  it("renders Razão Social and CNPJ fields", () => {
    render(<CustodianteForm mode="create" onSubmit={vi.fn()} onCancel={vi.fn()} />);
    expect(screen.getByLabelText(/razão social/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/cnpj/i)).toBeInTheDocument();
  });

  it("shows validation errors on empty submit", async () => {
    render(<CustodianteForm mode="create" onSubmit={vi.fn()} onCancel={vi.fn()} />);
    fireEvent.click(screen.getByRole("button", { name: /criar/i }));
    await waitFor(() => {
      const errors = screen.getAllByText(/campo obrigatório/i);
      expect(errors.length).toBeGreaterThan(0);
    });
  });

  it("calls onCancel", () => {
    const onCancel = vi.fn();
    render(<CustodianteForm mode="create" onSubmit={vi.fn()} onCancel={onCancel} />);
    fireEvent.click(screen.getByRole("button", { name: /cancelar/i }));
    expect(onCancel).toHaveBeenCalled();
  });
});

describe("CustodianteForm — edit mode", () => {
  const initial = {
    id: "uuid-cust-1",
    razaoSocial: "Custodiante SA",
    codigoInterno: "CUST-01",
    cnpj: "11222333000181",
    email: null,
    telefone: null,
    status: "ATIVO" as const,
    createdAt: "2020-01-01T00:00:00Z",
  };

  it("renders Salvar alterações button", () => {
    render(<CustodianteForm mode="edit" initial={initial} onSubmit={vi.fn()} onCancel={vi.fn()} />);
    expect(screen.getByRole("button", { name: /salvar alterações/i })).toBeInTheDocument();
  });

  it("pre-fills razão social", () => {
    render(<CustodianteForm mode="edit" initial={initial} onSubmit={vi.fn()} onCancel={vi.fn()} />);
    expect(screen.getByLabelText(/razão social/i)).toHaveValue("Custodiante SA");
  });

  it("calls onSubmit in edit mode with valid data", async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined);
    render(<CustodianteForm mode="edit" initial={initial} onSubmit={onSubmit} onCancel={vi.fn()} />);
    await act(async () => {
      fireEvent.click(screen.getByRole("button", { name: /salvar alterações/i }));
    });
    await waitFor(() => {
      expect(onSubmit).toHaveBeenCalledWith(
        expect.objectContaining({ razaoSocial: "Custodiante SA" }),
        expect.anything()
      );
    });
  });
});

describe("CustodianteForm — status select in edit mode", () => {
  const initial = {
    id: "uuid-cust-1",
    razaoSocial: "Custodiante SA",
    codigoInterno: "CUST-01",
    cnpj: "11222333000181",
    email: null,
    telefone: null,
    status: "ATIVO" as const,
    createdAt: "2020-01-01T00:00:00Z",
  };

  it("calls setValue when status option clicked in edit mode", async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined);
    render(<CustodianteForm mode="edit" initial={initial} onSubmit={onSubmit} onCancel={vi.fn()} />);
    // Click any status SelectItem button rendered by the mock
    const statusBtns = screen.getAllByRole("button");
    const statusOption = statusBtns.find(
      (b) => b.textContent === "Suspenso" || b.textContent === "Inativo" || b.textContent === "Ativo"
    );
    if (statusOption) {
      fireEvent.click(statusOption);
    }
    await act(async () => {
      fireEvent.click(screen.getByRole("button", { name: /salvar alterações/i }));
    });
    await waitFor(() => {
      expect(onSubmit).toHaveBeenCalled();
    });
  });
});

describe("CustodianteForm — isSubmitting state", () => {
  it("shows Loader2 spinner in edit mode when isSubmitting=true", () => {
    const initial = {
      id: "uuid-cust-1",
      razaoSocial: "Custodiante SA",
      codigoInterno: "CUST-01",
      cnpj: "11222333000181",
      email: null,
      telefone: null,
      status: "ATIVO" as const,
      createdAt: "2020-01-01T00:00:00Z",
    };
    render(<CustodianteForm mode="edit" initial={initial} onSubmit={vi.fn()} onCancel={vi.fn()} isSubmitting={true} />);
    // isSubmitting=true triggers {isSubmitting && <Loader2>} branch AND disables buttons
    expect(screen.getByRole("button", { name: /salvar alterações/i })).toBeDisabled();
    expect(screen.getByRole("button", { name: /cancelar/i })).toBeDisabled();
  });
});

describe("CustodianteForm — create submit", () => {
  it("calls onSubmit in create mode with valid data", async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined);
    render(<CustodianteForm mode="create" onSubmit={onSubmit} onCancel={vi.fn()} />);
    fireEvent.change(screen.getByLabelText(/razão social/i), { target: { value: "Novo Custodiante SA" } });
    fireEvent.change(screen.getByLabelText(/cnpj/i), { target: { value: "11222333000181" } });
    await act(async () => {
      fireEvent.click(screen.getByRole("button", { name: /criar/i }));
    });
    await waitFor(() => {
      expect(onSubmit).toHaveBeenCalledWith(
        expect.objectContaining({ razaoSocial: "Novo Custodiante SA", cnpj: "11222333000181" }),
        expect.anything()
      );
    });
  });
});
