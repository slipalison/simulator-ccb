// ---------------------------------------------------------------------------
// Registration API client
// ---------------------------------------------------------------------------
// Typed client for POST /api/registration.
// Uses native fetch — no external dependencies.
// ---------------------------------------------------------------------------

import type { PfRegistrationData, PjRegistrationData } from "@/lib/validation-schemas";

// ---------------------------------------------------------------------------
// Request type — matches RegisterClientRequest DTO on the server
// ---------------------------------------------------------------------------

export interface RegisterClientRequest {
  nome?: string;
  cpf?: string;
  razaoSocial?: string;
  cnpj?: string;
  email?: string;
  phone?: string;
  password?: string;
}

// ---------------------------------------------------------------------------
// Custom error classes
// ---------------------------------------------------------------------------

export class RegistrationValidationError extends Error {
  constructor(public errors: Record<string, string[]>) {
    super("Registration validation failed");
    this.name = "RegistrationValidationError";
  }
}

export class DuplicateClientError extends Error {
  constructor(message: string) {
    super(message);
    this.name = "DuplicateClientError";
  }
}

export class RegistrationUnavailable extends Error {
  constructor(message: string) {
    super(message);
    this.name = "RegistrationUnavailable";
  }
}

export class ApiError extends Error {
  constructor(message: string) {
    super(message);
    this.name = "ApiError";
  }
}

// ---------------------------------------------------------------------------
// API function
// ---------------------------------------------------------------------------

export async function registerClient(
  data: PfRegistrationData | PjRegistrationData
): Promise<{ id: string }> {
  const body: RegisterClientRequest = {
    nome: "nome" in data ? data.nome : undefined,
    cpf: "cpf" in data ? data.cpf : undefined,
    razaoSocial: "razaoSocial" in data ? data.razaoSocial : undefined,
    cnpj: "cnpj" in data ? data.cnpj : undefined,
    email: data.email,
    phone: data.phone,
    password: data.password,
  };

  const response = await fetch("/api/registration", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  });

  if (response.status === 201) {
    const json = await response.json();
    return { id: json.id as string };
  }

  if (response.status === 422) {
    const problemDetails = (await response.json()) as {
      errors?: Record<string, string[]>;
    };
    throw new RegistrationValidationError(problemDetails.errors ?? {});
  }

  if (response.status === 409) {
    throw new DuplicateClientError(
      "A client with the provided information already exists."
    );
  }

  if (response.status === 503) {
    throw new RegistrationUnavailable(
      "Please try again in a few moments."
    );
  }

  // Fallback for any other error
  throw new ApiError("An unexpected error occurred.");
}
