// ---------------------------------------------------------------------------
// SearchInput.test.tsx — render + interaction + a11y tests (T-3)
// ---------------------------------------------------------------------------

import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import { render, screen, fireEvent, act } from "@testing-library/react";
import { SearchInput } from "@/components/molecules/SearchInput";

describe("SearchInput", () => {
  beforeEach(() => {
    vi.useFakeTimers();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it("renders with placeholder", () => {
    render(
      <SearchInput value="" onChange={vi.fn()} placeholder="Buscar por nome..." />
    );
    expect(screen.getByPlaceholderText("Buscar por nome...")).toBeInTheDocument();
  });

  it("has accessible label via aria-label", () => {
    render(
      <SearchInput value="" onChange={vi.fn()} placeholder="Buscar por nome..." />
    );
    expect(screen.getByRole("searchbox", { name: "Buscar por nome..." })).toBeInTheDocument();
  });

  it("debounces onChange call by 300ms", () => {
    const onChange = vi.fn();
    render(<SearchInput value="" onChange={onChange} debounceMs={300} />);
    const input = screen.getByRole("searchbox");

    fireEvent.change(input, { target: { value: "test" } });
    expect(onChange).not.toHaveBeenCalled();

    act(() => {
      vi.advanceTimersByTime(300);
    });
    expect(onChange).toHaveBeenCalledWith("test");
  });

  it("shows clear button when value is non-empty", () => {
    render(<SearchInput value="abc" onChange={vi.fn()} />);
    expect(screen.getByLabelText("Limpar busca")).toBeInTheDocument();
  });

  it("does not show clear button when value is empty", () => {
    render(<SearchInput value="" onChange={vi.fn()} />);
    expect(screen.queryByLabelText("Limpar busca")).not.toBeInTheDocument();
  });

  it("clears value and calls onChange('') on clear click", () => {
    const onChange = vi.fn();
    render(<SearchInput value="something" onChange={onChange} />);
    fireEvent.click(screen.getByLabelText("Limpar busca"));
    expect(onChange).toHaveBeenCalledWith("");
  });

  it("syncs local value when external value changes", () => {
    const { rerender } = render(<SearchInput value="initial" onChange={vi.fn()} />);
    const input = screen.getByRole("searchbox") as HTMLInputElement;
    expect(input.value).toBe("initial");

    rerender(<SearchInput value="updated" onChange={vi.fn()} />);
    expect(input.value).toBe("updated");
  });
});
