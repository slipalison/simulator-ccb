// ---------------------------------------------------------------------------
// API error helpers (D-26)
// ---------------------------------------------------------------------------
// mapApiErrorToForm: maps ProblemDetails.errors[] → react-hook-form setError
//   by field; domain exceptions → toast.error() (sonner) destructive variant.
// Domain exceptions recognized: DuplicateActiveAssociation*, DuplicateCnpj*,
//   DuplicateCpf*, InvalidStatusTransition*, Conflict*.
// ---------------------------------------------------------------------------

import type { UseFormSetError, FieldValues, Path } from "react-hook-form";
import { toast } from "sonner";

// ---------------------------------------------------------------------------
// ProblemDetails shape (RFC 7807 — .NET returns this from FluentValidation)
// ---------------------------------------------------------------------------

export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  errors?: Record<string, string[]>;
}

// ---------------------------------------------------------------------------
// Domain exception type identifiers (matched against title/detail)
// ---------------------------------------------------------------------------

const DOMAIN_EXCEPTION_PATTERNS: Array<{
  pattern: RegExp;
  message: string;
}> = [
  {
    pattern: /DuplicateActiveAssociation/i,
    message:
      "Já existe uma associação ativa para este par. Mova a existente para INATIVO ou HISTORICO antes de criar uma nova.",
  },
  {
    pattern: /DuplicateCnpj/i,
    message: "Já existe um cadastro com este CNPJ nesta empresa.",
  },
  {
    pattern: /DuplicateCpf/i,
    message: "Já existe um cadastro com este CPF nesta empresa.",
  },
  {
    pattern: /InvalidStatusTransition|InvalidStateTransition/i,
    message:
      "Transição de status inválida. O recurso pode ter sido alterado por outra operação.",
  },
  {
    pattern: /Conflict/i,
    message: "Conflito ao processar a operação. Tente novamente.",
  },
];

// ---------------------------------------------------------------------------
// isDomainException: returns the user message if this error is a recognized
// domain exception; returns null otherwise.
// ---------------------------------------------------------------------------

function getDomainExceptionMessage(problem: ProblemDetails): string | null {
  const searchText = [problem.title, problem.detail, problem.type]
    .filter(Boolean)
    .join(" ");

  for (const { pattern, message } of DOMAIN_EXCEPTION_PATTERNS) {
    if (pattern.test(searchText)) {
      return message;
    }
  }
  return null;
}

// ---------------------------------------------------------------------------
// mapApiErrorToForm
// ---------------------------------------------------------------------------
// Parses the HTTP response (already consumed — pass ProblemDetails parsed).
// fieldMap: optional mapping from backend field names to form field paths.
//   Example: { "Cpf": "cpf", "Nome": "nome" }
//
// Behavior:
//   1. If domain exception → toast.error() with specific message. No setError.
//   2. If ProblemDetails.errors present → setError per field.
//      Unmapped fields → setError("root.serverError").
//   3. If generic error → setError("root.serverError").
// ---------------------------------------------------------------------------

export function mapApiErrorToForm<TFieldValues extends FieldValues>(
  problem: ProblemDetails,
  setError: UseFormSetError<TFieldValues>,
  fieldMap?: Record<string, Path<TFieldValues>>
): void {
  // 1. Domain exception → toast only
  const domainMsg = getDomainExceptionMessage(problem);
  if (domainMsg) {
    toast.error("Erro ao processar operação", { description: domainMsg });
    return;
  }

  // 2. Validation errors (FluentValidation ProblemDetails format)
  if (problem.errors && Object.keys(problem.errors).length > 0) {
    let hasUnmapped = false;
    const unmappedMessages: string[] = [];

    for (const [backendField, messages] of Object.entries(problem.errors)) {
      const formField = fieldMap?.[backendField] ?? fieldMap?.[backendField.toLowerCase()];
      const message = messages.join("; ");

      if (formField) {
        setError(formField, { type: "server", message });
      } else {
        // Normalize camelCase field names for common cases
        const camelField = backendField.charAt(0).toLowerCase() + backendField.slice(1);
        const camelMapped = fieldMap?.[camelField];
        if (camelMapped) {
          setError(camelMapped, { type: "server", message });
        } else {
          hasUnmapped = true;
          unmappedMessages.push(`${backendField}: ${message}`);
        }
      }
    }

    if (hasUnmapped) {
      setError("root.serverError" as Path<TFieldValues>, {
        type: "server",
        message: unmappedMessages.join("\n"),
      });
    }
    return;
  }

  // 3. Generic server error
  const message = problem.detail ?? problem.title ?? "Erro inesperado no servidor.";
  setError("root.serverError" as Path<TFieldValues>, {
    type: "server",
    message,
  });
}

// ---------------------------------------------------------------------------
// parseApiError: fetch a Response and extract ProblemDetails
// ---------------------------------------------------------------------------

export async function parseApiError(response: Response): Promise<ProblemDetails> {
  try {
    return (await response.json()) as ProblemDetails;
  } catch {
    return {
      status: response.status,
      title: `HTTP ${response.status}`,
      detail: response.statusText || "An unexpected error occurred.",
    };
  }
}

// ---------------------------------------------------------------------------
// handleMutationError: utility for mutation onError handlers
// Handles both toast-only (domain exceptions) and form errors.
// ---------------------------------------------------------------------------

export async function handleMutationError<TFieldValues extends FieldValues>(
  response: Response,
  setError: UseFormSetError<TFieldValues>,
  fieldMap?: Record<string, Path<TFieldValues>>
): Promise<void> {
  const problem = await parseApiError(response);
  mapApiErrorToForm(problem, setError, fieldMap);
}

// ---------------------------------------------------------------------------
// showSuccessToast: standard success notification
// ---------------------------------------------------------------------------

export function showSuccessToast(message: string, description?: string): void {
  toast.success(message, description ? { description } : undefined);
}

// ---------------------------------------------------------------------------
// showErrorToast: standard error notification (for non-form errors)
// ---------------------------------------------------------------------------

export function showErrorToast(message: string, description?: string): void {
  toast.error(message, description ? { description } : undefined);
}
