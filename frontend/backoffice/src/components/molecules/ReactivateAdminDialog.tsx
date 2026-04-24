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
import { Loader2 } from "lucide-react";
import type { AdminUserDto } from "@/lib/admin-api";

interface ReactivateAdminDialogProps {
  open: boolean;
  admin: AdminUserDto | null;
  onClose: () => void;
  onReactivate: (adminId: string) => Promise<void>;
}

export function ReactivateAdminDialog({
  open,
  admin,
  onClose,
  onReactivate,
}: ReactivateAdminDialogProps) {
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleConfirm = async () => {
    if (!admin) return;
    setIsSubmitting(true);
    try {
      await onReactivate(admin.id);
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
      <DialogContent data-testid="reactivate-admin-dialog">
        <DialogHeader>
          <DialogTitle>Reativar Administrador</DialogTitle>
          <DialogDescription>
            O administrador {admin?.fullName ?? ""} recuperará acesso ao backoffice.
          </DialogDescription>
        </DialogHeader>

        <DialogFooter>
          <Button
            type="button"
            variant="outline"
            onClick={onClose}
            disabled={isSubmitting}
            data-testid="reactivate-cancel-button"
          >
            Cancelar reativação
          </Button>
          <Button
            type="button"
            variant="default"
            onClick={handleConfirm}
            disabled={isSubmitting}
            data-testid="reactivate-confirm-button"
          >
            {isSubmitting ? (
              <>
                <Loader2 className="h-4 w-4 mr-1 animate-spin" />
                Reativando...
              </>
            ) : (
              "Reativar"
            )}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}