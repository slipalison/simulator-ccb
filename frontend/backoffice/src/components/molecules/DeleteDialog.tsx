import { useState } from "react";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
  DialogDescription,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { AlertTriangle, Loader2 } from "lucide-react";
import { toast } from "sonner";
import { AdminApiError } from "@/lib/admin-api";

interface DeleteDialogProps {
  userName: string;
  userEmail: string;
  userDocument?: string;
  onDelete: (confirmEmail: string) => Promise<void>;
  onSuccess: () => void;
  onClose: () => void;
  open: boolean;
}

export function DeleteDialog({
  userName,
  userEmail,
  userDocument,
  onDelete,
  onSuccess,
  onClose,
  open,
}: DeleteDialogProps) {
  const [emailInput, setEmailInput] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);

  const emailMatches = emailInput.trim().toLowerCase() === userEmail.toLowerCase();

  const handleDelete = async () => {
    if (!emailMatches) return;
    setIsSubmitting(true);
    try {
      await onDelete(emailInput.trim());
      toast.success("Usuario deletado com sucesso.");
      setEmailInput("");
      onSuccess();
      onClose();
    } catch (err: unknown) {
      if (err instanceof AdminApiError && err.status === 409) {
        toast.error("Usuario ja foi deletado.");
      } else {
        toast.error("Falha ao deletar usuario", {
          description: "Tente novamente.",
        });
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleOpenChange = (isOpen: boolean) => {
    if (!isOpen) {
      setEmailInput("");
      onClose();
    }
  };

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Delete User</DialogTitle>
          <DialogDescription>
            This action is permanent and cannot be undone.
          </DialogDescription>
        </DialogHeader>

        <Alert variant="destructive">
          <AlertTriangle className="h-4 w-4" />
          <AlertDescription>
            This action is PERMANENT and cannot be undone. This will anonymize all data in our database
            and delete the user from Keycloak (LGPD compliance).
          </AlertDescription>
        </Alert>

        <div className="space-y-3 py-2">
          <div className="space-y-1">
            <Label className="text-sm font-medium">User to delete</Label>
            <div className="text-sm text-muted-foreground">
              <p><span className="font-medium">Name:</span> {userName}</p>
              <p><span className="font-medium">Email:</span> {userEmail}</p>
              {userDocument && <p><span className="font-medium">Document:</span> {userDocument}</p>}
            </div>
          </div>

          <div className="space-y-2">
            <Label htmlFor="delete-email-confirm">
              Type the user&apos;s email to confirm:
            </Label>
            <Input
              id="delete-email-confirm"
              type="email"
              value={emailInput}
              onChange={(e) => setEmailInput(e.target.value)}
              placeholder={userEmail}
              disabled={isSubmitting}
              data-testid="email-confirm-input"
              autoComplete="off"
            />
            {emailInput.length > 0 && !emailMatches && (
              <p className="text-sm text-destructive" data-testid="email-mismatch-error">
                Email does not match. Type the exact email to confirm deletion.
              </p>
            )}
          </div>
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
            type="button"
            variant="destructive"
            disabled={isSubmitting || !emailMatches}
            onClick={handleDelete}
            data-testid="confirm-delete-button"
          >
            {isSubmitting ? (
              <>
                <Loader2 className="h-4 w-4 mr-1 animate-spin" />
                Deleting...
              </>
            ) : (
              "Delete User"
            )}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
