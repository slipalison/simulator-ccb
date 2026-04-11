import { useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { adminBlockUserSchema, type AdminBlockUserInput } from "@/lib/validation-schemas";
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
import { AlertTriangle, Loader2 } from "lucide-react";
import { toast } from "sonner";

interface BlockDialogProps {
  userName: string;
  onBlock: (reason: string) => Promise<void>;
  onClose: () => void;
  open: boolean;
}

export function BlockDialog({ userName, onBlock, onClose, open }: BlockDialogProps) {
  const [isSubmitting, setIsSubmitting] = useState(false);
  const {
    register,
    handleSubmit,
    formState: { errors },
    reset,
  } = useForm<AdminBlockUserInput>({
    resolver: zodResolver(adminBlockUserSchema as any),
    defaultValues: { reason: "" },
  });

  const onSubmit = async (data: AdminBlockUserInput) => {
    setIsSubmitting(true);
    try {
      await onBlock(data.reason);
      toast.success("Usuario bloqueado com sucesso.");
      reset();
      onClose();
    } catch {
      toast.error("Falha ao bloquear usuario", {
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
          <DialogTitle>Block User</DialogTitle>
          <DialogDescription>
            This action will block {userName} from logging in.
          </DialogDescription>
        </DialogHeader>

        <Alert variant="destructive">
          <AlertTriangle className="h-4 w-4" />
          <AlertDescription>
            This will prevent the user from logging in.
          </AlertDescription>
        </Alert>

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="block-reason">
              Reason for Blocking <span className="text-destructive">*</span>
            </Label>
            <Textarea
              id="block-reason"
              {...register("reason")}
              placeholder="Enter the reason for blocking this user..."
              disabled={isSubmitting}
              data-testid="reason-input"
            />
            {errors.reason && (
              <p className="text-sm text-destructive" data-testid="reason-error">
                {errors.reason.message}
              </p>
            )}
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
              variant="destructive"
              disabled={isSubmitting}
              data-testid="block-button"
            >
              {isSubmitting ? (
                <>
                  <Loader2 className="h-4 w-4 mr-1 animate-spin" />
                  Blocking...
                </>
              ) : (
                "Block User"
              )}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
