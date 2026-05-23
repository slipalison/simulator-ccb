import { describe, it, expect, vi } from "vitest";
import { render, screen, fireEvent } from "@testing-library/react";
import { AdminPaginator } from "@/components/molecules/AdminPaginator";

describe("AdminPaginator", () => {
  it("renders page buttons", () => {
    render(
      <AdminPaginator page={1} pageSize={20} totalCount={60} onPageChange={vi.fn()} />
    );
    expect(screen.getByTestId("paginator-page-1")).toBeInTheDocument();
    expect(screen.getByTestId("paginator-page-2")).toBeInTheDocument();
    expect(screen.getByTestId("paginator-page-3")).toBeInTheDocument();
  });

  it("calls onPageChange when next clicked", () => {
    const onPageChange = vi.fn();
    render(
      <AdminPaginator page={1} pageSize={20} totalCount={60} onPageChange={onPageChange} />
    );
    fireEvent.click(screen.getByTestId("paginator-next"));
    expect(onPageChange).toHaveBeenCalledWith(2);
  });

  it("calls onPageChange when prev clicked", () => {
    const onPageChange = vi.fn();
    render(
      <AdminPaginator page={2} pageSize={20} totalCount={60} onPageChange={onPageChange} />
    );
    fireEvent.click(screen.getByTestId("paginator-prev"));
    expect(onPageChange).toHaveBeenCalledWith(1);
  });

  it("disables prev button on first page", () => {
    render(
      <AdminPaginator page={1} pageSize={20} totalCount={60} onPageChange={vi.fn()} />
    );
    expect(screen.getByTestId("paginator-prev")).toBeDisabled();
  });

  it("disables next button on last page", () => {
    render(
      <AdminPaginator page={3} pageSize={20} totalCount={60} onPageChange={vi.fn()} />
    );
    expect(screen.getByTestId("paginator-next")).toBeDisabled();
  });

  it("returns null when only 1 page", () => {
    const { container } = render(
      <AdminPaginator page={1} pageSize={20} totalCount={20} onPageChange={vi.fn()} />
    );
    expect(container.firstChild).toBeNull();
  });

  it("marks current page with aria-current", () => {
    render(
      <AdminPaginator page={2} pageSize={20} totalCount={60} onPageChange={vi.fn()} />
    );
    const page2Btn = screen.getByTestId("paginator-page-2");
    expect(page2Btn.getAttribute("aria-current")).toBe("page");
  });

  it("shows page info text", () => {
    render(
      <AdminPaginator page={2} pageSize={20} totalCount={60} onPageChange={vi.fn()} />
    );
    expect(screen.getByTestId("paginator-info")).toHaveTextContent("2");
  });

  // a11y: pagination nav has aria-label
  it("has accessible nav element", () => {
    render(
      <AdminPaginator page={1} pageSize={20} totalCount={60} onPageChange={vi.fn()} />
    );
    expect(screen.getByRole("navigation")).toBeInTheDocument();
  });
});
