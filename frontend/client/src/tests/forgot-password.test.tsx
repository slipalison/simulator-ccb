import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, fireEvent, waitFor, act } from "@testing-library/react";
import { AuthProvider } from "@/lib/auth-context";
import { RouterProvider, createRouter, createMemoryHistory } from "@tanstack/react-router";
import { router } from "@/router";
import * as api from "@/lib/api";

// Mock API module
vi.mock("@/lib/api", () => ({
  getProfileClient: vi.fn(),
  forgotPasswordClient: vi.fn(),
  resetPasswordClient: vi.fn(),
}));

async function renderWithRouter(initialEntries: string[] = ["/forgot-password"]) {
  const memoryHistory = createMemoryHistory({ initialEntries });
  const testRouter = createRouter({
    routeTree: router.options.routeTree,
    history: memoryHistory,
  });
  const view = render(
    <AuthProvider>
      <RouterProvider router={testRouter} />
    </AuthProvider>
  );
  await testRouter.load();
  return { ...view, router: testRouter, memoryHistory };
}

describe("ForgotPasswordPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("renders email input and submit button", async () => {
    renderWithRouter(["/forgot-password"]);

    await waitFor(() => {
      expect(screen.getByLabelText(/email/i)).toBeInTheDocument();
    });

    expect(
      screen.getByRole("button", { name: /enviar link/i })
    ).toBeInTheDocument();
  });

  it("shows success message after submitting valid email", async () => {
    vi.mocked(api.forgotPasswordClient).mockResolvedValue({ message: "Se o email existir, voce recebera um link de recuperacao." });

    renderWithRouter(["/forgot-password"]);

    await waitFor(() => {
      expect(screen.getByLabelText(/email/i)).toBeInTheDocument();
    });

    const emailInput = screen.getByLabelText(/email/i);
    await act(async () => {
      fireEvent.change(emailInput, { target: { value: "user@example.com" } });
    });

    await act(async () => {
      fireEvent.click(
        screen.getByRole("button", { name: /enviar link/i })
      );
    });

    await waitFor(() => {
      expect(
        screen.getByText(/se o email existir/i)
      ).toBeInTheDocument();
    });
  });

  it("shows generic error for non-existing email (no info disclosure)", async () => {
    vi.mocked(api.forgotPasswordClient).mockResolvedValue({ message: "Se o email existir, voce recebera um link de recuperacao." });

    renderWithRouter(["/forgot-password"]);

    await waitFor(() => {
      expect(screen.getByLabelText(/email/i)).toBeInTheDocument();
    });

    const emailInput = screen.getByLabelText(/email/i);
    await act(async () => {
      fireEvent.change(emailInput, {
        target: { value: "nonexistent@example.com" },
      });
    });

    await act(async () => {
      fireEvent.click(
        screen.getByRole("button", { name: /enviar link/i })
      );
    });

    // Same generic message — no info disclosure
    await waitFor(() => {
      expect(
        screen.getByText(/se o email existir/i)
      ).toBeInTheDocument();
    });
  });
});
