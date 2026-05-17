// ---------------------------------------------------------------------------
// CustodianteForm.test.tsx — render + interaction (D-2)
// ---------------------------------------------------------------------------

import { describe, it, expect, vi } from "vitest";
import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import { CustodianteForm } from "@/components/organisms/CustodianteForm";

// Mock Radix Select
vi.mock("@/components/ui/select", () => ({
  Select: ({ children }: { children: React.ReactNode }) => <div>{children}</div>,
  SelectTrigger: ({ children, id, "aria-label": al }: any) =>
    <button id={id} aria-label={al}>{children}</button>,
  SelectValue: ({ placeholder }: any) => <span>{placeholder}</span>,
  SelectContent: ({ children }: any) => <div>{children}</div>,
  SelectItem: ({ children, value }: any) =>
    <option value={value}>{children}</option>,
}));

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
});
