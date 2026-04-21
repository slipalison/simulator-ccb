import { useState, useEffect, useCallback } from "react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Shield, RefreshCw } from "lucide-react";
import { toast } from "sonner";
import { getAdministrators } from "@/lib/admin-api";
import type { AdminUserDto } from "@/lib/admin-api";

export function AdminAdministratorsPage() {
  const [admins, setAdmins] = useState<AdminUserDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isError, setIsError] = useState(false);

  const fetchAdmins = useCallback(async () => {
    setIsLoading(true);
    setIsError(false);
    try {
      const data = await getAdministrators();
      setAdmins(data);
    } catch (_err) {
      setIsError(true);
      toast.error("Falha ao carregar administradores", {
        description: "Tente novamente.",
      });
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchAdmins();
  }, [fetchAdmins]);

  return (
    <div className="space-y-6" data-testid="admin-administrators-page">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          <Shield className="h-5 w-5 text-primary" />
          <h2 className="text-xl font-semibold">Administradores</h2>
        </div>
        <Button
          variant="outline"
          size="sm"
          onClick={fetchAdmins}
          disabled={isLoading}
          data-testid="refresh-button"
        >
          <RefreshCw className={`h-4 w-4 mr-1 ${isLoading ? "animate-spin" : ""}`} />
          Atualizar
        </Button>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="text-base">Lista de Administradores</CardTitle>
        </CardHeader>
        <CardContent className="p-0">
          {isLoading && (
            <div className="p-6 text-center text-sm text-muted-foreground" data-testid="loading-state">
              Carregando administradores...
            </div>
          )}

          {isError && !isLoading && (
            <div className="p-6 text-center text-sm text-destructive" data-testid="error-state">
              Falha ao carregar administradores.{" "}
              <button
                className="underline hover:no-underline"
                onClick={fetchAdmins}
              >
                Tentar novamente
              </button>
            </div>
          )}

          {!isLoading && !isError && admins.length === 0 && (
            <div className="p-6 text-center text-sm text-muted-foreground" data-testid="empty-state">
              Nenhum administrador encontrado.
            </div>
          )}

          {!isLoading && !isError && admins.length > 0 && (
            <table className="w-full text-sm" data-testid="administrators-table">
              <thead>
                <tr className="border-b bg-muted/50">
                  <th className="text-left py-3 px-4 font-medium text-muted-foreground">
                    Nome
                  </th>
                  <th className="text-left py-3 px-4 font-medium text-muted-foreground">
                    Email
                  </th>
                  <th className="text-left py-3 px-4 font-medium text-muted-foreground">
                    Status
                  </th>
                  <th className="text-left py-3 px-4 font-medium text-muted-foreground">
                    Senha Temporaria
                  </th>
                </tr>
              </thead>
              <tbody>
                {admins.map((admin) => (
                  <tr
                    key={admin.id}
                    className="border-b last:border-0 hover:bg-muted/30 transition-colors"
                    data-testid={`admin-row-${admin.id}`}
                  >
                    <td className="py-3 px-4 font-medium">{admin.fullName}</td>
                    <td className="py-3 px-4 text-muted-foreground">{admin.email}</td>
                    <td className="py-3 px-4">
                      {admin.isEnabled ? (
                        <Badge variant="default" data-testid="badge-active">
                          Ativo
                        </Badge>
                      ) : (
                        <Badge variant="destructive" data-testid="badge-blocked">
                          Bloqueado
                        </Badge>
                      )}
                    </td>
                    <td className="py-3 px-4">
                      {admin.hasTemporaryPassword ? (
                        <Badge variant="outline" className="text-amber-600 border-amber-300" data-testid="badge-temp-password">
                          Pendente
                        </Badge>
                      ) : (
                        <Badge variant="outline" className="text-green-600 border-green-300" data-testid="badge-password-set">
                          Definida
                        </Badge>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
