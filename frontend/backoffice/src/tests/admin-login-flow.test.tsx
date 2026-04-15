import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import { render, screen } from "@testing-library/react";
import { userEvent } from "@testing-library/user-event";
import { AuthLoginPage } from "@/components/pages/AuthLoginPage";
import { AuthCallbackPage } from "@/components/pages/AuthCallbackPage";
import { AuthErrorPage } from "@/components/pages/AuthErrorPage";
import { AdminAuthProvider } from "@/lib/admin-auth-context";
import * as adminApi from "@/lib/admin-api";

vi.mock("@/lib/admin-api", () => ({
  getAdminMe: vi.fn(),
  AdminApiError: class AdminApiError extends Error {
    constructor(message: string) {
      super(message);
      this.name = "AdminApiError";
    }
  },
}));

describe("Auth Code Flow Pages", () => {
  let originalLocation: Location;

  beforeEach(() => {
    vi.clearAllMocks();
    originalLocation = window.location;
    Object.defineProperty(window, "location", {
      writable: true,
      value: { href: "" },
    });
  });

  afterEach(() => {
    Object.defineProperty(window, "location", {
      writable: true,
      value: originalLocation,
    });
  });

  describe("AuthLoginPage", () => {
    it("redirects to /auth/login server route on mount", async () => {
      vi.mocked(adminApi.getAdminMe).mockRejectedValue(new Error("No session"));

      render(
        <AdminAuthProvider>
          <AuthLoginPage />
        </AdminAuthProvider>
      );

      await vi.waitFor(() => {
        expect(window.location.href).toBe("/auth/login");
      });
    });

    it("shows redirecting message", () => {
      vi.mocked(adminApi.getAdminMe).mockRejectedValue(new Error("No session"));

      render(
        <AdminAuthProvider>
          <AuthLoginPage />
        </AdminAuthProvider>
      );

      expect(screen.getByText(/redirecionando/i)).toBeInTheDocument();
    });
  });

  describe("AuthCallbackPage", () => {
    it("shows completing authentication message", () => {
      render(<AuthCallbackPage />);
      expect(screen.getByText(/completando/i)).toBeInTheDocument();
    });
  });

  describe("AuthErrorPage", () => {
    it("shows error message and retry button", () => {
      render(<AuthErrorPage />);
      expect(screen.getByText(/erro de autenticacao/i)).toBeInTheDocument();
      expect(
        screen.getByRole("button", { name: /tentar novamente/i })
      ).toBeInTheDocument();
    });

    it("retry button redirects to /auth/login", async () => {
      const user = userEvent.setup();
      render(<AuthErrorPage />);

      await user.click(screen.getByRole("button", { name: /tentar novamente/i }));
      expect(window.location.href).toBe("/auth/login");
    });
  });
});
