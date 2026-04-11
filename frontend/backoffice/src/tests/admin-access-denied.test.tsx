import { describe, it, expect } from "vitest";
import { render, screen } from "@testing-library/react";
import { AdminAccessDeniedPage } from "@/components/pages/AdminAccessDeniedPage";

describe("Admin Access Denied Page", () => {
  it("renders access denied message", () => {
    render(<AdminAccessDeniedPage />);

    expect(screen.getByText(/acesso negado/i)).toBeInTheDocument();
    expect(
      screen.getByText(/voce nao tem permissao para acessar esta area/i)
    ).toBeInTheDocument();
  });

  it("has a button to go back home", () => {
    render(<AdminAccessDeniedPage />);

    const link = screen.getByRole("link", { name: /voltar para home/i });
    expect(link).toBeInTheDocument();
    expect(link.getAttribute("href")).toBe("/");
  });
});
