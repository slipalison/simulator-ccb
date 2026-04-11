import { describe, it, expect, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { AdminUsersTable } from "@/components/molecules/AdminUsersTable";
import type { UserSummaryDto } from "@/lib/admin-api";

const mockUsers: UserSummaryDto[] = [
  {
    id: "1",
    name: "John Doe",
    email: "john@example.com",
    document: "123.456.789-00",
    type: "PF",
    enabled: true,
  },
  {
    id: "2",
    name: "Acme Corp",
    email: "contact@acme.com",
    document: "12.345.678/0001-99",
    type: "PJ",
    enabled: false,
  },
  {
    id: "3",
    name: "Deleted User",
    email: "deleted@example.com",
    document: "987.654.321-00",
    type: "PF",
    enabled: true,
    deletedAt: "2026-01-01T00:00:00Z",
  },
];

describe("Admin Users Table", () => {
  it("renders table with correct columns", () => {
    render(
      <AdminUsersTable users={mockUsers} onViewDetails={vi.fn()} />
    );

    expect(screen.getByText("Nome")).toBeInTheDocument();
    expect(screen.getByText("Documento")).toBeInTheDocument();
    expect(screen.getByText("Email")).toBeInTheDocument();
    expect(screen.getByText("Status")).toBeInTheDocument();
    expect(screen.getByText("Acoes")).toBeInTheDocument();
  });

  it("displays user data in table cells", () => {
    render(
      <AdminUsersTable users={mockUsers} onViewDetails={vi.fn()} />
    );

    expect(screen.getByText("John Doe")).toBeInTheDocument();
    expect(screen.getByText("123.456.789-00")).toBeInTheDocument();
    expect(screen.getByText("john@example.com")).toBeInTheDocument();
  });

  it("shows dash for missing document", () => {
    const usersWithoutDoc = [{ ...mockUsers[0], document: undefined }];
    render(
      <AdminUsersTable users={usersWithoutDoc} onViewDetails={vi.fn()} />
    );

    const cells = screen.getAllByText("-");
    expect(cells.length).toBeGreaterThan(0);
  });

  it("shows status badges with correct text - active user", () => {
    render(
      <AdminUsersTable users={mockUsers} onViewDetails={vi.fn()} />
    );

    const activeBadge = screen.getByTestId("status-badge-1");
    expect(activeBadge).toHaveTextContent("Ativo");
  });

  it("shows status badges with correct text - blocked user", () => {
    render(
      <AdminUsersTable users={mockUsers} onViewDetails={vi.fn()} />
    );

    const blockedBadge = screen.getByTestId("status-badge-2");
    expect(blockedBadge).toHaveTextContent("Bloqueado");
  });

  it("shows status badges with correct text - deleted user", () => {
    render(
      <AdminUsersTable users={mockUsers} onViewDetails={vi.fn()} />
    );

    const deletedBadge = screen.getByTestId("status-badge-3");
    expect(deletedBadge).toHaveTextContent("Deletado");
  });

  it("active badge uses default variant (green)", () => {
    render(
      <AdminUsersTable users={mockUsers} onViewDetails={vi.fn()} />
    );

    const activeBadge = screen.getByTestId("status-badge-1");
    // Default variant has no data-variant attribute (or no variant class)
    expect(activeBadge).not.toHaveClass("bg-secondary");
    expect(activeBadge).not.toHaveClass("bg-destructive");
  });

  it("blocked badge uses secondary variant (yellow)", () => {
    render(
      <AdminUsersTable users={mockUsers} onViewDetails={vi.fn()} />
    );

    const blockedBadge = screen.getByTestId("status-badge-2");
    expect(blockedBadge).toHaveClass("bg-secondary");
  });

  it("deleted badge uses destructive variant (red)", () => {
    render(
      <AdminUsersTable users={mockUsers} onViewDetails={vi.fn()} />
    );

    const deletedBadge = screen.getByTestId("status-badge-3");
    expect(deletedBadge).toHaveClass("bg-destructive");
  });

  it("calls onViewDetails when Ver button clicked", async () => {
    const onViewDetails = vi.fn();
    render(
      <AdminUsersTable users={mockUsers} onViewDetails={onViewDetails} />
    );

    await userEvent.click(screen.getByTestId("view-details-1"));
    expect(onViewDetails).toHaveBeenCalledWith("1");
  });

  it("shows skeleton rows when loading", () => {
    render(
      <AdminUsersTable users={[]} onViewDetails={vi.fn()} isLoading />
    );

    expect(screen.getByTestId("skeleton-row-0")).toBeInTheDocument();
    expect(screen.getByTestId("skeleton-row-4")).toBeInTheDocument();
  });

  it("shows 5 skeleton rows when loading", () => {
    render(
      <AdminUsersTable users={[]} onViewDetails={vi.fn()} isLoading />
    );

    for (let i = 0; i < 5; i++) {
      expect(screen.getByTestId(`skeleton-row-${i}`)).toBeInTheDocument();
    }
  });

  it("shows empty state when isEmpty", () => {
    render(
      <AdminUsersTable users={[]} onViewDetails={vi.fn()} isEmpty />
    );

    expect(screen.getByTestId("empty-state")).toBeInTheDocument();
    expect(screen.getByText("Nenhum usuario encontrado")).toBeInTheDocument();
  });

  it("shows retry button when isError", () => {
    render(
      <AdminUsersTable users={[]} onViewDetails={vi.fn()} isError onRetry={vi.fn()} />
    );

    expect(screen.getByTestId("error-state")).toBeInTheDocument();
    expect(screen.getByTestId("retry-button")).toBeInTheDocument();
    expect(screen.getByText("Erro ao carregar usuarios")).toBeInTheDocument();
  });

  it("calls onRetry when retry button clicked", async () => {
    const onRetry = vi.fn();
    render(
      <AdminUsersTable users={[]} onViewDetails={vi.fn()} isError onRetry={onRetry} />
    );

    await userEvent.click(screen.getByTestId("retry-button"));
    expect(onRetry).toHaveBeenCalled();
  });

  it("does not show retry button when onRetry is not provided", () => {
    render(
      <AdminUsersTable users={[]} onViewDetails={vi.fn()} isError />
    );

    expect(screen.getByTestId("error-state")).toBeInTheDocument();
    expect(screen.queryByTestId("retry-button")).not.toBeInTheDocument();
  });
});

describe("Admin Users Table - Delete and Deleted Users", () => {
  it("Delete button disabled for deleted users", () => {
    const onDelete = vi.fn();
    render(
      <AdminUsersTable users={mockUsers} onViewDetails={vi.fn()} onDelete={onDelete} />
    );

    const deleteButton = screen.getByTestId("delete-action-3");
    expect(deleteButton).toBeDisabled();
  });

  it("Delete button enabled for non-deleted users", () => {
    const onDelete = vi.fn();
    render(
      <AdminUsersTable users={mockUsers} onViewDetails={vi.fn()} onDelete={onDelete} />
    );

    const deleteButton = screen.getByTestId("delete-action-1");
    expect(deleteButton).not.toBeDisabled();
  });

  it("Deleted badge shows for users with deletedAt", () => {
    render(
      <AdminUsersTable users={mockUsers} onViewDetails={vi.fn()} />
    );

    const badge = screen.getByTestId("status-badge-3");
    expect(badge).toHaveTextContent("Deletado");
    expect(badge).toHaveClass("bg-destructive");
  });

  it("Row styling different for deleted users (opacity reduced)", () => {
    render(
      <AdminUsersTable users={mockUsers} onViewDetails={vi.fn()} />
    );

    const deletedRow = screen.getByTestId("user-row-3");
    expect(deletedRow).toHaveClass("opacity-60");

    const activeRow = screen.getByTestId("user-row-1");
    expect(activeRow).not.toHaveClass("opacity-60");
  });

  it("Edit button disabled for deleted users", () => {
    const onEdit = vi.fn();
    render(
      <AdminUsersTable users={mockUsers} onViewDetails={vi.fn()} onEdit={onEdit} />
    );

    const editButton = screen.getByTestId("edit-action-3");
    expect(editButton).toBeDisabled();
  });

  it("Block/Unblock buttons not shown for deleted users", () => {
    render(
      <AdminUsersTable users={mockUsers} onViewDetails={vi.fn()} onBlock={vi.fn()} onUnblock={vi.fn()} />
    );

    expect(screen.queryByTestId("block-action-3")).not.toBeInTheDocument();
    expect(screen.queryByTestId("unblock-action-3")).not.toBeInTheDocument();
  });

  it("calls onDelete when delete button clicked for non-deleted user", async () => {
    const onDelete = vi.fn();
    render(
      <AdminUsersTable users={mockUsers} onViewDetails={vi.fn()} onDelete={onDelete} />
    );

    await userEvent.click(screen.getByTestId("delete-action-1"));
    expect(onDelete).toHaveBeenCalledWith("1");
  });

  it("does not render delete button when onDelete prop is not provided", () => {
    render(
      <AdminUsersTable users={mockUsers} onViewDetails={vi.fn()} />
    );

    expect(screen.queryByTestId("delete-action-1")).not.toBeInTheDocument();
    expect(screen.queryByTestId("delete-action-2")).not.toBeInTheDocument();
    expect(screen.queryByTestId("delete-action-3")).not.toBeInTheDocument();
  });
});
