/**
 * AuthErrorPage: displays auth errors from the ACF callback flow.
 * Reads ?error= from URL and shows a friendly message with a retry link.
 */
export function AuthErrorPage() {
  const searchParams = new URLSearchParams(window.location.search);
  const errorMessage =
    searchParams.get("error") || "Ocorreu um erro durante a autenticação.";

  return (
    <div className="min-h-screen flex items-center justify-center bg-background p-4">
      <div className="w-full max-w-md text-center space-y-4">
        <div className="rounded-lg border border-destructive/50 bg-destructive/10 p-6 space-y-3">
          <p className="text-destructive font-medium">Erro de autenticação</p>
          <p className="text-sm text-muted-foreground">{decodeURIComponent(errorMessage)}</p>
          <a
            href="/auth/login"
            className="inline-block text-sm text-primary hover:underline font-medium"
          >
            Tentar novamente
          </a>
        </div>
      </div>
    </div>
  );
}
