import { useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import {
  adminEditAdministratorSchema,
  type AdminEditAdministratorInput,
} from "@/lib/validation-schemas";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Loader2 } from "lucide-react";
import type { AdminUserDto } from "@/lib/admin-api";

interface EditAdminDialogProps {
  open: boolean;
  admin: AdminUserDto | null;
  onClose: () => void;
  onSave: (adminId: string, data: AdminEditAdministratorInput) => Promise<void>;
}

export function EditAdminDialog({ open, admin, onClose, onSave }: EditAdminDialogProps) {
  const [isSubmitting, setIsSubmitting] = useState(false);

  const {
    register,
    handleSubmit,
    formState: { errors },
    reset,
  } = useForm<AdminEditAdministratorInput>({
    resolver: zodResolver(adminEditAdministratorSchema),
    defaultValues: { fullName: "", email: "" },
  });

  useEffect(() => {
    if (admin) {
      reset({ fullName: admin.fullName, email: admin.email });
    }
  }, [admin, reset]);

  const onSubmit = async (data: AdminEditAdministratorInput) => {
    if (!admin) return;
    setIsSubmitting(true);
    try {
      await onSave(admin.id, data);
      onClose();
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleOpenChange = (isOpen: boolean) => {
    if (!isOpen) {
      reset({ fullName: "", email: "" });
      onClose();
    }
  };

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent data-testid="edit-admin-dialog">
        <DialogHeader>
          <DialogTitle>Editar Administrador</DialogTitle>
          <DialogDescription>
            Atualize o nome e o email do administrador.
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="edit-fullName">Nome</Label>
            <Input
              id="edit-fullName"
              {...register("fullName")}
              placeholder="Nome completo"
              disabled={isSubmitting}
              autoFocus
              data-testid="edit-fullname-input"
            />
            {errors.fullName && (
              <p className="text-sm text-destructive" data-testid="edit-fullname-error">
                {errors.fullName.message}
              </p>
            )}
          </div>

          <div className="space-y-2">
            <Label htmlFor="edit-email">Email</Label>
            <Input
              id="edit-email"
              type="email"
              {...register("email")}
              placeholder="email@exemplo.com"
              disabled={isSubmitting}
              data-testid="edit-email-input"
            />
            {errors.email && (
              <p className="text-sm text-destructive" data-testid="edit-email-error">
                {errors.email.message}
              </p>
            )}
          </div>

          <DialogFooter>
            <Button
              type="button"
              variant="outline"
              onClick={onClose}
              disabled={isSubmitting}
              data-testid="edit-cancel-button"
            >
              Cancelar edição
            </Button>
            <Button
              type="submit"
              variant="default"
              disabled={isSubmitting}
              data-testid="edit-save-button"
            >
              {isSubmitting ? (
                <>
                  <Loader2 className="h-4 w-4 mr-1 animate-spin" />
                  Salvando...
                </>
              ) : (
                "Salvar alterações"
              )}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}