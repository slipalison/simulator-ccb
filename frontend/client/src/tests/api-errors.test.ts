// ---------------------------------------------------------------------------
// api-errors.test.ts — unit tests for mapApiErrorToForm helper (T-2)
// D-26: ProblemDetails.errors → setError; domain exceptions → toast
// ---------------------------------------------------------------------------

import { describe, it, expect, vi, beforeEach } from "vitest";
import {
  mapApiErrorToForm,
  parseApiError,
  handleMutationError,
  showSuccessToast,
  showErrorToast,
  type ProblemDetails,
} from "@/lib/api-errors";
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

  it("handles problem with errors field using direct fieldMap key match", () => {
    const setError = makeSetError();
    const problem: ProblemDetails = {
      errors: {
        cnpj: ["CNPJ inválido"],
      },
    };

    mapApiErrorToForm(problem, setError, { cnpj: "cnpj" } as any);

    expect(setError).toHaveBeenCalledWith("cnpj", {
      type: "server",
      message: "CNPJ inválido",
    });
  });

  it("handles DuplicateCpf domain exception", async () => {
    const { toast } = await import("sonner");
    const setError = makeSetError();
    const problem: ProblemDetails = {
      title: "DuplicateCpfException",
      status: 409,
    };

    mapApiErrorToForm(problem, setError);

    expect(toast.error).toHaveBeenCalled();
    expect(setError).not.toHaveBeenCalled();
  });

  it("handles Conflict domain exception", async () => {
    const { toast } = await import("sonner");
    const setError = makeSetError();
    const problem: ProblemDetails = {
      title: "Conflict",
      status: 409,
      detail: "Conflict occurred",
    };

    mapApiErrorToForm(problem, setError);

    expect(toast.error).toHaveBeenCalled();
  });

  it("handles problem with no title, no detail, no errors → uses default message", () => {
    const setError = makeSetError();
    const problem: ProblemDetails = {};

    mapApiErrorToForm(problem, setError);

    expect(setError).toHaveBeenCalledWith("root.serverError", {
      type: "server",
      message: "Erro inesperado no servidor.",
    });
  });

  it("handles mixed mapped/unmapped fields", () => {
    const setError = makeSetError();
    const problem: ProblemDetails = {
      errors: {
        Nome: ["Nome inválido"],
        SomeOtherField: ["Outro erro"],
      },
    };

    mapApiErrorToForm(problem, setError, { nome: "nome" } as any);

    expect(setError).toHaveBeenCalledWith("nome", {
      type: "server",
      message: "Nome inválido",
    });
    expect(setError).toHaveBeenCalledWith("root.serverError", {
      type: "server",
      message: expect.stringContaining("SomeOtherField"),
    });
  });
});

// ---------------------------------------------------------------------------
// parseApiError
// ---------------------------------------------------------------------------

describe("parseApiError", () => {
  it("parses JSON body as ProblemDetails", async () => {
    const body = { title: "Bad Request", status: 400, errors: { nome: ["Required"] } };
    const response = new Response(JSON.stringify(body), {
      status: 400,
      headers: { "Content-Type": "application/json" },
    });

    const result = await parseApiError(response);

    expect(result.title).toBe("Bad Request");
    expect(result.status).toBe(400);
    expect(result.errors?.nome).toEqual(["Required"]);
  });

  it("returns synthetic ProblemDetails when response is not JSON", async () => {
    const response = new Response("Not JSON", {
      status: 500,
      statusText: "Internal Server Error",
    });

    const result = await parseApiError(response);

    expect(result.status).toBe(500);
    expect(result.title).toBe("HTTP 500");
    expect(result.detail).toBeTruthy();
  });
});

// ---------------------------------------------------------------------------
// handleMutationError
// ---------------------------------------------------------------------------

describe("handleMutationError", () => {
  it("calls parseApiError then mapApiErrorToForm", async () => {
    const { toast } = await import("sonner");
    vi.clearAllMocks();

    const setError = makeSetError();
    const body = { title: "InvalidStatusTransition", status: 400 };
    const response = new Response(JSON.stringify(body), {
      status: 400,
      headers: { "Content-Type": "application/json" },
    });

    await handleMutationError(response, setError);

    expect(toast.error).toHaveBeenCalled();
  });
});

// ---------------------------------------------------------------------------
// showSuccessToast / showErrorToast
// ---------------------------------------------------------------------------

describe("showSuccessToast", () => {
  it("calls toast.success with message", async () => {
    const { toast } = await import("sonner");
    vi.clearAllMocks();

    showSuccessToast("Operação realizada");

    expect(toast.success).toHaveBeenCalledWith("Operação realizada", undefined);
  });

  it("calls toast.success with message and description", async () => {
    const { toast } = await import("sonner");
    vi.clearAllMocks();

    showSuccessToast("Operação realizada", "Detalhes aqui");

    expect(toast.success).toHaveBeenCalledWith("Operação realizada", { description: "Detalhes aqui" });
  });
});

describe("showErrorToast", () => {
  it("calls toast.error with message", async () => {
    const { toast } = await import("sonner");
    vi.clearAllMocks();

    showErrorToast("Erro inesperado");

    expect(toast.error).toHaveBeenCalledWith("Erro inesperado", undefined);
  });

  it("calls toast.error with message and description", async () => {
    const { toast } = await import("sonner");
    vi.clearAllMocks();

    showErrorToast("Erro inesperado", "Tente novamente");

    expect(toast.error).toHaveBeenCalledWith("Erro inesperado", { description: "Tente novamente" });
  });
});
