// ---------------------------------------------------------------------------
// use-admin-list-search.test.tsx — unit tests for useAdminListSearch hook
// BFE-5: adds coverage for src/lib/use-admin-list-search.ts (D-2 gate 80%).
//
// Tests:
//   (a) render: hook returns setPage, setSearch, setEmpresaId functions
//   (b) setPage(N) → navigate called with { to: path, search: { ...current, page: N } }
//   (c) setSearch(s) → navigate called with { ...current, search: s, page: 1 }
//   (d) setSearch("") → navigate called with search: undefined (falsy clears to undefined)
//   (e) setEmpresaId(id) → navigate called with { ...current, empresaId: id, page: 1 }
//   (f) setEmpresaId(undefined) → empresaId removed (set to undefined)
//
// Strategy:
//   - Mock useNavigate from @tanstack/react-router to return a vi.fn() spy.
//   - Use renderHook() from @testing-library/react to execute the hook.
//   - Assert navigate spy call shape matches the expected search param updates.
// ---------------------------------------------------------------------------

import { describe, it, expect, vi, beforeEach } from "vitest";
import { renderHook, act } from "@testing-library/react";

// ---------------------------------------------------------------------------
// Mock @tanstack/react-router — only useNavigate is needed.
// Using importOriginal to preserve any other exports the hook may depend on.
// ---------------------------------------------------------------------------
const mockNavigate = vi.fn();

vi.mock("@tanstack/react-router", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@tanstack/react-router")>();
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  };
});

// ---------------------------------------------------------------------------
// Mock @/lib/admin-fundos-schemas — only AdminListSearch type is used, but
// the import path must resolve. No runtime value needed from the schema.
// ---------------------------------------------------------------------------
vi.mock("@/lib/admin-fundos-schemas", () => ({
  adminListSearchSchema: {},
}));

// ---------------------------------------------------------------------------
// Static import AFTER mocks.
// ---------------------------------------------------------------------------
import { useAdminListSearch, DEFAULT_PAGE_SIZE } from "@/lib/use-admin-list-search";

// ---------------------------------------------------------------------------
// Default current-search used by most tests.
// ---------------------------------------------------------------------------
const BASE_SEARCH = { page: 2, search: "foo", empresaId: "empresa-uuid-1" };
const TEST_PATH = "/admin/fundos";

beforeEach(() => {
  vi.clearAllMocks();
});

// ---------------------------------------------------------------------------
// (a) Render — hook returns the three updater functions
// ---------------------------------------------------------------------------
describe("useAdminListSearch — hook surface", () => {
  it("returns setPage, setSearch, setEmpresaId", () => {
    const { result } = renderHook(() =>
      useAdminListSearch(BASE_SEARCH, TEST_PATH)
    );

    expect(typeof result.current.setPage).toBe("function");
    expect(typeof result.current.setSearch).toBe("function");
    expect(typeof result.current.setEmpresaId).toBe("function");
  });

  it("does not call navigate on mount (no side effects at init)", () => {
    renderHook(() => useAdminListSearch(BASE_SEARCH, TEST_PATH));
    expect(mockNavigate).not.toHaveBeenCalled();
  });
});

// ---------------------------------------------------------------------------
// (b) DEFAULT_PAGE_SIZE export
// ---------------------------------------------------------------------------
describe("DEFAULT_PAGE_SIZE", () => {
  it("is 20", () => {
    expect(DEFAULT_PAGE_SIZE).toBe(20);
  });
});

// ---------------------------------------------------------------------------
// (c) setPage(N) → navigate called with updated page
// ---------------------------------------------------------------------------
describe("useAdminListSearch — setPage", () => {
  it("navigates with { to: path, search: { ...current, page: N } }", () => {
    const { result } = renderHook(() =>
      useAdminListSearch(BASE_SEARCH, TEST_PATH)
    );

    act(() => result.current.setPage(5));

    expect(mockNavigate).toHaveBeenCalledTimes(1);
    expect(mockNavigate).toHaveBeenCalledWith({
      to: TEST_PATH,
      search: { ...BASE_SEARCH, page: 5 },
    });
  });

  it("navigates to page 1 when setPage(1) called", () => {
    const search = { page: 3, search: "bar", empresaId: undefined };
    const { result } = renderHook(() =>
      useAdminListSearch(search, TEST_PATH)
    );

    act(() => result.current.setPage(1));

    expect(mockNavigate).toHaveBeenCalledWith({
      to: TEST_PATH,
      search: { ...search, page: 1 },
    });
  });

  it("preserves existing search and empresaId when changing page", () => {
    const { result } = renderHook(() =>
      useAdminListSearch(BASE_SEARCH, TEST_PATH)
    );

    act(() => result.current.setPage(10));

    const callArg = mockNavigate.mock.calls[0][0] as {
      search: typeof BASE_SEARCH;
    };
    expect(callArg.search.search).toBe("foo");
    expect(callArg.search.empresaId).toBe("empresa-uuid-1");
  });
});

// ---------------------------------------------------------------------------
// (d) setSearch(s) → search updated, page reset to 1
// ---------------------------------------------------------------------------
describe("useAdminListSearch — setSearch", () => {
  it("navigates with updated search string and page reset to 1", () => {
    const { result } = renderHook(() =>
      useAdminListSearch(BASE_SEARCH, TEST_PATH)
    );

    act(() => result.current.setSearch("newquery"));

    expect(mockNavigate).toHaveBeenCalledTimes(1);
    expect(mockNavigate).toHaveBeenCalledWith({
      to: TEST_PATH,
      search: { ...BASE_SEARCH, search: "newquery", page: 1 },
    });
  });

  it("sets search to undefined when empty string passed (clears filter)", () => {
    const { result } = renderHook(() =>
      useAdminListSearch(BASE_SEARCH, TEST_PATH)
    );

    act(() => result.current.setSearch(""));

    expect(mockNavigate).toHaveBeenCalledTimes(1);
    const callArg = mockNavigate.mock.calls[0][0] as {
      search: { search: string | undefined; page: number };
    };
    // Falsy string → undefined (the || undefined pattern)
    expect(callArg.search.search).toBeUndefined();
    expect(callArg.search.page).toBe(1);
  });

  it("preserves empresaId when updating search string", () => {
    const { result } = renderHook(() =>
      useAdminListSearch(BASE_SEARCH, TEST_PATH)
    );

    act(() => result.current.setSearch("updated"));

    const callArg = mockNavigate.mock.calls[0][0] as {
      search: typeof BASE_SEARCH;
    };
    expect(callArg.search.empresaId).toBe("empresa-uuid-1");
  });
});

// ---------------------------------------------------------------------------
// (e) setEmpresaId(id) → empresaId updated, page reset to 1
// ---------------------------------------------------------------------------
describe("useAdminListSearch — setEmpresaId", () => {
  it("navigates with updated empresaId and page reset to 1", () => {
    const { result } = renderHook(() =>
      useAdminListSearch(BASE_SEARCH, TEST_PATH)
    );

    act(() => result.current.setEmpresaId("new-empresa-uuid"));

    expect(mockNavigate).toHaveBeenCalledTimes(1);
    expect(mockNavigate).toHaveBeenCalledWith({
      to: TEST_PATH,
      search: { ...BASE_SEARCH, empresaId: "new-empresa-uuid", page: 1 },
    });
  });

  it("sets empresaId to undefined when clearing the filter", () => {
    const { result } = renderHook(() =>
      useAdminListSearch(BASE_SEARCH, TEST_PATH)
    );

    act(() => result.current.setEmpresaId(undefined));

    expect(mockNavigate).toHaveBeenCalledWith({
      to: TEST_PATH,
      search: { ...BASE_SEARCH, empresaId: undefined, page: 1 },
    });
  });

  it("preserves search string when updating empresaId", () => {
    const { result } = renderHook(() =>
      useAdminListSearch(BASE_SEARCH, TEST_PATH)
    );

    act(() => result.current.setEmpresaId("another-uuid"));

    const callArg = mockNavigate.mock.calls[0][0] as {
      search: typeof BASE_SEARCH;
    };
    expect(callArg.search.search).toBe("foo");
  });

  it("uses the path argument as the to value for navigate", () => {
    const customPath = "/admin/cedentes";
    const { result } = renderHook(() =>
      useAdminListSearch({ page: 1, search: undefined, empresaId: undefined }, customPath)
    );

    act(() => result.current.setEmpresaId("emp-1"));

    expect(mockNavigate).toHaveBeenCalledWith(
      expect.objectContaining({ to: customPath })
    );
  });
});
