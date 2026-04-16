// ---------------------------------------------------------------------------
// Client API functions
// ---------------------------------------------------------------------------
// Uses httpOnly cookies managed by Vinxi auth-server (/auth/* routes).
// All requests MUST include credentials: 'include'.
// ---------------------------------------------------------------------------

// ---------------------------------------------------------------------------
// Registration API client
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

export async function registerClient(
  data: RegisterClientRequest
): Promise<{ id: string }> {
  const body: RegisterClientRequest = {
    nome: data.nome,
    cpf: data.cpf,
    razaoSocial: data.razaoSocial,
    cnpj: data.cnpj,
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

  throw new ApiError("An unexpected error occurred.");
}

// ---------------------------------------------------------------------------
// Profile API client
// ---------------------------------------------------------------------------

import type { ClientProfileDto } from "@/lib/types";

export class ProfileError extends Error {
  constructor(message: string) {
    super(message);
    this.name = "ProfileError";
  }
}

/**
 * Fetch current user's profile using httpOnly cookie.
 * No Bearer token needed — auth is via cookie (ACF).
 */
export async function getProfileClient(): Promise<ClientProfileDto> {
  const response = await fetch("/api/clients/me", {
    method: "GET",
    credentials: "include",
    headers: {
      "Content-Type": "application/json",
    },
  });

  if (!response.ok) {
    if (response.status === 401) {
      throw new ProfileError("Authentication required");
    }
    throw new ProfileError("Failed to fetch profile data");
  }

  return response.json() as Promise<ClientProfileDto>;
}

// ---------------------------------------------------------------------------
// Forgot/Reset Password API clients
// ---------------------------------------------------------------------------

export class ForgotPasswordError extends Error {
  constructor(message: string) {
    super(message);
    this.name = "ForgotPasswordError";
  }
}

export class ResetPasswordError extends Error {
  constructor(message: string) {
    super(message);
    this.name = "ResetPasswordError";
  }
}

export interface ForgotPasswordResponse {
  message: string;
}

export async function forgotPasswordClient(
  email: string
): Promise<ForgotPasswordResponse> {
  const response = await fetch("/api/auth/forgot-password", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ email }),
  });

  if (response.status === 429) {
    const error = await response.json() as { detail?: string };
    throw new ForgotPasswordError(error.detail || "Muitas tentativas. Tente novamente mais tarde.");
  }

  if (response.status === 400) {
    const error = await response.json() as { title?: string };
    throw new ForgotPasswordError(error.title || "Email invalido.");
  }

  if (response.status === 200) {
    return (await response.json()) as ForgotPasswordResponse;
  }

  throw new ApiError("An unexpected error occurred.");
}

export async function resetPasswordClient(
  token: string,
  newPassword: string
): Promise<{ message: string }> {
  const response = await fetch("/api/auth/reset-password", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ token, newPassword }),
  });

  if (!response.ok) {
    const error = await response.json() as { detail?: string };
    throw new ResetPasswordError(error.detail || "Erro ao redefinir senha.");
  }

  return (await response.json()) as { message: string };
}
