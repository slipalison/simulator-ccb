import { useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { adminUnblockUserSchema, type AdminUnblockUserInput } from "@/lib/validation-schemas";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
  DialogDescription,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Textarea } from "@/components/ui/textarea";
import { Label } from "@/components/ui/label";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { CheckCircle, Loader2 } from "lucide-react";
import { toast } from "sonner";

interface UnblockDialogProps {
  userName: string;
  onUnblock: (reason?: string) => Promise<void>;
  onClose: () => void;
  open: boolean;
}

export function UnblockDialog({ userName, onUnblock, onClose, open }: UnblockDialogProps) {
  const [isSubmitting, setIsSubmitting] = useState(false);
  const {
    register,
    handleSubmit,
    reset,
  } = useForm<AdminUnblockUserInput>({
    resolver: zodResolver(adminUnblockUserSchema as any),
    defaultValues: { reason: "" },
  });

  const onSubmit = async (data: AdminUnblockUserInput) => {
    setIsSubmitting(true);
    try {
      const reason = data.reason && data.reason.trim() ? data.reason : undefined;
      await onUnblock(reason);
      toast.success("Usuario desbloqueado com sucesso.");
      reset();
      onClose();
    } catch {
      toast.error("Falha ao desbloquear usuario", {
        description: "Tente novamente.",
      });
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleOpenChange = (isOpen: boolean) => {
    if (!isOpen) {
      reset();
      onClose();
    }
  };

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Unblock User</DialogTitle>
          <DialogDescription>
            This action will restore access for {userName}.
          </DialogDescription>
        </DialogHeader>

        <Alert>
          <CheckCircle className="h-4 w-4" />
          <AlertDescription>
            This will restore user access.
          </AlertDescription>
        </Alert>

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="unblock-reason">
              Reason for Unblocking (optional)
            </Label>
            <Textarea
              id="unblock-reason"
              {...register("reason")}
              placeholder="Enter the reason for unblocking (optional)..."
              disabled={isSubmitting}
              data-testid="reason-input"
            />
          </div>

          <DialogFooter>
            <Button
              type="button"
              variant="outline"
              onClick={onClose}
              disabled={isSubmitting}
              data-testid="cancel-button"
            >
              Cancel
            </Button>
            <Button
              type="submit"
              disabled={isSubmitting}
              data-testid="unblock-button"
            >
              {isSubmitting ? (
                <>
                  <Loader2 className="h-4 w-4 mr-1 animate-spin" />
                  Unblocking...
                </>
              ) : (
                "Unblock User"
              )}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
