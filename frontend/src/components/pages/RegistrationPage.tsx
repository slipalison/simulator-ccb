import { useState } from "react";
import { useNavigate } from "@tanstack/react-router";
import { PageLayout } from "@/components/templates/PageLayout";
import { RegistrationTypeSelector, type RegistrationType } from "@/components/molecules/RegistrationTypeSelector";
import { PfRegistrationForm } from "@/components/molecules/PfRegistrationForm";
import { PjRegistrationForm } from "@/components/molecules/PjRegistrationForm";
import type { PfRegistrationData } from "@/lib/validation-schemas";
import type { PjRegistrationData } from "@/lib/validation-schemas";
import { Button } from "@/components/ui/button";
import {
  registerClient,
  RegistrationValidationError,
  DuplicateClientError,
  RegistrationUnavailable,
  ApiError,
} from "@/lib/api";

export function RegistrationPage() {
  const navigate = useNavigate();
  const [selectedType, setSelectedType] = useState<RegistrationType | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]> | null>(null);

  const handleBack = () => {
    setSelectedType(null);
    setSubmitError(null);
    setFieldErrors(null);
  };

  const handlePfSubmit = async (data: PfRegistrationData) => {
    setIsSubmitting(true);
    setSubmitError(null);
    setFieldErrors(null);

    try {
      await registerClient(data);
      navigate({ to: "/login" });
    } catch (err) {
      if (err instanceof RegistrationValidationError) {
        setFieldErrors(err.errors);
      } else if (err instanceof DuplicateClientError) {
        setSubmitError(err.message);
      } else if (err instanceof RegistrationUnavailable) {
        setSubmitError(err.message);
      } else if (err instanceof ApiError) {
        setSubmitError(err.message);
      } else {
        setSubmitError("An unexpected error occurred.");
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  const handlePjSubmit = async (data: PjRegistrationData) => {
    setIsSubmitting(true);
    setSubmitError(null);
    setFieldErrors(null);

    try {
      await registerClient(data);
      navigate({ to: "/login" });
    } catch (err) {
      if (err instanceof RegistrationValidationError) {
        setFieldErrors(err.errors);
      } else if (err instanceof DuplicateClientError) {
        setSubmitError(err.message);
      } else if (err instanceof RegistrationUnavailable) {
        setSubmitError(err.message);
      } else if (err instanceof ApiError) {
        setSubmitError(err.message);
      } else {
        setSubmitError("An unexpected error occurred.");
      }
    } finally {
      setIsSubmitting(false);
    }
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
            {submitError && (
              <div className="mb-4 rounded-lg border border-red-200 bg-red-50 p-3 text-sm text-red-600">
                {submitError}
              </div>
            )}
            <div className="rounded-lg border bg-card p-6">
              <PfRegistrationForm
                onSubmit={handlePfSubmit}
                isSubmitting={isSubmitting}
                fieldErrors={fieldErrors ?? undefined}
              />
            </div>
          </>
        ) : (
          <>
            <div className="mb-6 flex items-center justify-between">
              <h1 className="text-2xl font-bold text-foreground">Cadastro &#8212; Pessoa Jur&#237;dica</h1>
              <Button variant="outline" onClick={handleBack}>Voltar</Button>
            </div>
            {submitError && (
              <div className="mb-4 rounded-lg border border-red-200 bg-red-50 p-3 text-sm text-red-600">
                {submitError}
              </div>
            )}
            <div className="rounded-lg border bg-card p-6">
              <PjRegistrationForm
                onSubmit={handlePjSubmit}
                isSubmitting={isSubmitting}
                fieldErrors={fieldErrors ?? undefined}
              />
            </div>
          </>
        )}
      </div>
    </PageLayout>
  );
}
