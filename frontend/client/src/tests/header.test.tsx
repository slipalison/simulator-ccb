import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { Header } from "@/components/organisms/Header";
import { AuthProvider } from "@/lib/auth-context";

// Mock router
const mockNavigate = vi.fn();
vi.mock("@tanstack/react-router", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@tanstack/react-router")>();
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  };
});

// Mock auth context
const mockLogout = vi.fn(() => {
  window.location.href = "/auth/logout";
});
vi.mock("@/lib/auth-context", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@/lib/auth-context")>();
  return {
    ...actual,
    useAuth: () => ({
      auth: { isAuthenticated: true, isLoading: false },
      logout: mockLogout,
      login: vi.fn(),
    }),
    AuthProvider: ({ children }: { children: React.ReactNode }) => children,
  };
});

// Mock window.location
const originalLocation = window.location;
beforeEach(() => {
  vi.clearAllMocks();
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

function renderHeader() {
  const user = userEvent.setup();
  return { ...render(
    <AuthProvider>
      <Header />
    </AuthProvider>
  ), user };
}

describe("Header", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("renders with logo on left, controls on right", () => {
    renderHeader();

    // Logo should be visible
    expect(screen.getByText(/Onboarding/i)).toBeInTheDocument();
  });

  it("shows theme toggle button", () => {
    renderHeader();

    // Theme toggle should be visible
    const themeToggle = screen.getByRole("button", { name: /alternar tema/i });
    expect(themeToggle).toBeInTheDocument();
  });

  it("shows user menu dropdown when authenticated", async () => {
    const { user } = renderHeader();

    // User menu trigger button should exist
    const userButton = screen.getByRole("button", { name: /user menu/i });
    expect(userButton).toBeInTheDocument();

    // Click to open dropdown
    await user.click(userButton);

    // Dropdown should show menu items
    await waitFor(() => {
      expect(screen.getByText(/Meu Perfil/i)).toBeInTheDocument();
    });
  });

  it("user menu has Profile and Logout items", async () => {
    const { user } = renderHeader();

    const userButton = screen.getByRole("button", { name: /user menu/i });
    await user.click(userButton);

    await waitFor(() => {
      expect(screen.getByText(/Meu Perfil/i)).toBeInTheDocument();
      expect(screen.getByText(/Sair/i)).toBeInTheDocument();
    });

    // Click logout
    await user.click(screen.getByText(/Sair/i));

    await waitFor(() => {
      expect(mockLogout).toHaveBeenCalled();
      // logout() does window.location.href = "/auth/logout"
      expect(window.location.href).toBe("/auth/logout");
    });
  });
});
