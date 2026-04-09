import { describe, it, expect, vi } from "vitest";
import { render, screen, fireEvent } from "@testing-library/react";
import { UserDetailCard } from "@/components/molecules/UserDetailCard";
import type { UserDetailDto } from "@/lib/admin-api";

const mockPFUser: UserDetailDto = {
  id: "1",
  name: "Joao Silva",
  email: "joao@example.com",
  phone: "(11) 99999-9999",
  document: "123.456.789-00",
  type: "PF",
  createdAt: "2026-01-15T10:00:00Z",
  keycloakEnabled: true,
  keycloakEmailVerified: true,
  keycloakUserId: "kc-123",
};

const mockPJUser: UserDetailDto = {
  id: "2",
  name: "Acme Corp",
  email: "contact@acme.com",
  phone: "(11) 88888-8888",
  document: "12.345.678/0001-99",
  type: "PJ",
  razaoSocial: "Acme Corporation Ltda",
  createdAt: "2026-02-20T14:30:00Z",
  keycloakEnabled: false,
  keycloakEmailVerified: false,
  keycloakUserId: "kc-456",
};

const mockDeletedUser: UserDetailDto = {
  ...mockPFUser,
  deletedAt: "2026-03-01T09:00:00Z",
};

describe("User Detail Card", () => {
  it("renders PF user data correctly", () => {
    render(<UserDetailCard user={mockPFUser} />);

    expect(screen.getByTestId("user-name")).toHaveTextContent("Joao Silva");
    expect(screen.getByTestId("user-email")).toHaveTextContent("joao@example.com");
    expect(screen.getByTestId("user-type")).toHaveTextContent("Pessoa Fisica");
    expect(screen.getByTestId("user-document")).toHaveTextContent("123.456.789-00");
    expect(screen.getByTestId("user-phone")).toHaveTextContent("(11) 99999-9999");
    expect(screen.getByTestId("user-created-at")).toHaveTextContent("15/01/2026");
  });

  it("renders PJ user data with razao social", () => {
    render(<UserDetailCard user={mockPJUser} />);

    expect(screen.getByTestId("user-name")).toHaveTextContent("Acme Corp");
    expect(screen.getByTestId("user-type")).toHaveTextContent("Pessoa Juridica");
    expect(screen.getByTestId("user-document")).toHaveTextContent("12.345.678/0001-99");
    expect(screen.getByTestId("pj-info")).toBeInTheDocument();
    expect(screen.getByTestId("user-razao-social")).toHaveTextContent("Acme Corporation Ltda");
  });

  it("does not show PJ section for PF users", () => {
    render(<UserDetailCard user={mockPFUser} />);

    expect(screen.queryByTestId("pj-info")).not.toBeInTheDocument();
    expect(screen.queryByTestId("user-razao-social")).not.toBeInTheDocument();
  });

  it("shows Keycloak status badges", () => {
    render(<UserDetailCard user={mockPFUser} />);

    expect(screen.getByTestId("status-enabled")).toBeInTheDocument();
    expect(screen.getByTestId("status-email-verified")).toBeInTheDocument();
  });

  it("shows Keycloak status badges for disabled user", () => {
    render(<UserDetailCard user={mockPJUser} />);

    expect(screen.getByTestId("status-disabled")).toBeInTheDocument();
    expect(screen.getByTestId("status-email-not-verified")).toBeInTheDocument();
  });

  it("shows block button when user is enabled", () => {
    render(<UserDetailCard user={mockPFUser} />);

    expect(screen.getByTestId("block-button")).toBeInTheDocument();
    expect(screen.queryByTestId("unblock-button")).not.toBeInTheDocument();
  });

  it("shows unblock button when user is disabled", () => {
    render(<UserDetailCard user={mockPJUser} />);

    expect(screen.getByTestId("unblock-button")).toBeInTheDocument();
    expect(screen.queryByTestId("block-button")).not.toBeInTheDocument();
  });

  it("disables action buttons when user is deleted", () => {
    render(<UserDetailCard user={mockDeletedUser} />);

    expect(screen.getByTestId("edit-button")).toBeDisabled();
    expect(screen.queryByTestId("block-button")).not.toBeInTheDocument();
    expect(screen.queryByTestId("unblock-button")).not.toBeInTheDocument();
    expect(screen.queryByTestId("delete-button")).not.toBeInTheDocument();
  });

  it("shows deleted date when user is deleted", () => {
    render(<UserDetailCard user={mockDeletedUser} />);

    expect(screen.getByTestId("deleted-info")).toBeInTheDocument();
    expect(screen.getByTestId("user-deleted-at")).toHaveTextContent("01/03/2026");
  });

  it("does not show deleted section for active users", () => {
    render(<UserDetailCard user={mockPFUser} />);

    expect(screen.queryByTestId("deleted-info")).not.toBeInTheDocument();
    expect(screen.queryByTestId("user-deleted-at")).not.toBeInTheDocument();
  });

  it("calls onEdit when Edit button clicked", () => {
    const onEdit = vi.fn();
    render(<UserDetailCard user={mockPFUser} onEdit={onEdit} />);

    fireEvent.click(screen.getByTestId("edit-button"));
    expect(onEdit).toHaveBeenCalledTimes(1);
  });

  it("calls onBlock when Block button clicked", () => {
    const onBlock = vi.fn();
    render(<UserDetailCard user={mockPFUser} onBlock={onBlock} />);

    fireEvent.click(screen.getByTestId("block-button"));
    expect(onBlock).toHaveBeenCalledTimes(1);
  });

  it("calls onUnblock when Unblock button clicked", () => {
    const onUnblock = vi.fn();
    render(<UserDetailCard user={mockPJUser} onUnblock={onUnblock} />);

    fireEvent.click(screen.getByTestId("unblock-button"));
    expect(onUnblock).toHaveBeenCalledTimes(1);
  });

  it("calls onDelete when Delete button clicked", () => {
    const onDelete = vi.fn();
    render(<UserDetailCard user={mockPFUser} onDelete={onDelete} />);

    fireEvent.click(screen.getByTestId("delete-button"));
    expect(onDelete).toHaveBeenCalledTimes(1);
  });

  it("shows dash when document is undefined", () => {
    const userWithoutDoc = { ...mockPFUser, document: undefined };
    render(<UserDetailCard user={userWithoutDoc} />);

    expect(screen.getByTestId("user-document")).toHaveTextContent("-");
  });

  it("shows dash when phone is empty", () => {
    const userWithoutPhone = { ...mockPFUser, phone: "" };
    render(<UserDetailCard user={userWithoutPhone} />);

    expect(screen.getByTestId("user-phone")).toHaveTextContent("-");
  });
});
