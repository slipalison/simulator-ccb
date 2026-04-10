import { Badge } from "@/components/ui/badge";
import { Shield, ShieldAlert, Mail, MailWarning } from "lucide-react";

interface KeycloakStatusBadgeProps {
  enabled: boolean;
  emailVerified: boolean;
}

export function KeycloakStatusBadge({ enabled, emailVerified }: KeycloakStatusBadgeProps) {
  return (
    <div className="flex gap-2 flex-wrap" data-testid="keycloak-status">
      <Badge
        variant={enabled ? "default" : "destructive"}
        data-testid={enabled ? "status-enabled" : "status-disabled"}
      >
        {enabled ? (
          <Shield className="h-3 w-3 mr-1" />
        ) : (
          <ShieldAlert className="h-3 w-3 mr-1" />
        )}
        {enabled ? "Ativo" : "Inativo"}
      </Badge>
      <Badge
        variant={emailVerified ? "secondary" : "outline"}
        data-testid={emailVerified ? "status-email-verified" : "status-email-not-verified"}
      >
        {emailVerified ? (
          <Mail className="h-3 w-3 mr-1" />
        ) : (
          <MailWarning className="h-3 w-3 mr-1" />
        )}
        {emailVerified ? "Email verificado" : "Email nao verificado"}
      </Badge>
    </div>
  );
}
