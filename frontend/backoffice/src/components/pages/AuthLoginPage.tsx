import { useEffect } from "react";

export function AuthLoginPage() {
  useEffect(() => {
    window.location.href = "/auth/login";
  }, []);

  return (
    <div className="flex items-center justify-center min-h-screen">
      <p className="text-muted-foreground">Redirecionando para login...</p>
    </div>
  );
}
