// ---------------------------------------------------------------------------
// LimiteExposicaoInput.test.tsx — render + interaction + a11y (D-2)
// Covers rendering, value changes, null/empty transitions, error display.
// ---------------------------------------------------------------------------

import { describe, it, expect, vi } from "vitest";
import { render, screen, fireEvent } from "@testing-library/react";
import { LimiteExposicaoInput } from "@/components/molecules/LimiteExposicaoInput";

function renderComponent(overrides?: Partial<Parameters<typeof LimiteExposicaoInput>[0]>) {
  const onLimitePercentualChange = vi.fn();
  const onLimiteValorChange = vi.fn();
  render(
    <LimiteExposicaoInput
      limitePercentual={null}
      limiteValor={null}
      onLimitePercentualChange={onLimitePercentualChange}
      onLimiteValorChange={onLimiteValorChange}
      {...overrides}
    />
  );
  return { onLimitePercentualChange, onLimiteValorChange };
}

describe("LimiteExposicaoInput", () => {
  it("renders Limite Percentual label", () => {
    renderComponent();
    expect(screen.getByLabelText(/limite percentual/i)).toBeInTheDocument();
  });

  it("renders Limite de Valor label", () => {
    renderComponent();
    expect(screen.getByLabelText(/limite de valor/i)).toBeInTheDocument();
  });

  it("shows empty string when limitePercentual is null", () => {
    renderComponent({ limitePercentual: null });
    const input = screen.getByLabelText(/limite percentual/i) as HTMLInputElement;
    expect(input.value).toBe("");
  });

  it("shows empty string when limiteValor is null", () => {
    renderComponent({ limiteValor: null });
    const input = screen.getByLabelText(/limite de valor/i) as HTMLInputElement;
    expect(input.value).toBe("");
  });

  it("shows numeric value when limitePercentual is set", () => {
    renderComponent({ limitePercentual: 50 });
    const input = screen.getByLabelText(/limite percentual/i) as HTMLInputElement;
    expect(input.value).toBe("50");
  });

  it("shows numeric value when limiteValor is set", () => {
    renderComponent({ limiteValor: 100000 });
    const input = screen.getByLabelText(/limite de valor/i) as HTMLInputElement;
    expect(input.value).toBe("100000");
  });

  it("calls onLimitePercentualChange with parsed number when value typed", () => {
    const { onLimitePercentualChange } = renderComponent();
    fireEvent.change(screen.getByLabelText(/limite percentual/i), {
      target: { value: "25" },
    });
    expect(onLimitePercentualChange).toHaveBeenCalledWith(25);
  });

  it("calls onLimitePercentualChange with null when value cleared", () => {
    const { onLimitePercentualChange } = renderComponent({ limitePercentual: 25 });
    fireEvent.change(screen.getByLabelText(/limite percentual/i), {
      target: { value: "" },
    });
    expect(onLimitePercentualChange).toHaveBeenCalledWith(null);
  });

  it("calls onLimiteValorChange with parsed number when value typed", () => {
    const { onLimiteValorChange } = renderComponent();
    fireEvent.change(screen.getByLabelText(/limite de valor/i), {
      target: { value: "500000" },
    });
    expect(onLimiteValorChange).toHaveBeenCalledWith(500000);
  });

  it("calls onLimiteValorChange with null when value cleared", () => {
    const { onLimiteValorChange } = renderComponent({ limiteValor: 500000 });
    fireEvent.change(screen.getByLabelText(/limite de valor/i), {
      target: { value: "" },
    });
    expect(onLimiteValorChange).toHaveBeenCalledWith(null);
  });

  it("shows limitePercentual error alert when provided", () => {
    renderComponent({ limitePercentualError: "Limite percentual inválido" });
    expect(screen.getByRole("alert")).toHaveTextContent("Limite percentual inválido");
  });

  it("shows limiteValor error alert when provided", () => {
    renderComponent({ limiteValorError: "Valor inválido" });
    expect(screen.getByRole("alert")).toHaveTextContent("Valor inválido");
  });

  it("disables both inputs when disabled=true", () => {
    renderComponent({ disabled: true });
    expect(screen.getByLabelText(/limite percentual/i)).toBeDisabled();
    expect(screen.getByLabelText(/limite de valor/i)).toBeDisabled();
  });

  it("has aria-invalid on percentual input when error present", () => {
    renderComponent({ limitePercentualError: "erro" });
    const input = screen.getByLabelText(/limite percentual/i);
    expect(input).toHaveAttribute("aria-invalid", "true");
  });

  it("has aria-invalid on valor input when error present", () => {
    renderComponent({ limiteValorError: "erro" });
    const input = screen.getByLabelText(/limite de valor/i);
    expect(input).toHaveAttribute("aria-invalid", "true");
  });
});
