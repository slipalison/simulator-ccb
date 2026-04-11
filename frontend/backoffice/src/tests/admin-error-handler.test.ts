import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import {
  checkAdminResponse,
  handleAdminApiCall,
  SessionExpiredError,
  AccessDeniedError,
} from "@/lib/admin-error-handler";

// Mock sonner toast
vi.mock("sonner", () => ({
  toast: {
    error: vi.fn(),
    success: vi.fn(),
  },
}));

import { toast } from "sonner";

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

describe("admin-error-handler.ts", () => {
  // ---------------------------------------------------------------------------
  // checkAdminResponse
  // ---------------------------------------------------------------------------

  describe("checkAdminResponse", () => {
    it("returns true for 200 response", () => {
      const response = new Response(JSON.stringify({ data: "ok" }), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      });

      expect(checkAdminResponse(response)).toBe(true);
    });

    it("401 throws SessionExpiredError, shows toast, redirects to login", () => {
      const response = new Response(null, { status: 401 });

      expect(() => checkAdminResponse(response)).toThrow(SessionExpiredError);
      expect(toast.error).toHaveBeenCalledWith("Sessao expirada", expect.any(Object));
      expect(window.location.href).toBe("/admin/login?expired=true");
    });

    it("403 throws AccessDeniedError, redirects to access-denied", () => {
      const response = new Response(null, { status: 403 });

      expect(() => checkAdminResponse(response)).toThrow(AccessDeniedError);
      expect(window.location.href).toBe("/admin/access-denied");
    });

    it("500 throws Error with toast", () => {
      const response = new Response(null, { status: 500 });

      expect(() => checkAdminResponse(response)).toThrow(Error);
      expect(toast.error).toHaveBeenCalledWith("Erro interno", expect.any(Object));
    });

    it("503 throws Error with toast", () => {
      const response = new Response(null, { status: 503 });

      expect(() => checkAdminResponse(response)).toThrow(Error);
    });
  });

  // ---------------------------------------------------------------------------
  // handleAdminApiCall
  // ---------------------------------------------------------------------------

  describe("handleAdminApiCall", () => {
    it("returns parsed JSON on success", async () => {
      const mockResponse = new Response(JSON.stringify({ users: [] }), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      });

      const apiCall = vi.fn().mockResolvedValue(mockResponse);

      const result = await handleAdminApiCall(apiCall);

      expect(result).toEqual({ users: [] });
    });

    it("throws SessionExpiredError on 401", async () => {
      const mockResponse = new Response(null, { status: 401 });
      const apiCall = vi.fn().mockResolvedValue(mockResponse);

      await expect(handleAdminApiCall(apiCall)).rejects.toThrow(SessionExpiredError);
    });

    it("throws AccessDeniedError on 403", async () => {
      const mockResponse = new Response(null, { status: 403 });
      const apiCall = vi.fn().mockResolvedValue(mockResponse);

      await expect(handleAdminApiCall(apiCall)).rejects.toThrow(AccessDeniedError);
    });

    it("throws Error on 500", async () => {
      const mockResponse = new Response(null, { status: 500 });
      const apiCall = vi.fn().mockResolvedValue(mockResponse);

      await expect(handleAdminApiCall(apiCall)).rejects.toThrow(Error);
    });
  });
});
