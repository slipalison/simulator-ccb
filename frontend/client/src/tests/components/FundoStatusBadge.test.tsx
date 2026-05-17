// ---------------------------------------------------------------------------
// FundoStatusBadge.test.tsx — render + a11y (D-2)
// ---------------------------------------------------------------------------

import { describe, it, expect } from "vitest";
import { render, screen } from "@testing-library/react";
import { FundoStatusBadge } from "@/components/organisms/FundoStatusBadge";

describe("FundoStatusBadge", () => {
  it("renders RASCUNHO with gray color class", () => {
    const { container } = render(<FundoStatusBadge status="RASCUNHO" />);
    expect(container.firstChild).toHaveClass("bg-gray-500");
    expect(screen.getByText("Rascunho")).toBeInTheDocument();
  });

  it("renders ATIVO with green color class", () => {
    const { container } = render(<FundoStatusBadge status="ATIVO" />);
    expect(container.firstChild).toHaveClass("bg-green-500");
    expect(screen.getByText("Ativo")).toBeInTheDocument();
  });

  it("renders SUSPENSO with yellow color class", () => {
    const { container } = render(<FundoStatusBadge status="SUSPENSO" />);
    expect(container.firstChild).toHaveClass("bg-yellow-500");
    expect(screen.getByText("Suspenso")).toBeInTheDocument();
  });

  it("renders EM_LIQUIDACAO with orange color class", () => {
    const { container } = render(<FundoStatusBadge status="EM_LIQUIDACAO" />);
    expect(container.firstChild).toHaveClass("bg-orange-500");
    expect(screen.getByText("Em Liquidação")).toBeInTheDocument();
  });

  it("renders ENCERRADO with red color class", () => {
    const { container } = render(<FundoStatusBadge status="ENCERRADO" />);
    expect(container.firstChild).toHaveClass("bg-red-500");
    expect(screen.getByText("Encerrado")).toBeInTheDocument();
  });

  it("has aria-label with status text", () => {
    render(<FundoStatusBadge status="ATIVO" />);
    expect(screen.getByLabelText(/status: ativo/i)).toBeInTheDocument();
  });

  it("accepts custom className", () => {
    const { container } = render(<FundoStatusBadge status="ATIVO" className="custom-class" />);
    expect(container.firstChild).toHaveClass("custom-class");
  });
});
