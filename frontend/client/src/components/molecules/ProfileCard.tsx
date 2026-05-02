// ---------------------------------------------------------------------------
// Molecule: ProfileCard
// ---------------------------------------------------------------------------
// Groups company profile fields into a cohesive card.
// PJ-only layout (Phase 40).
// ---------------------------------------------------------------------------

import { Card, CardContent, CardHeader } from "@/components/ui/card";
import { ProfileBadge } from "@/components/atoms/ProfileBadge";
import { ProfileField } from "@/components/atoms/ProfileField";
import type { CompanyProfileDto } from "@/lib/types";
import type { AccessGroup } from "@/lib/auth-context";

export interface ProfileCardProps {
  profile: CompanyProfileDto;
  accessGroup: AccessGroup;
}

/**
 * Molecule: exibe todos os campos do perfil da empresa em um card.
 * Layout: Razão Social, CNPJ, Email, Telefone + badge de grupo.
 */
export function ProfileCard({ profile, accessGroup }: ProfileCardProps) {
  return (
    <Card className="w-full max-w-md">
      <CardHeader className="pb-2">
        <ProfileBadge group={accessGroup} />
      </CardHeader>
      <CardContent className="space-y-4">
        <ProfileField label="Razão Social" value={profile.razaoSocial} />
        <ProfileField label="CNPJ" value={profile.cnpj} />
        <ProfileField label="E-mail" value={profile.email} />
        <ProfileField label="Telefone" value={profile.phone} />
      </CardContent>
    </Card>
  );
}