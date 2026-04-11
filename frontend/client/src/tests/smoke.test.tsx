import { describe, it, expect } from "vitest";
import { render } from "@testing-library/react";
import { RouterProvider } from "@tanstack/react-router";
import { router } from "@/router";

describe("FRONT-02: App SPA smoke test", () => {
  it("renderiza o componente raiz sem lançar exceção", () => {
    // Usar RouterProvider com o router configurado
    expect(() =>
      render(<RouterProvider router={router} />)
    ).not.toThrow();
  });
});
