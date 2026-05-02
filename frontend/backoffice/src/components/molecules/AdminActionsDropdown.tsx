import { MoreHorizontal, Loader2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from "@/components/ui/tooltip";
import { useAdminAuth } from "@/lib/admin-auth-context";
import type { AdminUserDto } from "@/lib/admin-api";

interface AdminActionsDropdownProps {
  admin: AdminUserDto;
  onEdit: (admin: AdminUserDto) => void;
  onResetPassword: (admin: AdminUserDto) => void;
  onDeactivate: (admin: AdminUserDto) => void;
  onReactivate: (admin: AdminUserDto) => void;
  isResettingPassword?: boolean;
}

export function AdminActionsDropdown({
  admin,
  onEdit,
  onResetPassword,
  onDeactivate,
  onReactivate,
  isResettingPassword = false,
}: AdminActionsDropdownProps) {
  const { admin: authAdmin } = useAdminAuth();
  const isSelf = authAdmin.adminId !== null && authAdmin.adminId.toLowerCase() === admin.id.toLowerCase();

  if (isSelf) {
    return (
      <TooltipProvider>
        <Tooltip>
          <TooltipTrigger asChild>
            <span className="inline-flex" aria-disabled="true">
              <Button
                variant="ghost"
                size="icon"
                disabled
                className="opacity-50 cursor-not-allowed h-8 w-8"
                aria-label="Ações desabilitadas"
                data-testid="actions-dropdown-disabled"
              >
                <MoreHorizontal className="h-4 w-4" />
              </Button>
            </span>
          </TooltipTrigger>
          <TooltipContent>
            <p>Você não pode modificar a própria conta</p>
          </TooltipContent>
        </Tooltip>
      </TooltipProvider>
    );
  }

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button
          variant="ghost"
          size="icon"
          className="h-8 w-8"
          aria-label="Abrir menu de ações"
          aria-haspopup="true"
          data-testid="actions-dropdown-trigger"
        >
          <MoreHorizontal className="h-4 w-4" />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" data-testid="actions-dropdown-content">
        <DropdownMenuItem
          onClick={() => onEdit(admin)}
          data-testid="action-edit"
        >
          Editar
        </DropdownMenuItem>
        <DropdownMenuItem
          onClick={() => onResetPassword(admin)}
          disabled={isResettingPassword}
          data-testid="action-reset-password"
        >
          {isResettingPassword ? (
            <>
              <Loader2 className="h-4 w-4 mr-2 animate-spin" />
              Resetando...
            </>
          ) : (
            "Resetar senha"
          )}
        </DropdownMenuItem>
        <DropdownMenuSeparator />
        {admin.isEnabled ? (
          <DropdownMenuItem
            onClick={() => onDeactivate(admin)}
            className="text-destructive focus:text-destructive"
            data-testid="action-deactivate"
          >
            Desativar
          </DropdownMenuItem>
        ) : (
          <DropdownMenuItem
            onClick={() => onReactivate(admin)}
            data-testid="action-reactivate"
          >
            Reativar
          </DropdownMenuItem>
        )}
      </DropdownMenuContent>
    </DropdownMenu>
  );
}