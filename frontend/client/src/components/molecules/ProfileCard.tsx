// ---------------------------------------------------------------------------
// Molecule: ProfileCard
// ---------------------------------------------------------------------------
// Groups profile fields into a cohesive card.
// Handles PF and PJ layouts with conditional field rendering.
// ---------------------------------------------------------------------------

import { Card, CardContent, CardHeader } from "@/components/ui/card";
import { ProfileBadge } from "@/components/atoms/ProfileBadge";
import { ProfileField } from "@/components/atoms/ProfileField";
import type { ClientProfileDto } from "@/lib/types";

export interface ProfileCardProps {
  profile: ClientProfileDto;
}

/**
 * Molecule: exibe todos os campos do perfil em um card.
 * Layout PF: Nome, CPF, Email, Telefone.
 * Layout PJ: Razão Social, CNPJ, Email, Telefone.
 */
export function ProfileCard({ profile }: ProfileCardProps) {
  const isPF = profile.type === "PessoaFisica";

  return (
    <Card className="w-full max-w-md">
      <CardHeader className="pb-2">
        <ProfileBadge type={profile.type} />
      </CardHeader>
      <CardContent className="space-y-4">
        {isPF ? (
          <>
            <ProfileField label="Nome" value={profile.name} />
            {profile.cpf && <ProfileField label="CPF" value={profile.cpf} />}
            <ProfileField label="E-mail" value={profile.email} />
            <ProfileField label="Telefone" value={profile.phone} />
          </>
        ) : (
          <>
            <ProfileField
              label="Razão Social"
              value={profile.razaoSocial ?? profile.name}
            />
            {profile.cnpj && <ProfileField label="CNPJ" value={profile.cnpj} />}
            <ProfileField label="E-mail" value={profile.email} />
            <ProfileField label="Telefone" value={profile.phone} />
          </>
        )}
      </CardContent>
    </Card>
  );
}
