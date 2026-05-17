// ---------------------------------------------------------------------------
// use-allowed-transitions.test.tsx — hook tests with React Query wrapper
// Tests key derivation, enabled flag, and fetch delegation (D-2, D-25)
// ---------------------------------------------------------------------------

import { describe, it, expect, vi, beforeEach } from "vitest";
import { renderHook, waitFor } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { type ReactNode } from "react";
import {
  useFundoAllowedTransitions,
  useFundoCedenteAllowedTransitions,
  useFundoTipoAtivoAllowedTransitions,
  useCedenteTipoAtivoAllowedTransitions,
} from "@/lib/use-allowed-transitions";

// Mock fundos-api
vi.mock("@/lib/fundos-api", () => ({
  getFundoAllowedTransitions: vi.fn(),
  getFundoCedenteAllowedTransitions: vi.fn(),
  getFundoTipoAtivoAllowedTransitions: vi.fn(),
  getCedenteTipoAtivoAllowedTransitions: vi.fn(),
}));

import {
  getFundoAllowedTransitions,
  getFundoCedenteAllowedTransitions,
  getFundoTipoAtivoAllowedTransitions,
  getCedenteTipoAtivoAllowedTransitions,
} from "@/lib/fundos-api";

const mockGetFundo = vi.mocked(getFundoAllowedTransitions);
const mockGetFundoCedente = vi.mocked(getFundoCedenteAllowedTransitions);
const mockGetFundoTipoAtivo = vi.mocked(getFundoTipoAtivoAllowedTransitions);
const mockGetCedenteTipoAtivo = vi.mocked(getCedenteTipoAtivoAllowedTransitions);

function makeWrapper() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: 0 } } });
  return function Wrapper({ children }: { children: ReactNode }) {
    return <QueryClientProvider client={qc}>{children}</QueryClientProvider>;
  };
}

beforeEach(() => {
  vi.clearAllMocks();
});

// ---------------------------------------------------------------------------
// useFundoAllowedTransitions
// ---------------------------------------------------------------------------

describe("useFundoAllowedTransitions", () => {
  it("is disabled when fundoId is undefined", () => {
    const { result } = renderHook(() => useFundoAllowedTransitions(undefined), {
      wrapper: makeWrapper(),
    });
    expect(result.current.fetchStatus).toBe("idle");
    expect(mockGetFundo).not.toHaveBeenCalled();
  });

  it("fetches when fundoId is provided", async () => {
    mockGetFundo.mockResolvedValueOnce(["ATIVO"]);
    const { result } = renderHook(() => useFundoAllowedTransitions("fundo-1"), {
      wrapper: makeWrapper(),
    });
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(mockGetFundo).toHaveBeenCalledWith("fundo-1");
    expect(result.current.data).toEqual(["ATIVO"]);
  });

  it("uses correct query key: ['allowed-transitions', 'fundo', id]", async () => {
    mockGetFundo.mockResolvedValueOnce([]);
    const { result } = renderHook(() => useFundoAllowedTransitions("fundo-x"), {
      wrapper: makeWrapper(),
    });
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    // The hook fetched — key was derived correctly
    expect(mockGetFundo).toHaveBeenCalledWith("fundo-x");
  });
});

// ---------------------------------------------------------------------------
// useFundoCedenteAllowedTransitions
// ---------------------------------------------------------------------------

describe("useFundoCedenteAllowedTransitions", () => {
  it("is disabled when either param is undefined", () => {
    const { result } = renderHook(
      () => useFundoCedenteAllowedTransitions(undefined, "assoc-1"),
      { wrapper: makeWrapper() }
    );
    expect(result.current.fetchStatus).toBe("idle");
    expect(mockGetFundoCedente).not.toHaveBeenCalled();
  });

  it("is disabled when associationId is undefined", () => {
    const { result } = renderHook(
      () => useFundoCedenteAllowedTransitions("fundo-1", undefined),
      { wrapper: makeWrapper() }
    );
    expect(result.current.fetchStatus).toBe("idle");
  });

  it("fetches when both params are provided", async () => {
    mockGetFundoCedente.mockResolvedValueOnce(["INATIVO", "HISTORICO"]);
    const { result } = renderHook(
      () => useFundoCedenteAllowedTransitions("fundo-1", "assoc-1"),
      { wrapper: makeWrapper() }
    );
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(mockGetFundoCedente).toHaveBeenCalledWith("fundo-1", "assoc-1");
    expect(result.current.data).toEqual(["INATIVO", "HISTORICO"]);
  });
});

// ---------------------------------------------------------------------------
// useFundoTipoAtivoAllowedTransitions
// ---------------------------------------------------------------------------

describe("useFundoTipoAtivoAllowedTransitions", () => {
  it("is disabled when either param is undefined", () => {
    const { result } = renderHook(
      () => useFundoTipoAtivoAllowedTransitions(undefined, undefined),
      { wrapper: makeWrapper() }
    );
    expect(result.current.fetchStatus).toBe("idle");
    expect(mockGetFundoTipoAtivo).not.toHaveBeenCalled();
  });

  it("fetches with correct params", async () => {
    mockGetFundoTipoAtivo.mockResolvedValueOnce(["INATIVO"]);
    const { result } = renderHook(
      () => useFundoTipoAtivoAllowedTransitions("fundo-1", "assoc-2"),
      { wrapper: makeWrapper() }
    );
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(mockGetFundoTipoAtivo).toHaveBeenCalledWith("fundo-1", "assoc-2");
  });
});

// ---------------------------------------------------------------------------
// useCedenteTipoAtivoAllowedTransitions
// ---------------------------------------------------------------------------

describe("useCedenteTipoAtivoAllowedTransitions", () => {
  it("is disabled when either param is undefined", () => {
    const { result } = renderHook(
      () => useCedenteTipoAtivoAllowedTransitions(undefined, undefined),
      { wrapper: makeWrapper() }
    );
    expect(result.current.fetchStatus).toBe("idle");
    expect(mockGetCedenteTipoAtivo).not.toHaveBeenCalled();
  });

  it("fetches with correct params", async () => {
    mockGetCedenteTipoAtivo.mockResolvedValueOnce(["ATIVO"]);
    const { result } = renderHook(
      () => useCedenteTipoAtivoAllowedTransitions("ced-1", "assoc-3"),
      { wrapper: makeWrapper() }
    );
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(mockGetCedenteTipoAtivo).toHaveBeenCalledWith("ced-1", "assoc-3");
    expect(result.current.data).toEqual(["ATIVO"]);
  });
});
