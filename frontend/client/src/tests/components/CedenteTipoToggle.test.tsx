// ---------------------------------------------------------------------------
// CedenteTipoToggle.test.tsx — render + interaction + a11y (D-2)
// ---------------------------------------------------------------------------

import { describe, it, expect, vi } from "vitest";
import { render, screen, fireEvent } from "@testing-library/react";
import { CedenteTipoToggle } from "@/components/atoms/CedenteTipoToggle";

describe("CedenteTipoToggle", () => {
  it("renders PF and PJ buttons", () => {
    render(<CedenteTipoToggle value="PF" onChange={vi.fn()} />);
    expect(screen.getByRole("button", { name: /pessoa física/i })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /pessoa jurídica/i })).toBeInTheDocument();
  });

  it("marks PF button as pressed when value=PF", () => {
    render(<CedenteTipoToggle value="PF" onChange={vi.fn()} />);
    expect(screen.getByRole("button", { name: /pessoa física/i })).toHaveAttribute("aria-pressed", "true");
    expect(screen.getByRole("button", { name: /pessoa jurídica/i })).toHaveAttribute("aria-pressed", "false");
  });

  it("marks PJ button as pressed when value=PJ", () => {
    render(<CedenteTipoToggle value="PJ" onChange={vi.fn()} />);
    expect(screen.getByRole("button", { name: /pessoa jurídica/i })).toHaveAttribute("aria-pressed", "true");
    expect(screen.getByRole("button", { name: /pessoa física/i })).toHaveAttribute("aria-pressed", "false");
  });

  it("calls onChange with PJ when PJ button clicked", () => {
    const onChange = vi.fn();
    render(<CedenteTipoToggle value="PF" onChange={onChange} />);
    fireEvent.click(screen.getByRole("button", { name: /pessoa jurídica/i }));
    expect(onChange).toHaveBeenCalledWith("PJ");
  });

  it("calls onChange with PF when PF button clicked", () => {
    const onChange = vi.fn();
    render(<CedenteTipoToggle value="PJ" onChange={onChange} />);
    fireEvent.click(screen.getByRole("button", { name: /pessoa física/i }));
    expect(onChange).toHaveBeenCalledWith("PF");
  });

  it("disables both buttons when disabled prop is true", () => {
    render(<CedenteTipoToggle value="PF" onChange={vi.fn()} disabled={true} />);
    expect(screen.getByRole("button", { name: /pessoa física/i })).toBeDisabled();
    expect(screen.getByRole("button", { name: /pessoa jurídica/i })).toBeDisabled();
  });

  it("has accessible fieldset with group label", () => {
    render(<CedenteTipoToggle value="PF" onChange={vi.fn()} />);
    expect(screen.getByRole("group", { name: /tipo de cedente/i })).toBeInTheDocument();
  });
});
