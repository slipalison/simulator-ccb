// ---------------------------------------------------------------------------
// api-errors.test.ts — unit tests for mapApiErrorToForm helper (T-2)
// D-26: ProblemDetails.errors → setError; domain exceptions → toast
// ---------------------------------------------------------------------------

import { describe, it, expect, vi, beforeEach } from "vitest";
import { mapApiErrorToForm, type ProblemDetails } from "@/lib/api-errors";
import type { UseFormSetError, FieldValues } from "react-hook-form";

// Mock sonner toast
vi.mock("sonner", () => ({
  toast: {
    error: vi.fn(),
    success: vi.fn(),
  },
}));

// Typed mock for setError
function makeSetError() {
  return vi.fn() as unknown as UseFormSetError<FieldValues & Record<string, unknown>>;
}

describe("mapApiErrorToForm", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("calls setError for each ProblemDetails.errors field", () => {
    const setError = makeSetError();
    const problem: ProblemDetails = {
      title: "Validation failed",
      status: 422,
      errors: {
        nome: ["Nome é obrigatório", "Nome muito curto"],
        cnpj: ["CNPJ inválido"],
      },
    };

    mapApiErrorToForm(problem, setError, { nome: "nome", cnpj: "cnpj" } as any);

    expect(setError).toHaveBeenCalledWith("nome", {
      type: "server",
      message: "Nome é obrigatório; Nome muito curto",
    });
    expect(setError).toHaveBeenCalledWith("cnpj", {
      type: "server",
      message: "CNPJ inválido",
    });
  });

  it("maps unmapped backend fields to root.serverError", () => {
    const setError = makeSetError();
    const problem: ProblemDetails = {
      title: "Validation failed",
      status: 422,
      errors: {
        UnknownField: ["Some error"],
      },
    };

    mapApiErrorToForm(problem, setError);

    expect(setError).toHaveBeenCalledWith("root.serverError", {
      type: "server",
      message: expect.stringContaining("UnknownField"),
    });
  });

  it("emits toast for DuplicateActiveAssociationException", async () => {
    const { toast } = await import("sonner");
    const setError = makeSetError();
    const problem: ProblemDetails = {
      title: "Conflict",
      status: 409,
      detail: "DuplicateActiveAssociationException: já existe associação ativa",
    };

    mapApiErrorToForm(problem, setError);

    expect(toast.error).toHaveBeenCalledWith(
      "Erro ao processar operação",
      expect.objectContaining({ description: expect.stringContaining("associação ativa") })
    );
    expect(setError).not.toHaveBeenCalled();
  });

  it("emits toast for DuplicateCnpjException", async () => {
    const { toast } = await import("sonner");
    const setError = makeSetError();
    const problem: ProblemDetails = {
      title: "DuplicateCnpjException",
      status: 409,
      detail: "CNPJ already exists",
    };

    mapApiErrorToForm(problem, setError);

    expect(toast.error).toHaveBeenCalled();
    expect(setError).not.toHaveBeenCalled();
  });

  it("emits toast for InvalidStatusTransition", async () => {
    const { toast } = await import("sonner");
    const setError = makeSetError();
    const problem: ProblemDetails = {
      title: "InvalidStatusTransition",
      status: 400,
    };

    mapApiErrorToForm(problem, setError);

    expect(toast.error).toHaveBeenCalled();
  });

  it("sets root.serverError for generic error without errors field", () => {
    const setError = makeSetError();
    const problem: ProblemDetails = {
      title: "Internal error",
      detail: "Something went wrong",
      status: 500,
    };

    mapApiErrorToForm(problem, setError);

    expect(setError).toHaveBeenCalledWith("root.serverError", {
      type: "server",
      message: "Something went wrong",
    });
  });

  it("applies camelCase field name normalization", () => {
    const setError = makeSetError();
    const problem: ProblemDetails = {
      errors: {
        RazaoSocial: ["Campo obrigatório"],
      },
    };

    mapApiErrorToForm(problem, setError, { razaoSocial: "razaoSocial" } as any);

    expect(setError).toHaveBeenCalledWith("razaoSocial", {
      type: "server",
      message: "Campo obrigatório",
    });
  });
});
