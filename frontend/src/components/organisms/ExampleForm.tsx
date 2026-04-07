import { LabeledField } from "@/components/molecules/LabeledField";
import { AppButton } from "@/components/atoms/AppButton";

/**
 * Organism: estrutura de formulário de exemplo.
 * Sem lógica RHF + Zod nesta wave — wiring completo em 07-03-PLAN.md.
 * Demonstra composição de molecules dentro de um organism.
 */
export function ExampleForm() {
  return (
    <form className="space-y-4 w-full max-w-sm">
      <LabeledField
        id="name"
        label="Nome"
        inputProps={{ placeholder: "Seu nome completo" }}
      />
      <LabeledField
        id="email"
        label="Email"
        inputProps={{ type: "email", placeholder: "seu@email.com" }}
      />
      <AppButton type="submit" className="w-full">
        Enviar
      </AppButton>
    </form>
  );
}
