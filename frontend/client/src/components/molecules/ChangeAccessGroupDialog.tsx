import { useState, useEffect } from "react";
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
import { getAccessGroups, type AccessGroupDto } from "@/lib/api";

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

const GROUP_LABEL_MAP: Record<string, string> = {
  "admin-empresa": "Admin Empresa",
  viewer: "Viewer",
  dashboard: "Dashboard",
};

interface ChangeAccessGroupDialogProps {
  open: boolean;
  employee: EmployeeDto;
  companyId: string;
  onConfirm: (employeeId: string, newGroupId: string) => Promise<void>;
  onClose: () => void;
}

export function ChangeAccessGroupDialog({
  open,
  employee,
  companyId,
  onConfirm,
  onClose,
}: ChangeAccessGroupDialogProps) {
  const [selectedGroupId, setSelectedGroupId] = useState(employee.accessGroupId);
  const [accessGroups, setAccessGroups] = useState<AccessGroupDto[]>([]);
  const [isLoadingGroups, setIsLoadingGroups] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);

  useEffect(() => {
    if (!open || !companyId) return;
    setIsLoadingGroups(true);
    getAccessGroups(companyId)
      .then(setAccessGroups)
      .catch(() => setAccessGroups([]))
      .finally(() => setIsLoadingGroups(false));
  }, [open, companyId]);

  const currentBadge = GROUP_BADGE_MAP[employee.accessGroupName] ?? {
    label: employee.accessGroupName,
    className: "bg-gray-100 text-gray-500 border-gray-200",
  };

  const hasChanged = selectedGroupId !== employee.accessGroupId;

  const handleConfirm = async () => {
    if (!hasChanged) return;
    setIsSubmitting(true);
    try {
      await onConfirm(employee.id, selectedGroupId);
      onClose();
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleOpenChange = (isOpen: boolean) => {
    if (!isOpen && !isSubmitting) {
      setSelectedGroupId(employee.accessGroupId);
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
              value={selectedGroupId}
              onValueChange={setSelectedGroupId}
              disabled={isSubmitting || isLoadingGroups}
            >
              <SelectTrigger id="new-access-group" data-testid="new-access-group-select">
                <SelectValue placeholder={isLoadingGroups ? "Carregando..." : "Selecione o grupo"} />
              </SelectTrigger>
              <SelectContent>
                {accessGroups.map((group) => (
                  <SelectItem key={group.id} value={group.id}>
                    {GROUP_LABEL_MAP[group.name] ?? group.name}
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
            disabled={isSubmitting || !hasChanged || isLoadingGroups}
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