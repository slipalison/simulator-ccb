import { useState, useEffect, useCallback } from "react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { toast } from "sonner";
import { getAuditLog } from "@/lib/admin-api";
import { Shield, Filter, RefreshCw } from "lucide-react";

interface AuditLogEntry {
  id: string;
  timestamp: string;
  adminUserId: string;
  adminUserName: string;
  actionType: string;
  targetUserId: string | null;
  targetUserName: string | null;
  details: string | null;
  ipAddress: string | null;
}

interface PaginatedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

const ACTION_TYPE_LABELS: Record<string, string> = {
  AdminCreated: "Admin Criado",
  AdminBlocked: "Admin Bloqueado",
  AdminUnblocked: "Admin Desbloqueado",
  AdminDeleted: "Admin Excluido",
  AdminLogin: "Admin Login",
  AdminLogout: "Admin Logout",
  AdminPasswordChanged: "Senha Alterada",
  AdminProfileUpdated: "Perfil Atualizado",
  UserCreated: "Usuario Criado",
  UserBlocked: "Usuario Bloqueado",
  UserUnblocked: "Usuario Desbloqueado",
  UserDeleted: "Usuario Excluido",
  UserUpdated: "Usuario Atualizado",
};

export function AuditLogPage() {
  const [page, setPage] = useState(1);
  const [pageSize] = useState(20);
  const [startDate, setStartDate] = useState("");
  const [endDate, setEndDate] = useState("");
  const [actionType, setActionType] = useState("all");
  const [adminUserName, setAdminUserName] = useState("");
  const [result, setResult] = useState<PaginatedResult<AuditLogEntry> | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isError, setIsError] = useState(false);

  const fetchAuditLog = useCallback(async () => {
    setIsLoading(true);
    setIsError(false);
    try {
      const data = await getAuditLog({
        page,
        pageSize,
        startDate: startDate || undefined,
        endDate: endDate || undefined,
        actionType: actionType === "all" ? undefined : actionType,
        adminUserName: adminUserName || undefined,
      });
      setResult(data);
    } catch (_err) {
      setIsError(true);
      toast.error("Falha ao carregar audit log", {
        description: "Tente novamente.",
      });
    } finally {
      setIsLoading(false);
    }
  }, [page, pageSize, startDate, endDate, actionType, adminUserName]);

  useEffect(() => {
    fetchAuditLog();
  }, [fetchAuditLog]);

  const handleFilter = () => {
    setPage(1);
    fetchAuditLog();
  };

  const handleReset = () => {
    setStartDate("");
    setEndDate("");
    setActionType("all");
    setAdminUserName("");
    setPage(1);
  };

  const totalPages = result ? Math.ceil(result.totalCount / result.pageSize) : 0;

  return (
    <div className="space-y-6">
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Shield className="h-5 w-5" />
            Audit Log
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4 mb-4">
            <div className="space-y-2">
              <Label htmlFor="startDate">Data Inicio</Label>
              <Input
                id="startDate"
                type="datetime-local"
                value={startDate}
                onChange={(e) => setStartDate(e.target.value)}
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="endDate">Data Fim</Label>
              <Input
                id="endDate"
                type="datetime-local"
                value={endDate}
                onChange={(e) => setEndDate(e.target.value)}
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="actionType">Tipo de Acao</Label>
              <Select value={actionType} onValueChange={setActionType}>
                <SelectTrigger>
                  <SelectValue placeholder="Todos" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">Todos</SelectItem>
                  {Object.entries(ACTION_TYPE_LABELS).map(([key, label]) => (
                    <SelectItem key={key} value={key}>
                      {label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-2">
              <Label htmlFor="adminUserName">Admin</Label>
              <Input
                id="adminUserName"
                placeholder="Nome do admin"
                value={adminUserName}
                onChange={(e) => setAdminUserName(e.target.value)}
              />
            </div>
          </div>

          <div className="flex gap-2">
            <Button onClick={handleFilter} variant="default">
              <Filter className="h-4 w-4 mr-2" />
              Filtrar
            </Button>
            <Button onClick={handleReset} variant="outline">
              <RefreshCw className="h-4 w-4 mr-2" />
              Limpar
            </Button>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardContent className="p-0">
          {isLoading && (
            <div className="p-8 text-center text-gray-500">Carregando...</div>
          )}

          {isError && (
            <div className="p-8 text-center text-red-500">
              Erro ao carregar audit log. Tente novamente.
            </div>
          )}

          {!isLoading && !isError && result && result.items.length === 0 && (
            <div className="p-8 text-center text-gray-500">
              Nenhuma entrada encontrada.
            </div>
          )}

          {!isLoading && !isError && result && result.items.length > 0 && (
            <>
              <div className="overflow-x-auto">
                <table className="w-full">
                  <thead className="bg-gray-50 dark:bg-gray-900 border-b">
                    <tr>
                      <th className="text-left py-3 px-4 text-sm font-medium text-gray-600 dark:text-gray-400">Data/Hora</th>
                      <th className="text-left py-3 px-4 text-sm font-medium text-gray-600 dark:text-gray-400">Admin</th>
                      <th className="text-left py-3 px-4 text-sm font-medium text-gray-600 dark:text-gray-400">Acao</th>
                      <th className="text-left py-3 px-4 text-sm font-medium text-gray-600 dark:text-gray-400">Alvo</th>
                      <th className="text-left py-3 px-4 text-sm font-medium text-gray-600 dark:text-gray-400">IP</th>
                    </tr>
                  </thead>
                  <tbody>
                    {result.items.map((entry) => (
                      <tr key={entry.id} className="border-b hover:bg-gray-50 dark:hover:bg-gray-900/50">
                        <td className="py-3 px-4 text-sm">
                          {new Date(entry.timestamp).toLocaleString("pt-BR")}
                        </td>
                        <td className="py-3 px-4 text-sm">{entry.adminUserName}</td>
                        <td className="py-3 px-4 text-sm">
                          <span className="inline-flex items-center px-2 py-1 rounded-full text-xs font-medium bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-200">
                            {ACTION_TYPE_LABELS[entry.actionType] || entry.actionType}
                          </span>
                        </td>
                        <td className="py-3 px-4 text-sm">
                          {entry.targetUserName || "—"}
                        </td>
                        <td className="py-3 px-4 text-sm font-mono text-xs">
                          {entry.ipAddress || "—"}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>

              {totalPages > 1 && (
                <div className="flex items-center justify-between p-4 border-t">
                  <p className="text-sm text-gray-600 dark:text-gray-400">
                    {result.totalCount} entradas | Pagina {result.page} de {totalPages}
                  </p>
                  <div className="flex gap-2">
                    <Button
                      variant="outline"
                      size="sm"
                      disabled={page <= 1}
                      onClick={() => setPage(page - 1)}
                    >
                      Anterior
                    </Button>
                    <Button
                      variant="outline"
                      size="sm"
                      disabled={page >= totalPages}
                      onClick={() => setPage(page + 1)}
                    >
                      Proxima
                    </Button>
                  </div>
                </div>
              )}
            </>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
