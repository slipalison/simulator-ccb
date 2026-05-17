// ---------------------------------------------------------------------------
// CedentePjForm.test.tsx — render + interaction + a11y (D-2)
// ---------------------------------------------------------------------------

import { describe, it, expect, vi } from "vitest";
import { render, screen, fireEvent, waitFor } from "@testing-library/react";
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
});
