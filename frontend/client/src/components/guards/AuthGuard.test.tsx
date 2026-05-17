import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen } from "@testing-library/react";
import { useNavigate } from "@tanstack/react-router";
import { useAuth } from "@/lib/auth-context";
import { AuthGuard } from "./AuthGuard";

// ---------------------------------------------------------------------------
// Mocks
// ---------------------------------------------------------------------------

vi.mock("@tanstack/react-router", () => ({
  useNavigate: vi.fn(),
}));

vi.mock("@/lib/auth-context", () => ({
  useAuth: vi.fn(),
}));

const mockNavigate = vi.fn();

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

function setupAuth(isLoading: boolean, isAuthenticated: boolean) {
  vi.mocked(useAuth).mockReturnValue({
    auth: {
      isLoading,
      isAuthenticated,
      userName: null,
      email: null,
      accessGroup: null,
      companyId: null,
      permissions: [],
    },
    login: vi.fn(),
    logout: vi.fn(),
  });
}

function renderGuard() {
  return render(
    <AuthGuard>
      <div data-testid="protected-content">Protected</div>
    </AuthGuard>
  );
}

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

describe("AuthGuard — isLoading race fix (T-5a)", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(useNavigate).mockReturnValue(mockNavigate);
  });

  describe("State: isLoading=true (tryRestore in progress)", () => {
    beforeEach(() => {
      setupAuth(true, false);
    });

    it("renders the loading skeleton", () => {
      renderGuard();
      expect(screen.getByTestId("auth-guard-loading")).toBeInTheDocument();
    });

    it("does NOT render protected children while loading", () => {
      renderGuard();
      expect(screen.queryByTestId("protected-content")).not.toBeInTheDocument();
    });

    it("does NOT call navigate while isLoading is true", () => {
      renderGuard();
      expect(mockNavigate).not.toHaveBeenCalled();
    });

    it("shows the spinner element", () => {
      renderGuard();
      const spinner = screen
        .getByTestId("auth-guard-loading")
        .querySelector(".animate-spin");
      expect(spinner).toBeInTheDocument();
    });
  });

  describe("State: isLoading=false, isAuthenticated=false (unauthenticated)", () => {
    beforeEach(() => {
      setupAuth(false, false);
    });

    it("calls navigate with /login and replace:true", () => {
      renderGuard();
      expect(mockNavigate).toHaveBeenCalledWith({
        to: "/login",
        replace: true,
      });
    });

    it("renders null (no skeleton, no children) after redirect intent", () => {
      renderGuard();
      // null render — neither skeleton nor children present
      expect(screen.queryByTestId("auth-guard-loading")).not.toBeInTheDocument();
      expect(screen.queryByTestId("protected-content")).not.toBeInTheDocument();
    });
  });

  describe("State: isLoading=false, isAuthenticated=true (authenticated)", () => {
    beforeEach(() => {
      setupAuth(false, true);
    });

    it("renders children", () => {
      renderGuard();
      expect(screen.getByTestId("protected-content")).toBeInTheDocument();
    });

    it("does NOT render the loading skeleton", () => {
      renderGuard();
      expect(screen.queryByTestId("auth-guard-loading")).not.toBeInTheDocument();
    });

    it("does NOT call navigate", () => {
      renderGuard();
      expect(mockNavigate).not.toHaveBeenCalled();
    });
  });
});
