import { describe, it, expect, vi } from "vitest";
import { render, screen, fireEvent } from "@testing-library/react";
import { AdminSearchInput } from "@/components/molecules/AdminSearchInput";

describe("AdminSearchInput", () => {
  it("renders with placeholder", () => {
    render(<AdminSearchInput value="" onChange={vi.fn()} placeholder="Buscar..." />);
    expect(screen.getByPlaceholderText("Buscar...")).toBeInTheDocument();
  });

  it("has accessible label (sr-only)", () => {
    render(<AdminSearchInput value="" onChange={vi.fn()} placeholder="Buscar..." />);
    expect(screen.getByLabelText("Buscar...")).toBeInTheDocument();
  });

  it("shows clear button when value is non-empty", () => {
    render(<AdminSearchInput value="test" onChange={vi.fn()} />);
    expect(screen.getByTestId("search-clear-button")).toBeInTheDocument();
  });

  it("does not show clear button when value is empty", () => {
    render(<AdminSearchInput value="" onChange={vi.fn()} />);
    expect(screen.queryByTestId("search-clear-button")).not.toBeInTheDocument();
  });

  it("calls onChange with empty string when clear clicked", () => {
    const onChange = vi.fn();
    render(<AdminSearchInput value="test" onChange={onChange} />);
    fireEvent.click(screen.getByTestId("search-clear-button"));
    expect(onChange).toHaveBeenCalledWith("");
  });

  it("renders with custom data-testid", () => {
    render(<AdminSearchInput value="" onChange={vi.fn()} data-testid="custom-search" />);
    expect(screen.getByTestId("custom-search")).toBeInTheDocument();
  });

  it("is a search input type", () => {
    render(<AdminSearchInput value="" onChange={vi.fn()} />);
    const input = screen.getByRole("searchbox");
    expect(input).toBeInTheDocument();
  });

  it("updates local value when typing in input", () => {
    render(<AdminSearchInput value="" onChange={vi.fn()} />);
    const input = screen.getByRole("searchbox") as HTMLInputElement;
    fireEvent.change(input, { target: { value: "abc" } });
    expect(input.value).toBe("abc");
  });

  it("syncs external value change via useEffect", () => {
    const { rerender } = render(<AdminSearchInput value="initial" onChange={vi.fn()} />);
    const input = screen.getByRole("searchbox") as HTMLInputElement;
    expect(input.value).toBe("initial");
    rerender(<AdminSearchInput value="updated" onChange={vi.fn()} />);
    expect(input.value).toBe("updated");
  });

  it("uses custom placeholder when provided", () => {
    render(<AdminSearchInput value="" onChange={vi.fn()} placeholder="Pesquisar fundos" />);
    expect(screen.getByPlaceholderText("Pesquisar fundos")).toBeInTheDocument();
  });
});
