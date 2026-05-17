// ---------------------------------------------------------------------------
// CedentePjForm.test.tsx — render + interaction + a11y (D-2)
// ---------------------------------------------------------------------------

import { describe, it, expect, vi } from "vitest";
import { render, screen, fireEvent, waitFor, act } from "@testing-library/react";
import { CedentePjForm } from "@/components/organisms/CedentePjForm";

function renderForm(props: Partial<React.ComponentProps<typeof CedentePjForm>> = {}) {
  return render(<CedentePjForm onSubmit={vi.fn()} onCancel={vi.fn()} {...props} />);
}

describe("CedentePjForm", () => {
  it("renders form with accessible label", () => {
    renderForm();
    expect(screen.getByRole("form", { name: /criar cedente pessoa jurídica/i })).toBeInTheDocument();
  });

  it("renders CNPJ and Razão Social fields", () => {
    renderForm();
    expect(screen.getByLabelText(/cnpj/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/razão social/i)).toBeInTheDocument();
  });

  it("shows CNPJ format validation on submit with wrong input", async () => {
    renderForm();
    fireEvent.change(screen.getByLabelText(/cnpj/i), { target: { value: "123" } });
    fireEvent.change(screen.getByLabelText(/razão social/i), { target: { value: "SA" } });
    fireEvent.click(screen.getByRole("button", { name: /criar cedente pj/i }));
    await waitFor(() => {
      expect(screen.getByText(/cnpj deve conter 14 dígitos/i)).toBeInTheDocument();
    });
  });

  it("calls onCancel when cancel clicked", () => {
    const onCancel = vi.fn();
    renderForm({ onCancel });
    fireEvent.click(screen.getByRole("button", { name: /cancelar/i }));
    expect(onCancel).toHaveBeenCalled();
  });

  it("disables submit button when isSubmitting", () => {
    renderForm({ isSubmitting: true });
    expect(screen.getByRole("button", { name: /criar cedente pj/i })).toBeDisabled();
  });

  it("disables cancel button when isSubmitting", () => {
    renderForm({ isSubmitting: true });
    expect(screen.getByRole("button", { name: /cancelar/i })).toBeDisabled();
  });

  it("calls onSubmit with valid data", async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined);
    renderForm({ onSubmit });
    fireEvent.change(screen.getByLabelText(/cnpj/i), { target: { value: "11222333000181" } });
    fireEvent.change(screen.getByLabelText(/razão social/i), { target: { value: "Empresa SA" } });
    await act(async () => {
      fireEvent.click(screen.getByRole("button", { name: /criar cedente pj/i }));
    });
    await waitFor(() => {
      expect(onSubmit).toHaveBeenCalledWith(
        expect.objectContaining({ cnpj: "11222333000181", razaoSocial: "Empresa SA" }),
        expect.anything()
      );
    });
  });

  it("shows email validation error when invalid email provided", async () => {
    renderForm();
    fireEvent.change(screen.getByLabelText(/cnpj/i), { target: { value: "11222333000181" } });
    fireEvent.change(screen.getByLabelText(/razão social/i), { target: { value: "Empresa SA" } });
    fireEvent.change(screen.getByLabelText(/email/i), { target: { value: "not-an-email" } });
    await act(async () => {
      fireEvent.click(screen.getByRole("button", { name: /criar cedente pj/i }));
    });
    await waitFor(() => {
      const alerts = screen.queryAllByRole("alert");
      expect(alerts.length).toBeGreaterThan(0);
    });
  });

  it("renders optional fields: email, telefone, endereço", () => {
    renderForm();
    expect(screen.getByLabelText(/email/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/telefone/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/endereço/i)).toBeInTheDocument();
  });
});
