// ---------------------------------------------------------------------------
// Login API client
// ---------------------------------------------------------------------------
// Typed client for POST /api/auth/login and POST /api/auth/refresh.
// Uses native fetch — no external dependencies.
// ---------------------------------------------------------------------------

// ---------------------------------------------------------------------------
// Response type — mirrors backend TokenResponse
// ---------------------------------------------------------------------------

export interface LoginResponse {
  accessToken: string;
  refreshToken: string;
  expiresIn: number;
  tokenType: string;
  refreshExpiresIn: number;
  scope: string;
}

// ---------------------------------------------------------------------------
// Refresh token request type
// ---------------------------------------------------------------------------

export interface RefreshTokenRequest {
  refreshToken: string;
}

// ---------------------------------------------------------------------------
// Custom error classes for auth
// ---------------------------------------------------------------------------

export class LoginError extends Error {
  constructor(message: string) {
    super(message);
    this.name = "LoginError";
  }
}

export class RefreshTokenError extends Error {
  constructor(message: string) {
    super(message);
    this.name = "RefreshTokenError";
  }
}

// ---------------------------------------------------------------------------
// API functions
// ---------------------------------------------------------------------------

export async function loginClient(
  email: string,
  password: string
): Promise<LoginResponse> {
  const response = await fetch("/api/auth/login", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ email, password }),
    credentials: "include", // Allow httpOnly cookies
  });

  if (response.status === 200) {
    return (await response.json()) as LoginResponse;
  }

  if (response.status === 422) {
    throw new LoginError("Validation failed");
  }

  if (response.status === 401) {
    throw new LoginError("Invalid credentials.");
  }

  throw new ApiError("An unexpected error occurred.");
}

export async function refreshTokenClient(
  refreshToken: string
): Promise<LoginResponse> {
  const response = await fetch("/api/auth/refresh", {
    method: "POST",
    credentials: "include", // Send httpOnly cookie with refresh token
  });

  if (response.status === 200) {
    return (await response.json()) as LoginResponse;
  }

  if (response.status === 401) {
    throw new RefreshTokenError("Invalid or expired refresh token.");
  }

  throw new ApiError("An unexpected error occurred.");
}

// ---------------------------------------------------------------------------
// Registration API client
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

// ---------------------------------------------------------------------------
// Profile API client
// ---------------------------------------------------------------------------

import type { ClientProfileDto } from "@/lib/types";

// ---------------------------------------------------------------------------
// Custom error class for profile fetch failures
// ---------------------------------------------------------------------------

export class ProfileError extends Error {
  constructor(message: string) {
    super(message);
    this.name = "ProfileError";
  }
}

// ---------------------------------------------------------------------------
// Fetch current user's profile using Bearer token from AuthContext
// Dynamic import avoids circular dependency: api.ts → auth-context.tsx → api.ts
// ---------------------------------------------------------------------------

export async function getProfileClient(): Promise<ClientProfileDto> {
  const { getAccessToken } = await import("./auth-context");
  const token = getAccessToken();

  if (!token) {
    throw new ProfileError("No authentication token available");
  }

  const response = await fetch("/api/clients/me", {
    method: "GET",
    headers: {
      Authorization: `Bearer ${token}`,
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
// Forgot/Reset Password API clients — Phase 11 UX-05
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

  // Always returns success (no info disclosure)
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
