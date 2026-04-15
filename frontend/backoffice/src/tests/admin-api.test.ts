import { describe, it, expect, vi, beforeEach } from "vitest";
import {
  getAdminMe,
  AdminApiError,
} from "@/lib/admin-api";

// ---------------------------------------------------------------------------
// Mock fetch
// ---------------------------------------------------------------------------

const mockFetch = vi.fn();
global.fetch = mockFetch;

describe("admin-api.ts", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockFetch.mockReset();
  });

  // ---------------------------------------------------------------------------
  // getAdminMe
  // ---------------------------------------------------------------------------

  describe("getAdminMe", () => {
    it("calls GET /auth/me with credentials: include", async () => {
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: () =>
          Promise.resolve({
            adminName: "Test Admin",
            adminEmail: "test@onboarding.local",
          }),
      });

      const result = await getAdminMe();

      expect(mockFetch).toHaveBeenCalledWith(
        "/auth/me",
        expect.objectContaining({
          method: "GET",
          credentials: "include",
        })
      );
      expect(result).toEqual({
        adminName: "Test Admin",
        adminEmail: "test@onboarding.local",
      });
    });

    it("throws AdminApiError on failure", async () => {
      mockFetch.mockResolvedValueOnce({ ok: false, status: 401 });

      await expect(getAdminMe()).rejects.toThrow(AdminApiError);
    });
  });
});
