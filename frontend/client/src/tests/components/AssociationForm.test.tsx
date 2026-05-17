// ---------------------------------------------------------------------------
// AssociationForm.test.tsx — render + interaction + a11y tests (T-7)
// ---------------------------------------------------------------------------

import { describe, it, expect, vi } from "vitest";
import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import { AssociationForm } from "@/components/organisms/AssociationForm";

const TARGET_OPTIONS = [
  { id: "uuid-1", label: "Cedente A" },
  { id: "uuid-2", label: "Cedente B" },
];

function renderForm(props?: Partial<Parameters<typeof AssociationForm>[0]>) {
  return render(
    <AssociationForm
      targetOptions={TARGET_OPTIONS}
      targetLabel="Cedente"
      onSubmit={vi.fn().mockResolvedValue(undefined)}
      onCancel={vi.fn()}
      isSubmitting={false}
      {...props}
    />
  );
}

describe("AssociationForm", () => {
  it("renders with accessible form label", () => {
    renderForm();
    expect(
      screen.getByRole("form", { name: /criar associação com cedente/i })
    ).toBeInTheDocument();
  });

  it("renders target dropdown with label", () => {
    renderForm();
    expect(screen.getByText("Cedente")).toBeInTheDocument();
  });

  it("renders date range inputs", () => {
    renderForm();
    expect(screen.getByLabelText(/data de início/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/data de fim/i)).toBeInTheDocument();
  });

  it("renders submit and cancel buttons", () => {
    renderForm();
    expect(screen.getByRole("button", { name: /criar associação/i })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /cancelar/i })).toBeInTheDocument();
  });

  it("calls onCancel when cancel button is clicked", () => {
    const onCancel = vi.fn();
    renderForm({ onCancel });
    fireEvent.click(screen.getByRole("button", { name: /cancelar/i }));
    expect(onCancel).toHaveBeenCalled();
  });

  it("disables buttons when isSubmitting", () => {
    renderForm({ isSubmitting: true });
    expect(screen.getByRole("button", { name: /cancelar/i })).toBeDisabled();
    expect(screen.getByRole("button", { name: /criar associação/i })).toBeDisabled();
  });

  it("shows validation error when submitted without targetId", async () => {
    renderForm();
    fireEvent.click(screen.getByRole("button", { name: /criar associação/i }));
    await waitFor(() => {
      // Form should show some error — targetId is required (UUID validation)
      const alerts = screen.queryAllByRole("alert");
      expect(alerts.length).toBeGreaterThan(0);
    });
  });

  it("uses targetLabel in select trigger aria-label", () => {
    renderForm();
    expect(
      screen.getByRole("combobox", { name: /selecionar cedente/i })
    ).toBeInTheDocument();
  });
});
