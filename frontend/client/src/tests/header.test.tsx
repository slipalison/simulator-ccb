import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { Header } from "@/components/organisms/Header";
import { AuthProvider } from "@/lib/auth-context";

// Mock auth context — logout is now a synchronous redirect (ACF)
const mockLogout = vi.fn();
vi.mock("@/lib/auth-context", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@/lib/auth-context")>();
  return {
    ...actual,
    useAuth: () => ({
      auth: { isAuthenticated: true, isLoading: false, userName: "Test User", email: "test@example.com" },
      logout: mockLogout,
      login: vi.fn(),
    }),
    AuthProvider: ({ children }: { children: React.ReactNode }) => children,
  };
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

    expect(screen.getByText(/Onboarding/i)).toBeInTheDocument();
  });

  it("shows theme toggle button", () => {
    renderHeader();

    const themeToggle = screen.getByRole("button", { name: /alternar tema/i });
    expect(themeToggle).toBeInTheDocument();
  });

  it("shows user menu dropdown when authenticated", async () => {
    const { user } = renderHeader();

    const userButton = screen.getByRole("button", { name: /user menu/i });
    expect(userButton).toBeInTheDocument();

    await user.click(userButton);

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

    await user.click(screen.getByText(/Sair/i));

    await waitFor(() => {
      expect(mockLogout).toHaveBeenCalled();
    });
  });
});
