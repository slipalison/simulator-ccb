import { useState } from "react";
import { PageLayout } from "@/components/templates/PageLayout";
import { RegistrationTypeSelector, type RegistrationType } from "@/components/molecules/RegistrationTypeSelector";
import { PfRegistrationForm } from "@/components/molecules/PfRegistrationForm";
import { PjRegistrationForm } from "@/components/molecules/PjRegistrationForm";
import type { PfRegistrationData } from "@/lib/validation-schemas";
import type { PjRegistrationData } from "@/lib/validation-schemas";
import { Button } from "@/components/ui/button";

function handlePfSubmit(data: PfRegistrationData) {
  console.log("PF registration data:", data);
}

function handlePjSubmit(data: PjRegistrationData) {
  console.log("PJ registration data:", data);
}

export function RegistrationPage() {
  const [selectedType, setSelectedType] = useState<RegistrationType | null>(null);

  const handleBack = () => {
    setSelectedType(null);
  };

  return (
    <PageLayout>
      <div className="mx-auto max-w-2xl">
        {selectedType === null ? (
          <>
            <h1 className="mb-6 text-3xl font-bold text-foreground">Criar sua conta</h1>
            <p className="mb-6 text-muted-foreground">
              Escolha o tipo de cadastro para continuar.
            </p>
            <RegistrationTypeSelector onSelect={setSelectedType} />
          </>
        ) : selectedType === 'PF' ? (
          <>
            <div className="mb-6 flex items-center justify-between">
              <h1 className="text-2xl font-bold text-foreground">Cadastro &#8212; Pessoa F&#237;sica</h1>
              <Button variant="outline" onClick={handleBack}>Voltar</Button>
            </div>
            <div className="rounded-lg border bg-card p-6">
              <PfRegistrationForm onSubmit={handlePfSubmit} />
            </div>
          </>
        ) : (
          <>
            <div className="mb-6 flex items-center justify-between">
              <h1 className="text-2xl font-bold text-foreground">Cadastro &#8212; Pessoa Jur&#237;dica</h1>
              <Button variant="outline" onClick={handleBack}>Voltar</Button>
            </div>
            <div className="rounded-lg border bg-card p-6">
              <PjRegistrationForm onSubmit={handlePjSubmit} />
            </div>
          </>
        )}
      </div>
    </PageLayout>
  );
}
