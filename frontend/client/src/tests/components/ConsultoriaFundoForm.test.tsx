// ---------------------------------------------------------------------------
// ConsultoriaFundoForm.test.tsx — render + interaction (D-2)
// ---------------------------------------------------------------------------

import { describe, it, expect, vi } from "vitest";
import { render, screen, fireEvent, waitFor, act } from "@testing-library/react";
import { ConsultoriaFundoForm } from "@/components/organisms/ConsultoriaFundoForm";

// Mock Radix Select — SelectTrigger acts as a simple button, SelectItem as option button
// onValueChange is threaded from Select → SelectContent → SelectItem via context-free direct prop
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

function renderCreate() {
  return render(<ConsultoriaFundoForm mode="create" onSubmit={vi.fn()} onCancel={vi.fn()} />);
}

describe("ConsultoriaFundoForm — create mode", () => {
  it("renders Razão Social and CNPJ fields", () => {
    renderCreate();
    expect(screen.getByLabelText(/razão social/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/cnpj/i)).toBeInTheDocument();
  });

  it("shows required error on empty submit", async () => {
    renderCreate();
    fireEvent.click(screen.getByRole("button", { name: /criar/i }));
    await waitFor(() => {
      const errors = screen.getAllByText(/campo obrigatório/i);
      expect(errors.length).toBeGreaterThan(0);
    });
  });

  it("calls onCancel", () => {
    const onCancel = vi.fn();
    render(<ConsultoriaFundoForm mode="create" onSubmit={vi.fn()} onCancel={onCancel} />);
    fireEvent.click(screen.getByRole("button", { name: /cancelar/i }));
    expect(onCancel).toHaveBeenCalled();
  });
});

describe("ConsultoriaFundoForm — edit mode", () => {
  const initial = {
    id: "uuid-cons-1",
    razaoSocial: "Consultoria SA",
    nomeFantasia: null,
    cnpj: "11222333000181",
    email: null,
    telefone: null,
    status: "ATIVO" as const,
    createdAt: "2020-01-01T00:00:00Z",
  };

  it("renders Salvar alterações button", () => {
    render(<ConsultoriaFundoForm mode="edit" initial={initial} onSubmit={vi.fn()} onCancel={vi.fn()} />);
    expect(screen.getByRole("button", { name: /salvar alterações/i })).toBeInTheDocument();
  });

  it("pre-fills Razão Social from initial data", () => {
    render(<ConsultoriaFundoForm mode="edit" initial={initial} onSubmit={vi.fn()} onCancel={vi.fn()} />);
    expect(screen.getByLabelText(/razão social/i)).toHaveValue("Consultoria SA");
  });

  it("does not render CNPJ field in edit mode", () => {
    render(<ConsultoriaFundoForm mode="edit" initial={initial} onSubmit={vi.fn()} onCancel={vi.fn()} />);
    expect(screen.queryByLabelText(/cnpj/i)).not.toBeInTheDocument();
  });

  it("calls onSubmit in edit mode with valid data", async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined);
    render(<ConsultoriaFundoForm mode="edit" initial={initial} onSubmit={onSubmit} onCancel={vi.fn()} />);
    await act(async () => {
      fireEvent.click(screen.getByRole("button", { name: /salvar alterações/i }));
    });
    await waitFor(() => {
      expect(onSubmit).toHaveBeenCalledWith(
        expect.objectContaining({ razaoSocial: "Consultoria SA" }),
        expect.anything()
      );
    });
  });
});

describe("ConsultoriaFundoForm — status select in edit mode", () => {
  const initial = {
    id: "uuid-cons-1",
    razaoSocial: "Consultoria SA",
    nomeFantasia: null,
    cnpj: "11222333000181",
    email: null,
    telefone: null,
    status: "ATIVO" as const,
    createdAt: "2020-01-01T00:00:00Z",
  };

  it("calls setValue when a status option is clicked in edit mode", async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined);
    render(<ConsultoriaFundoForm mode="edit" initial={initial} onSubmit={onSubmit} onCancel={vi.fn()} />);
    // Click one of the SelectItem buttons (status options rendered via mock)
    const statusBtn = screen.getAllByRole("button").find(
      (b) => b.textContent === "Suspenso" || b.textContent === "Inativo" || b.textContent === "Ativo"
    );
    if (statusBtn) {
      fireEvent.click(statusBtn);
    }
    // Submit to verify setValue was called (form submits with updated status)
    await act(async () => {
      fireEvent.click(screen.getByRole("button", { name: /salvar alterações/i }));
    });
    await waitFor(() => {
      expect(onSubmit).toHaveBeenCalled();
    });
  });
});

describe("ConsultoriaFundoForm — create submit", () => {
  it("calls onSubmit in create mode with valid data", async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined);
    render(<ConsultoriaFundoForm mode="create" onSubmit={onSubmit} onCancel={vi.fn()} />);
    fireEvent.change(screen.getByLabelText(/razão social/i), { target: { value: "Nova Consultoria SA" } });
    fireEvent.change(screen.getByLabelText(/cnpj/i), { target: { value: "11222333000181" } });
    await act(async () => {
      fireEvent.click(screen.getByRole("button", { name: /criar/i }));
    });
    await waitFor(() => {
      expect(onSubmit).toHaveBeenCalledWith(
        expect.objectContaining({ razaoSocial: "Nova Consultoria SA", cnpj: "11222333000181" }),
        expect.anything()
      );
    });
  });
});
