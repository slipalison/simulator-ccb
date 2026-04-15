export function AuthErrorPage() {
  function retry() {
    window.location.href = "/auth/login";
  }

  return (
    <div className="flex flex-col items-center justify-center min-h-screen gap-4">
      <h1 className="text-xl font-semibold">Erro de autenticacao</h1>
      <p className="text-muted-foreground">
        Nao foi possivel completar o login. Tente novamente.
      </p>
      <button
        onClick={retry}
        className="px-4 py-2 bg-primary text-primary-foreground rounded-md"
      >
        Tentar novamente
      </button>
    </div>
  );
}
