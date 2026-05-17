// ---------------------------------------------------------------------------
// DateRangeInput.test.tsx — render + interaction + a11y tests (T-7)
// ---------------------------------------------------------------------------

import { describe, it, expect, vi } from "vitest";
import { render, screen, fireEvent } from "@testing-library/react";
import { DateRangeInput } from "@/components/molecules/DateRangeInput";

describe("DateRangeInput", () => {
  it("renders both date inputs with labels", () => {
    render(
      <DateRangeInput
        dataInicio="2024-01-01"
        dataFim={null}
        onDataInicioChange={vi.fn()}
        onDataFimChange={vi.fn()}
      />
    );
    expect(screen.getByLabelText(/data de início/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/data de fim/i)).toBeInTheDocument();
  });

  it("dataInicio input shows correct value", () => {
    render(
      <DateRangeInput
        dataInicio="2024-06-15"
        dataFim={null}
        onDataInicioChange={vi.fn()}
        onDataFimChange={vi.fn()}
      />
    );
    const input = screen.getByLabelText(/data de início/i) as HTMLInputElement;
    expect(input.value).toBe("2024-06-15");
  });

  it("dataFim input shows empty when null", () => {
    render(
      <DateRangeInput
        dataInicio="2024-01-01"
        dataFim={null}
        onDataInicioChange={vi.fn()}
        onDataFimChange={vi.fn()}
      />
    );
    const input = screen.getByLabelText(/data de fim/i) as HTMLInputElement;
    expect(input.value).toBe("");
  });

  it("dataFim input shows correct value when provided", () => {
    render(
      <DateRangeInput
        dataInicio="2024-01-01"
        dataFim="2024-12-31"
        onDataInicioChange={vi.fn()}
        onDataFimChange={vi.fn()}
      />
    );
    const input = screen.getByLabelText(/data de fim/i) as HTMLInputElement;
    expect(input.value).toBe("2024-12-31");
  });

  it("calls onDataInicioChange on input", () => {
    const onDataInicioChange = vi.fn();
    render(
      <DateRangeInput
        dataInicio=""
        dataFim={null}
        onDataInicioChange={onDataInicioChange}
        onDataFimChange={vi.fn()}
      />
    );
    fireEvent.change(screen.getByLabelText(/data de início/i), {
      target: { value: "2024-03-01" },
    });
    expect(onDataInicioChange).toHaveBeenCalledWith("2024-03-01");
  });

  it("calls onDataFimChange on input", () => {
    const onDataFimChange = vi.fn();
    render(
      <DateRangeInput
        dataInicio="2024-01-01"
        dataFim={null}
        onDataInicioChange={vi.fn()}
        onDataFimChange={onDataFimChange}
      />
    );
    fireEvent.change(screen.getByLabelText(/data de fim/i), {
      target: { value: "2024-06-30" },
    });
    expect(onDataFimChange).toHaveBeenCalledWith("2024-06-30");
  });

  it("shows error messages when provided", () => {
    render(
      <DateRangeInput
        dataInicio=""
        dataFim={null}
        onDataInicioChange={vi.fn()}
        onDataFimChange={vi.fn()}
        dataInicioError="Data de início obrigatória"
        dataFimError="Data de fim deve ser após data de início"
      />
    );
    expect(screen.getByText("Data de início obrigatória")).toBeInTheDocument();
    expect(screen.getByText("Data de fim deve ser após data de início")).toBeInTheDocument();
  });

  it("dataInicio has aria-required", () => {
    render(
      <DateRangeInput
        dataInicio="2024-01-01"
        dataFim={null}
        onDataInicioChange={vi.fn()}
        onDataFimChange={vi.fn()}
      />
    );
    expect(screen.getByLabelText(/data de início/i)).toHaveAttribute("aria-required", "true");
  });

  it("sets min on dataFim to prevent before dataInicio", () => {
    render(
      <DateRangeInput
        dataInicio="2024-06-01"
        dataFim={null}
        onDataInicioChange={vi.fn()}
        onDataFimChange={vi.fn()}
      />
    );
    const dataFimInput = screen.getByLabelText(/data de fim/i);
    expect(dataFimInput).toHaveAttribute("min", "2024-06-01");
  });

  it("disables inputs when disabled prop is true", () => {
    render(
      <DateRangeInput
        dataInicio="2024-01-01"
        dataFim={null}
        onDataInicioChange={vi.fn()}
        onDataFimChange={vi.fn()}
        disabled
      />
    );
    expect(screen.getByLabelText(/data de início/i)).toBeDisabled();
    expect(screen.getByLabelText(/data de fim/i)).toBeDisabled();
  });
});
