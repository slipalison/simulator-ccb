import { useEffect, useState } from "react";

/**
 * AuthCallbackPage: polls /auth/me after Keycloak redirects back.
 * On success (200) → redirect to /profile.
 * On error → show error card with link to /auth/login.
 */
export function AuthCallbackPage() {
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    let attempts = 0;
    const MAX_ATTEMPTS = 5;

    async function checkSession() {
      while (attempts < MAX_ATTEMPTS && !cancelled) {
        attempts++;
        try {
          const res = await fetch("/auth/me", { credentials: "include" });
          if (res.ok) {
            const data = (await res.json()) as { isAuthenticated: boolean };
            if (data.isAuthenticated) {
              if (!cancelled) window.location.href = "/profile";
              return;
            }
          }
        } catch {
          // Ignore network errors, retry
        }
        if (attempts < MAX_ATTEMPTS && !cancelled) {
          await new Promise((r) => setTimeout(r, 500));
        }
      }

      if (!cancelled) {
        setError("Falha ao estabelecer sessão. Por favor, tente fazer login novamente.");
      }
    }

    checkSession();
    return () => {
      cancelled = true;
    };
  }, []);

  if (error) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-background p-4">
        <div className="w-full max-w-md text-center space-y-4">
          <div className="rounded-lg border border-destructive/50 bg-destructive/10 p-6 space-y-3">
            <p className="text-destructive font-medium">Erro de autenticação</p>
            <p className="text-sm text-muted-foreground">{error}</p>
            <a
              href="/auth/login"
              className="inline-block text-sm text-primary hover:underline font-medium"
            >
              Voltar ao login
            </a>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-background">
      <div className="text-center space-y-4">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary mx-auto" />
        <p className="text-muted-foreground text-sm">Concluindo autenticação...</p>
      </div>
    </div>
  );
}
