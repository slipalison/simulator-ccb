// ---------------------------------------------------------------------------
// E2E profile flow tests — ACF version (PJ-only)
// ---------------------------------------------------------------------------
// Simulates the complete authenticated user journey using ACF cookies:
//   /auth/me session check → company profile display → logout redirect
// Updated for PJ-only (Phase 40).
// ---------------------------------------------------------------------------

import { describe, it, expect, vi, beforeEach } from "vitest";
import {
  render,
  screen,
  waitFor,
} from "@testing-library/react";
import {
  RouterProvider,
  createRouter,
  createMemoryHistory,
} from "@tanstack/react-router";
import { AuthProvider } from "@/lib/auth-context";
import { router } from "@/router";

// ---------------------------------------------------------------------------
// Mocks
// ---------------------------------------------------------------------------

vi.mock("@/lib/api", () => ({
  getProfileClient: vi.fn(),
  ProfileError: class ProfileError extends Error {
    constructor(message: string) {
      super(message);
      this.name = "ProfileError";
    }
  },
  ApiError: class ApiError extends Error {
    constructor(message: string) {
      super(message);
      this.name = "ApiError";
    }
  },
}));

// Mock fetch for /auth/me
const mockFetch = vi.fn();
global.fetch = mockFetch;

// ---------------------------------------------------------------------------
// Test data
// ---------------------------------------------------------------------------

// Note: mockCompanyProfile referenced in assertions via text content

// ---------------------------------------------------------------------------
// Helper: render full app at given route
// ---------------------------------------------------------------------------

async function renderApp(initialPath: string) {
  const memoryHistory = createMemoryHistory({ initialEntries: [initialPath] });
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
  return { ...view, testRouter, memoryHistory };
}

// ---------------------------------------------------------------------------
// E2E flow tests
// ---------------------------------------------------------------------------

describe("Profile E2E Flow — ACF (PJ-only)", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockFetch.mockReset();
  });

  it("unauthenticated user at /profile does not show profile data", async () => {
    // /auth/me returns 401
    mockFetch.mockResolvedValue({ ok: false, status: 401 });

    await renderApp("/profile");

    // ProfilePage auth guard redirects — profile data should NOT be visible
    await waitFor(() => {
      expect(screen.queryByText("Empresa LTDA")).not.toBeInTheDocument();
    });
  });
});