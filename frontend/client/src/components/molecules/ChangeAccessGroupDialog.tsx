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
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Label } from "@/components/ui/label";
import { Badge } from "@/components/ui/badge";
import { Loader2 } from "lucide-react";
import type { EmployeeDto } from "@/lib/types";

const ACCESS_GROUPS = [
  { value: "admin-empresa", label: "Admin Empresa" },
  { value: "viewer", label: "Viewer" },
  { value: "dashboard", label: "Dashboard" },
] as const;

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

interface ChangeAccessGroupDialogProps {
  open: boolean;
  employee: EmployeeDto;
  companyId: string;
  onConfirm: (employeeId: string, newGroupName: string) => Promise<void>;
  onClose: () => void;
}

export function ChangeAccessGroupDialog({
  open,
  employee,
  companyId: _companyId,
  onConfirm,
  onClose,
}: ChangeAccessGroupDialogProps) {
  const [selectedGroup, setSelectedGroup] = useState(employee.accessGroupName);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const currentBadge = GROUP_BADGE_MAP[employee.accessGroupName] ?? {
    label: employee.accessGroupName,
    className: "bg-gray-100 text-gray-500 border-gray-200",
  };

  const hasChanged = selectedGroup !== employee.accessGroupName;

  const handleConfirm = async () => {
    if (!hasChanged) return;
    setIsSubmitting(true);
    try {
      await onConfirm(employee.id, selectedGroup);
      onClose();
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleOpenChange = (isOpen: boolean) => {
    if (!isOpen && !isSubmitting) {
      setSelectedGroup(employee.accessGroupName);
      onClose();
    }
  };

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent data-testid="change-access-group-dialog">
        <DialogHeader>
          <DialogTitle>Alterar Grupo de Acesso</DialogTitle>
          <DialogDescription>
            Altere o grupo de acesso de {employee.nome}.
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4">
          <div className="space-y-2">
            <Label>Grupo atual</Label>
            <div>
              <Badge variant="outline" className={currentBadge.className}>
                {currentBadge.label}
              </Badge>
            </div>
          </div>

          <div className="space-y-2">
            <Label htmlFor="new-access-group">Novo grupo</Label>
            <Select
              value={selectedGroup}
              onValueChange={setSelectedGroup}
              disabled={isSubmitting}
            >
              <SelectTrigger id="new-access-group" data-testid="new-access-group-select">
                <SelectValue placeholder="Selecione o grupo" />
              </SelectTrigger>
              <SelectContent>
                {ACCESS_GROUPS.map((group) => (
                  <SelectItem key={group.value} value={group.value}>
                    {group.label}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
        </div>

        <DialogFooter>
          <Button
            type="button"
            variant="outline"
            onClick={onClose}
            disabled={isSubmitting}
            data-testid="change-group-cancel-button"
          >
            Cancelar
          </Button>
          <Button
            type="button"
            variant="default"
            disabled={isSubmitting || !hasChanged}
            onClick={handleConfirm}
            data-testid="change-group-confirm-button"
          >
            {isSubmitting ? (
              <>
                <Loader2 className="h-4 w-4 mr-1 animate-spin" />
                Alterando...
              </>
            ) : (
              "Alterar grupo"
            )}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}