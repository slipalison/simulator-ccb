// ---------------------------------------------------------------------------
// CedentePfForm.test.tsx — render + interaction + a11y (D-2)
// ---------------------------------------------------------------------------

import { describe, it, expect, vi } from "vitest";
import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import { CedentePfForm } from "@/components/organisms/CedentePfForm";

function renderForm(props: Partial<React.ComponentProps<typeof CedentePfForm>> = {}) {
  return render(
    <CedentePfForm
      onSubmit={vi.fn()}
      onCancel={vi.fn()}
      {...props}
    />
  );
}

describe("CedentePfForm", () => {
  it("renders form with accessible label", () => {
    renderForm();
    expect(screen.getByRole("form", { name: /criar cedente pessoa física/i })).toBeInTheDocument();
  });

  it("renders CPF and Nome fields", () => {
    renderForm();
    expect(screen.getByLabelText(/cpf/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/nome completo/i)).toBeInTheDocument();
  });

  it("renders optional email, telefone, endereco fields", () => {
    renderForm();
    expect(screen.getByLabelText(/email/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/telefone/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/endereço/i)).toBeInTheDocument();
  });

  it("shows validation error when CPF is empty on submit", async () => {
    renderForm();
    fireEvent.click(screen.getByRole("button", { name: /criar cedente pf/i }));
    await waitFor(() => {
      const errors = screen.getAllByText(/campo obrigatório/i);
      expect(errors.length).toBeGreaterThan(0);
    });
  });

  it("calls onCancel when cancel button clicked", () => {
    const onCancel = vi.fn();
    renderForm({ onCancel });
    fireEvent.click(screen.getByRole("button", { name: /cancelar/i }));
    expect(onCancel).toHaveBeenCalled();
  });

  it("disables buttons when isSubmitting", () => {
    renderForm({ isSubmitting: true });
    expect(screen.getByRole("button", { name: /criar cedente pf/i })).toBeDisabled();
    expect(screen.getByRole("button", { name: /cancelar/i })).toBeDisabled();
  });

  it("shows CPF format validation error for wrong length", async () => {
    renderForm();
    fireEvent.change(screen.getByLabelText(/cpf/i), { target: { value: "123" } });
    fireEvent.change(screen.getByLabelText(/nome completo/i), { target: { value: "Jo" } });
    fireEvent.click(screen.getByRole("button", { name: /criar cedente pf/i }));
    await waitFor(() => {
      expect(screen.getByText(/cpf deve conter 11 dígitos/i)).toBeInTheDocument();
    });
  });
});
