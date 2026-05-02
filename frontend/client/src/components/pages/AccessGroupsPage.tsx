import { useState, useEffect, useCallback } from "react";
import { useAuth } from "@/lib/auth-context";
import { Navigate } from "@tanstack/react-router";
import { Card, CardContent } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Loader2, Shield, Plus, Pencil, Trash2, Lock } from "lucide-react";
import { toast } from "sonner";
import {
  getAccessGroups,
  createAccessGroup,
  updateAccessGroup,
  deleteAccessGroup,
  type AccessGroupDto,
  PERMISSION_OPTIONS,
  PERMISSION_LABELS,
} from "@/lib/api";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Badge } from "@/components/ui/badge";

interface DialogState {
  type: "create" | "edit" | "delete";
  group: AccessGroupDto | null;
}

export function AccessGroupsPage() {
  const { auth } = useAuth();
  const companyId = auth.companyId;
  const accessGroup = auth.accessGroup;
  const [groups, setGroups] = useState<AccessGroupDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [dialog, setDialog] = useState<DialogState | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [formName, setFormName] = useState("");
  const [formPermissions, setFormPermissions] = useState<string[]>([]);
  const [apiError, setApiError] = useState<string | null>(null);

  const canManage = accessGroup === "admin-empresa";

  const fetchGroups = useCallback(async () => {
    if (!companyId) return;
    setIsLoading(true);
    try {
      const result = await getAccessGroups(companyId);
      setGroups(result);
    } catch {
      toast.error("Erro ao carregar grupos de acesso.");
    } finally {
      setIsLoading(false);
    }
  }, [companyId]);

  useEffect(() => {
    fetchGroups();
  }, [fetchGroups]);

  if (!companyId) return <Navigate to="/" />;

  const openCreateDialog = () => {
    setFormName("");
    setFormPermissions(["employees:read"]);
    setApiError(null);
    setDialog({ type: "create", group: null });
  };

  const openEditDialog = (group: AccessGroupDto) => {
    setFormName(group.name);
    setFormPermissions([...group.permissions]);
    setApiError(null);
    setDialog({ type: "edit", group });
  };

  const openDeleteDialog = (group: AccessGroupDto) => {
    setApiError(null);
    setDialog({ type: "delete", group });
  };

  const handleSubmit = async () => {
    if (!companyId || !formName.trim() || formPermissions.length === 0) return;
    setIsSubmitting(true);
    setApiError(null);
    try {
      if (dialog?.type === "create") {
        await createAccessGroup(companyId, { name: formName.trim(), permissions: formPermissions });
        toast.success("Grupo de acesso criado!");
      } else if (dialog?.type === "edit" && dialog.group) {
        await updateAccessGroup(companyId, dialog.group.id, {
          name: formName.trim() !== dialog.group.name ? formName.trim() : undefined,
          permissions: formPermissions,
        });
        toast.success("Grupo de acesso atualizado!");
      }
      setDialog(null);
      fetchGroups();
    } catch (err: unknown) {
      if (err && typeof err === "object" && "message" in err) {
        setApiError((err as { message: string }).message);
      } else {
        setApiError("Erro ao salvar grupo de acesso.");
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleDelete = async () => {
    if (!companyId || !dialog?.group) return;
    setIsSubmitting(true);
    setApiError(null);
    try {
      await deleteAccessGroup(companyId, dialog.group.id);
      toast.success("Grupo de acesso excluído!");
      setDialog(null);
      fetchGroups();
    } catch (err: unknown) {
      if (err && typeof err === "object" && "message" in err) {
        setApiError((err as { message: string }).message);
      } else {
        setApiError("Erro ao excluir grupo de acesso.");
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  const togglePermission = (perm: string) => {
    setFormPermissions((prev) =>
      prev.includes(perm) ? prev.filter((p) => p !== perm) : [...prev, perm]
    );
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          <Shield className="h-5 w-5 text-primary" aria-hidden="true" />
          <h2 className="text-xl font-semibold">Grupos de Acesso</h2>
        </div>
        <div className="flex gap-2">
          {canManage && (
            <Button variant="default" size="sm" onClick={openCreateDialog} data-testid="create-group-button">
              <Plus className="h-4 w-4 mr-1" aria-hidden="true" />
              Novo Grupo
            </Button>
          )}
          <Button variant="outline" size="sm" onClick={fetchGroups} disabled={isLoading} data-testid="refresh-groups-button">
            <Loader2 className={`h-4 w-4 mr-1 ${isLoading ? "animate-spin" : ""}`} aria-hidden="true" />
            Atualizar
          </Button>
        </div>
      </div>

      {isLoading ? (
        <div className="flex justify-center py-8"><Loader2 className="h-6 w-6 animate-spin text-primary" /></div>
      ) : groups.length === 0 ? (
        <Card><CardContent className="py-8 text-center text-muted-foreground">Nenhum grupo de acesso encontrado.</CardContent></Card>
      ) : (
        <div className="space-y-3">
          {groups.map((group) => {
            return (
              <Card key={group.id} data-testid={`access-group-${group.id}`}>
                <CardContent className="flex items-center justify-between py-4">
                  <div className="flex-1">
                    <div className="flex items-center gap-2 mb-1">
                      <span className="font-medium">{group.name}</span>
                      {group.isDefault && <Lock className="h-3.5 w-3.5 text-muted-foreground" aria-label="Grupo padrão" />}
                    </div>
                    <div className="flex flex-wrap gap-1.5 mt-2">
                      {group.permissions.map((perm) => (
                        <Badge key={perm} variant="outline" className="text-xs">
                          {PERMISSION_LABELS[perm] ?? perm}
                        </Badge>
                      ))}
                    </div>
                  </div>
                  <div className="flex gap-1 ml-4">
                    {canManage && !group.isDefault && (
                      <>
                        <Button variant="ghost" size="sm" onClick={() => openEditDialog(group)} data-testid={`edit-group-${group.id}`}>
                          <Pencil className="h-4 w-4" />
                        </Button>
                        <Button variant="ghost" size="sm" onClick={() => openDeleteDialog(group)} data-testid={`delete-group-${group.id}`}>
                          <Trash2 className="h-4 w-4 text-destructive" />
                        </Button>
                      </>
                    )}
                  </div>
                </CardContent>
              </Card>
            );
          })}
        </div>
      )}

      {/* Create/Edit Dialog */}
      <Dialog open={dialog?.type === "create" || dialog?.type === "edit"} onOpenChange={(open) => { if (!open) setDialog(null); }}>
        <DialogContent className="sm:max-w-md" data-testid="access-group-form-dialog">
          <DialogHeader>
            <DialogTitle>{dialog?.type === "create" ? "Novo Grupo de Acesso" : "Editar Grupo de Acesso"}</DialogTitle>
            <DialogDescription>
              {dialog?.type === "create"
                ? "Defina o nome e as permissões do novo grupo."
                : "Altere o nome e/ou as permissões do grupo."}
            </DialogDescription>
          </DialogHeader>
          {apiError && <p className="text-sm text-destructive" data-testid="group-api-error">{apiError}</p>}
          <div className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="group-name">Nome</Label>
              <Input id="group-name" value={formName} onChange={(e) => setFormName(e.target.value)} placeholder="Ex: financeiro" disabled={isSubmitting} data-testid="group-name-input" />
            </div>
            <div className="space-y-2">
              <Label>Permissões</Label>
              <div className="space-y-2">
                {PERMISSION_OPTIONS.map((opt) => (
                  <label key={opt.value} className="flex items-center gap-2 cursor-pointer">
                    <input type="checkbox" checked={formPermissions.includes(opt.value)} onChange={() => togglePermission(opt.value)} disabled={isSubmitting} className="rounded border-gray-300" data-testid={`perm-${opt.value}`} />
                    <span className="text-sm">{opt.label}</span>
                  </label>
                ))}
              </div>
            </div>
          </div>
          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => setDialog(null)} disabled={isSubmitting}>Cancelar</Button>
            <Button type="button" onClick={handleSubmit} disabled={isSubmitting || !formName.trim() || formPermissions.length === 0} data-testid="group-submit-button">
              {isSubmitting ? <><Loader2 className="h-4 w-4 mr-1 animate-spin" /> Salvando...</> : "Salvar"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Delete Confirmation Dialog */}
      <Dialog open={dialog?.type === "delete"} onOpenChange={(open) => { if (!open) setDialog(null); }}>
        <DialogContent className="sm:max-w-md" data-testid="delete-group-dialog">
          <DialogHeader>
            <DialogTitle>Excluir Grupo de Acesso</DialogTitle>
            <DialogDescription>
              Tem certeza que deseja excluir o grupo <strong>{dialog?.group?.name}</strong>?
              Funcionários vinculados a este grupo precisam ser movidos antes da exclusão.
            </DialogDescription>
          </DialogHeader>
          {apiError && <p className="text-sm text-destructive" data-testid="delete-group-api-error">{apiError}</p>}
          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => setDialog(null)} disabled={isSubmitting}>Cancelar</Button>
            <Button type="button" variant="destructive" onClick={handleDelete} disabled={isSubmitting} data-testid="delete-group-confirm-button">
              {isSubmitting ? <><Loader2 className="h-4 w-4 mr-1 animate-spin" /> Excluindo...</> : "Excluir"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}