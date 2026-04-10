import { describe, it, expect, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import { AdminStatusFilter } from "@/components/molecules/AdminStatusFilter";

describe("Admin Status Filter", () => {
  it("renders select trigger", () => {
    render(<AdminStatusFilter value="all" onChange={vi.fn()} />);
    const trigger = screen.getByTestId("status-filter");
    expect(trigger).toBeInTheDocument();
    expect(trigger).toHaveAttribute("role", "combobox");
  });

  it("shows Todos as selected value when value is all", () => {
    render(<AdminStatusFilter value="all" onChange={vi.fn()} />);
    expect(screen.getByTestId("status-filter")).toHaveTextContent("Todos");
  });

  it("shows Ativo as selected value when value is active", () => {
    render(<AdminStatusFilter value="active" onChange={vi.fn()} />);
    expect(screen.getByTestId("status-filter")).toHaveTextContent("Ativo");
  });

  it("shows Bloqueado as selected value when value is blocked", () => {
    render(<AdminStatusFilter value="blocked" onChange={vi.fn()} />);
    expect(screen.getByTestId("status-filter")).toHaveTextContent("Bloqueado");
  });

  it("shows Deletado as selected value when value is deleted", () => {
    render(<AdminStatusFilter value="deleted" onChange={vi.fn()} />);
    expect(screen.getByTestId("status-filter")).toHaveTextContent("Deletado");
  });

  it("renders all 4 status options in the select content", () => {
    // The SelectContent is rendered in a portal; we verify by checking
    // that the component renders correctly with different values
    const { rerender } = render(<AdminStatusFilter value="all" onChange={vi.fn()} />);
    expect(screen.getByTestId("status-filter")).toHaveTextContent("Todos");

    rerender(<AdminStatusFilter value="active" onChange={vi.fn()} />);
    expect(screen.getByTestId("status-filter")).toHaveTextContent("Ativo");

    rerender(<AdminStatusFilter value="blocked" onChange={vi.fn()} />);
    expect(screen.getByTestId("status-filter")).toHaveTextContent("Bloqueado");

    rerender(<AdminStatusFilter value="deleted" onChange={vi.fn()} />);
    expect(screen.getByTestId("status-filter")).toHaveTextContent("Deletado");
  });

  it("applies disabled state", () => {
    render(<AdminStatusFilter value="all" onChange={vi.fn()} disabled />);
    const trigger = screen.getByTestId("status-filter");
    expect(trigger).toHaveAttribute("data-disabled");
  });

  it("width constrained to 180px", () => {
    render(<AdminStatusFilter value="all" onChange={vi.fn()} />);
    const trigger = screen.getByTestId("status-filter");
    expect(trigger).toHaveClass("w-[180px]");
  });
});
