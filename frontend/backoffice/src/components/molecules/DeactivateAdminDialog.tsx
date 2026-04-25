import { useState } from "react";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { AlertTriangle, Loader2 } from "lucide-react";
import type { AdminUserDto } from "@/lib/admin-api";

interface DeactivateAdminDialogProps {
  open: boolean;
  admin: AdminUserDto | null;
  onClose: () => void;
  onDeactivate: (adminId: string) => Promise<void>;
}

export function DeactivateAdminDialog({
  open,
  admin,
  onClose,
  onDeactivate,
}: DeactivateAdminDialogProps) {
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleConfirm = async () => {
    if (!admin) return;
    setIsSubmitting(true);
    try {
      await onDeactivate(admin.id);
      onClose();
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleOpenChange = (isOpen: boolean) => {
    if (!isOpen && !isSubmitting) {
      onClose();
    }
  };

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent data-testid="deactivate-admin-dialog">
        <DialogHeader>
          <DialogTitle>Desativar Administrador</DialogTitle>
          <DialogDescription>
            O administrador {admin?.fullName ?? ""} perderá acesso ao backoffice.
            A conta é preservada para auditoria.
          </DialogDescription>
        </DialogHeader>

        <Alert variant="destructive">
          <AlertTriangle className="h-4 w-4" />
          <AlertDescription>
            Esta ação pode ser revertida reativando o administrador.
          </AlertDescription>
        </Alert>

        <DialogFooter>
          <Button
            type="button"
            variant="outline"
            onClick={onClose}
            disabled={isSubmitting}
            data-testid="deactivate-cancel-button"
          >
            Cancelar desativação
          </Button>
          <Button
            type="button"
            variant="destructive"
            onClick={handleConfirm}
            disabled={isSubmitting}
            data-testid="deactivate-confirm-button"
          >
            {isSubmitting ? (
              <>
                <Loader2 className="h-4 w-4 mr-1 animate-spin" />
                Desativando...
              </>
            ) : (
              "Desativar"
            )}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}