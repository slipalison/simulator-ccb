import { describe, it, expect } from "vitest";
import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import { ExampleForm } from "@/components/organisms/ExampleForm";

describe("FRONT-04: React Hook Form + Zod — validação inline", () => {
  it("submeter ExampleForm com campo 'name' vazio mostra mensagem de erro inline", async () => {
    render(<ExampleForm />);

    // Submit sem preencher nenhum campo
    fireEvent.submit(screen.getByRole("button", { name: /enviar/i }));

    // Aguardar validação assíncrona do RHF (pode haver múltiplos alertas — name + email)
    await waitFor(() => {
      expect(screen.getAllByRole("alert").length).toBeGreaterThan(0);
    });

    // Verificar mensagem de erro específica do Zod para o campo name
    expect(screen.getByText(/nome é obrigatório|nome deve ter/i)).toBeInTheDocument();
  });

  it("mensagem de erro aparece no DOM antes de qualquer requisição de rede", async () => {
    // Spy em fetch para confirmar que nenhuma chamada de rede é feita
    const fetchSpy = vi.spyOn(globalThis, "fetch");

    render(<ExampleForm />);
    fireEvent.submit(screen.getByRole("button", { name: /enviar/i }));

    await waitFor(() => {
      expect(screen.getAllByRole("alert").length).toBeGreaterThan(0);
    });

    // Confirmar que nenhuma chamada de rede foi feita
    expect(fetchSpy).not.toHaveBeenCalled();
    fetchSpy.mockRestore();
  });
});

describe("FRONT-05: Tailwind CSS — componentes usam classes utilitárias", () => {
  it("ExampleForm possui elementos com classes Tailwind (space-y-*, text-*, etc.)", () => {
    const { container } = render(<ExampleForm />);

    // Verificar que o form tem classes Tailwind
    const form = container.querySelector("form");
    expect(form?.className).toMatch(/space-y/);
  });
});
