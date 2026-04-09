import { useEffect, useState } from "react";
import { useNavigate } from "@tanstack/react-router";
import { AdminLoginForm, type AdminLoginData } from "@/components/molecules/AdminLoginForm";
import { useAdminAuth } from "@/lib/admin-auth-context";
import { AdminLoginError } from "@/lib/admin-api";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { toast } from "sonner";

/**
 * AdminLoginPage: dedicated login for admin backoffice.
 * Uses separate AdminAuthContext (no session conflicts with user auth).
 * Redirects to /admin/users on success.
 */
export function AdminLoginPage() {
  const { admin, login } = useAdminAuth();
  const navigate = useNavigate();
  const [serverError, setServerError] = useState<string | null>(null);

  // If already authenticated, redirect to admin users
  useEffect(() => {
    if (admin.isAuthenticated) {
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      navigate({ to: "/admin/users" as any, replace: true });
    }
  }, [admin.isAuthenticated, navigate]);

  const handleSubmit = async (data: AdminLoginData) => {
    setServerError(null);
    try {
      await login(data.email, data.password);
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      navigate({ to: "/admin/users" as any, replace: true });
    } catch (error) {
      if (error instanceof AdminLoginError) {
        setServerError(error.message);
      } else if (error instanceof Error) {
        toast.error("Erro interno do servidor, tente novamente mais tarde");
      } else {
        setServerError("Credenciais invalidas.");
      }
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-background p-4">
      <Card className="w-full max-w-md">
        <CardHeader className="space-y-1">
          <CardTitle className="text-2xl text-center">Admin Backoffice</CardTitle>
          <CardDescription className="text-center">
            Entre com suas credenciais de administrador
          </CardDescription>
        </CardHeader>
        <CardContent>
          <AdminLoginForm
            onSubmit={handleSubmit}
            serverError={serverError}
            isLoading={admin.isLoading}
          />
        </CardContent>
      </Card>
    </div>
  );
}
