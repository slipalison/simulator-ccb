// ---------------------------------------------------------------------------
// admin-companies-api.ts unit tests
// ---------------------------------------------------------------------------

import { describe, it, expect, vi, beforeEach } from "vitest";
import { listCompaniesForFilter } from "@/lib/admin-companies-api";
import { AdminApiError } from "@/lib/admin-api";

vi.mock("@/lib/admin-http-interceptor", () => ({
  adminFetch: vi.fn(),
}));

import { adminFetch } from "@/lib/admin-http-interceptor";
const mockFetch = vi.mocked(adminFetch);

beforeEach(() => vi.clearAllMocks());

const COMPANY_ITEM = {
  id: "123e4567-e89b-12d3-a456-426614174000",
  razaoSocial: "Empresa Alpha",
  email: "contact@alpha.com",
  phone: "11999999999",
  cnpj: "12345678000195",
  isDeleted: false,
};

const DELETED_COMPANY = {
  id: "223e4567-e89b-12d3-a456-426614174000",
  razaoSocial: "Empresa Deletada",
  email: "deleted@test.com",
  phone: "11000000000",
  isDeleted: true,
};

describe("listCompaniesForFilter", () => {
  it("returns mapped company options filtered to non-deleted", async () => {
    mockFetch.mockResolvedValue({
      ok: true,
      status: 200,
      json: () => Promise.resolve({
        items: [COMPANY_ITEM, DELETED_COMPANY],
        totalCount: 2,
        page: 1,
        pageSize: 200,
      }),
    } as unknown as Response);

    const result = await listCompaniesForFilter();

    expect(result).toHaveLength(1);
    expect(result[0]).toEqual({ id: COMPANY_ITEM.id, razaoSocial: COMPANY_ITEM.razaoSocial });
    expect(mockFetch).toHaveBeenCalledWith(
      "/api/admin/companies?pageSize=200&page=1",
      { method: "GET" }
    );
  });

  it("returns empty array when all companies are deleted", async () => {
    mockFetch.mockResolvedValue({
      ok: true,
      status: 200,
      json: () => Promise.resolve({ items: [DELETED_COMPANY], totalCount: 1, page: 1, pageSize: 200 }),
    } as unknown as Response);

    const result = await listCompaniesForFilter();
    expect(result).toHaveLength(0);
  });

  it("returns empty array when items is empty", async () => {
    mockFetch.mockResolvedValue({
      ok: true,
      status: 200,
      json: () => Promise.resolve({ items: [], totalCount: 0, page: 1, pageSize: 200 }),
    } as unknown as Response);

    const result = await listCompaniesForFilter();
    expect(result).toHaveLength(0);
  });

  it("throws AdminApiError on non-ok response", async () => {
    mockFetch.mockResolvedValue({
      ok: false,
      status: 403,
      json: () => Promise.resolve({}),
    } as unknown as Response);

    await expect(listCompaniesForFilter()).rejects.toBeInstanceOf(AdminApiError);
  });

  it("throws AdminApiError on 500", async () => {
    mockFetch.mockResolvedValue({
      ok: false,
      status: 500,
      json: () => Promise.resolve({}),
    } as unknown as Response);

    await expect(listCompaniesForFilter()).rejects.toBeInstanceOf(AdminApiError);
  });
});
