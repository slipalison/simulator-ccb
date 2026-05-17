// ---------------------------------------------------------------------------
// Paginator.test.tsx — render + interaction + a11y tests (T-3)
// ---------------------------------------------------------------------------

import { describe, it, expect, vi } from "vitest";
import { render, screen, fireEvent } from "@testing-library/react";
import { Paginator } from "@/components/molecules/Paginator";

describe("Paginator", () => {
  it("renders nothing when totalPages is 1", () => {
    const { container } = render(
      <Paginator page={1} totalPages={1} onPageChange={vi.fn()} />
    );
    expect(container.firstChild).toBeNull();
  });

  it("renders prev/next buttons and page buttons", () => {
    render(<Paginator page={2} totalPages={5} onPageChange={vi.fn()} />);
    expect(screen.getByLabelText("Página anterior")).toBeInTheDocument();
    expect(screen.getByLabelText("Próxima página")).toBeInTheDocument();
    // Pages 1-5 all visible (<=7)
    for (let i = 1; i <= 5; i++) {
      expect(screen.getByLabelText(`Página ${i}`)).toBeInTheDocument();
    }
  });

  it("prev button disabled on first page", () => {
    render(<Paginator page={1} totalPages={5} onPageChange={vi.fn()} />);
    expect(screen.getByLabelText("Página anterior")).toBeDisabled();
  });

  it("next button disabled on last page", () => {
    render(<Paginator page={5} totalPages={5} onPageChange={vi.fn()} />);
    expect(screen.getByLabelText("Próxima página")).toBeDisabled();
  });

  it("calls onPageChange with correct page on button click", () => {
    const onPageChange = vi.fn();
    render(<Paginator page={2} totalPages={5} onPageChange={onPageChange} />);
    fireEvent.click(screen.getByLabelText("Página 3"));
    expect(onPageChange).toHaveBeenCalledWith(3);
  });

  it("calls onPageChange page-1 on prev click", () => {
    const onPageChange = vi.fn();
    render(<Paginator page={3} totalPages={5} onPageChange={onPageChange} />);
    fireEvent.click(screen.getByLabelText("Página anterior"));
    expect(onPageChange).toHaveBeenCalledWith(2);
  });

  it("calls onPageChange page+1 on next click", () => {
    const onPageChange = vi.fn();
    render(<Paginator page={3} totalPages={5} onPageChange={onPageChange} />);
    fireEvent.click(screen.getByLabelText("Próxima página"));
    expect(onPageChange).toHaveBeenCalledWith(4);
  });

  it("marks current page button with aria-current=page", () => {
    render(<Paginator page={2} totalPages={5} onPageChange={vi.fn()} />);
    expect(screen.getByLabelText("Página 2")).toHaveAttribute("aria-current", "page");
  });

  it("has accessible nav landmark", () => {
    render(<Paginator page={2} totalPages={5} onPageChange={vi.fn()} />);
    expect(screen.getByRole("navigation", { name: "Paginação" })).toBeInTheDocument();
  });

  it("renders ellipsis when totalPages > 7 and page is near start", () => {
    // page=2, totalPages=10 → ellipsis after page 3
    render(<Paginator page={2} totalPages={10} onPageChange={vi.fn()} />);
    // First and last pages always shown; ellipsis somewhere
    expect(screen.getByLabelText("Página 1")).toBeInTheDocument();
    expect(screen.getByLabelText("Página 10")).toBeInTheDocument();
    // At least one "…" rendered (aria-hidden span)
    const ellipsisSpans = document.querySelectorAll("[aria-hidden='true']");
    expect(ellipsisSpans.length).toBeGreaterThan(0);
  });

  it("renders ellipsis when totalPages > 7 and page is near end", () => {
    // page=9, totalPages=10 → ellipsis before last cluster
    render(<Paginator page={9} totalPages={10} onPageChange={vi.fn()} />);
    expect(screen.getByLabelText("Página 1")).toBeInTheDocument();
    expect(screen.getByLabelText("Página 10")).toBeInTheDocument();
    const ellipsisSpans = document.querySelectorAll("[aria-hidden='true']");
    expect(ellipsisSpans.length).toBeGreaterThan(0);
  });

  it("renders ellipsis on both sides when page is in middle of large range", () => {
    // page=5, totalPages=10 → ellipsis on both sides
    render(<Paginator page={5} totalPages={10} onPageChange={vi.fn()} />);
    const ellipsisSpans = document.querySelectorAll("[aria-hidden='true']");
    expect(ellipsisSpans.length).toBeGreaterThanOrEqual(2);
  });

  it("returns null when totalPages is 0", () => {
    const { container } = render(
      <Paginator page={1} totalPages={0} onPageChange={vi.fn()} />
    );
    expect(container.firstChild).toBeNull();
  });
});
