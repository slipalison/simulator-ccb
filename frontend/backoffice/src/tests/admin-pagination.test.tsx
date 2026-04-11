import { describe, it, expect, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { AdminPagination } from "@/components/molecules/AdminPagination";

describe("Admin Pagination", () => {
  it("renders prev/next buttons", () => {
    render(
      <AdminPagination
        page={2}
        pageSize={20}
        totalCount={100}
        onPageChange={vi.fn()}
      />
    );
    expect(screen.getByTestId("prev-button")).toBeInTheDocument();
    expect(screen.getByTestId("next-button")).toBeInTheDocument();
  });

  it("prev disabled on page 1", () => {
    render(
      <AdminPagination
        page={1}
        pageSize={20}
        totalCount={100}
        onPageChange={vi.fn()}
      />
    );
    const prev = screen.getByTestId("prev-button");
    expect(prev).toHaveClass("pointer-events-none");
    expect(prev).toHaveClass("opacity-50");
  });

  it("next disabled on last page", () => {
    render(
      <AdminPagination
        page={5}
        pageSize={20}
        totalCount={100}
        onPageChange={vi.fn()}
      />
    );
    const next = screen.getByTestId("next-button");
    expect(next).toHaveClass("pointer-events-none");
    expect(next).toHaveClass("opacity-50");
  });

  it("calls onPageChange when next is clicked", async () => {
    const onPageChange = vi.fn();
    render(
      <AdminPagination
        page={1}
        pageSize={20}
        totalCount={100}
        onPageChange={onPageChange}
      />
    );

    await userEvent.click(screen.getByTestId("next-button"));
    expect(onPageChange).toHaveBeenCalledWith(2);
  });

  it("calls onPageChange when prev is clicked", async () => {
    const onPageChange = vi.fn();
    render(
      <AdminPagination
        page={3}
        pageSize={20}
        totalCount={100}
        onPageChange={onPageChange}
      />
    );

    await userEvent.click(screen.getByTestId("prev-button"));
    expect(onPageChange).toHaveBeenCalledWith(2);
  });

  it("calls onPageChange when page number is clicked", async () => {
    const onPageChange = vi.fn();
    render(
      <AdminPagination
        page={1}
        pageSize={20}
        totalCount={60}
        onPageChange={onPageChange}
      />
    );

    await userEvent.click(screen.getByTestId("page-3"));
    expect(onPageChange).toHaveBeenCalledWith(3);
  });

  it("shows page X of Y text", () => {
    render(
      <AdminPagination
        page={2}
        pageSize={20}
        totalCount={100}
        onPageChange={vi.fn()}
      />
    );
    expect(screen.getByTestId("page-info")).toHaveTextContent("Pagina 2 de 5");
  });

  it("shows correct total pages calculation", () => {
    render(
      <AdminPagination
        page={1}
        pageSize={20}
        totalCount={45}
        onPageChange={vi.fn()}
      />
    );
    expect(screen.getByTestId("page-info")).toHaveTextContent("Pagina 1 de 3");
  });

  it("renders page number links", () => {
    render(
      <AdminPagination
        page={1}
        pageSize={20}
        totalCount={60}
        onPageChange={vi.fn()}
      />
    );
    expect(screen.getByTestId("page-1")).toBeInTheDocument();
    expect(screen.getByTestId("page-2")).toBeInTheDocument();
    expect(screen.getByTestId("page-3")).toBeInTheDocument();
  });

  it("active page has aria-current page", () => {
    render(
      <AdminPagination
        page={2}
        pageSize={20}
        totalCount={60}
        onPageChange={vi.fn()}
      />
    );
    const activePage = screen.getByTestId("page-2");
    expect(activePage).toHaveAttribute("aria-current", "page");
  });

  it("handles edge case: totalCount === 0 returns null", () => {
    const { container } = render(
      <AdminPagination
        page={1}
        pageSize={20}
        totalCount={0}
        onPageChange={vi.fn()}
      />
    );
    expect(container.firstChild).toBeNull();
  });

  it("shows ellipsis when totalPages > 7", () => {
    render(
      <AdminPagination
        page={5}
        pageSize={10}
        totalCount={200}
        onPageChange={vi.fn()}
      />
    );
    // 200/10 = 20 pages, should show ellipsis
    const ellipsis = screen.getAllByTestId("ellipsis");
    expect(ellipsis.length).toBeGreaterThanOrEqual(1);
  });
});
