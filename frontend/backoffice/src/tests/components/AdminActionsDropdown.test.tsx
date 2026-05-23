// ---------------------------------------------------------------------------
// AdminActionsDropdown — render, isSelf logic, callbacks
// ---------------------------------------------------------------------------

import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen } from "@testing-library/react";
import { AdminActionsDropdown } from "@/components/molecules/AdminActionsDropdown";
import type { AdminUserDto } from "@/lib/admin-api";

vi.mock("@/lib/admin-auth-context", () => ({
  useAdminAuth: vi.fn(),
}));

import { useAdminAuth } from "@/lib/admin-auth-context";

const ADMIN: AdminUserDto = {
  id: "user-abc-123",
  email: "admin@test.com",
  fullName: "Test Admin",
  isEnabled: true,
  hasTemporaryPassword: false,
};

beforeEach(() => vi.clearAllMocks());

function makeAuthContext(adminId: string) {
  return {
    admin: { adminId, email: "auth@test.com", name: "Auth", role: "Admin" },
    isAuthenticated: true,
    isLoading: false,
    logout: vi.fn(),
    refreshSession: vi.fn(),
  };
}

describe("AdminActionsDropdown", () => {
  it("renders disabled button when admin is self", () => {
    vi.mocked(useAdminAuth).mockReturnValue(makeAuthContext("user-abc-123") as any);
    render(
      <AdminActionsDropdown
        admin={ADMIN}
        onEdit={vi.fn()}
        onResetPassword={vi.fn()}
        onDeactivate={vi.fn()}
        onReactivate={vi.fn()}
      />
    );
    expect(screen.getByTestId("actions-dropdown-disabled")).toBeInTheDocument();
    expect(screen.getByTestId("actions-dropdown-disabled")).toBeDisabled();
  });

  it("renders action trigger when admin is not self", () => {
    vi.mocked(useAdminAuth).mockReturnValue(makeAuthContext("other-user-999") as any);
    render(
      <AdminActionsDropdown
        admin={ADMIN}
        onEdit={vi.fn()}
        onResetPassword={vi.fn()}
        onDeactivate={vi.fn()}
        onReactivate={vi.fn()}
      />
    );
    expect(screen.getByTestId("actions-dropdown-trigger")).toBeInTheDocument();
    expect(screen.queryByTestId("actions-dropdown-disabled")).not.toBeInTheDocument();
  });

  it("is case-insensitive for isSelf check", () => {
    vi.mocked(useAdminAuth).mockReturnValue(makeAuthContext("USER-ABC-123") as any);
    render(
      <AdminActionsDropdown
        admin={ADMIN}
        onEdit={vi.fn()}
        onResetPassword={vi.fn()}
        onDeactivate={vi.fn()}
        onReactivate={vi.fn()}
      />
    );
    expect(screen.getByTestId("actions-dropdown-disabled")).toBeInTheDocument();
  });

  it("renders trigger button with accessible label", () => {
    vi.mocked(useAdminAuth).mockReturnValue(makeAuthContext("other-user") as any);
    render(
      <AdminActionsDropdown
        admin={ADMIN}
        onEdit={vi.fn()}
        onResetPassword={vi.fn()}
        onDeactivate={vi.fn()}
        onReactivate={vi.fn()}
      />
    );
    expect(screen.getByTestId("actions-dropdown-trigger")).toHaveAttribute("aria-label");
  });
});
