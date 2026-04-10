import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Separator } from "@/components/ui/separator";
import { KeycloakStatusBadge } from "./KeycloakStatusBadge";
import { Pencil, Ban, CheckCircle, Trash2 } from "lucide-react";
import type { UserDetailDto } from "@/lib/admin-api";

interface UserDetailCardProps {
  user: UserDetailDto;
  onEdit?: () => void;
  onBlock?: () => void;
  onUnblock?: () => void;
  onDelete?: () => void;
}

export function UserDetailCard({
  user,
  onEdit,
  onBlock,
  onUnblock,
  onDelete,
}: UserDetailCardProps) {
  const isDeleted = !!user.deletedAt;
  const isPF = user.type === "PF";

  return (
    <Card data-testid="user-detail-card">
      <CardHeader>
        <div className="flex justify-between items-start">
          <div>
            <CardTitle data-testid="user-name">{user.name}</CardTitle>
            <p className="text-sm text-muted-foreground mt-1" data-testid="user-email">
              {user.email}
            </p>
          </div>
          <KeycloakStatusBadge
            enabled={user.keycloakEnabled}
            emailVerified={user.keycloakEmailVerified}
          />
        </div>
      </CardHeader>
      <CardContent>
        <div className="space-y-4">
          {/* Basic Info */}
          <div data-testid="basic-info-section">
            <h3 className="text-sm font-semibold mb-2">Informacoes Basicas</h3>
            <div className="grid grid-cols-2 gap-4">
              <div data-testid="user-type">
                <label className="text-xs text-muted-foreground">Tipo</label>
                <p>{isPF ? "Pessoa Fisica" : "Pessoa Juridica"}</p>
              </div>
              <div data-testid="user-document">
                <label className="text-xs text-muted-foreground">
                  {isPF ? "CPF" : "CNPJ"}
                </label>
                <p>{user.document || "-"}</p>
              </div>
              <div data-testid="user-phone">
                <label className="text-xs text-muted-foreground">Telefone</label>
                <p>{user.phone || "-"}</p>
              </div>
              <div data-testid="user-created-at">
                <label className="text-xs text-muted-foreground">Criado em</label>
                <p>{new Date(user.createdAt).toLocaleDateString("pt-BR")}</p>
              </div>
            </div>
          </div>

          {/* PJ-specific */}
          {user.razaoSocial && (
            <>
              <Separator />
              <div data-testid="pj-info">
                <h3 className="text-sm font-semibold mb-2">Dados da Empresa</h3>
                <div>
                  <label className="text-xs text-muted-foreground">Razao Social</label>
                  <p data-testid="user-razao-social">{user.razaoSocial}</p>
                </div>
              </div>
            </>
          )}

          {/* Deleted info */}
          {isDeleted && (
            <>
              <Separator />
              <div className="text-destructive" data-testid="deleted-info">
                <label className="text-xs">Deletado em</label>
                <p data-testid="user-deleted-at">
                  {new Date(user.deletedAt!).toLocaleDateString("pt-BR")}
                </p>
              </div>
            </>
          )}

          {/* Action buttons (Phase 19 will wire these) */}
          <Separator />
          <div className="flex gap-2" data-testid="action-buttons">
            <Button
              variant="outline"
              onClick={onEdit}
              disabled={isDeleted}
              data-testid="edit-button"
            >
              <Pencil className="h-4 w-4 mr-1" />
              Editar
            </Button>
            {!isDeleted && (
              <>
                {user.keycloakEnabled ? (
                  <Button
                    variant="outline"
                    onClick={onBlock}
                    data-testid="block-button"
                  >
                    <Ban className="h-4 w-4 mr-1" />
                    Bloquear
                  </Button>
                ) : (
                  <Button
                    variant="outline"
                    onClick={onUnblock}
                    data-testid="unblock-button"
                  >
                    <CheckCircle className="h-4 w-4 mr-1" />
                    Desbloquear
                  </Button>
                )}
                <Button
                  variant="destructive"
                  onClick={onDelete}
                  data-testid="delete-button"
                >
                  <Trash2 className="h-4 w-4 mr-1" />
                  Excluir
                </Button>
              </>
            )}
          </div>
        </div>
      </CardContent>
    </Card>
  );
}
