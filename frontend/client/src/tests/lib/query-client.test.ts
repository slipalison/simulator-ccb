// ---------------------------------------------------------------------------
// query-client.test.ts — asserts QueryClient default config (D-2, D-24)
// ---------------------------------------------------------------------------

import { describe, it, expect } from "vitest";
import { queryClient } from "@/lib/query-client";

describe("queryClient defaults (D-24)", () => {
  it("queries have retry: 1", () => {
    const defaults = queryClient.getDefaultOptions();
    expect(defaults.queries?.retry).toBe(1);
  });

  it("queries have staleTime: 30_000", () => {
    const defaults = queryClient.getDefaultOptions();
    expect(defaults.queries?.staleTime).toBe(30_000);
  });

  it("queries have refetchOnWindowFocus: false", () => {
    const defaults = queryClient.getDefaultOptions();
    expect(defaults.queries?.refetchOnWindowFocus).toBe(false);
  });

  it("mutations have retry: 0 (pessimistic)", () => {
    const defaults = queryClient.getDefaultOptions();
    expect(defaults.mutations?.retry).toBe(0);
  });
});
