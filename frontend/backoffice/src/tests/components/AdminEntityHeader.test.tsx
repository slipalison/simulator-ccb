import { describe, it, expect, vi } from "vitest";
import { render, screen, fireEvent } from "@testing-library/react";
import { AdminEntityHeader } from "@/components/molecules/AdminEntityHeader";

describe("AdminEntityHeader", () => {
  it("renders nome, empresa, status", () => {
    render(
      <AdminEntityHeader
        title="Detalhe do Fundo"
        nome="Fundo Alpha"
        empresaNome="Empresa X"
        status="ATIVO"
      />
    );
    expect(screen.getByTestId("entity-header-nome")).toHaveTextContent("Fundo Alpha");
    expect(screen.getByTestId("entity-header-empresa")).toHaveTextContent("Empresa X");
    expect(screen.getByTestId("entity-header-status")).toHaveTextContent("Ativo");
  });

  it("renders back button when onBack provided", () => {
    const onBack = vi.fn();
    render(
      <AdminEntityHeader
        title="Detalhe"
        nome="Test"
        empresaNome="Empresa"
        status="ATIVO"
        onBack={onBack}
      />
    );
    const backBtn = screen.getByTestId("entity-back-button");
    expect(backBtn).toBeInTheDocument();
    fireEvent.click(backBtn);
    expect(onBack).toHaveBeenCalledOnce();
  });

  it("does not render back button when onBack not provided", () => {
    render(
      <AdminEntityHeader
        title="Detalhe"
        nome="Test"
        empresaNome="Empresa"
        status="ATIVO"
      />
    );
    expect(screen.queryByTestId("entity-back-button")).not.toBeInTheDocument();
  });

  it("applies correct status badge class for ATIVO", () => {
    render(
      <AdminEntityHeader title="T" nome="N" empresaNome="E" status="ATIVO" />
    );
    const badge = screen.getByTestId("entity-header-status");
    expect(badge.className).toContain("bg-green-500");
  });

  it("applies correct status badge class for ENCERRADO", () => {
    render(
      <AdminEntityHeader title="T" nome="N" empresaNome="E" status="ENCERRADO" />
    );
    const badge = screen.getByTestId("entity-header-status");
    expect(badge.className).toContain("bg-red-500");
  });

  it("uses fallback bg-gray-500 for unknown status", () => {
    render(
      <AdminEntityHeader title="T" nome="N" empresaNome="E" status="UNKNOWN_STATUS" />
    );
    const badge = screen.getByTestId("entity-header-status");
    expect(badge.className).toContain("bg-gray-500");
    // getStatusLabel falls back to raw status string
    expect(badge).toHaveTextContent("UNKNOWN_STATUS");
  });

  it("renders correct label for SUSPENSO status", () => {
    render(
      <AdminEntityHeader title="T" nome="N" empresaNome="E" status="SUSPENSO" />
    );
    expect(screen.getByTestId("entity-header-status")).toHaveTextContent("Suspenso");
  });

  it("renders correct label for INATIVO status", () => {
    render(
      <AdminEntityHeader title="T" nome="N" empresaNome="E" status="INATIVO" />
    );
    expect(screen.getByTestId("entity-header-status")).toHaveTextContent("Inativo");
  });
});
