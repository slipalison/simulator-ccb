import { Skeleton } from "@/components/ui/skeleton";
import { Badge } from "@/components/ui/badge";
import { EmployeeActionsDropdown } from "@/components/molecules/EmployeeActionsDropdown";
import type { EmployeeDto, PaginatedEmployeesResult } from "@/lib/types";

// ---------------------------------------------------------------------------
// Access group badge colours per D-04
// ---------------------------------------------------------------------------

const GROUP_BADGE_MAP: Record<string, { label: string; className: string }> = {
  "admin-empresa": {
    label: "Admin Empresa",
    className: "bg-green-100 text-green-800 border-green-300",
  },
  viewer: {
    label: "Viewer",
    className: "bg-gray-100 text-gray-800 border-gray-300",
  },
  dashboard: {
    label: "Dashboard",
    className: "bg-blue-100 text-blue-800 border-blue-300",
  },
};

const DEFAULT_GROUP_BADGE = { label: "—", className: "bg-gray-100 text-gray-500 border-gray-200" };

// ---------------------------------------------------------------------------
// Props
// ---------------------------------------------------------------------------

interface EmployeesTableProps {
  result: PaginatedEmployeesResult | null;
  isLoading: boolean;
  isError: boolean;
  onRetry: () => void;
  accessGroup: string;
  onEdit: (employee: EmployeeDto) => void;
  onBlockUnblock: (employee: EmployeeDto) => void;
  onResetPassword: (employee: EmployeeDto) => void;
  onDelete: (employee: EmployeeDto) => void;
  onChangeAccessGroup: (employee: EmployeeDto) => void;
}

const SKELETON_ROWS = 5;

export function EmployeesTable({
  result,
  isLoading,
  isError,
  onRetry,
  accessGroup,
  onEdit,
  onBlockUnblock,
  onResetPassword,
  onDelete,
  onChangeAccessGroup,
}: EmployeesTableProps) {
  const isViewer = accessGroup === "viewer";

  // --- Loading skeleton ---
  if (isLoading && !result) {
    return (
      <div
        aria-busy="true"
        aria-label="Carregando funcionários..."
        data-testid="table-loading"
      >
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b bg-muted/50">
              <th className="text-left py-3 px-4 font-semibold text-muted-foreground">Nome</th>
              <th className="text-left py-3 px-4 font-semibold text-muted-foreground">Email</th>
              <th className="text-left py-3 px-4 font-semibold text-muted-foreground">Grupo</th>
              <th className="text-left py-3 px-4 font-semibold text-muted-foreground w-[100px]">Status</th>
              {!isViewer && (
                <th className="text-left py-3 px-4 font-semibold text-muted-foreground w-[64px]">Ações</th>
              )}
            </tr>
          </thead>
          <tbody>
            {Array.from({ length: SKELETON_ROWS }).map((_, i) => (
              <tr key={i} className="border-b last:border-0">
                <td className="py-3 px-4"><Skeleton className="h-4 w-full" /></td>
                <td className="py-3 px-4"><Skeleton className="h-4 w-full" /></td>
                <td className="py-3 px-4"><Skeleton className="h-4 w-24" /></td>
                <td className="py-3 px-4"><Skeleton className="h-4 w-16" /></td>
                {!isViewer && (
                  <td className="py-3 px-4"><Skeleton className="h-8 w-8 rounded" /></td>
                )}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    );
  }

  // --- Error state ---
  if (isError && !result) {
    return (
      <div
        className="p-6 text-center text-sm text-destructive"
        data-testid="table-error"
      >
        Falha ao carregar funcionários. Tente novamente.{" "}
        <button
          type="button"
          className="underline hover:no-underline"
          onClick={onRetry}
          data-testid="retry-button"
        >
          Tentar novamente
        </button>
      </div>
    );
  }

  // --- Empty state ---
  if (!isLoading && result && result.totalCount === 0) {
    return (
      <div className="p-6 text-center space-y-1" data-testid="table-empty">
        <p className="text-sm font-semibold">Nenhum funcionário encontrado.</p>
        <p className="text-sm text-muted-foreground">Ajuste os filtros ou cadastre um novo funcionário.</p>
      </div>
    );
  }

  const items = result?.items ?? [];

  return (
    <div
      className={isLoading ? "opacity-60 pointer-events-none" : undefined}
      aria-busy={isLoading}
      data-testid="employees-table-wrapper"
    >
      <table className="w-full text-sm" data-testid="employees-table">
        <thead>
          <tr className="border-b bg-muted/50">
            <th scope="col" className="text-left py-3 px-4 font-semibold text-muted-foreground">Nome</th>
            <th scope="col" className="text-left py-3 px-4 font-semibold text-muted-foreground">Email</th>
            <th scope="col" className="text-left py-3 px-4 font-semibold text-muted-foreground">Grupo</th>
            <th scope="col" className="text-left py-3 px-4 font-semibold text-muted-foreground w-[100px]">Status</th>
            {!isViewer && (
              <th scope="col" className="text-left py-3 px-4 font-semibold text-muted-foreground w-[64px]">Ações</th>
            )}
          </tr>
        </thead>
        <tbody>
          {items.map((employee) => {
            const groupBadge = GROUP_BADGE_MAP[employee.accessGroupName] ?? DEFAULT_GROUP_BADGE;
            return (
              <tr
                key={employee.id}
                className="border-b last:border-0 hover:bg-muted/30 transition-colors"
                data-testid={`employee-row-${employee.id}`}
              >
                <td className="py-3 px-4 font-semibold">{employee.nome}</td>
                <td className="py-3 px-4 text-muted-foreground">{employee.email}</td>
                <td className="py-3 px-4">
                  <Badge
                    variant="outline"
                    className={groupBadge.className}
                    data-testid={`badge-group-${employee.id}`}
                  >
                    {groupBadge.label}
                  </Badge>
                </td>
                <td className="py-3 px-4">
                  {employee.keycloakEnabled ? (
                    <Badge variant="default" data-testid={`badge-status-active-${employee.id}`}>
                      Ativo
                    </Badge>
                  ) : (
                    <Badge variant="destructive" data-testid={`badge-status-blocked-${employee.id}`}>
                      Bloqueado
                    </Badge>
                  )}
                </td>
                {!isViewer && (
                  <td className="py-3 px-4">
                    <EmployeeActionsDropdown
                      employee={employee}
                      onEdit={onEdit}
                      onBlockUnblock={onBlockUnblock}
                      onResetPassword={onResetPassword}
                      onDelete={onDelete}
                      onChangeAccessGroup={onChangeAccessGroup}
                    />
                  </td>
                )}
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}