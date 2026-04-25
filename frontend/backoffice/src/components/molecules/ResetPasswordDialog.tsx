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
import { Input } from "@/components/ui/input";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { AlertTriangle, Copy, Check } from "lucide-react";

interface ResetPasswordDialogProps {
  open: boolean;
  generatedPassword: string | null;
  onClose: () => void;
}

export function ResetPasswordDialog({
  open,
  generatedPassword,
  onClose,
}: ResetPasswordDialogProps) {
  const [copied, setCopied] = useState(false);

  const handleCopy = async () => {
    if (!generatedPassword) return;
    try {
      await navigator.clipboard.writeText(generatedPassword);
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    } catch {
      // Fallback: selecionar o texto manualmente
    }
  };

  const handleOpenChange = (isOpen: boolean) => {
    if (!isOpen) {
      setCopied(false);
      onClose();
    }
  };

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent data-testid="reset-password-dialog">
        <DialogHeader>
          <DialogTitle>Senha Temporária Gerada</DialogTitle>
          <DialogDescription>
            Compartilhe esta senha com o administrador agora. Ela não poderá ser recuperada depois.
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4">
          <div className="space-y-2">
            <Input
              readOnly
              value={generatedPassword ?? ""}
              className="font-mono text-sm select-all"
              aria-label="Senha temporária gerada"
              data-testid="generated-password-input"
            />
          </div>

          <Alert variant="destructive">
            <AlertTriangle className="h-4 w-4" />
            <AlertDescription>
              Esta senha não pode ser recuperada. Feche somente após copiar.
            </AlertDescription>
          </Alert>

          <Button
            type="button"
            variant="outline"
            className="w-full"
            onClick={handleCopy}
            aria-label={copied ? "Senha copiada" : "Copiar senha para clipboard"}
            data-testid="copy-password-button"
          >
            {copied ? (
              <>
                <Check className="h-4 w-4 mr-2" />
                Copiado!
              </>
            ) : (
              <>
                <Copy className="h-4 w-4 mr-2" />
                Copiar senha
              </>
            )}
          </Button>
        </div>

        <DialogFooter>
          <Button
            type="button"
            variant="default"
            onClick={() => handleOpenChange(false)}
            data-testid="close-password-dialog-button"
          >
            Fechar
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}